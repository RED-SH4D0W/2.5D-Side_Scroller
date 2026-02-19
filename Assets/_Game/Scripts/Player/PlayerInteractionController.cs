using DScrollerGame.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DScrollerGame.Player
{
    /// <summary>
    /// Phase 4 — Player Interaction Controller.
    ///
    /// Handles all player-initiated world interactions:
    ///   F   → Interact (IInteractable)
    ///   E   → Push toggle (IInteractable on PushableObject)
    ///   RMB → Slash / Cut (ICuttable, environmental only)
    ///   LMB Hold    → Show throw trajectory preview
    ///   LMB Release → Throw projectile
    ///
    /// ARCHITECTURE RULES:
    /// ───────────────────
    /// • This script does NOT reference PlayerStealthState or PlayerPhysicalState directly.
    /// • All cross-system communication uses interfaces.
    /// • No per-frame heap allocations (LineRenderer buffer is pre-allocated).
    /// • Facing direction is derived from transform.localScale.x sign (2.5D convention).
    /// </summary>
    public class PlayerInteractionController : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR – Interaction
        // ================================================================

        [Header("Interaction")]
        [Tooltip("Maximum raycast distance for detecting interactables.")]
        [SerializeField] private float _interactRange = 2f;

        [Tooltip("Layer mask for interactable objects.")]
        [SerializeField] private LayerMask _interactableLayer = ~0;

        // ================================================================
        // INSPECTOR – Throw
        // ================================================================

        [Header("Throw")]
        [Tooltip("World-space origin point for thrown objects. Assign a child Transform.")]
        [SerializeField] private Transform _throwOrigin;

        [Tooltip("Prefab to instantiate when throwing. Must have Rigidbody + ThrowableObject.")]
        [SerializeField] private GameObject _throwPrefab;

        [Tooltip("Force magnitude applied to the thrown object (before multipliers).")]
        [SerializeField] private float _throwForce = 12f;

        [Tooltip("Vertical throw angle in degrees (0 = pure horizontal).")]
        [SerializeField] private float _throwAngle = 30f;

        [Tooltip("If true, aim toward mouse instead of using the fixed Throw Angle.")]
        [SerializeField] private bool _aimWithMouse = true;

        [Tooltip("Min/max aim angle (degrees) when aiming with mouse.")]
        [SerializeField] private Vector2 _mouseAimAngleClamp = new Vector2(-10f, 80f);

        [Tooltip("Camera used for aiming. If null, falls back to Camera.main.")]
        [SerializeField] private Camera _aimCamera;

        [Header("Throw Force Multiplier")]
        [Tooltip("If true, mouse distance from the throw origin scales the throw force (farther mouse = bigger force). Keep this OFF for gamepad/dpad-only setups.")]
        [SerializeField] private bool _useMouseForceMultiplier = true;

        [Tooltip("World-space mouse distance that maps to Max Force Multiplier (distances beyond this clamp).")]
        [SerializeField] private float _mouseForceMaxDistance = 6f;

        [Tooltip("Min/Max multiplier applied to _throwForce when using mouse distance scaling.")]
        [SerializeField] private Vector2 _mouseForceMultiplierClamp = new Vector2(0.35f, 1.5f);

        // ================================================================
        // INSPECTOR – Trajectory Preview
        // ================================================================

        [Header("Trajectory Preview")]
        [Tooltip("LineRenderer used to display the throw trajectory arc.")]
        [SerializeField] private LineRenderer _trajectoryLine;

        [Tooltip("Number of points in the trajectory preview.")]
        [SerializeField] private int _trajectoryPoints = 25;

        [Tooltip("Time step between trajectory sample points (seconds).")]
        [SerializeField] private float _trajectoryTimeStep = 0.05f;

        // ================================================================
        // INSPECTOR – Slash
        // ================================================================

        [Header("Slash")]
        [Tooltip("Fixed normalized noise emitted when slashing an object (0–1).")]
        [SerializeField] private float _slashNoise = 0.3f;

        // ================================================================
        // INSPECTOR – Push
        // ================================================================

        [Header("Push")]
        [Tooltip("Maximum distance to maintain push engagement before auto-disengage.")]
        [SerializeField] private float _pushDisengageRange = 3f;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        /// <summary>Currently engaged pushable object (null if not pushing).</summary>
        private IInteractable _activePushTarget;

        /// <summary>Transform of the currently pushed object (for range checks).</summary>
        private Transform _activePushTransform;

        /// <summary>Pre-allocated buffer for trajectory preview positions (no GC).</summary>
        private Vector3[] _trajectoryBuffer;

        /// <summary>True while LMB is held and trajectory is being previewed.</summary>
        private bool _isPreviewing;

        // ================================================================
        // PUBLIC — Push State
        // ================================================================

        public bool IsPushing => _activePushTarget != null;

        private void Awake()
        {
            // Pre-allocate trajectory buffer — never re-created at runtime.
            _trajectoryBuffer = new Vector3[_trajectoryPoints];

            // Default throw origin to this transform if unassigned.
            if (_throwOrigin == null)
                _throwOrigin = transform;

            // Ensure trajectory line starts hidden.
            if (_trajectoryLine != null)
            {
                _trajectoryLine.positionCount = 0;
                _trajectoryLine.enabled = false;
            }

            if (_aimCamera == null)
                _aimCamera = Camera.main;
        }

        private void Update()
        {
            HandleInteract();
            HandlePushToggle();
            HandleSlash();
            HandleThrow();
            ValidatePushState();
        }

        // ================================================================
        // F — INTERACT
        // ================================================================

        /// <summary>
        /// Raycasts forward and calls Interact on the first IInteractable hit.
        /// </summary>
        private void HandleInteract()
        {
            if (!Keyboard.current.fKey.wasPressedThisFrame) return;

            if (TryRaycastInteractable(out RaycastHit hit))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                    interactable.Interact(this);
            }
        }

        // ================================================================
        // E — PUSH TOGGLE
        // ================================================================

        /// <summary>
        /// Toggles push mode on the nearest IInteractable that is also pushable.
        /// Push mode automatically disengages if:
        ///   • Player moves out of range
        ///   • Rigidbody becomes null
        ///   • E is pressed again
        /// </summary>
        private void HandlePushToggle()
        {
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            // If already pushing — disengage.
            if (_activePushTarget != null)
            {
                DisengagePush();
                return;
            }

            // Try to find a pushable object.
            if (TryRaycastInteractable(out RaycastHit hit))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    _activePushTarget = interactable;
                    _activePushTransform = hit.collider.transform;
                    interactable.Interact(this);
                }
            }
        }

        /// <summary>Disengages push mode safely.</summary>
        private void DisengagePush()
        {
            _activePushTarget = null;
            _activePushTransform = null;
        }

        /// <summary>
        /// Validates push engagement every frame.
        /// Disengages if the push target is missing, destroyed, or out of range.
        /// </summary>
        private void ValidatePushState()
        {
            if (_activePushTarget == null) return;

            // Rigidbody or object destroyed.
            if (_activePushTransform == null)
            {
                DisengagePush();
                return;
            }

            // Out of range — auto-disengage.
            float distance = Vector3.Distance(transform.position, _activePushTransform.position);

            if (distance > _pushDisengageRange)
                DisengagePush();
        }

        // ================================================================
        // RMB — SLASH (Environmental Only)
        // ================================================================

        /// <summary>
        /// Raycasts forward and calls Cut on the first ICuttable hit.
        /// If the hit object also implements INoiseEmitter, a small fixed noise is emitted.
        /// Slash must NOT break lights or apply combat damage.
        /// </summary>
        private void HandleSlash()
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;

            if (TryRaycastInteractable(out RaycastHit hit))
            {
                // Environmental cut.
                ICuttable cuttable = hit.collider.GetComponent<ICuttable>();

                if (cuttable != null)
                    cuttable.Cut();

                // Emit a small noise at the slash point (if the object supports it).
                INoiseEmitter noiseEmitter = hit.collider.GetComponent<INoiseEmitter>();

                if (noiseEmitter != null)
                    noiseEmitter.EmitNoise(_slashNoise, hit.point);
            }
        }

        // ================================================================
        // LMB — THROW (Hold = Preview, Release = Launch)
        // ================================================================

        /// <summary>
        /// Hold LMB to show a parabolic trajectory preview.
        /// Release LMB to instantiate and launch the throw prefab.
        /// </summary>
        private void HandleThrow()
        {
            // --- Begin preview ---
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isPreviewing = true;

                if (_trajectoryLine != null)
                    _trajectoryLine.enabled = true;
            }

            // --- Update preview while held ---
            if (_isPreviewing && Mouse.current.leftButton.isPressed)
            {
                UpdateTrajectoryPreview();
            }

            // --- Release: throw and clear preview ---
            if (Mouse.current.leftButton.wasReleasedThisFrame && _isPreviewing)
            {
                _isPreviewing = false;
                ClearTrajectoryPreview();
                ExecuteThrow();
            }
        }

        // ================================================================
        // THROW MATH
        // ================================================================

        private float CalculateThrowForceMultiplier(Vector3 origin)
        {
            if (!_useMouseForceMultiplier) return 1f;

            Camera cam = _aimCamera != null ? _aimCamera : Camera.main;
            if (cam == null || Mouse.current == null) return 1f;

            Vector3 mouseWorld = GetMouseWorldAtSameDepthAs(origin);
            Vector2 toMouse = (Vector2)(mouseWorld - origin);

            float maxDist = Mathf.Max(0.0001f, _mouseForceMaxDistance);
            float t = Mathf.Clamp01(toMouse.magnitude / maxDist);

            float minMul = _mouseForceMultiplierClamp.x;
            float maxMul = _mouseForceMultiplierClamp.y;

            // If someone sets them backwards in the inspector, keep it sane.
            if (maxMul < minMul)
            {
                float tmp = minMul;
                minMul = maxMul;
                maxMul = tmp;
            }

            return Mathf.Lerp(minMul, maxMul, t);
        }

        /// <summary>
        /// Computes the throw velocity vector.
        /// If Aim With Mouse is enabled, the aim angle is derived from mouse position.
        /// Otherwise, uses facing direction and fixed throw angle.
        ///
        /// Arc distance is reduced/increased by scaling force with a multiplier (mouse distance when enabled).
        /// </summary>
        private Vector3 CalculateThrowVelocity()
        {
            float facingSign = Mathf.Sign(transform.localScale.x);

            Vector3 origin = _throwOrigin != null ? _throwOrigin.position : transform.position;
            origin.z = 0f;

            float angleDeg = _throwAngle;

            if (_aimWithMouse)
            {
                Vector3 mouseWorld = GetMouseWorldAtSameDepthAs(origin);

                Vector2 toMouse = (Vector2)(mouseWorld - origin);
                if (toMouse.sqrMagnitude > 0.0001f)
                {
                    // Keep aim in front of the character (2.5D typical behavior).
                    toMouse.x = Mathf.Abs(toMouse.x) * facingSign;

                    angleDeg = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
                    angleDeg = Mathf.Clamp(angleDeg, _mouseAimAngleClamp.x, _mouseAimAngleClamp.y);
                }
            }

            float forceMultiplier = CalculateThrowForceMultiplier(origin);
            float effectiveForce = _throwForce * forceMultiplier;

            float angleRad = angleDeg * Mathf.Deg2Rad;

            return new Vector3(
                Mathf.Cos(angleRad) * effectiveForce,
                Mathf.Sin(angleRad) * effectiveForce,
                0f
            );
        }

        private Vector3 GetMouseWorldAtSameDepthAs(Vector3 worldPoint)
        {
            Camera cam = _aimCamera != null ? _aimCamera : Camera.main;
            if (cam == null || Mouse.current == null)
                return worldPoint;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();

            // Critical: use the same camera-space depth as the origin
            float depth = cam.WorldToScreenPoint(worldPoint).z;

            Vector3 w = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, depth));
            w.z = 0f;
            return w;
        }

        /// <summary>
        /// Calculates trajectory preview using the projectile formula:
        /// P = origin + velocity*t + 0.5*gravity*t²
        /// Uses the pre-allocated buffer — zero per-frame allocations.
        /// </summary>
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

                // Enforce 2.5D — zero out Z.
                _trajectoryBuffer[i].z = 0f;
            }

            _trajectoryLine.positionCount = _trajectoryPoints;
            _trajectoryLine.SetPositions(_trajectoryBuffer);
        }

        /// <summary>Hides the trajectory preview line.</summary>
        private void ClearTrajectoryPreview()
        {
            if (_trajectoryLine == null) return;

            _trajectoryLine.positionCount = 0;
            _trajectoryLine.enabled = false;
        }

        /// <summary>
        /// Instantiates the throw prefab and applies force via Rigidbody.
        /// The spawned object must have a Rigidbody with Z position and Z rotation frozen
        /// to maintain strict 2.5D constraints.
        /// </summary>
        private void ExecuteThrow()
        {
            if (_throwPrefab == null) return;

            Vector3 spawnPosition = _throwOrigin.position;
            spawnPosition.z = 0f; // Enforce 2.5D spawn position.

            GameObject thrown = Instantiate(_throwPrefab, spawnPosition, Quaternion.identity);

            Rigidbody rb = thrown.GetComponent<Rigidbody>();

            if (rb != null)
            {
                // Enforce 2.5D physics constraints.
                rb.constraints = RigidbodyConstraints.FreezePositionZ
                               | RigidbodyConstraints.FreezeRotationZ;

                Vector3 velocity = CalculateThrowVelocity();
                rb.AddForce(velocity, ForceMode.VelocityChange);
            }
        }

        // ================================================================
        // SHARED RAYCAST
        // ================================================================

        /// <summary>
        /// Raycasts along the X axis in the player's facing direction.
        /// Reused by Interact, Push, Slash, and any future interaction types.
        /// </summary>
        private bool TryRaycastInteractable(out RaycastHit hit)
        {
            float facingSign = Mathf.Sign(transform.localScale.x);
            Vector3 direction = new Vector3(facingSign, 0f, 0f);

            return Physics.Raycast(
                transform.position,
                direction,
                out hit,
                _interactRange,
                _interactableLayer,
                QueryTriggerInteraction.Ignore
            );
        }

        // ================================================================
        // EDITOR GIZMOS
        // ================================================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float facingSign = Mathf.Sign(transform.localScale.x);
            Vector3 direction = new Vector3(facingSign, 0f, 0f);
            Vector3 endpoint = transform.position + direction * _interactRange;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, endpoint);
            Gizmos.DrawWireSphere(endpoint, 0.1f);
        }
#endif
    }
}