using UnityEngine;

namespace Player
{
    public class PlayerRender : MonoBehaviour
    {
        [SerializeField] private Transform meshTransform;
        
        //Crouch serialization
        [SerializeField] private float crouchScaleY = 0.5f;
        [SerializeField] private float crouchHeightOffset = 0.1f;

        private Vector3 _originalLocalPosition;
        private Vector3 _originalLocalScale;

        private void Awake()
        {
            if (meshTransform != null)
            {
                _originalLocalPosition = meshTransform.localPosition;
                _originalLocalScale = meshTransform.localScale;
            }
        }

        public void CrouchMesh()
        {
            if (meshTransform != null)
            {
                meshTransform.localScale = new Vector3(1f, crouchScaleY, 1f);
                meshTransform.localPosition += Vector3.up * crouchHeightOffset;
            }
        }

        public void UncrouchMesh()
        {
            if (meshTransform != null)
            {
                meshTransform.localPosition = _originalLocalPosition;
                meshTransform.localScale = _originalLocalScale;
            }
        }
    }
}