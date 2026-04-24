// Author MikeNspired.
// Edited for Cyber Siege: supports XR player guns + enemy animation-triggered guns.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace MikeNspired.XRIStarterKit
{
    public class ProjectileWeapon : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private bool useXRInput = true;
        [SerializeField] private InputActionProperty triggerAction;

        [Header("Required Refs")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Rigidbody projectilePrefab;
        [SerializeField] private ParticleSystem cartridgeEjection;
        [SerializeField] private AudioSource fireAudio;
        [SerializeField] private AudioSource outOfAmmoAudio;
        [SerializeField] private MatchTransform bulletFlash;

        [Header("Settings")]
        public MagazineAttachPoint magazineAttach = null;
        public float recoilAmount = -0.03f;
        public float recoilRotation = 1f;
        public float recoilTime = 0.06f;
        public int bulletsPerShot = 1;
        public float bulletSpreadAngle = 1f;
        public float bulletSpeed = 150f;
        public bool infiniteAmmo = true;
        public float hapticDuration = 0.1f;
        public float hapticStrength = 0.5f;

        [Header("Enemy Spawn Override")]
        [Tooltip("When true, ignores firePoint and spawns bullets from this gun's own transform. " +
                 "Use for enemies whose firePoint isn't tracking correctly in world space.")]
        [SerializeField] private bool useTransformAsFirePoint = false;
        [Tooltip("How far in front of the gun's forward to spawn the bullet (metres).")]
        [SerializeField] private float spawnForwardOffset = 0.3f;

        [Header("Auto-Fire")]
        public float fireSpeed = 0.25f;
        public bool automaticFiring = false;

        [Header("Debug")]
        [SerializeField] private bool showFirePointDebug = true;

        [Header("Optional Haptics")]
        [SerializeField] private HapticImpulsePlayer hapticPlayer;

        private Collider[] gunColliders;
        private bool wasPressed;
        private bool isFiring;
        private float fireTimer;

        public UnityEvent BulletFiredEvent, OutOfAmmoEvent, FiredLastBulletEvent;

        private void OnEnable()
        {
            if (useXRInput && triggerAction.action != null)
                triggerAction.action.Enable();

            Application.onBeforeRender += RecoilUpdate;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= RecoilUpdate;
        }

        private void Update()
        {
            if (!useXRInput) return;
            if (triggerAction.action == null) return;

            float triggerValue = triggerAction.action.ReadValue<float>();
            bool pressed = triggerValue > 0.5f;

            if (automaticFiring)
            {
                isFiring = pressed;

                if (isFiring && fireTimer >= fireSpeed)
                {
                    FireGun();
                    fireTimer = 0f;
                }

                fireTimer += Time.deltaTime;
            }
            else
            {
                if (pressed && !wasPressed)
                    FireGun();
            }

            wasPressed = pressed;
        }

        public void ShootFromAnimation()
        {
            FireGun();
        }

        public void FireGun()
        {
            if (bulletsPerShot < 1) return;

            if (!useTransformAsFirePoint && firePoint == null)
            {
                Debug.LogWarning($"{name}: No Fire Point assigned.");
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{name}: No Projectile Prefab assigned.");
                return;
            }

            if (magazineAttach && !infiniteAmmo)
            {
                if (!magazineAttach.Magazine || !magazineAttach.Magazine.UseAmmo())
                {
                    OutOfAmmoEvent?.Invoke();

                    if (outOfAmmoAudio != null && outOfAmmoAudio.enabled && outOfAmmoAudio.clip != null)
                        outOfAmmoAudio.PlayOneShot(outOfAmmoAudio.clip);

                    return;
                }
            }

            // Resolve the spawn position and forward direction.
            // If useTransformAsFirePoint is on, use this gun's own transform
            // so the bullet always comes from the gun regardless of firePoint issues.
            Vector3 spawnPosition = useTransformAsFirePoint
                ? transform.position + transform.forward * spawnForwardOffset
                : firePoint.position;

            Vector3 spawnForward = useTransformAsFirePoint
                ? transform.forward
                : firePoint.forward;

            for (int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = Vector3.Slerp(
                    spawnForward,
                    UnityEngine.Random.insideUnitSphere,
                    bulletSpreadAngle / 180f
                ).normalized;

                if (showFirePointDebug)
                {
                    Debug.Log($"{name} spawn position: {spawnPosition}, forward: {spawnForward} " +
                              $"[{(useTransformAsFirePoint ? "TRANSFORM OVERRIDE" : "firePoint")}]");
                    Debug.DrawRay(spawnPosition, spawnForward * 5f, Color.red, 3f);
                }

                Rigidbody bullet = Instantiate(
                    projectilePrefab,
                    spawnPosition,
                    Quaternion.LookRotation(shotDirection)
                );

                IgnoreColliders(bullet);

                bullet.linearVelocity = Vector3.zero;
                bullet.angularVelocity = Vector3.zero;
                bullet.AddForce(shotDirection * bulletSpeed, ForceMode.VelocityChange);

                BulletFiredEvent?.Invoke();

                StopAllCoroutines();
                StartRecoil();
            }

            if (magazineAttach && magazineAttach.Magazine && magazineAttach.Magazine.CurrentAmmo == 0)
                FiredLastBulletEvent?.Invoke();

            if (bulletFlash)
            {
                MatchTransform flash = Instantiate(
                    bulletFlash,
                    spawnPosition,
                    useTransformAsFirePoint ? transform.rotation : firePoint.rotation
                );

                flash.positionToMatch = useTransformAsFirePoint ? transform : firePoint;
            }

            if (fireAudio != null && fireAudio.enabled && fireAudio.gameObject.activeInHierarchy && fireAudio.clip != null)
                fireAudio.PlayOneShot(fireAudio.clip);

            if (cartridgeEjection)
                cartridgeEjection.Play();

            if (hapticPlayer != null && useXRInput)
                hapticPlayer.SendHapticImpulse(hapticStrength, hapticDuration);
        }

        private void IgnoreColliders(Component bullet)
        {
            gunColliders = GetComponentsInChildren<Collider>(true);
            Collider bulletCollider = bullet.GetComponentInChildren<Collider>();

            if (bulletCollider == null) return;

            foreach (Collider c in gunColliders)
            {
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                if (!bulletCollider.gameObject.scene.IsValid()) continue;

                Physics.IgnoreCollision(c, bulletCollider);
            }
        }

        #region Recoil

        private Quaternion startingRotation;
        private Vector3 startingPosition;
        private Vector3 recoilTargetPosition;
        private Quaternion recoilTargetRotation;
        private float timer;
        private bool isRecoiling;

        private void StartRecoil()
        {
            startingPosition = transform.localPosition;
            startingRotation = transform.localRotation;

            recoilTargetPosition = startingPosition + new Vector3(0f, 0f, recoilAmount);
            recoilTargetRotation = startingRotation * Quaternion.Euler(-recoilRotation, 0f, 0f);

            timer = 0f;
            isRecoiling = true;
        }

        [BeforeRenderOrder(101)]
        private void RecoilUpdate()
        {
            if (!isRecoiling) return;

            timer += Time.deltaTime;
            float t = timer / recoilTime;

            if (t < 0.5f)
            {
                float pushT = t / 0.5f;
                transform.localPosition = Vector3.Lerp(startingPosition, recoilTargetPosition, pushT);
                transform.localRotation = Quaternion.Lerp(startingRotation, recoilTargetRotation, pushT);
            }
            else
            {
                float returnT = (t - 0.5f) / 0.5f;
                transform.localPosition = Vector3.Lerp(recoilTargetPosition, startingPosition, returnT);
                transform.localRotation = Quaternion.Lerp(recoilTargetRotation, startingRotation, returnT);
            }

            if (timer >= recoilTime)
            {
                transform.localPosition = startingPosition;
                transform.localRotation = startingRotation;
                isRecoiling = false;
            }
        }

        #endregion
    }
}