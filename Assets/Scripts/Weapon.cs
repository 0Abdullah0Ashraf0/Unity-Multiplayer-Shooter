using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : NetworkBehaviour
{
    public Camera playerCamera;

    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _range = 70f;
    [SerializeField] private int _damage = 10;
    private float _nextFireTime = 0f;

    [SerializeField] private int _maxAmmo = 30;
    [SerializeField] private int _ammoPerReload = 6;
    [SerializeField] private int _totalAmmo;
    [SerializeField] private int _currentAmmo;

    private PlayerInput _playerInput;
    private InputAction _shootAction;
    private InputAction _reloadAction;

    void Awake()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
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
            return;
        }

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
        
        if (_shootAction.WasPressedThisFrame() && Time.time >= _nextFireTime && _currentAmmo > 0)
        {
            _nextFireTime = Time.time + _fireRate;
            Shoot();
            Debug.Log("Shoot called");
        }
        if (_reloadAction.WasPressedThisFrame() || _currentAmmo <= 0)
        {
            Reload();
        }
    }

    private void Shoot()
    {
        _currentAmmo--;

        RefreshAmmoHUD();   //update 

        Debug.Log("Shot fired. Ammo left: " + _currentAmmo);
       
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * _range, Color.red, 5f);

        // Send to server for authoritative hit validation
        ShootServerRpc(ray.origin, ray.direction);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        Ray ray = new Ray(origin, direction);

        if (Physics.Raycast(ray, out hit, _range))
        {
            Debug.Log($"[Server] Hit: {hit.collider.name}");

            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();

            // Make sure we didn't shoot ourselves
            if (playerHealth != null && playerHealth.OwnerClientId != OwnerClientId)
            {
                playerHealth.TakeDamage(_damage, OwnerClientId);

                // Tell the shooter's client to flash the hit-marker
                ShowHitMarkerClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] {OwnerClientId}
                    }
                });
            }
        }
    }




    [ClientRpc]
    private void ShowHitMarkerClientRpc(ClientRpcParams _ = default)
    {
        HUDManager.Instance?.ShowHitMarker();
    }






    private void Reload()
    {
        if(_totalAmmo <= 0)
        {
            Debug.Log("Cannot reload. No ammo left.");
            return;
        }
        _currentAmmo = _ammoPerReload;
        _totalAmmo -= _ammoPerReload;   


        RefreshAmmoHUD();     // ← update HUD after reloading


        Debug.Log("Weapon reloaded.");
    }

    public void ResetAmmo()
    {
        _currentAmmo = _ammoPerReload;
        _totalAmmo   = _maxAmmo;
        RefreshAmmoHUD();
    }


    private void PickupAmmo()
    {
        _totalAmmo += _maxAmmo;
        _currentAmmo = _ammoPerReload;
        RefreshAmmoHUD();
        Debug.Log("Picked up ammo. Total ammo: " + _totalAmmo);
    }
     
     
    private void RefreshAmmoHUD()
    {
        HUDManager.Instance?.UpdateAmmo(_currentAmmo, _totalAmmo);
    }


}
