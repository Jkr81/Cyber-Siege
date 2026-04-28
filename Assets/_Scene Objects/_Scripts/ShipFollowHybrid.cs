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

    [Header("Light Speed Burst")]
    [SerializeField] private float lightSpeedAcceleration = 2.5f;
    [SerializeField] private float lightSpeedVisualSpeed = 45f;
    [SerializeField] private float lightSpeedEnginePitch = 2.4f;
    [SerializeField] private float lightSpeedEngineVolume = 0.85f;
    [SerializeField] private bool moveDuringLightSpeed = false;

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

    [Header("Intro Auto Centering")]
    [SerializeField] private float introPositionCenterSpeed = 1.8f;
    [SerializeField] private float introRotationCenterSpeed = 2.5f;

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private bool allowTurningDuringBoss = false;

    [Header("Engine Audio")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private bool playEngineOnStart = true;
    [SerializeField] private float minEnginePitch = 0.8f;
    [SerializeField] private float maxEnginePitch = 1.4f;
    [SerializeField] private float normalEngineVolume = 0.35f;
    [SerializeField] private float bossEngineVolume = 0.2f;

    private float currentForwardSpeed;
    private float forwardDistance;
    private Vector2 tunnelOffset;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private bool bossPaused = false;
    private bool introPaused = false;
    private bool introAutoCentering = false;

    private bool lightSpeedActive = false;
    private float lightSpeedTimer = 0f;
    private float lightSpeedDuration = 0f;
    private float lightSpeedTargetSpeed = 45f;

    public float DifficultyMultiplier { get; private set; } = 1f;
    public float ForwardDistance => forwardDistance;

    public void SetBossPaused(bool paused) => bossPaused = paused;
    public void SetIntroPaused(bool paused) => introPaused = paused;
    public void SetIntroAutoCentering(bool enabled) => introAutoCentering = enabled;

    public void StartLightSpeedBurst(float duration, float targetSpeed)
    {
        lightSpeedActive = true;
        lightSpeedTimer = 0f;
        lightSpeedDuration = duration;
        lightSpeedTargetSpeed = targetSpeed;

        if (engineAudio != null && !engineAudio.isPlaying)
            engineAudio.Play();
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
        startRotation = transform.rotation;
        currentForwardSpeed = startForwardSpeed;

        if (engineAudio != null)
        {
            engineAudio.loop = true;

            if (playEngineOnStart && !engineAudio.isPlaying)
                engineAudio.Play();
        }
    }

    private void Update()
    {
        Vector2 moveInput = leftMove.action != null ? leftMove.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 turnInput = rightTurn.action != null ? rightTurn.action.ReadValue<Vector2>() : Vector2.zero;

        if (moveInput.magnitude < inputDeadzone)
            moveInput = Vector2.zero;

        HandleForwardMovement();

        if (moveInput.x != 0f && !introAutoCentering && !lightSpeedActive)
            tunnelOffset.x += moveInput.x * moveSpeed * Time.deltaTime;
        else
            tunnelOffset.x = Mathf.Lerp(tunnelOffset.x, 0f, returnToCenterSpeed * Time.deltaTime);

        tunnelOffset.y = 0f;

        if (introAutoCentering || lightSpeedActive)
            tunnelOffset.x = Mathf.Lerp(tunnelOffset.x, 0f, introPositionCenterSpeed * Time.deltaTime);

        tunnelOffset = Vector2.ClampMagnitude(tunnelOffset, maxTunnelRadius);

        Vector3 center = GetTunnelCenter(forwardDistance);
        Vector3 targetPosition = center + Vector3.right * tunnelOffset.x;

        // 🔥 hyperspace drift
        if (lightSpeedActive)
            transform.position = Vector3.Lerp(transform.position, targetPosition, 0.6f * Time.deltaTime);
        else
            transform.position = targetPosition;

        if ((!bossPaused || allowTurningDuringBoss) && !lightSpeedActive)
        {
            float yawAmount = turnInput.x * turnSpeed * Time.deltaTime;
            transform.Rotate(0f, yawAmount, 0f, Space.World);
        }

        if (introAutoCentering || lightSpeedActive)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                startRotation,
                introRotationCenterSpeed * Time.deltaTime
            );
        }

        DifficultyMultiplier = Mathf.Clamp(1f + (forwardDistance / 50f), 1f, 3f);

        UpdateEngineAudio();
    }

    private void HandleForwardMovement()
    {
        if (!autoMoveForward || bossPaused || introPaused)
            return;

        if (lightSpeedActive)
        {
            lightSpeedTimer += Time.deltaTime;

            currentForwardSpeed = Mathf.Lerp(
                currentForwardSpeed,
                lightSpeedTargetSpeed,
                lightSpeedAcceleration * Time.deltaTime
            );

            if (moveDuringLightSpeed)
                forwardDistance += currentForwardSpeed * Time.deltaTime;

            if (lightSpeedTimer >= lightSpeedDuration)
            {
                lightSpeedActive = false;
                currentForwardSpeed = startForwardSpeed;
            }

            return;
        }

        currentForwardSpeed += speedIncreaseRate * Time.deltaTime;
        currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, startForwardSpeed, maxForwardSpeed);

        forwardDistance += currentForwardSpeed * Time.deltaTime;
    }

    private void UpdateEngineAudio()
    {
        if (engineAudio == null) return;

        if (lightSpeedActive)
        {
            engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, lightSpeedEnginePitch, 5f * Time.deltaTime);
            engineAudio.volume = Mathf.Lerp(engineAudio.volume, lightSpeedEngineVolume, 5f * Time.deltaTime);
            return;
        }

        float speedPercent = Mathf.InverseLerp(startForwardSpeed, maxForwardSpeed, currentForwardSpeed);
        engineAudio.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedPercent);
        engineAudio.volume = bossPaused ? bossEngineVolume : normalEngineVolume;
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