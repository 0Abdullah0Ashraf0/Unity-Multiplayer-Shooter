using System;
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
        ScoreManager.Instance?.RegisterKill(attackerClientId);
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        if (IsOwner)
        {
            // Visual death effects go here later
            Debug.Log("Death ClientRpc received");
            
            //lock input
            GetComponent<PlayerMovement>().enabled = false;
            GetComponentInChildren<MouseMovement>().enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Show death screen
            HUDManager.Instance?.ShowDeathScreen();
        }

        //hide players
        // On death - play death animation instead
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;
        CurrentHealth.Value = _maxHealth;
        TeleportClientRpc(position, rotation);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        cc.enabled = true;

        // Unhide the body 
        // On respawn - play respawn animation instead
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        if (IsOwner)
        {
            // Re-lock cursor and hide it again
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Hide death screen
            HUDManager.Instance?.HideDeathScreen();
        }
    }


    private void OnHealthChanged(int oldVal, int newVal)
    {
        Debug.Log($"Health changed: {oldVal} -> {newVal}");

        if (IsOwner) HUDManager.Instance?.UpdateHealth(newVal);
    }
}
