using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Helpers
{
    /// <summary>
    /// A minimal, physics-based character controller for 2.5D movement.
    /// Useful for testing the camera system without a complex character.
    /// Requirements: Rigidbody (Constraints: Freeze Rotation X/Y/Z, Freeze Position Z).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private float _groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask _groundLayer = 1; // Default to Default layer

        private Rigidbody _rb;
        private bool _isGrounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Auto-configure Rigidbody for 2.5D
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Update()
        {
            // Simple Ground Check
            _isGrounded = Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer);

            // Jump Input
            if (Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpForce, 0f); // Reset Y velocity for consistent jump height
            }
        }

        private void FixedUpdate()
        {
            // Horizontal Input
            float moveInput = 0f;
            if (Keyboard.current.aKey.isPressed) moveInput = -1f;
            if (Keyboard.current.dKey.isPressed) moveInput = 1f;

            // Apply Velocity (Linear logic for snappy control)
            Vector3 targetVelocity = new Vector3(moveInput * _moveSpeed, _rb.linearVelocity.y, 0f);
            
            // Smoothly interpolate for less jitter (optional, but good for camera)
            // For "snappy" platformers, direct assignment is often preferred, but let's do a slight acceleration
            // _rb.velocity = Vector3.MoveTowards(_rb.velocity, targetVelocity, 50f * Time.fixedDeltaTime);
            
            // Direct assignment for responsiveness
            _rb.linearVelocity = targetVelocity;

            // Rotation (Face direction)
            if (moveInput != 0)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                // If moving left (-1), we could rotate 180 on Y, or just flip model.
                // For a capsule, it doesn't matter much visually, but let's face the move direction?
                // Actually 2.5D often just rotates the mesh.
                // Let's just keep rotation locked for now as per constraints.
            }
        }
    }
}
