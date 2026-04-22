using UnityEngine;

public class ShipFollowHybrid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform mainCamera;

    [Header("Follow Settings")]
    [SerializeField] private bool followYaw = true;
    [SerializeField] private bool lockY = true;
    [SerializeField] private Vector3 shipOffset = Vector3.zero;

    [Header("Desktop Controls")]
    [SerializeField] private bool enableDesktopControls = true;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;

    private float fixedY;
    private float yaw;

    void Start()
    {
        if (xrOrigin != null)
            yaw = xrOrigin.eulerAngles.y;

        fixedY = transform.position.y;

        if (enableDesktopControls)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!enableDesktopControls || xrOrigin == null) return;

        // --- MOUSE LOOK (rotates XR Origin) ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        yaw += mouseX;
        xrOrigin.rotation = Quaternion.Euler(0f, yaw, 0f);

        // --- WASD MOVEMENT (moves XR Origin) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = xrOrigin.forward * v + xrOrigin.right * h;
        move = move.normalized;

        xrOrigin.position += move * moveSpeed * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (xrOrigin == null) return;

        // --- SHIP FOLLOWS XR ORIGIN ---
        Vector3 targetPos = xrOrigin.position + shipOffset;

        if (lockY)
            targetPos.y = fixedY;

        transform.position = targetPos;

        // --- MATCH ROTATION ---
        if (followYaw)
        {
            Vector3 rot = transform.eulerAngles;
            rot.y = xrOrigin.eulerAngles.y;
            transform.eulerAngles = rot;
        }
    }
}