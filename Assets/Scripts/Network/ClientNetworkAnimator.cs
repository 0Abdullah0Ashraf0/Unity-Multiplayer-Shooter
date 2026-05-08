using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;

public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    // --- Input ---
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _fireAction;

    [SerializeField] private Renderer[] _bodyRenderers;

    private static readonly int MoveX = Animator.StringToHash("moveX");
    private static readonly int MoveY = Animator.StringToHash("moveY");
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsFiring = Animator.StringToHash("isFiring");

    // Smooth damp refs
    private float _currentX, _currentY;
    private float _velX, _velY;
    private const float SmoothTime = 0.1f;

    void Awake()
    {
        if (Animator == null)
            Animator = GetComponentInChildren<Animator>();

        base.Awake();

        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        _fireAction = _playerInput.actions["Shoot"];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        if (IsOwner)
        {
            // Hide own body from self — other clients still see it normally
            foreach (var r in _bodyRenderers)
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector2 input = _moveAction.ReadValue<Vector2>();
        bool isMoving = input.sqrMagnitude > 0.01f;

        // Smooth the blend tree values so transitions aren't snappy
        _currentX = Mathf.SmoothDamp(_currentX, input.x, ref _velX, SmoothTime);
        _currentY = Mathf.SmoothDamp(_currentY, input.y, ref _velY, SmoothTime);

        Animator.SetFloat(MoveX, _currentX);
        Animator.SetFloat(MoveY, _currentY);
        Animator.SetBool(IsMoving, isMoving);
        Animator.SetBool(IsFiring, _fireAction.IsPressed());
    }

    // Call from your weapon script
    public void SetFiring(bool firing)
    {
        if (!IsOwner) return;
        Animator.SetBool(IsFiring, firing);
    }
}