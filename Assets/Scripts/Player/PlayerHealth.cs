using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    public bool IsDead => CurrentHealth.Value <= 0;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            CurrentHealth.Value = _maxHealth;

        CurrentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    // ── Taking Damage ──────────────────────────────────────────────────────────
    public void TakeDamage(int damage, ulong attackerClientId)
    {
        if (!IsServer || IsDead) return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damage);
        Debug.Log($"Player {OwnerClientId} took {damage} damage. HP: {CurrentHealth.Value}");

        if (CurrentHealth.Value <= 0)
            Die(attackerClientId);
    }

    private void Die(ulong attackerClientId)
    {
        Debug.Log($"Player {OwnerClientId} died! Killed by {attackerClientId}");
        ScoreManager.Instance?.RegisterKill(attackerClientId);
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        // Hide the body
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        if (!IsOwner) return;

        // Lock input on death
        GetComponent<PlayerMovement>().enabled = false;
        GetComponentInChildren<MouseMovement>().enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Local player died — input disabled.");
    }

    // ── Respawn ────────────────────────────────────────────────────────────────
    // Called by ScoreManager with no arguments — picks a random spawn point
    public void Respawn()
    {
        if (!IsServer) return;
        CurrentHealth.Value = _maxHealth;

        // Find a spawn point — tag yours as "SpawnPoint" in Unity
        // or hardcode a position if you don't have spawn points yet
        Vector3    spawnPos = GetSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        RespawnClientRpc(spawnPos, spawnRot);
    }

    // Called by ScoreManager (or anything) with an explicit position
    public void Respawn(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;
        CurrentHealth.Value = _maxHealth;
        RespawnClientRpc(position, rotation);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 position, Quaternion rotation)
    {
        // Teleport
        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        cc.enabled = true;

        // Show body again
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        if (!IsOwner) return;

        // Re-enable input
        GetComponent<PlayerMovement>().enabled = true;
        GetComponentInChildren<MouseMovement>().enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Local player respawned — input re-enabled.");
    }

    // ── Spawn Point Helper ─────────────────────────────────────────────────────
    private Vector3 GetSpawnPosition()
    {
        // Looks for GameObjects tagged "SpawnPoint" in your scene.
        // If you don't have any, it just respawns at the world origin.
        var spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length == 0)
            return Vector3.zero;

        // Pick one randomly
        return spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
    }

    // ── HUD Update ─────────────────────────────────────────────────────────────
    private void OnHealthChanged(int oldVal, int newVal)
    {
        Debug.Log($"Health changed: {oldVal} -> {newVal}");
        if (IsOwner) HUDManager.Instance?.UpdateHealth(newVal);
    }
}