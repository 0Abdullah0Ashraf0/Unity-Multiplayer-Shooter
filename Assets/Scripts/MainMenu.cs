using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject joinPanel;     
    [SerializeField] private GameObject MapPanel;     
    [SerializeField] private GameObject SettingsPanel;   
    [SerializeField] private TextMeshProUGUI _errorText;  
    [SerializeField] private TMP_InputField ipInputField;
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

        joinPanel.SetActive(false);
        MapPanel.SetActive(false);
        SettingsPanel.SetActive(false);
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

        joinPanel.SetActive(false);
        SettingsPanel.SetActive(false);

        _transport.SetConnectionData("0.0.0.0", 7777);

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("[Host] Started on port 7777");
            MapPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[Host] Failed to start!");
        }
    }

    public void ShowJoinPanel()
    {
        MapPanel.SetActive(false);
        SettingsPanel.SetActive(false);

        // start client

        // enter IP address/party code and connect
        joinPanel.SetActive(true);
    }

    public void JoinGame()
    {
        // Guard against double-invocation
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) return;
        
        string ipAddress = ipInputField.text;
        if (!IsValidIP(ipAddress))
        {
            _errorText.text = "Invalid IP address."; // add a TMP error label in UI
            return;
        }

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
            NetworkManager.Singleton.Shutdown();
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionFailed;
            _errorText.text = "Could not reach host.";
        }
    }

    private void OnConnectionFailed(ulong clientId)
    {
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
        joinPanel.SetActive(false);
        MapPanel.SetActive(false);
        SettingsPanel.SetActive(true);

        //sensitivity, audio, etc.
    }

    public void QuitGame()
    {
        joinPanel.SetActive(false);
        MapPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        
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
            GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            
            Debug.Log($"Client {clientId}. Spawned player at {spawnPos}");
            index++;
        }    
    }
}
