using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : NetworkBehaviour
{
    [SerializeField] private Transform _playerRoot;

    // _mouseSensitivity is kept as a fallback for when SettingsManager isn't
    // present (e.g. quick Play-mode tests without the full scene setup).
    [SerializeField] private float _mouseSensitivity = 0.1f;

    [SerializeField] private float _ytopRotationLimit    = -90f;
    [SerializeField] private float _ybottomRotationLimit =  90f;

    private float _xRotation = 0f;
    private PlayerInput _playerInput;
    private InputAction _lookAction;

    void Awake()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        _lookAction  = _playerInput.actions["Look"];
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<Camera>().enabled = false;
            enabled = false;
            return;
        }
    }

    public void OnMatchStart()
    {
        if (!IsOwner) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        // Prefer the live value from SettingsManager; fall back to Inspector field.
        float sens = SettingsManager.Instance != null
            ? SettingsManager.Instance.Sensitivity
            : _mouseSensitivity;

        Vector2 lookInput = _lookAction.ReadValue<Vector2>();

        _xRotation -= lookInput.y * sens;
        _xRotation  = Mathf.Clamp(_xRotation, _ytopRotationLimit, _ybottomRotationLimit);

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        _playerRoot.Rotate(Vector3.up * lookInput.x * sens);
    }
}
