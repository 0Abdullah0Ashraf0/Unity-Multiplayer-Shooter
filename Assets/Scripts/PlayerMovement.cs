using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _gravity = -9.81f * 2f;

    private CharacterController _controller;
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _jumpAction;

    private bool _isGrounded;
    private Vector3 _velocity;


    // public LayerMask groundMask;
    // public Transform groundCheck;
    // [SerializeField] private float _groundDistance = 0.4f;
    // private bool _isMoving; 
    // private Vector3 _lastPosition;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
    }
    void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
    }

    void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
    }


    void Update()
    {
        //jump&fall
        _isGrounded = _controller.isGrounded;

        if (_isGrounded && _velocity.y < 0f) //reset velocity so it doesnt pile up
            _velocity.y = -2f; 

        if (_jumpAction.WasPressedThisFrame() && _isGrounded)
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

        _velocity.y += _gravity * Time.deltaTime;
        
        //movement
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        _controller.Move(move * _speed * Time.deltaTime);
        _controller.Move(_velocity * Time.deltaTime);
    }
}
