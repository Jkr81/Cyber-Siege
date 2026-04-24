using UnityEngine;
using UnityEngine.InputSystem;

public class ShipFollowHybrid : MonoBehaviour
{
    [Header("XR Input")]
    [SerializeField] private InputActionProperty leftMove;
    [SerializeField] private InputActionProperty rightTurn;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private bool lockY = true;

    private float fixedY;

    private void OnEnable()
    {
        if (leftMove.action != null) leftMove.action.Enable();
        if (rightTurn.action != null) rightTurn.action.Enable();
    }

    private void OnDisable()
    {
        if (leftMove.action != null) leftMove.action.Disable();
        if (rightTurn.action != null) rightTurn.action.Disable();
    }

    private void Start()
    {
        fixedY = transform.position.y;
    }

    private void Update()
    {
        Vector2 moveInput = leftMove.action != null ? leftMove.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 turnInput = rightTurn.action != null ? rightTurn.action.ReadValue<Vector2>() : Vector2.zero;

        Vector3 move = (transform.forward * moveInput.y) + (transform.right * moveInput.x);
        move.y = 0f;
        transform.position += move * moveSpeed * Time.deltaTime;

        float yawAmount = turnInput.x * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, yawAmount, 0f, Space.World);

        if (lockY)
        {
            Vector3 pos = transform.position;
            pos.y = fixedY;
            transform.position = pos;
        }
    }
}