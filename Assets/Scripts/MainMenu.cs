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
    [SerializeField] private TMP_InputField ipInputField;
    private UnityTransport _transport;
    private bool _gameStarted = false; // Flag to prevent multiple game starts

    public void Start()
    {
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        joinPanel.SetActive(false);
        MapPanel.SetActive(false);
        SettingsPanel.SetActive(false);
    }

    public void HostGameAndShowMapSelection()
    {
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
        string ipAddress = ipInputField.text;
        _transport.SetConnectionData(ipAddress, 7777);

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log($"[Client] Attempting to connect to {ipAddress}:7777");
        }
        else
        {
            Debug.LogError("[Client] Failed to start!");
        }
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
