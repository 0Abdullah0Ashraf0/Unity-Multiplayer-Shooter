using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    // ui //
    [SerializeField] private GameObject _joinPanel;     
    [SerializeField] private GameObject _MapPanel;     
    [SerializeField] private GameObject _SettingsPanel;   
    [SerializeField] private GameObject _waitingPanel;
    [SerializeField] private TextMeshProUGUI _errorText;  
    [SerializeField] private TMP_InputField _ipInputField;

    // network shit //
    private UnityTransport _transport;
    private Coroutine _connectionTimeoutCoroutine;
    private const float CONNECTION_TIMEOUT = 5f;
    private bool _gameStarted = false; // Flag to prevent multiple game starts

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // 1. Allow the player to join the server
        response.Approved = true;
    
        // 2. CRITICAL FIX: Tell NGO *not* to auto-spawn the default player immediately, 
        // because your StartGame() method will handle it manually later!
        response.CreatePlayerObject = false; 
    }
    
    public void Start()
    {
        _errorText.text = "";
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        _joinPanel.SetActive(false);
        _MapPanel.SetActive(false);
        _SettingsPanel.SetActive(false);
        _waitingPanel.SetActive(false); 
    }

    public void HostGameAndShowMapSelection()
    {
         // Guard against double-invocation
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) return;

        // If NetworkManager is already running from a previous session, shut it down first
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[Host] Shut down existing session before rehosting");
        }

        _joinPanel.SetActive(false);
        _SettingsPanel.SetActive(false);

        _transport.SetConnectionData("0.0.0.0", 7777);

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("[Host] Started on port 7777");
            _waitingPanel.SetActive(true);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            Debug.LogError("[Host] Failed to start!");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            _waitingPanel.SetActive(false);
            _MapPanel.SetActive(true);
        }
    }

    public void CancelHosting()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.Shutdown();
        _waitingPanel.SetActive(false);
    }

    public void ShowJoinPanel()
    {
        _MapPanel.SetActive(false);
        _SettingsPanel.SetActive(false);

        // start client

        // enter IP address/party code and connect
        _joinPanel.SetActive(true);
    }

    public void JoinGame()
    {
        MenuAudio.Instance?.PlayClick();
        _errorText.text = "";

        // Guard against double-invocation
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) return;
        
        string ipAddress = _ipInputField.text;
        if (!IsValidIP(ipAddress))
        {
            MenuAudio.Instance?.PlayWrongIp();
            _errorText.text = "Invalid IP address."; // add a TMP error label in UI
            return;
        }

        MenuAudio.Instance?.PlayConnecting(); 
        _errorText.text = "Connecting...";
        _transport.SetConnectionData(ipAddress, 7777);

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log($"[Client] Attempting to connect to {ipAddress}:7777");
        }
        else
        {
            Debug.LogError("[Client] Failed to start!");
        }

        //listen for failure
        NetworkManager.Singleton.OnClientDisconnectCallback += OnConnectionFailed;
        // Start timeout timer
        _connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeout());
    }

    private bool IsValidIP(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        // also accept "localhost"
        if (ip == "localhost") return true;
        return System.Net.IPAddress.TryParse(ip, out _);
    }
    private IEnumerator ConnectionTimeout()
    {
        yield return new WaitForSeconds(CONNECTION_TIMEOUT);

        // If we're still not connected after timeout, kill it
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            MenuAudio.Instance?.StopConnecting();
            MenuAudio.Instance?.PlayWrongIp();
            NetworkManager.Singleton.Shutdown();
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
            _errorText.text = "Could not reach host.";
        }
        else
        {
            MenuAudio.Instance?.StopConnecting();
            MenuAudio.Instance?.PlayConnected(); 
        }
    }

    private void OnConnectionFailed(ulong clientId)
    {
        MenuAudio.Instance?.StopConnecting();
        MenuAudio.Instance?.PlayWrongIp();

        // Stop the timeout coroutine since we already got a response
        if (_connectionTimeoutCoroutine != null)
            StopCoroutine(_connectionTimeoutCoroutine);

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
        NetworkManager.Singleton.Shutdown();
        _errorText.text = "Could not reach host. Check IP and try again.";
    }

    public void ShowSettingsPanel()
    {
        // Show settings menu only
        _joinPanel.SetActive(false);
        _MapPanel.SetActive(false);
        _SettingsPanel.SetActive(true);

        //sensitivity, audio, etc.
    }

    public void QuitGame()
    {
        _joinPanel.SetActive(false);
        _MapPanel.SetActive(false);
        _SettingsPanel.SetActive(false);
        
        // Quit the application
        Application.Quit();
    }

    public void PickMap(string mapName)
    {
        // load the selected map
        // NetworkManager.Singleton.SceneManager.LoadScene(mapName, LoadSceneMode.Additive);

        if (!NetworkManager.Singleton.IsHost) return;

        // Subscribe to scene load event BEFORE loading
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnMapLoaded;

        NetworkManager.Singleton.SceneManager.LoadScene(mapName, LoadSceneMode.Single);
    }

    private void OnMapLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        // Fires for EACH client that finishes loading
        // Only the server should spawn players
        if (!NetworkManager.Singleton.IsServer) return;
        if (_gameStarted) return;

        // wait for other player to load before spawning
        if (NetworkManager.Singleton.ConnectedClients.Count < 2) return;

        Debug.Log($"All players loaded {sceneName}. Starting game...");
        _gameStarted = true;
        StartGame();

        // Cleanup listener when both players are in
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnMapLoaded;
    }

    private void StartGame()
    {
        // set spawn point
        GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint");

        int index = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            Vector3 spawnPos = points[index % points.Length].transform.position;
            Quaternion spawnRot = points[index % points.Length].transform.rotation;

            // Spawn at a position 
            GameObject player = Instantiate(_playerPrefab, spawnPos, spawnRot);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            
            Debug.Log($"Client {clientId}. Spawned player at {spawnPos}");
            index++;
        }    
    }
}
