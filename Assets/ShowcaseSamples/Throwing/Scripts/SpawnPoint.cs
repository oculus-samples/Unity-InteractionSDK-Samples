// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(Grabbable))]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [Tooltip("The prefab that will be pooled.")]
        [SerializeField]
        private ThrowingPrefabDefinition _definition;
        [Tooltip("The delay in seconds before the replacement object appears.")]
        [SerializeField]
        private float _respawnDelay = 1f;
        [SerializeField] private int _swapIndex;

        private Vector3 _tablePosition;
        private Quaternion _tableRotation;
        private Grabbable _grabbable;
        private SwapTargets _swapTargets;

        private bool _isArmed = true;

        private void Awake()
        {
            TryGetComponent(out _grabbable);
            _swapTargets = FindObjectOfType<SwapTargets>();

            if (_swapTargets == null)
            {
                Debug.LogError("No SwapTargets component in scene!");
            }
        }

        private void Start()
        {
            _tablePosition = transform.position;
            _tableRotation = transform.rotation;
        }

        private void OnEnable()
        {
            _isArmed = true;
            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        private void OnDisable()
        {
            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            if (!_isArmed || evt.Type != PointerEventType.Select)
            {
                return;
            }
            _isArmed = false;
            _swapTargets.IsGrabbed(_swapIndex);
            Invoke(nameof(SpawnPooledObject), _respawnDelay);
        }

        private void SpawnPooledObject()
        {
            PooledThrowable.Get(_definition.prefab, _tablePosition, _tableRotation);
        }
    }
}
