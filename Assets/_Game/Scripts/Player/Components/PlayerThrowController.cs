using UnityEngine;
using UnityEngine.InputSystem;

namespace DScrollerGame.Player
{
    /// <summary>
    /// Dedicated controller for throwing objects with a power meter and aim pointer.
    /// Replaces the throwing functionality previously in PlayerInteractionController.
    /// 
    /// Features:
    /// - Charge-up power meter (Hold LMB to increase force).
    /// - Visual pointer for aim direction.
    /// - Parabolic trajectory preview.
    /// - 2.5D physics constraints.
    /// </summary>
    public class PlayerThrowController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("World-space origin point for thrown objects. Assign a child Transform.")]
        [SerializeField] private Transform _throwOrigin;

        [Tooltip("Prefab to instantiate when throwing. Must have Rigidbody.")]
        [SerializeField] private GameObject _throwPrefab;

        [Tooltip("Camera used for aiming. If null, falls back to Camera.main.")]
        [SerializeField] private Camera _aimCamera;

        [Header("Throw Settings")]
        [Tooltip("Minimum force magnitude.")]
        [SerializeField] private float _minThrowForce = 5f;

        [Tooltip("Maximum force magnitude.")]
        [SerializeField] private float _maxThrowForce = 20f;

        [Tooltip("How fast the throw force charges (units per second).")]
        [SerializeField] private float _chargeSpeed = 10f;

        [Tooltip("Min/max aim angle (degrees).")]
        [SerializeField] private Vector2 _aimAngleClamp = new Vector2(-10f, 80f);

        [Header("Visuals (Trajectory)")]
        [Tooltip("LineRenderer used to display the throw trajectory arc.")]
        [SerializeField] private LineRenderer _trajectoryLine;

        [Tooltip("Number of points in the trajectory preview.")]
        [SerializeField] private int _trajectoryPoints = 25;

        [Tooltip("Time step between trajectory sample points (seconds).")]
        [SerializeField] private float _trajectoryTimeStep = 0.05f;

        [Header("Visuals (Pointer & Power)")]
        [Tooltip("Transform representing the aim pointer (e.g., an arrow).")]
        [SerializeField] private Transform _aimPointer;

        [Tooltip("UI or world-space transform for the power meter. Scales based on charge.")]
        [SerializeField] private Transform _powerMeterBar;

        [Tooltip("How far the aim pointer is from the throw origin.")]
        [SerializeField] private float _aimPointerOffset = 0.5f;

        // Private state
        private float _currentThrowForce;
        private bool _isCharging;
        private Vector3[] _trajectoryBuffer;
        private float _currentAimAngle;

        private void Awake()
        {
            _trajectoryBuffer = new Vector3[_trajectoryPoints];

            if (_throwOrigin == null)
                _throwOrigin = transform;

            if (_aimCamera == null)
                _aimCamera = Camera.main;

            if (_trajectoryLine != null)
            {
                _trajectoryLine.positionCount = 0;
                _trajectoryLine.enabled = false;
            }

            if (_aimPointer != null)
                _aimPointer.gameObject.SetActive(false);

            if (_powerMeterBar != null)
                _powerMeterBar.gameObject.SetActive(false);
        }

        private void Update()
        {
            HandleInput();
            
            if (_isCharging)
            {
                UpdateCharge();
                UpdateAim();
                UpdateVisuals();
            }
        }

        private void HandleInput()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartCharging();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame && _isCharging)
            {
                ExecuteThrow();
                StopCharging();
            }
        }

        private void StartCharging()
        {
            _isCharging = true;
            _currentThrowForce = _minThrowForce;

            if (_trajectoryLine != null) _trajectoryLine.enabled = true;
            if (_aimPointer != null) _aimPointer.gameObject.SetActive(true);
            if (_powerMeterBar != null) _powerMeterBar.gameObject.SetActive(true);
        }

        private void StopCharging()
        {
            _isCharging = false;
            
            if (_trajectoryLine != null)
            {
                _trajectoryLine.positionCount = 0;
                _trajectoryLine.enabled = false;
            }

            if (_aimPointer != null) _aimPointer.gameObject.SetActive(false);
            if (_powerMeterBar != null) _powerMeterBar.gameObject.SetActive(false);
        }

        private void UpdateCharge()
        {
            _currentThrowForce += _chargeSpeed * Time.deltaTime;
            _currentThrowForce = Mathf.Min(_currentThrowForce, _maxThrowForce);

            if (_powerMeterBar != null)
            {
                float t = Mathf.InverseLerp(_minThrowForce, _maxThrowForce, _currentThrowForce);
                // Simple scale-based power meter. Adjust as needed for specific UI.
                Vector3 scale = _powerMeterBar.localScale;
                scale.x = t; 
                _powerMeterBar.localScale = scale;
            }
        }

        private void UpdateAim()
        {
            float facingSign = Mathf.Sign(transform.localScale.x);
            Vector3 origin = _throwOrigin.position;
            origin.z = 0f;

            Vector3 mouseWorld = GetMouseWorldAtSameDepthAs(origin);
            Vector2 toMouse = (Vector2)(mouseWorld - origin);

            if (toMouse.sqrMagnitude > 0.0001f)
            {
                // Constrain aim to the side the player is facing
                toMouse.x = Mathf.Abs(toMouse.x) * facingSign;

                _currentAimAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
                _currentAimAngle = Mathf.Clamp(_currentAimAngle, _aimAngleClamp.x, _aimAngleClamp.y);
            }

            if (_aimPointer != null)
            {
                float angleRad = _currentAimAngle * Mathf.Deg2Rad;
                Vector3 offsetVector = new Vector3(
                    Mathf.Cos(angleRad) * _aimPointerOffset,
                    Mathf.Sin(angleRad) * _aimPointerOffset,
                    0f
                );

                _aimPointer.position = _throwOrigin.position + offsetVector;
                _aimPointer.rotation = Quaternion.Euler(0, 0, _currentAimAngle);
            }
        }

        private void UpdateVisuals()
        {
            UpdateTrajectoryPreview();
        }

        private void UpdateTrajectoryPreview()
        {
            if (_trajectoryLine == null) return;

            Vector3 origin = _throwOrigin.position;
            Vector3 velocity = CalculateThrowVelocity();
            Vector3 gravity = Physics.gravity;

            for (int i = 0; i < _trajectoryPoints; i++)
            {
                float t = i * _trajectoryTimeStep;
                _trajectoryBuffer[i] = origin + velocity * t + 0.5f * gravity * t * t;
                _trajectoryBuffer[i].z = 0f;
            }

            _trajectoryLine.positionCount = _trajectoryPoints;
            _trajectoryLine.SetPositions(_trajectoryBuffer);
        }

        private Vector3 CalculateThrowVelocity()
        {
            float angleRad = _currentAimAngle * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(angleRad) * _currentThrowForce,
                Mathf.Sin(angleRad) * _currentThrowForce,
                0f
            );
        }

        private void ExecuteThrow()
        {
            if (_throwPrefab == null) return;

            Vector3 spawnPosition = _throwOrigin.position;
            spawnPosition.z = 0f;

            GameObject thrown = Instantiate(_throwPrefab, spawnPosition, Quaternion.identity);
            Rigidbody rb = thrown.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
                Vector3 velocity = CalculateThrowVelocity();
                rb.AddForce(velocity, ForceMode.VelocityChange);
            }
        }

        private Vector3 GetMouseWorldAtSameDepthAs(Vector3 worldPoint)
        {
            Camera cam = _aimCamera != null ? _aimCamera : Camera.main;
            if (cam == null || Mouse.current == null) return worldPoint;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            float depth = cam.WorldToScreenPoint(worldPoint).z;
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, depth));
            w.z = 0f;
            return w;
        }
    }
}
