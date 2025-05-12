using UnityEngine;
using UnityEngine.InputSystem;

public interface IInteractable
{
    public void PickUp();
    public void StartUse();
    public void Using();
    public void UseDone(InputAction.CallbackContext ctx);
    public Transform GetTransform();
}
