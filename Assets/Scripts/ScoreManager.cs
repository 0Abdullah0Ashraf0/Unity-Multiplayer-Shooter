using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;
    [SerializeField] private int _killsToWin = 2;
    [SerializeField] private float _respawnDelay = 1.0f;
    public NetworkVariable<int> Player1Kills = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Player2Kills = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake() => Instance = this;

    public void RegisterKill(ulong killerClientId)
    {
        if (!IsServer) return;

        if (killerClientId == 0) Player1Kills.Value++;
        else                     Player2Kills.Value++;

        Debug.Log($"[ScoreManager] Kill registered. Killer: Client {killerClientId} | Score → P1: {Player1Kills.Value} | P2: {Player2Kills.Value} | Kills to win: {_killsToWin}");
        HUDManager.Instance?.UpdateScore(Player1Kills.Value, Player2Kills.Value);

        if (Player1Kills.Value >= _killsToWin || Player2Kills.Value >= _killsToWin)
        {
            Debug.Log($"[ScoreManager] Game over! Winner: Client {killerClientId}");
            EndGameClientRpc(killerClientId);
        }
        else
        {
            //new round respawn logic will go here later
            Debug.Log("Next round! Respawning players...");

            SetAllPlayersInputClientRpc(false); // Disable input during respawn
            Invoke(nameof(RespawnAllPlayers), _respawnDelay);
        }
    }

    private void RespawnAllPlayers()
    {
        // Respawn logic will go here later (e.g., resetting player positions, health, etc.)
        Debug.Log($"[ScoreManager] Respawning {NetworkManager.Singleton.ConnectedClients.Count} players...");

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Debug.Log($"[ScoreManager] Found {spawnPoints.Length} spawn points");
        int index = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = client.Value.PlayerObject;
            if (playerObj == null) {
                Debug.LogWarning($"[ScoreManager] Client {client.Key} has no PlayerObject!");
                continue;
            }

            Vector3 pos = spawnPoints[index % spawnPoints.Length].transform.position;
            Quaternion rot = spawnPoints[index % spawnPoints.Length].transform.rotation;

            playerObj.GetComponent<PlayerHealth>().Respawn(pos, rot);
            index++;

            Debug.Log($"Client {client}. Spawned player at {pos}");
        }


        SetAllPlayersInputClientRpc(true); // Re-enable input after respawn
        Debug.Log("[ScoreManager] All players respawned. Input re-enabled.");
    }

    [ClientRpc]
    private void SetAllPlayersInputClientRpc(bool enabled)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        localPlayer.GetComponent<PlayerMovement>().enabled = enabled;
        localPlayer.GetComponentInChildren<MouseMovement>().enabled = enabled;
    }

    [ClientRpc]
    private void EndGameClientRpc(ulong winnerClientId)
    {
        bool localWin = winnerClientId == NetworkManager.Singleton.LocalClientId;
        HUDManager.Instance?.ShowEndScreen(localWin);
    }
}
