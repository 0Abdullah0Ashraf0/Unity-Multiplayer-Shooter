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
        // Reset health when spawned
        if (IsServer)
            CurrentHealth.Value = _maxHealth;

        // Subscribe to health changes for HUD later
        CurrentHealth.OnValueChanged += OnHealthChanged;
    }
    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

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
        // We'll hook ScoreManager and respawn here in the next step
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        // Visual death effects go here later
        Debug.Log("Death ClientRpc received");
    }

    public void Respawn()
    {
        if (!IsServer) return;
        CurrentHealth.Value = _maxHealth;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        // HUD will subscribe to this later
        Debug.Log($"Health changed: {oldVal} -> {newVal}");
    }
}
