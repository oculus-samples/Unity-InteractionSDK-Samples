// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that can translate, rotate and scale a transform using any
    /// number of GrabPoints while also constraining the transformation if desired.
    /// </summary>
    public class HeavyFreeTransformer : MonoBehaviour, ITransformer
    {
        public float Delay = 0.001f;
        public bool isHeavy = true;

        public float velocity;
        [SerializeField]
        [Tooltip("Constrains the position of the object along different axes. Units are meters.")]
        private TransformerUtils.PositionConstraints _positionConstraints =
            new TransformerUtils.PositionConstraints()
            {
                XAxis = new TransformerUtils.ConstrainedAxis(),
                YAxis = new TransformerUtils.ConstrainedAxis(),
                ZAxis = new TransformerUtils.ConstrainedAxis()
            };

        [SerializeField]
        [Tooltip("Constrains the rotation of the object along different axes. Units are degrees.")]
        private TransformerUtils.RotationConstraints _rotationConstraints =
            new TransformerUtils.RotationConstraints()
            {
                XAxis = new TransformerUtils.ConstrainedAxis(),
                YAxis = new TransformerUtils.ConstrainedAxis(),
                ZAxis = new TransformerUtils.ConstrainedAxis()
            };

        [SerializeField]
        [Tooltip("Constrains the local scale of the object along different axes. Expressed as a scale factor.")]
        private TransformerUtils.ScaleConstraints _scaleConstraints =
            new TransformerUtils.ScaleConstraints()
            {
                ConstraintsAreRelative = true,
                XAxis = new TransformerUtils.ConstrainedAxis()
                {
                    ConstrainAxis = true,
                    AxisRange = new TransformerUtils.FloatRange() { Min = 1, Max = 1 }
                },
                YAxis = new TransformerUtils.ConstrainedAxis()
                {
                    ConstrainAxis = true,
                    AxisRange = new TransformerUtils.FloatRange() { Min = 1, Max = 1 }
                },
                ZAxis = new TransformerUtils.ConstrainedAxis()
                {
                    ConstrainAxis = true,
                    AxisRange = new TransformerUtils.FloatRange() { Min = 1, Max = 1 }
                },
            };

        private IGrabbable _grabbable;
        private Pose _grabDeltaInLocalSpace;
        private TransformerUtils.PositionConstraints _relativePositionConstraints;
        private TransformerUtils.ScaleConstraints _relativeScaleConstraints;

        private Quaternion _lastRotation = Quaternion.identity;
        private Vector3 _lastScale = Vector3.one;

        private GrabPointDelta[] _deltas;
        private Vector3 prevPosition;
        private struct GrabPointDelta
        {
            private const float _epsilon = 0.000001f;

            public Vector3 PrevCentroidOffset { get; private set; }
            public Vector3 CentroidOffset { get; private set; }

            public Quaternion PrevRotation { get; private set; }
            public Quaternion Rotation { get; private set; }

            public GrabPointDelta(Vector3 centroidOffset, Quaternion rotation)
            {
                this.PrevCentroidOffset = this.CentroidOffset = centroidOffset;
                this.PrevRotation = this.Rotation = rotation;
            }

            public void UpdateData(Vector3 centroidOffset, Quaternion rotation)
            {
                this.PrevCentroidOffset = this.CentroidOffset;
                this.CentroidOffset = centroidOffset;

                this.PrevRotation = this.Rotation;

                //Quaternions have two ways of expressing the same rotation.
                //This code ensures that the result is the same rotation but expressed in the desired sign.
                if (Quaternion.Dot(rotation, this.Rotation) < 0)
                {
                    rotation.x = -rotation.x;
                    rotation.y = -rotation.y;
                    rotation.z = -rotation.z;
                    rotation.w = -rotation.w;
                }

                this.Rotation = rotation;
            }

            public bool IsValidAxis()
            {
                return CentroidOffset.sqrMagnitude > _epsilon;
            }
        }

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
            _relativePositionConstraints = TransformerUtils.GenerateParentConstraints(_positionConstraints, _grabbable.Transform.localPosition);
            _relativeScaleConstraints = TransformerUtils.GenerateParentConstraints(_scaleConstraints, _grabbable.Transform.localScale);
        }

        public void BeginTransform()
        {
            int count = _grabbable.GrabPoints.Count;
            Vector3 centroid = GetCentroid(_grabbable.GrabPoints);

            //rent space only while using
            _deltas = ArrayPool<GrabPointDelta>.Shared.Rent(count);

            for (int i = 0; i < count; i++)
            {
                Vector3 centroidOffset = GetCentroidOffset(_grabbable.GrabPoints[i], centroid);
                _deltas[i] = new GrabPointDelta(centroidOffset, _grabbable.GrabPoints[i].rotation);
            }

            Transform targetTransform = _grabbable.Transform;

            _grabDeltaInLocalSpace = new Pose(
                targetTransform.InverseTransformVector(centroid - targetTransform.position),
                targetTransform.rotation);
            _lastRotation = Quaternion.identity;
            _lastScale = targetTransform.localScale;

            prevPosition = UpdateTransformerPointData(_grabbable.GrabPoints);
        }

        public void UpdateTransform()
        {
            int count = _grabbable.GrabPoints.Count;
            Transform targetTransform = _grabbable.Transform;

            Vector3 localPosition = UpdateTransformerPointData(_grabbable.GrabPoints);

            _lastScale = count <= 1 ? targetTransform.localScale : UpdateScale(count) * _lastScale;
            targetTransform.localScale = TransformerUtils.GetConstrainedTransformScale(_lastScale, _relativeScaleConstraints);


            _lastRotation = UpdateRotation(count) * _lastRotation;
            Quaternion rotation = _lastRotation * _grabDeltaInLocalSpace.rotation;
            targetTransform.rotation = TransformerUtils.GetConstrainedTransformRotation(rotation, _rotationConstraints, targetTransform.parent);


            //Vector3 position = localPosition - targetTransform.TransformVector(_grabDeltaInLocalSpace.position);
            //targetTransform.position = TransformerUtils.GetConstrainedTransformPosition(position, _relativePositionConstraints, targetTransform.parent);


            Vector3 dir = localPosition - prevPosition;
            //float distance = dir.magnitude;
            //print(distance);



            if (isHeavy)
            {
                velocity = velocity + dir.y * 0.02f;

                float otherMult = (Delay + velocity * 0.05f) * 10f;
                if (otherMult > 1) otherMult = 1;

                if (dir.y > 0)
                {
                    //velocity = velocity * 0.99f + dir.y * 0.01f;

                    dir.y = dir.y * (Delay + velocity * 0.05f) * 1f;
                }
                else
                {

                    //dir.y = dir.y * otherMult
                    velocity = 0;
                }

                dir.x = dir.x * otherMult;
                dir.z = dir.z * otherMult;
            }
            else
            {
                //velocity = velocity * 0.99f + dir.magnitude * 0.01f;
                velocity = velocity + dir.y * 0.01f;
                dir = dir * (Delay + velocity * 0.05f) * 0.5f;
            }
            localPosition = prevPosition + dir;
            Vector3 position = localPosition - targetTransform.TransformVector(_grabDeltaInLocalSpace.position);
            prevPosition = localPosition;
            targetTransform.position = TransformerUtils.GetConstrainedTransformPosition(position, _relativePositionConstraints, targetTransform.parent);

        }

        public void EndTransform()
        {
            //return the uneeded space
            ArrayPool<GrabPointDelta>.Shared.Return(_deltas);
            _deltas = null;
            velocity = 0;
        }

        private Vector3 UpdateTransformerPointData(List<Pose> poses)
        {
            Vector3 centroid = GetCentroid(poses);
            for (int i = 0; i < poses.Count; i++)
            {
                Vector3 centroidOffset = GetCentroidOffset(poses[i], centroid);
                _deltas[i].UpdateData(centroidOffset, poses[i].rotation);
            }
            return centroid;
        }

        private Vector3 GetCentroid(List<Pose> poses)
        {
            int count = poses.Count;
            Vector3 sumPosition = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Pose pose = poses[i];
                sumPosition += pose.position;
            }

            return sumPosition / count;
        }

        private Vector3 GetCentroidOffset(Pose pose, Vector3 centre)
        {
            Vector3 centroidOffset = centre - pose.position;
            return centroidOffset;
        }

        private Quaternion UpdateRotation(int count)
        {
            Quaternion combinedRotation = Quaternion.identity;

            //each point can only affect a fraction of the rotation
            float fraction = 1f / count;
            for (int i = 0; i < count; i++)
            {
                GrabPointDelta data = _deltas[i];

                //overall delta rotation since last update
                Quaternion rotDelta = data.Rotation * Quaternion.Inverse(data.PrevRotation);

                if (data.IsValidAxis())
                {
                    Vector3 aimingAxis = data.CentroidOffset.normalized;
                    //rotation along aiming axis
                    Quaternion dirDelta = Quaternion.FromToRotation(data.PrevCentroidOffset.normalized, aimingAxis);
                    combinedRotation = Quaternion.Slerp(Quaternion.identity, dirDelta, fraction) * combinedRotation;

                    //twist along the aiming axis
                    rotDelta.ToAngleAxis(out float angle, out Vector3 axis);
                    float projectionFactor = Vector3.Dot(axis, aimingAxis);
                    rotDelta = Quaternion.AngleAxis(angle * projectionFactor, aimingAxis);
                }

                combinedRotation = Quaternion.Slerp(Quaternion.identity, rotDelta, fraction) * combinedRotation;
            }

            return combinedRotation;
        }

        private float UpdateScale(int count)
        {
            float scaleDelta = 0f;
            for (int i = 0; i < count; i++)
            {
                GrabPointDelta data = _deltas[i];
                if (data.IsValidAxis())
                {
                    float factor = Mathf.Sqrt(data.CentroidOffset.sqrMagnitude / data.PrevCentroidOffset.sqrMagnitude);
                    scaleDelta += factor / count;
                }
                else
                {
                    scaleDelta += 1f / count;
                }
            }
            return scaleDelta;
        }


        #region Inject

        public void InjectOptionalPositionConstraints(TransformerUtils.PositionConstraints constraints)
        {
            _positionConstraints = constraints;
        }

        public void InjectOptionalRotationConstraints(TransformerUtils.RotationConstraints constraints)
        {
            _rotationConstraints = constraints;
        }

        public void InjectOptionalScaleConstraints(TransformerUtils.ScaleConstraints constraints)
        {
            _scaleConstraints = constraints;
        }

        #endregion
    }
}
