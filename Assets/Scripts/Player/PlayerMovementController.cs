using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        // ================================================================
        // INSPECTOR – Movement
        // ================================================================

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintMultiplier = 1.6f;
        [SerializeField] private float _crouchSpeedMultiplier = 0.45f;
        [SerializeField] private float _acceleration = 35f;
        [SerializeField] private float _deceleration = 25f;

        // ================================================================
        // INSPECTOR – Jump & Gravity
        // ================================================================

        [Header("Jump & Gravity")]
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _fallMultiplier = 1.5f;
        [SerializeField] private float _maxFallSpeed = -50f;
        [SerializeField] private float _coyoteTime = 0.12f;
        [SerializeField] private float _jumpBufferTime = 0.1f;

        // ================================================================
        // INSPECTOR – Crouch
        // ================================================================

        [Header("Crouch")]
        [SerializeField] private float _standingHeight = 2f;
        [SerializeField] private float _crouchHeight = 1.2f;
        [SerializeField] private LayerMask _ceilingMask = ~0;

        // ================================================================
        // PUBLIC READ-ONLY STATE
        // ================================================================

        public bool IsGrounded { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float VerticalVelocity => _verticalVelocity;

        // ================================================================
        // PRIVATE STATE
        // ================================================================

        private CharacterController _cc;
        private float _horizontalVelocity;
        private float _verticalVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isHoldingJump;
        private bool _jumpCutApplied;
        private float _currentHeight;
        private bool _wantsToCrouch;
        private float _inputX;
        private bool _inputSprint;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _standingHeight = _cc.height;
            _currentHeight = _standingHeight;
        }

        private void Update()
        {
            ReadInput();
            UpdateGrounding();
            UpdateCrouch();
            UpdateHorizontalMovement();
            UpdateVerticalMovement();
            ApplyMotion();
            LockZPosition();
            UpdatePublicState();
        }

        // ================================================================
        // INPUT
        // ================================================================

        private void ReadInput()
        {
            _inputX = 0f;
            if (Keyboard.current.aKey.isPressed) _inputX -= 1f;
            if (Keyboard.current.dKey.isPressed) _inputX += 1f;

            _inputSprint = Keyboard.current.leftShiftKey.isPressed &&
                           !IsCrouching &&
                           IsGrounded;

            _wantsToCrouch = Keyboard.current.leftCtrlKey.isPressed;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                _jumpBufferTimer = _jumpBufferTime;

            _isHoldingJump = Keyboard.current.spaceKey.isPressed;
        }

        // ================================================================
        // GROUNDING
        // ================================================================

        private void UpdateGrounding()
        {
            IsGrounded = _cc.isGrounded;

            if (IsGrounded)
            {
                _coyoteTimer = _coyoteTime;

                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f; // stick to ground
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }
        }

        // ================================================================
        // CROUCH
        // ================================================================

        private void UpdateCrouch()
        {
            if (_wantsToCrouch)
            {
                SetHeight(_crouchHeight);
                IsCrouching = true;
            }
            else if (IsCrouching)
            {
                if (!IsCeilingBlocked())
                {
                    SetHeight(_standingHeight);
                    IsCrouching = false;
                }
            }
        }

        private void SetHeight(float targetHeight)
        {
            if (Mathf.Approximately(_currentHeight, targetHeight))
                return;

            float previousHeight = _currentHeight;
            float delta = targetHeight - previousHeight;

            _cc.height = targetHeight;
            _cc.center = new Vector3(0f, targetHeight * 0.5f, 0f);

            // Keep feet planted visually
            transform.position += Vector3.up * (delta * 0.5f);

            _currentHeight = targetHeight;
        }

        private bool IsCeilingBlocked()
        {
            float checkDistance = _standingHeight - _crouchHeight;
            Vector3 origin = transform.position + Vector3.up * _crouchHeight;

            return Physics.Raycast(
                origin,
                Vector3.up,
                checkDistance,
                _ceilingMask,
                QueryTriggerInteraction.Ignore
            );
        }

        // ================================================================
        // HORIZONTAL MOVEMENT
        // ================================================================

        private void UpdateHorizontalMovement()
        {
            float maxSpeed = _walkSpeed;

            if (_inputSprint)
                maxSpeed *= _sprintMultiplier;

            if (IsCrouching)
                maxSpeed *= _crouchSpeedMultiplier;

            float targetSpeed = _inputX * maxSpeed;

            float rate = Mathf.Abs(targetSpeed) > 0.01f
                ? _acceleration
                : _deceleration;

            _horizontalVelocity = Mathf.MoveTowards(
                _horizontalVelocity,
                targetSpeed,
                rate * Time.deltaTime
            );
        }

        // ================================================================
        // VERTICAL MOVEMENT
        // ================================================================

        private void UpdateVerticalMovement()
        {
            _jumpBufferTimer -= Time.deltaTime;

            bool canJump = _coyoteTimer > 0f;

            // Jump start
            if (_jumpBufferTimer > 0f && canJump)
            {
                _verticalVelocity = _jumpForce;
                _coyoteTimer = 0f;
                _jumpBufferTimer = 0f;
                _jumpCutApplied = false;
            }

            // Variable jump cut
            if (!_isHoldingJump && _verticalVelocity > 0f && !_jumpCutApplied)
            {
                _verticalVelocity *= 0.5f;
                _jumpCutApplied = true;
            }

            float gravityThisFrame = _gravity;

            if (_verticalVelocity < 0f)
                gravityThisFrame *= _fallMultiplier;

            _verticalVelocity += gravityThisFrame * Time.deltaTime;

            // Clamp fall speed
            _verticalVelocity = Mathf.Max(_verticalVelocity, _maxFallSpeed);
        }

        // ================================================================
        // APPLY MOTION
        // ================================================================

        private void ApplyMotion()
        {
            Vector3 move = new Vector3(_horizontalVelocity, _verticalVelocity, 0f);
            _cc.Move(move * Time.deltaTime);
        }

        // ================================================================
        // Z LOCK
        // ================================================================

        private void LockZPosition()
        {
            Vector3 pos = transform.position;

            if (pos.z != 0f)
            {
                pos.z = 0f;
                transform.position = pos;
            }
        }

        // ================================================================
        // STATE BOOKKEEPING
        // ================================================================

        private void UpdatePublicState()
        {
            CurrentSpeed = Mathf.Abs(_horizontalVelocity);
            IsMoving = CurrentSpeed > 0.01f;
            IsSprinting = _inputSprint && IsMoving;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _crouchHeight = Mathf.Clamp(
                _crouchHeight,
                0.5f,
                _standingHeight - 0.1f
            );
        }
#endif
    }
}