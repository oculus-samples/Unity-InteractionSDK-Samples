/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Oculus.Interaction
{
    public class DynamicMoveTowardsTargetProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField]
        private float _attractionFactor = 4f;

        public IMovement CreateMovement()
        {
            return new DynamicMoveTowardsTarget(_attractionFactor);
        }

        #region Inject
        public void InjectAllMoveTowardsTargetProvider(float attractionFactor)
        {
            InjectAttractionFactor(attractionFactor);
        }

        public void InjectAttractionFactor(float attractionFactor)
        {
            _attractionFactor = attractionFactor;
        }
        #endregion

        private class DynamicMoveTowardsTarget : IMovement
        {
            private float _attractionSpeed;
            private Pose _source;
            private Pose _target;
            private Pose _offset = Pose.identity;
            private Vector3 _prevTargetPos;

            public Pose Pose => _target;
            public bool Stopped => true;

            public DynamicMoveTowardsTarget(float attractionSpeed)
            {
                _attractionSpeed = attractionSpeed;
            }

            public void MoveTo(Pose target)
            {
                _offset = PoseUtils.Delta(target, _source);
                _prevTargetPos = target.position;
                target.Premultiply(_offset);
                _target = target;
            }

            public void UpdateTarget(Pose target)
            {
                if (_target != target)
                {
                    float difference = (target.position - _prevTargetPos).magnitude;
                    _prevTargetPos = target.position;
                    _offset.Lerp(Pose.identity, difference * _attractionSpeed);
                    target.Premultiply(_offset);
                    _target = target;
                }
            }

            public void StopAndSetPose(Pose pose)
            {
                _source = pose;
            }

            public void Tick()
            {
            }
        }
    }

}
