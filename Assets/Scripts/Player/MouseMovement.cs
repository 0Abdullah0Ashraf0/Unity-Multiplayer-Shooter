using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : NetworkBehaviour
{
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private float _mouseSensitivity = 0.1f;

    [SerializeField] private float _ytopRotationLimit = -90f;
    [SerializeField] private float _ybottomRotationLimit = 90f;

    private float _xRotation = 0f;
    private PlayerInput _playerInput;
    private InputAction _lookAction;

    void Awake() // for new input system
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        _lookAction = _playerInput.actions["Look"];
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    public void OnMatchStart()
    {
        if (!IsOwner) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if(!IsOwner) return; // Only allow the owning client to control camera movement
        //get inputs
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();

        //set vertical rotation on y axis (left and right)
        _xRotation -= lookInput.y * _mouseSensitivity;

        //set vertical rotation limits
        _xRotation = Mathf.Clamp(_xRotation, _ytopRotationLimit, _ybottomRotationLimit);

        //apply vertical rotation
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        
        //rotate player root on x axis (up and down)
        _playerRoot.Rotate(Vector3.up * lookInput.x * _mouseSensitivity); 
    }
}
