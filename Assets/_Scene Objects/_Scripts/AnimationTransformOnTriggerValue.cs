using UnityEngine;
using UnityEngine.InputSystem;

namespace MikeNspired.XRIStarterKit
{
    public class AnimationTransformOnTriggerValue : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionProperty triggerAction;

        [Header("Animation Objects")]
        [SerializeField] private Transform MovingObject;
        [SerializeField] private Transform endPosition;

        private Vector3 startLocalPosition;
        private Quaternion startLocalRotation;

        private void Start()
        {
            if (MovingObject == null || endPosition == null) return;

            startLocalPosition = MovingObject.localPosition;
            startLocalRotation = MovingObject.localRotation;
        }

        private void OnEnable()
        {
            if (triggerAction.action != null)
                triggerAction.action.Enable();
        }

        private void Update()
        {
            if (triggerAction.action == null) return;
            if (MovingObject == null || endPosition == null) return;

            float value = triggerAction.action.ReadValue<float>();

            MovingObject.localPosition = Vector3.Lerp(
                startLocalPosition,
                endPosition.localPosition,
                value
            );

            MovingObject.localRotation = Quaternion.Lerp(
                startLocalRotation,
                endPosition.localRotation,
                value
            );
        }
    }
}