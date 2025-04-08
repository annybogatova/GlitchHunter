using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerInputController _playerInputController;
    private CharacterController _characterController;
    private Transform _cameraTransform;

    [Header("Movement")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;
    [SerializeField] private float turnSmoothTime = 0.2f;
    private float _turnSmoothVelocity;
    private bool _isRunningInput = false;
    
    [Header("Jumping")]
    [SerializeField] private float jumpSpeed = 5.0f;
    [SerializeField] private float jumpButtonGracePeriod = 0.2f;
    //[SerializeField] private float gravityMultiplier = 2.0f;
    [SerializeField] private float coyoteTime = 0.2f;
    private float _ySpeed;
    private float? _lastGroundedTime;
    private float? _jumpButtonPressedTime;
    private float _originalStepOffset;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Range(0, 0.1f)] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundedYOffset = -0.5f;
    
    private void Awake()
    {
        _playerInputController = GetComponent<PlayerInputController>();
        _characterController = GetComponent<CharacterController>();
        _originalStepOffset = _characterController.stepOffset;

        _playerInputController.OnJumpButtonPressed += JumpButtonPressed;
        //_playerInputController.OnRunButtonPressed += HandleRunStateChanged;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera not found.");
        }
    }

    private void Update()
    {
        Vector2 input = _playerInputController.MovementInputVector;
        _isRunningInput = _playerInputController.IsRunning;
        
        Vector3 movement = Vector3.zero;
        
        bool hasMove = input.magnitude >= 0.1f;

        if (hasMove)
        {
            Vector3 cameraForward = Vector3.Scale(_cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 cameraRight = Vector3.Scale(_cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            
            movement = (cameraForward * input.y + cameraRight * input.x).normalized;
            
            // Поворачиваем персонажа лицом к направлению движения,
            // ТОЛЬКО ЕСЛИ игрок не движется преимущественно назад.
            // input.y > -0.5f: Поворачиваем, если идем вперед или не сильно назад.
            // input.x != 0: Поворачиваем, если есть боковое движение (стрейф).
            // Это предотвращает разворот на 180 градусов при движении строго назад.
            if (input.y > -0.5f || Mathf.Abs(input.x) > 0.1f) 
            {
                float targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            // Если условие выше НЕ выполнено (т.е. input.y <= -0.5f и input.x почти 0),
            // персонаж НЕ будет менять свое вращение, он просто попятится назад,
            // сохраняя текущее направление взгляда (или последнее направление).
        }
        
        float currentSpeed = _isRunningInput ? runSpeed : speed;
        Vector3 horizontalVelocity = movement * currentSpeed;
        
        bool isGrounded = CheckGrounded();

        if (isGrounded)
        {
            _lastGroundedTime = Time.time;
            _characterController.stepOffset = _originalStepOffset;
            _ySpeed = groundedYOffset;
        }
        else
        {
            _characterController.stepOffset = 0;
            _ySpeed += Physics.gravity.y  * Time.deltaTime;
        }

        // Прыжок — учитываем Jump Buffer И Coyote Time
        bool canJump = _jumpButtonPressedTime.HasValue &&
                       Time.time - _jumpButtonPressedTime.Value <= jumpButtonGracePeriod &&
                       _lastGroundedTime.HasValue &&
                       Time.time - _lastGroundedTime.Value <= coyoteTime;

        if (canJump)
        {
            _ySpeed = jumpSpeed;
            _jumpButtonPressedTime = null;
            _lastGroundedTime = null;
        }
        
        Vector3 velocity = horizontalVelocity;
        velocity.y = _ySpeed;
        _characterController.Move(velocity * Time.deltaTime);
        
        
        Debug.Log($"Current speed: {currentSpeed}");
        if (_ySpeed == jumpSpeed)
        {
            Debug.Log("Прыжок сработал! _ySpeed: " + _ySpeed);
        }
    }

    private void JumpButtonPressed()
    {
        Debug.Log("Jump button pressed");
        _jumpButtonPressedTime = Time.time;
    }

    private bool CheckGrounded()
    {
        Vector3 spherePosition = transform.position + _characterController.center + Vector3.down * (_characterController.height / 2f - _characterController.radius + 0.1f);
        return Physics.CheckSphere(spherePosition, _characterController.radius, groundLayer, QueryTriggerInteraction.Ignore);
    }
    private void OnDrawGizmosSelected()
    {
        // ... (код Gizmos остается тем же) ...
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_characterController == null) return;

        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Vector3 spherePosition = transform.position + _characterController.center + Vector3.down * (_characterController.height / 2f - _characterController.radius + groundCheckDistance);

        if (CheckGrounded()) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawSphere(spherePosition, _characterController.radius);
    }
    
    private void OnDestroy()
    {
        if (_playerInputController != null)
        {
            _playerInputController.OnJumpButtonPressed -= JumpButtonPressed;
            // Отписка от бега
        }
    }
}
