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

    [Header("Curved Tunnel")]
    [SerializeField] private bool followCurve = true;
    [SerializeField] private float curveSideAmount = 10f;
    [SerializeField] private float curveUpAmount = 4f;
    [SerializeField] private float curveLength = 250f;

    [Header("Player Control Inside Tunnel")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxTunnelRadius = 4.5f;
    [SerializeField] private float returnToCenterSpeed = 2f;
    [SerializeField] private float inputDeadzone = 0.15f;

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 90f;

    private float currentForwardSpeed;
    private float forwardDistance;
    private Vector2 tunnelOffset;
    private Vector3 startPosition;

    private bool bossPaused = false;

    public float DifficultyMultiplier { get; private set; } = 1f;

    public float ForwardDistance
    {
        get { return forwardDistance; }
    }

    public void SetBossPaused(bool paused)
    {
        bossPaused = paused;
    }

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
        currentForwardSpeed = startForwardSpeed;
    }

    private void Update()
    {
        Vector2 moveInput = leftMove.action != null ? leftMove.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 turnInput = rightTurn.action != null ? rightTurn.action.ReadValue<Vector2>() : Vector2.zero;

        if (moveInput.magnitude < inputDeadzone)
            moveInput = Vector2.zero;

        if (autoMoveForward && !bossPaused)
        {
            currentForwardSpeed += speedIncreaseRate * Time.deltaTime;
            currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, startForwardSpeed, maxForwardSpeed);
            forwardDistance += currentForwardSpeed * Time.deltaTime;
        }

        if (moveInput != Vector2.zero)
        {
            tunnelOffset += moveInput * moveSpeed * Time.deltaTime;
        }
        else
        {
            tunnelOffset = Vector2.Lerp(tunnelOffset, Vector2.zero, returnToCenterSpeed * Time.deltaTime);
        }

        tunnelOffset = Vector2.ClampMagnitude(tunnelOffset, maxTunnelRadius);

        Vector3 center = GetTunnelCenter(forwardDistance);

        Vector3 newPosition =
            center +
            Vector3.right * tunnelOffset.x +
            Vector3.up * tunnelOffset.y;

        transform.position = newPosition;

        float yawAmount = turnInput.x * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, yawAmount, 0f, Space.World);

        DifficultyMultiplier = 1f + (forwardDistance / 50f);
        DifficultyMultiplier = Mathf.Clamp(DifficultyMultiplier, 1f, 3f);
    }

    private Vector3 GetTunnelCenter(float distance)
    {
        Vector3 basePos = startPosition + Vector3.forward * distance;

        if (!followCurve)
            return basePos;

        float t = distance / curveLength;

        float sideCurve = Mathf.Sin(t) * curveSideAmount;
        float upCurve = Mathf.Sin(t * 0.6f) * curveUpAmount;

        return basePos + Vector3.right * sideCurve + Vector3.up * upCurve;
    }
}