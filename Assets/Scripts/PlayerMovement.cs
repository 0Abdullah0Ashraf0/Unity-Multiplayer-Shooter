using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float  _speed;
    [SerializeField] private float _runSpeed = 5f;
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _crouchSpeed = 2.5f;
    [SerializeField] private float _crouchTransitionSpeed = 8f;

    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _standHeight  = 3.3f;
    [SerializeField] private float _crouchHeight = 2.5f;
    private float _targetHeight;

    private float _gravity = -9.81f * 2f;
    private bool _isGrounded;
    private Vector3 _velocity;

    private CharacterController _controller;
    private PlayerInput _playerInput;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _walkAction;
    private InputAction _crouchAction; 

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
        _walkAction = _playerInput.actions["Walk"];
        _crouchAction = _playerInput.actions["Crouch"];

        if (_moveAction == null)   Debug.LogError("Move action not found!");
        if (_jumpAction == null)   Debug.LogError("Jump action not found!");
        if (_walkAction == null)   Debug.LogError("Walk action not found!");
        if (_crouchAction == null) Debug.LogError("Crouch action not found!");
    }
    // void OnEnable()
    // {
    //     _moveAction.Enable();
    //     _jumpAction.Enable();
    //     _walkAction.Enable();
    //     _crouchAction.Enable();
    // }

    // void OnDisable()
    // {
    //     _moveAction.Disable();
    //     _jumpAction.Disable();
    //     _walkAction.Disable();
    //     _crouchAction.Disable();
    // }


    void Update()
    {
        HandleGrounding();
        HandleJump();
        HandleCrouch();
        HandleMovement();

        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    void HandleGrounding()
    {
        _isGrounded = _controller.isGrounded;
        //reset velocity so it doesnt pile up
        if (_isGrounded && _velocity.y < 0f) 
            _velocity.y = -2f; 
    }

    void HandleJump()
    {
        if (_jumpAction.WasPressedThisFrame() && _isGrounded && !_crouchAction.IsPressed())
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
    }
    void HandleCrouch()
    {
        _targetHeight = _crouchAction.IsPressed() ? _crouchHeight : _standHeight;

        //height transition
        float newHeight = Mathf.Lerp(
            _controller.height,
            _targetHeight,
            Time.deltaTime * _crouchTransitionSpeed
        );

        //adjust center -> feet stay on the ground
        float heightDelta    = newHeight - _controller.height;
        _controller.center  += new Vector3(0f, heightDelta / 2f, 0f);
        _controller.height   = newHeight;
    }
    void HandleMovement()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 move  = transform.right * input.x + transform.forward * input.y;

        _speed = _runSpeed;

        if (_crouchAction.IsPressed())
            _speed = _crouchSpeed;
        else if (_walkAction.IsPressed())
            _speed = _walkSpeed;

        Debug.Log($"Walk pressed: {_walkAction.IsPressed()} | Crouch pressed: {_crouchAction.IsPressed()} | Speed: {_speed}");

        _controller.Move(move * _speed * Time.deltaTime);
    }
}
