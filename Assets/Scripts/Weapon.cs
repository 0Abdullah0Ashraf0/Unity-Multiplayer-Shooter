using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : NetworkBehaviour
{
    public Camera playerCamera;
    [SerializeField] private PlayerAudio _playerAudio;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _range = 500f;
    [SerializeField] private int _damage = 10;
    private float _nextFireTime = 0f;

    [SerializeField] private int _maxAmmo = 30;
    [SerializeField] private int _ammoPerReload = 6;
    [SerializeField] private int _totalAmmo;
    [SerializeField] private int _currentAmmo;

    [SerializeField] private int _headMultiplier = 2; // headshot deals 2x damage

    private PlayerInput _playerInput;
    private InputAction _shootAction;
    private InputAction _reloadAction;

    void Awake()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        _playerAudio = GetComponentInParent<PlayerAudio>();
        _shootAction = _playerInput.actions["Shoot"];
        _reloadAction = _playerInput.actions["Reload"];
        if (_shootAction == null) Debug.LogError("Shoot action not found!");
        if (_reloadAction == null) Debug.LogError("Reload action not found!");
    }

    public override void OnNetworkSpawn()
    {
        // Disable shooting on non-owner copies of this weapon
        if (!IsOwner)
        {
            enabled = false;
            this.enabled = false;

            // Move to Default layer so other players' main cameras can see it
            gameObject.layer = LayerMask.NameToLayer("Default");

            // Also set all children (the mesh renderers)
            foreach (Transform child in GetComponentsInChildren<Transform>())
                child.gameObject.layer = LayerMask.NameToLayer("Default");

            return;
        }

        // Owner: stays on FirstPersonWeapon layer — only seen by WeaponCamera
        gameObject.layer = LayerMask.NameToLayer("FirstPersonWeapon");
        foreach (Transform child in GetComponentsInChildren<Transform>())
            child.gameObject.layer = LayerMask.NameToLayer("FirstPersonWeapon");

        _currentAmmo = _ammoPerReload;
        _totalAmmo   = _maxAmmo;
        _nextFireTime = Time.time;


        // Push initial ammo count to HUD
        RefreshAmmoHUD();
    }

    // void OnEnable()  { _shootAction?.Enable();  _reloadAction?.Enable();  }
    // void OnDisable() { _shootAction?.Disable(); _reloadAction?.Disable(); }

    void Update()
    {
        if (!IsOwner) return;

        if (_shootAction.WasPressedThisFrame())
        {
            if (_currentAmmo <= 0)
                _playerAudio?.PlayEmpty();         
            else if (Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + _fireRate;
                Shoot();
            }
        }

        if (_reloadAction.WasPressedThisFrame())
            Reload();
    }

    private void Shoot()
    {
        _currentAmmo--;
        RefreshAmmoHUD();
        _playerAudio?.PlayShoot();

        // Pass 1: find exact world point crosshair is pointing at
        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = centerRay.origin + centerRay.direction * _range;

        int ignoreMask = LayerMask.GetMask("FirstPersonWeapon", "FirstPersonBody", "CameraWeapon");
        int layerMask = ~ignoreMask;

        // Pass 2: refine to exact hit point if something is in the way
        if (Physics.Raycast(centerRay, out RaycastHit targetHit, _range, layerMask))
            targetPoint = targetHit.point;

        // Final direction from camera to that world point
        Vector3 finalDirection = (targetPoint - playerCamera.transform.position).normalized;

        Debug.DrawRay(playerCamera.transform.position, finalDirection * _range, Color.red, 5f);
        Debug.Log($"Shot fired. Target point: {targetPoint}");

        ShootServerRpc(playerCamera.transform.position, finalDirection);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        Ray ray = new Ray(origin, direction);

        int ignoreMask = LayerMask.GetMask("FirstPersonWeapon", "FirstPersonBody", "CameraWeapon");
        int layerMask = ~ignoreMask;

        Debug.DrawRay(origin, direction * _range, Color.blue, 5f);

        if (Physics.Raycast(ray, out hit, _range, layerMask))
        {
            Debug.Log($"[Server] Hit: {hit.collider.name} tag={hit.collider.tag} dist={hit.distance}");

            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null && playerHealth.OwnerClientId != OwnerClientId)
            {
                bool isHeadshot = hit.collider.CompareTag("HeadCollider");
                int finalDamage = isHeadshot ? _damage * _headMultiplier : _damage;

                Debug.Log($"[Server] {(isHeadshot ? "HEADSHOT" : "Body")} — Damage: {finalDamage}");

                playerHealth.TakeDamage(finalDamage, OwnerClientId);
                ShowHitMarkerClientRpc(isHeadshot, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                });
            }
            else if (playerHealth == null)
                Debug.Log($"[Server] Hit {hit.collider.name} — no PlayerHealth in parents");
            else
                Debug.Log("[Server] Hit own player, ignoring");
        }
        else
        {
            Debug.Log("[Server] Hit nothing");
        }
    }

    [ClientRpc]
    private void ShowHitMarkerClientRpc(bool isHeadshot, ClientRpcParams _ = default)
    {
        HUDManager.Instance?.ShowHitMarker(isHeadshot);
    }

    private void Reload()
    {
        // Magazine already full
        if (_currentAmmo >= _ammoPerReload)
        {
            Debug.Log("Magazine already full.");
            return;
        }

        if (_totalAmmo <= 0)
        {
            Debug.Log("Cannot reload. No ammo left.");
            return;
        }
        int ammoNeeded = _ammoPerReload - _currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, _totalAmmo);

        _currentAmmo += ammoToLoad;
        _totalAmmo -= ammoToLoad;

        RefreshAmmoHUD();
        _playerAudio?.PlayReload();

        Debug.Log($"Reloaded {ammoToLoad} bullets.");
    }

    public void ResetAmmo()
    {
        _currentAmmo = _ammoPerReload;
        _totalAmmo   = _maxAmmo;
        RefreshAmmoHUD();
    }

    public void PickupAmmo(int amount)
    {
        _totalAmmo += amount;

        RefreshAmmoHUD();

        Debug.Log($"Picked up {amount} ammo");
    }

    private void RefreshAmmoHUD()
    {
        HUDManager.Instance?.UpdateAmmo(_currentAmmo, _totalAmmo);
    }
}
