using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    public Vector2 MovementInputVector{get; private set;}
    
    public event Action OnJumpButtonPressed; 
    
    public bool IsRunning {get; private set;}
    
    private void OnMove(InputValue inputValue)
    {
        MovementInputVector = inputValue.Get<Vector2>();
    }

    private void OnJump(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            OnJumpButtonPressed?.Invoke();
        }
    }

    private void OnRun(InputValue inputValue)
    {
        Debug.Log($"Run pressed: {inputValue.isPressed}");
        IsRunning = inputValue.isPressed;
    }
}
