using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerInputController _playerInputController;
    private CharacterController _characterController;
    private Transform _cameraTransform;
    private Animator _animator;

    [Header("Movement")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;
    [SerializeField] private float turnSmoothTime = 0.2f;
    [SerializeField] private float speedSmoothTime = 0.1f;
    private float _turnSmoothVelocity;
    private bool _isRunningInput = false;
    
    [Header("Jumping")]
    [SerializeField] private float jumpSpeed = 5.0f;
    [SerializeField] private float jumpButtonGracePeriod = 0.2f;
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
        // Кэшируем ссылки на компоненты и подписываемся на событие прыжка
        _playerInputController = GetComponent<PlayerInputController>();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _originalStepOffset = _characterController.stepOffset;

        _playerInputController.OnJumpButtonPressed += JumpButtonPressed;
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
        // Получаем вектор движения от пользовательского ввода
        Vector2 input = _playerInputController.MovementInputVector;
        _isRunningInput = _playerInputController.IsRunning;
        
        Vector3 movement = Vector3.zero;
        
        // Проверяем, есть ли движение — это убережёт от дрожания и ложных срабатываний
        bool hasMove = input.magnitude >= 0.1f;
        _animator.SetBool("IsMoving", hasMove);
        
        if (hasMove)
        {
            // Преобразуем вектор движения в мировые координаты на основе направления камеры
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
                // Плавно поворачиваем персонажа к направлению движения (если он не пятится)
                float targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            // Если условие выше НЕ выполнено (т.е. input.y <= -0.5f и input.x почти 0),
            // персонаж НЕ будет менять свое вращение, он просто попятится назад,
            // сохраняя текущее направление взгляда (или последнее направление).
        }
        
        float currentSpeed = _isRunningInput ? runSpeed : speed; // меняем скорость если персонаж бежит
        Vector3 horizontalVelocity = movement * currentSpeed;
        
        float animationSpeedPercent = (_isRunningInput ? 1f : 0.5f) * movement.magnitude;
        _animator.SetFloat("Speed", animationSpeedPercent, speedSmoothTime, Time.deltaTime); // плавные переходы между анимациями
        
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
        
        // до root motion анимации
        // Vector3 velocity = horizontalVelocity;
        // velocity.y = _ySpeed; // учитываем прыжок в итоговой скорости
        // _characterController.Move(velocity * Time.deltaTime);
        
        
        Debug.Log($"Current speed: {currentSpeed}");
        if (_ySpeed == jumpSpeed)
        {
            Debug.Log("Прыжок сработал! _ySpeed: " + _ySpeed);
        }
    }

    private void OnAnimatorMove()
    {
        // учитываем перемещение заложенное в анимации
        Vector3 velocity = _animator.deltaPosition;
        velocity.y = _ySpeed * Time.deltaTime;
        _characterController.Move(velocity);
    }

    private void JumpButtonPressed()
    {
        Debug.Log("Jump button pressed");
        _jumpButtonPressedTime = Time.time;
    }

    private bool CheckGrounded()
    {
        // Вычисление позиции сферы для проверки земли:
        // Начальная точка — это позиция персонажа, с добавлением смещения центра капсулы (_characterController.center).
        // Дальше смещаем точку вниз, с учётом половины высоты капсулы и радиуса (чтобы сфера была на уровне основания).
        // Маленькое смещение groundCheckDistance гарантирует, что сфера точно ниже основания капсулы, а не на нём.
        Vector3 spherePosition = transform.position + _characterController.center + Vector3.down * (_characterController.height / 2f - _characterController.radius + groundCheckDistance);
        // Проверка с использованием Physics.CheckSphere:
        // Проверяется пересечение сферы, созданной у основания персонажа, с объектами на слое "земля" (_groundLayer).
        // Если пересечение найдено, возвращаем true (персонаж на земле), иначе false (в воздухе).
        return Physics.CheckSphere(spherePosition, _characterController.radius, groundLayer, QueryTriggerInteraction.Ignore);
    }
    private void OnDrawGizmosSelected()
    {
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
