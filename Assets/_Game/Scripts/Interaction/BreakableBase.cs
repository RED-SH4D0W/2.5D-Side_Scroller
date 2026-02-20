using DScrollerGame.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace DScrollerGame.Interaction
{
    /// <summary>
    /// Base class for objects that can be damaged and broken.
    /// </summary>
    public abstract class BreakableBase : MonoBehaviour, IBreakable, INoiseEmitter
    {
        [Header("Breakable Settings")]
        [SerializeField] protected float _maxDamage = 10f;
        [SerializeField] protected float _breakNoise = 0.6f;

        [Header("Events")]
        [SerializeField] protected UnityEvent<float, Vector3> _onDamaged;
        [SerializeField] protected UnityEvent<Vector3> _onBroken;

        [Header("Breakable Runtime")]
        [SerializeField] protected float _currentDamage;
        [SerializeField] protected bool _isBroken;

        public float CurrentDamage => _currentDamage;
        public float MaxDamage => _maxDamage;
        public bool IsBroken => _isBroken;

        public virtual void ApplyDamage(float damage, Vector3 impactPoint)
        {
            if (_isBroken) return;

            _currentDamage += damage;
            _onDamaged?.Invoke(damage, impactPoint);
            
            if (_currentDamage >= _maxDamage)
            {
                Break(impactPoint);
            }
        }

        public virtual void Break(Vector3 impactPoint)
        {
            if (_isBroken) return;
            
            _isBroken = true;
            _currentDamage = _maxDamage;
            
            _onBroken?.Invoke(impactPoint);
            OnBreak(impactPoint);
            EmitNoise(_breakNoise, impactPoint);
        }

        protected abstract void OnBreak(Vector3 impactPoint);

        public virtual void EmitNoise(float normalizedAmount, Vector3 position)
        {
            // FUTURE: NoiseManager.Instance.RegisterNoise(normalizedAmount, position);
            Debug.Log(
                $"[{gameObject.name} Noise] intensity={normalizedAmount:F2} " +
                $"position={position} broken={_isBroken}"
            );
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _maxDamage = Mathf.Max(0.1f, _maxDamage);
            _breakNoise = Mathf.Clamp01(_breakNoise);
        }
#endif
    }
}
