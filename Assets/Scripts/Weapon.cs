using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public Camera playerCamera;

    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _range = 30f;
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

    void OnEnable()
    {
        _shootAction.Enable();
        _reloadAction.Enable();
    }
    void OnDisable()
    {
        _shootAction.Disable();
        _reloadAction.Disable();
    }
    void Start()
    {
        _nextFireTime = Time.time;
        _currentAmmo = _ammoPerReload;
        _totalAmmo = _maxAmmo;
    }

    void Update()
    {
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
        RaycastHit hit;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * _range, Color.red, 5f);

        if (Physics.Raycast(ray, out hit, _range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(_damage);
            }
            _currentAmmo--;
            Debug.Log("Shot fired. Ammo left: " + _currentAmmo);
        }
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
        Debug.Log("Weapon reloaded.");
    }

    private void PickupAmmo()
    {
        _totalAmmo += _maxAmmo;
        _currentAmmo = _ammoPerReload;
        Debug.Log("Picked up ammo. Total ammo: " + _totalAmmo);
    }
}
