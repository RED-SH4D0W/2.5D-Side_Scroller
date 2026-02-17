using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CameraSystem
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _virtualCamera;

        private CinemachinePositionComposer _positionComposer;

        private float _defaultFOV;
        private Vector3 _defaultDamping;
        private Vector3 _defaultOffset;
        private float _defaultLookahead;
        private float _defaultDeadZoneWidth;
        private float _defaultDeadZoneHeight;

        private List<CameraZone> _activeZones = new();

        private Coroutine _activeCoroutine;

        private void Awake()
        {
            if (_virtualCamera == null)
                _virtualCamera = GetComponent<CinemachineCamera>();

            _positionComposer = GetComponent<CinemachinePositionComposer>();

            _defaultFOV = _virtualCamera.Lens.FieldOfView;
            _defaultDamping = _positionComposer.Damping;
            _defaultOffset = _positionComposer.TargetOffset;
            _defaultLookahead = _positionComposer.Lookahead.Time;
            _defaultDeadZoneWidth = _positionComposer.Composition.DeadZone.Size.x;
            _defaultDeadZoneHeight = _positionComposer.Composition.DeadZone.Size.y;
        }

        // --------------------------------------------------------
        // Zone Registration
        // --------------------------------------------------------

        public void RegisterZone(CameraZone zone)
        {
            if (!_activeZones.Contains(zone))
            {
                _activeZones.Add(zone);
                ApplyHighestPriorityZone();
            }
        }

        public void UnregisterZone(CameraZone zone)
        {
            if (_activeZones.Contains(zone))
            {
                _activeZones.Remove(zone);
                ApplyHighestPriorityZone();
            }
        }

        private void ApplyHighestPriorityZone()
        {
            CameraZone topZone = _activeZones
                .OrderByDescending(z => z.Priority)
                .FirstOrDefault();

            if (topZone == null)
            {
                StartTransitionToDefaults(0.5f);
            }
            else
            {
                StartTransitionToZone(topZone);
            }
        }

        // --------------------------------------------------------
        // Transitions
        // --------------------------------------------------------

        private void StartTransitionToZone(CameraZone zone)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(
                LerpCameraSettings(
                    zone.OverrideFOV ? zone.TargetFOV : _defaultFOV,
                    zone.OverrideXDamping ? zone.TargetXDamping : _defaultDamping.x,
                    zone.OverrideYDamping ? zone.TargetYDamping : _defaultDamping.y,
                    zone.OverrideOffset ? zone.TargetOffset : _defaultOffset,
                    zone.OverrideLookahead ? zone.TargetLookahead : _defaultLookahead,
                    zone.OverrideDeadZoneWidth ? zone.TargetDeadZoneWidth : _defaultDeadZoneWidth,
                    zone.OverrideDeadZoneHeight ? zone.TargetDeadZoneHeight : _defaultDeadZoneHeight,
                    zone.TransitionTime
                )
            );
        }

        private void StartTransitionToDefaults(float duration)
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);

            _activeCoroutine = StartCoroutine(
                LerpCameraSettings(
                    _defaultFOV,
                    _defaultDamping.x,
                    _defaultDamping.y,
                    _defaultOffset,
                    _defaultLookahead,
                    _defaultDeadZoneWidth,
                    _defaultDeadZoneHeight,
                    duration
                )
            );
        }

        private IEnumerator LerpCameraSettings(
            float targetFOV,
            float targetXDamp,
            float targetYDamp,
            Vector3 targetOffset,
            float targetLookahead,
            float targetDeadZoneWidth,
            float targetDeadZoneHeight,
            float duration)
        {
            float startFOV = _virtualCamera.Lens.FieldOfView;
            Vector3 startDamp = _positionComposer.Damping;
            Vector3 startOffset = _positionComposer.TargetOffset;
            float startLookahead = _positionComposer.Lookahead.Time;
            var startComp = _positionComposer.Composition;

            float startDZWidth = startComp.DeadZone.Size.x;
            float startDZHeight = startComp.DeadZone.Size.y;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t);

                _virtualCamera.Lens.FieldOfView =
                    Mathf.Lerp(startFOV, targetFOV, t);

                _positionComposer.Damping =
                    Vector3.Lerp(startDamp, new Vector3(targetXDamp, targetYDamp, startDamp.z), t);

                _positionComposer.TargetOffset =
                    Vector3.Lerp(startOffset, targetOffset, t);

                _positionComposer.Lookahead.Time =
                    Mathf.Lerp(startLookahead, targetLookahead, t);

                var comp = _positionComposer.Composition;
                comp.DeadZone.Size.x =
                    Mathf.Lerp(startDZWidth, targetDeadZoneWidth, t);
                comp.DeadZone.Size.y =
                    Mathf.Lerp(startDZHeight, targetDeadZoneHeight, t);
                _positionComposer.Composition = comp;

                yield return null;
            }

            _activeCoroutine = null;
        }
    }
}