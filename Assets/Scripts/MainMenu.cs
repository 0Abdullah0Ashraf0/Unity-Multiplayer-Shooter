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

    public void Start()
    {
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        joinPanel.SetActive(false);
        MapPanel.SetActive(false);
        SettingsPanel.SetActive(false);
    }

    public void HostGameAndShowMapSelection()
    {
        joinPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        MapPanel.SetActive(false);

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

            NetworkManager.Singleton.SceneManager
                .LoadScene(mapName, LoadSceneMode.Single);
    }

    private void OnMapLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        // Fires for EACH client that finishes loading
        // Only the server should spawn players
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkClient client = NetworkManager.Singleton.ConnectedClients[clientId];

        // Spawn at a position — hook up spawnpoints later
        GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        // Cleanup listener when both players are in
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnMapLoaded;
    }
}
