using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    public Vector2 MovementInputVector{get; private set;}
    
    public event Action OnJumpButtonPressed; 
    public event Action OnInventory1Pressed;
    public event Action OnInventory2Pressed;
    public event Action OnInteractPressed;
    
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
        //Debug.Log($"Run pressed: {inputValue.isPressed}");
        IsRunning = inputValue.isPressed;
    }
    
    private void OnInventory1(InputValue inputValue)
    {
        if (inputValue.isPressed)
            OnInventory1Pressed?.Invoke();
    }

    private void OnInventory2(InputValue inputValue)
    {
        if (inputValue.isPressed)
            OnInventory2Pressed?.Invoke();
    }

    private void OnInteract(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            OnInteractPressed?.Invoke();
        }
    }
}
