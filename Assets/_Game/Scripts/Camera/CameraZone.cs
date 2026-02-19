using UnityEngine;

namespace DScrollerGame.CameraSystem
{
    [RequireComponent(typeof(BoxCollider))]
    public class CameraZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraFollowController _cameraController;

        [Header("Zone Settings")]
        [SerializeField] private float _transitionTime = 1f;
        [SerializeField] private int _priority = 0; // NEW

        [Header("Override Flags")]
        [SerializeField] private bool _overrideFOV;
        [SerializeField] private bool _overrideXDamping;
        [SerializeField] private bool _overrideYDamping;
        [SerializeField] private bool _overrideOffset;
        [SerializeField] private bool _overrideLookahead;
        [SerializeField] private bool _overrideDeadZoneWidth;
        [SerializeField] private bool _overrideDeadZoneHeight; // NEW

        [Header("Target Values")]
        [SerializeField] private float _targetFOV = 60f;
        [SerializeField] private float _targetXDamping = 1f;
        [SerializeField] private float _targetYDamping = 0.5f;
        [SerializeField] private Vector3 _targetOffset = Vector3.zero;
        [SerializeField] private float _targetLookaheadTime = 0.5f;
        [SerializeField] private float _targetDeadZoneWidth = 0.1f;
        [SerializeField] private float _targetDeadZoneHeight = 0.1f; // NEW

        public int Priority => _priority;
        public float TransitionTime => _transitionTime;

        public bool OverrideFOV => _overrideFOV;
        public float TargetFOV => _targetFOV;

        public bool OverrideXDamping => _overrideXDamping;
        public float TargetXDamping => _targetXDamping;

        public bool OverrideYDamping => _overrideYDamping;
        public float TargetYDamping => _targetYDamping;

        public bool OverrideOffset => _overrideOffset;
        public Vector3 TargetOffset => _targetOffset;

        public bool OverrideLookahead => _overrideLookahead;
        public float TargetLookahead => _targetLookaheadTime;

        public bool OverrideDeadZoneWidth => _overrideDeadZoneWidth;
        public float TargetDeadZoneWidth => _targetDeadZoneWidth;

        public bool OverrideDeadZoneHeight => _overrideDeadZoneHeight;
        public float TargetDeadZoneHeight => _targetDeadZoneHeight;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _cameraController.RegisterZone(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _cameraController.UnregisterZone(this);
            }
        }

        private void OnValidate()
        {
            if (_cameraController == null)
                _cameraController = FindFirstObjectByType<CameraFollowController>();

            GetComponent<BoxCollider>().isTrigger = true;
        }
    }
}