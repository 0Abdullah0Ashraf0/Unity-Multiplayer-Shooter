using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;
    public AmmoSpawner spawner;
     
    // ─── Settings
    [SerializeField] private int _killsToWin = 5;
    [SerializeField] private int _countdownSeconds = 5;   
    
    //network variables
    public NetworkVariable<int> Player1Kills = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Player2Kills = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    void Awake() => Instance = this;

    
    
    public override void OnNetworkSpawn()
    {
        // Every client subscribes so their HUD updates automatically when the
        // server changes any of these NetworkVariables.
        Player1Kills.OnValueChanged += (_, newVal) =>
            HUDManager.Instance?.UpdateScore(newVal, Player2Kills.Value);
 
        Player2Kills.OnValueChanged += (_, newVal) =>
            HUDManager.Instance?.UpdateScore(Player1Kills.Value, newVal);
 
        CurrentRound.OnValueChanged += (_, newVal) =>
            HUDManager.Instance?.ShowRoundBanner(newVal);
 
        // Initialise HUD with current values (important when a client reconnects)
        HUDManager.Instance?.UpdateScore(Player1Kills.Value, Player2Kills.Value);
    }




    public void RegisterKill(ulong killerClientId)
    {
        if (!IsServer) return;
 
        if (killerClientId == 0) Player1Kills.Value++;
        else                     Player2Kills.Value++;
 
        if (Player1Kills.Value >= _killsToWin || Player2Kills.Value >= _killsToWin)
        {
            // Game over – tell all clients who won
            spawner.ClearAllAmmo();
            EndGameClientRpc(killerClientId);
        }
        else
        {
            // Not over yet – start the countdown then begin the next round
            StartCoroutine(NextRoundCountdown());
        }
    }






    private IEnumerator NextRoundCountdown()
    {
        // 1. Show countdown UI on every client
        StartCountdownClientRpc(_countdownSeconds);
 
        // 2. Wait the same duration on the server
        yield return new WaitForSeconds(_countdownSeconds);
 
        // 3. Bump round number – OnValueChanged fires ShowRoundBanner on all clients
        CurrentRound.Value++;
 
        // 4. Respawn all players and reset their ammo
        RespawnAllPlayers();
    }





    private void RespawnAllPlayers()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        
        int index = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            Vector3 pos = spawnPoints[index % spawnPoints.Length].transform.position;
            Quaternion rot = spawnPoints[index % spawnPoints.Length].transform.rotation;

            // Restore ammo (calls HUD update internally)
            var weapon = playerObj.GetComponentInChildren<Weapon>();
            if (weapon != null) weapon.ResetAmmo();

            // Restore health
            var health = playerObj.GetComponent<PlayerHealth>();
            if (health != null) health.Respawn(pos, rot);

            index++;
        }
    }





    [ClientRpc]
    private void StartCountdownClientRpc(int seconds)
    {
        HUDManager.Instance?.StartCountdownUI(seconds);
    }
 


    [ClientRpc]
    private void EndGameClientRpc(ulong winnerClientId)
    {
        bool localWin = winnerClientId == NetworkManager.Singleton.LocalClientId;
        HUDManager.Instance?.ShowEndScreen(localWin);
    }
}
