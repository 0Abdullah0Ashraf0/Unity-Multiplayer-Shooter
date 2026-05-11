using Unity.Netcode;
using UnityEngine;

public class AmmoSpawner : MonoBehaviour
{
    [Header("Ammo Prefab")]
    [SerializeField] private GameObject ammoPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int maxAmmoBoxes = 10;
    [SerializeField] private float spawnInterval = 10f;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 areaCenter;
    [SerializeField] private Vector3 areaSize = new Vector3(50f, 1f, 50f);

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayHeight = 50f;

    private int currentAmmoBoxes;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        InvokeRepeating(nameof(SpawnAmmo), 1f, spawnInterval);
    }

    private void SpawnAmmo()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (currentAmmoBoxes >= maxAmmoBoxes)
            return;

        Vector3 randomPosition = GetRandomPosition();

        if (Physics.Raycast(
                randomPosition + Vector3.up * rayHeight,
                Vector3.down,
                out RaycastHit hit,
                100f,
                groundLayer))
        {
            Vector3 spawnPosition = hit.point + Vector3.up * 0.2f;

            GameObject ammo = Instantiate(ammoPrefab, spawnPosition, Quaternion.identity);

            ammo.GetComponent<NetworkObject>().Spawn();

            currentAmmoBoxes++;

            AmmoPickup pickup = ammo.GetComponent<AmmoPickup>();

            if (pickup != null)
                pickup.SetSpawner(this);
        }
    }

    private Vector3 GetRandomPosition()
    {
        float randomX = Random.Range(
            areaCenter.x - areaSize.x / 2,
            areaCenter.x + areaSize.x / 2);

        float randomZ = Random.Range(
            areaCenter.z - areaSize.z / 2,
            areaCenter.z + areaSize.z / 2);

        return new Vector3(randomX, areaCenter.y, randomZ);
    }

    public void AmmoCollected()
    {
        currentAmmoBoxes--;
    }
    public void ClearAllAmmo()
    {
        AmmoPickup[] pickups = FindObjectsOfType<AmmoPickup>();

        foreach (AmmoPickup pickup in pickups)
        {
            if (pickup != null && pickup.NetworkObject.IsSpawned)
                pickup.NetworkObject.Despawn(true);
        }

        currentAmmoBoxes = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            areaCenter,
            areaSize);
    }
}