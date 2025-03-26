using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float turnSmoothTime = 0.2f;
    private float _turnSmoothVelocity;
    
    public float speedSmoothTime = 0.1f;
    private float _speedSmoothVelocity;
    
    private Animator _animator;
    private CharacterController _controller;

    private Vector3 _velocity;
    private bool _isGrounded;
    
    void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        
    }

    // Update is called once per frame
    void Update()
    {
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
    }
}
