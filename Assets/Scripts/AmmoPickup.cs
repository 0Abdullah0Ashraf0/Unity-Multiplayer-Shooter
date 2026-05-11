using Unity.Netcode;
using UnityEngine;

public class AmmoPickup : NetworkBehaviour
{
    [SerializeField] private int ammoAmount = 30;
    private AmmoSpawner _spawner;
    private bool _pickedUp = false; // prevent double pickup

    public void SetSpawner(AmmoSpawner ammoSpawner)
    {
        _spawner = ammoSpawner;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Client just requests — no logic, no despawn here
        if (!other.CompareTag("Player")) return;
        if (_pickedUp) return;

        RequestPickupServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        // Guard against double pickup (two players touching at same time)
        if (_pickedUp) return;
        _pickedUp = true;

        PlayerHealth player = FindPlayerFromSender(rpcParams.Receive.SenderClientId);
        if (player != null)
        {
            Weapon weapon = player.GetComponentInChildren<Weapon>(true);
            weapon?.PickupAmmo(ammoAmount);
        }

        if (_spawner != null)
            _spawner.AmmoCollected();

        // Only server despawns — this is now only called from ServerRpc
        NetworkObject.Despawn(true);
    }

    private PlayerHealth FindPlayerFromSender(ulong clientId)
    {
        foreach (var player in FindObjectsOfType<PlayerHealth>())
        {
            if (player.OwnerClientId == clientId)
                return player;
        }
        return null;
    }
}