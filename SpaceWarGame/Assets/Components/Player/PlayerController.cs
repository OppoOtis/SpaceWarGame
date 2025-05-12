using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private const int PICKUP_OBJECTS_MAX_BUFFER_SIZE = 1;
    
    [SerializeField] private MainAttack mainAttack;
    [SerializeField] private Transform playerModelTransform;
    [SerializeField] private Transform playerPickupTransform;
    [SerializeField] private float pickupRadius = 1;
    private InputSystem_Actions inputActions;
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // degrees per second

    private Vector2 movementInput;
    private bool attacking;

    private float lastTimeAttack;
    private Collider[] pickupObjects;
    private int amountPickupableObjects;
    private bool usingPickuppedObject;
    private IInteractable pickuppedObject;

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
    #endregion

    private void Awake()
    {
        pickupObjects = new Collider[PICKUP_OBJECTS_MAX_BUFFER_SIZE];
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();

        inputActions.Player.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => movementInput = Vector2.zero;
        
        inputActions.Player.MainAttack.performed += StartAttacking;
        inputActions.Player.MainAttack.canceled += StopAttacking;
        
        inputActions.Player.Interact.performed += Interact;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void FixedUpdate()
    {
        Move();
        CheckPickupObjects();

        if (!attacking)
        {
            RotatePlayer(movementInput);
            RotateMainWeapon(movementInput);
        }
    }
    
    private void Update()
    {
        if(attacking)
            Attacking();

        if (usingPickuppedObject)
        {
            pickuppedObject.Using();
        }
    }

    private void Interact(InputAction.CallbackContext ctx)
    {
        if (amountPickupableObjects > 0)
        {
            pickuppedObject = pickupObjects[0].GetComponent<IInteractable>();
            pickuppedObject.PickUp();
            pickuppedObject.GetTransform().parent = playerPickupTransform;
            pickuppedObject.GetTransform().localPosition = Vector3.zero;
            pickuppedObject.GetTransform().localRotation = Quaternion.identity;
            
            inputActions.Player.MainAttack.performed -= StartAttacking;
            inputActions.Player.MainAttack.performed += UsePickuppedObject;
            inputActions.Player.MainAttack.canceled += DoneUsingPickuppedObject;
        }
    }

    private void UsePickuppedObject(InputAction.CallbackContext ctx)
    {
        usingPickuppedObject = true;
    }
    private void DoneUsingPickuppedObject(InputAction.CallbackContext ctx)
    {
        usingPickuppedObject = false;
        pickuppedObject.GetTransform().parent = null;
        pickuppedObject.UseDone(ctx);
        pickuppedObject = null;
        
        inputActions.Player.MainAttack.performed += StartAttacking;
        inputActions.Player.MainAttack.performed -= UsePickuppedObject;
        inputActions.Player.MainAttack.canceled -= DoneUsingPickuppedObject;
    }

    private void StartAttacking(InputAction.CallbackContext ctx)
    {
        attacking = true;
    }

    private void Attacking()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        bool hitGroundPlane = HelperFunctinos.GetRayYPlaneIntersection(ray, out Vector3 pos);
        Vector2 dir = new Vector2(pos.x - transform.position.x, pos.z - transform.position.z);
        if (hitGroundPlane)
        {
            RotatePlayer(dir);
        }
        
        if (lastTimeAttack + mainAttack.attackCooldown < Time.timeSinceLevelLoad)
        {
            if (hitGroundPlane)
            {
                RotateMainWeapon(dir);
            }
            
            lastTimeAttack = Time.timeSinceLevelLoad;
            mainAttack.animator.SetBool("Attacking", true);
            return;
        }
        mainAttack.animator.SetBool("Attacking", false);
    }

    private void StopAttacking(InputAction.CallbackContext ctx)
    {
        attacking = false;
        mainAttack.animator.SetBool("Attacking", false);
    }

    private void Move()
    {
        Vector3 moveVector = new Vector3(movementInput.x, 0f, movementInput.y);
        rb.linearVelocity = moveVector * moveSpeed + new Vector3(0, rb.linearVelocity.y, 0); // preserve gravity
    }

    private void RotateMainWeapon(Vector2 dir)
    {
        if(dir == Vector2.zero)
            return;
        
        mainAttack.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);
    }

    private void RotatePlayer(Vector2 direction)
    {
        Vector3 moveDirection = new Vector3(direction.x, 0f, direction.y);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion smoothedRotation = Quaternion.RotateTowards(playerModelTransform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            playerModelTransform.rotation = smoothedRotation;
        }
    }

    private void CheckPickupObjects()
    {
        int pickupMask = 0 | 1 << LayerMask.NameToLayer("Interactable");
        amountPickupableObjects = Physics.OverlapSphereNonAlloc(transform.position, pickupRadius, pickupObjects, pickupMask);
        // if (amountPickupableObjects > PICKUP_OBJECTS_MAX_BUFFER_SIZE)
        // {
        //     Debug.LogWarning($"Detecting {amountPickupableObjects} which is more then {PICKUP_OBJECTS_MAX_BUFFER_SIZE}. Some objects are not detected");
        // }
    }
}
