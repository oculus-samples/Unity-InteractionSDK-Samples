// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{

    public class BowlingExtendedVelocity : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _maxAngularVelocity = 10f;
        [SerializeField] private float _maxLinearVelocity = 9f;
        [SerializeField] private float _dampingFactor = 0.99f;
        [SerializeField] private float _velocityWeight = 0.01f;
        [SerializeField] private float _floorImpactBoost = 1.5f;

        private bool _isAdjusting;

        private Vector3 _smoothedVelocity;

        private void Start()
        {
            InitializeRigidbody();
            _smoothedVelocity = _rigidbody.velocity;
        }

        private void OnEnable()
        {
            _isAdjusting = false;
            _smoothedVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (_isAdjusting)
            {
                ApplyVelocityDamping();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            switch (collision.gameObject.tag)
            {
                case "Floor":
                    HandleFloorCollision();
                    break;
                case "Backboard":
                    break;
                default:
                    _isAdjusting = false;
                    break;
            }
        }

        private void InitializeRigidbody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_rigidbody != null)
            {
                _rigidbody.maxAngularVelocity = _maxAngularVelocity;
                _rigidbody.maxLinearVelocity = _maxLinearVelocity;
            }
        }

        private void ApplyVelocityDamping()
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
            {
                return;
            }

            Vector3 oldVelocity = _rigidbody.velocity;
            Vector3 newVelocity = oldVelocity * _velocityWeight + _smoothedVelocity * _dampingFactor;

            // Sanity check for NaN/Infinity
            if (float.IsNaN(newVelocity.x) || float.IsNaN(newVelocity.y) || float.IsNaN(newVelocity.z) ||
                float.IsInfinity(newVelocity.x) || float.IsInfinity(newVelocity.y) || float.IsInfinity(newVelocity.z))
            {
                newVelocity = Vector3.zero;
                _smoothedVelocity = Vector3.zero;
                _isAdjusting = false;
                return;
            }

            _rigidbody.velocity = newVelocity;
            _smoothedVelocity = newVelocity;
        }

        private void HandleFloorCollision()
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
            {
                return;
            }

            _smoothedVelocity = _rigidbody.velocity;
            _smoothedVelocity.z *= _floorImpactBoost;

            // Sanity check
            if (float.IsNaN(_smoothedVelocity.x) || float.IsNaN(_smoothedVelocity.y) || float.IsNaN(_smoothedVelocity.z))
            {
                _smoothedVelocity = Vector3.zero;
                return;
            }

            _rigidbody.velocity = _smoothedVelocity;
            _isAdjusting = true;
        }

    }
}
