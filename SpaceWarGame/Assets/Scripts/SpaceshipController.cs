using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpaceshipController : MonoBehaviour
{
    [SerializeField] private RawImage selectionBox;
    [SerializeField] private float selectionTransparency = 0.3f;
    
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool selecting;
    private Vector2 startSelectPos;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Select.performed += Select;
        inputActions.Player.Select.canceled += SelectCancel;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Select(InputAction.CallbackContext ctx)
    {
        startSelectPos = GetCurrentUIPos();
        selecting = true;
        
        Color color = selectionBox.color;
        color.a = selectionTransparency;
        selectionBox.color = color;
    }
    
    private void SelectCancel(InputAction.CallbackContext ctx)
    {
        selecting = false;
        
        Color color = selectionBox.color;
        color.a = 0;
        selectionBox.color = color;
    }

    private void Update()
    {
        if (selecting)
        {
            selectionBox.rectTransform.position = (GetCurrentUIPos() + startSelectPos) / 2;
            selectionBox.rectTransform.localScale = new Vector3(
                Mathf.Abs(GetCurrentUIPos().x - startSelectPos.x),
                Mathf.Abs(GetCurrentUIPos().y - startSelectPos.y),
                1
            );
        }
        
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
        transform.Translate(movement * Time.deltaTime * 5f);
    }

    private Vector2 GetCurrentUIPos()
    {
        return Mouse.current.position.ReadValue();
    }
}
