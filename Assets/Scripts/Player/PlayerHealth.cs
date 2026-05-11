using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    private PlayerAudio _playerAudio;
    [SerializeField] private int _maxHealth = 100;
    public bool IsDead => CurrentHealth.Value <= 0;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        _playerAudio = GetComponent<PlayerAudio>();

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
        NotifyKillerClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { attackerClientId } }
        });
    }

    [ClientRpc]
    private void NotifyKillerClientRpc(ClientRpcParams _ = default)
        => GetComponent<PlayerAudio>()?.PlayKill();

    [ClientRpc]
    private void DieClientRpc()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        if (!IsOwner) return;

        GetComponent<PlayerMovement>().enabled = false;
        GetComponentInChildren<MouseMovement>().enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        _playerAudio?.PlayDeath();
    }

    // ── Respawn ────────────────────────────────────────────────────────────────
    public void Respawn()
    {
        if (!IsServer) return;
        CurrentHealth.Value = _maxHealth;
        RespawnClientRpc(GetSpawnPosition(), Quaternion.identity);
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;
        CurrentHealth.Value = _maxHealth;
        RespawnClientRpc(position, rotation);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 position, Quaternion rotation)
    {
        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        cc.enabled = true;

        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        if (!IsOwner) return;

        GetComponent<PlayerMovement>().enabled = true;
        GetComponentInChildren<MouseMovement>().enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ── Spawn Point Helper ─────────────────────────────────────────────────────
    private Vector3 GetSpawnPosition()
    {
        var pts = GameObject.FindGameObjectsWithTag("SpawnPoint");
        return pts.Length == 0 ? Vector3.zero
            : pts[Random.Range(0, pts.Length)].transform.position;
    }

    // ── HUD Update ─────────────────────────────────────────────────────────────
    private void OnHealthChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;   // only the local player updates their own HUD

        int damageTaken = oldVal - newVal;

        // Update health bar
        HUDManager.Instance?.UpdateHealth(newVal);

        // Show floating damage number (only when actually losing HP)
        if (damageTaken > 0)
        {
            HUDManager.Instance?.ShowDamageNumber(damageTaken, newVal);
            _playerAudio?.PlayHurt();
        }

        Debug.Log($"Health changed: {oldVal} → {newVal}");
    }
}