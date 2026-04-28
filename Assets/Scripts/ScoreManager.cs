using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;
    [SerializeField] private int _killsToWin = 1;
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

        HUDManager.Instance?.UpdateScore(Player1Kills.Value, Player2Kills.Value);

        if (Player1Kills.Value >= _killsToWin || Player2Kills.Value >= _killsToWin)
            EndGameClientRpc(killerClientId);
        else
            //new round respawn logic will go here later
            Debug.Log("Next round! Respawning players...");
    }

    [ClientRpc]
    private void EndGameClientRpc(ulong winnerClientId)
    {
        bool localWin = winnerClientId == NetworkManager.Singleton.LocalClientId;
        HUDManager.Instance?.ShowEndScreen(localWin);
    }
}
