using UnityEngine;
using UnityEngine.InputSystem;

public class ShipFollowHybrid : MonoBehaviour
{
    [Header("XR Input")]
    [SerializeField] private InputActionProperty leftMove;
    [SerializeField] private InputActionProperty rightTurn;

    [Header("Tunnel Movement")]
    [SerializeField] private bool autoMoveForward = true;
    [SerializeField] private float startForwardSpeed = 6f;
    [SerializeField] private float maxForwardSpeed = 20f;
    [SerializeField] private float speedIncreaseRate = 0.1f;

    [Header("Player Control Inside Tunnel")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxTunnelRadius = 4.5f;

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 90f;

    [Header("Tunnel Axis")]
    [SerializeField] private Vector3 tunnelDirection = Vector3.forward;
    [SerializeField] private Vector3 tunnelRight = Vector3.right;
    [SerializeField] private Vector3 tunnelUp = Vector3.up;

    [Header("Height Lock")]
    [SerializeField] private bool lockY = false;

    private float currentForwardSpeed;
    private float forwardDistance;
    private Vector2 tunnelOffset;
    private Vector3 startPosition;
    private float fixedY;

    public float DifficultyMultiplier { get; private set; } = 1f;

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
        startPosition = transform.position;
        fixedY = transform.position.y;
        currentForwardSpeed = startForwardSpeed;

        tunnelDirection = tunnelDirection.normalized;
        tunnelRight = tunnelRight.normalized;
        tunnelUp = tunnelUp.normalized;
    }

    private void Update()
    {
        Vector2 moveInput = leftMove.action != null ? leftMove.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 turnInput = rightTurn.action != null ? rightTurn.action.ReadValue<Vector2>() : Vector2.zero;

        if (autoMoveForward)
        {
            currentForwardSpeed += speedIncreaseRate * Time.deltaTime;
            currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, startForwardSpeed, maxForwardSpeed);
            forwardDistance += currentForwardSpeed * Time.deltaTime;
        }

        // Move inside circular tunnel area
        tunnelOffset += moveInput * moveSpeed * Time.deltaTime;

        // This keeps you inside a circle, not just left/right
        tunnelOffset = Vector2.ClampMagnitude(tunnelOffset, maxTunnelRadius);

        Vector3 newPosition =
            startPosition +
            tunnelDirection * forwardDistance +
            tunnelRight * tunnelOffset.x +
            tunnelUp * tunnelOffset.y;

        if (lockY)
            newPosition.y = fixedY;

        transform.position = newPosition;

        // Turning only rotates view/ship, it does NOT change tunnel direction
        float yawAmount = turnInput.x * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, yawAmount, 0f, Space.World);

        DifficultyMultiplier = 1f + (forwardDistance / 75f);
    }
}