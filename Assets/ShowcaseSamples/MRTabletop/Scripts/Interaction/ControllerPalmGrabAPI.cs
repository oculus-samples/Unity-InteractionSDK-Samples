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

using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class ControllerPalmGrabAPI : MonoBehaviour, IFingerAPI
    {
        private IController _controller;

        private float _gripStrength = 0f;
        private bool _gripDown = false;

        private bool _prevGripDown;

        private Pose _indexPinchPose = Pose.identity;
        private Pose _middlePinchPose = Pose.identity;

        private Pose _pinchPose = Pose.identity;

        private void Awake()
        {
            _controller = GetComponentInParent<IController>();
        }

        public float GetFingerGrabScore(HandFinger finger)
        {
            return _gripStrength;
        }

        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            return _gripDown;
        }

        public bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetPinchState)
        {
            return _gripDown == targetPinchState && _gripDown != _prevGripDown;
        }

        public Vector3 GetWristOffsetLocal()
        {
            return _pinchPose.position;
        }

        void IFingerAPI.Update(IHand hand)
        {
            ControllerInput input = _controller.ControllerInput;

            _prevGripDown = _gripDown;
            _gripStrength = input.Grip;
            _gripDown = input.GripButton;

            hand.GetJointPoseFromWrist(HandJointId.HandIndexTip, out _indexPinchPose);
            hand.GetJointPoseFromWrist(HandJointId.HandMiddleTip, out _middlePinchPose);
            hand.GetJointPoseFromWrist(HandJointId.HandThumbTip, out Pose thumbPose);

            PoseUtils.Lerp(ref _indexPinchPose, thumbPose, 0.5f);
            PoseUtils.Lerp(ref _middlePinchPose, thumbPose, 0.5f);

            float t = _gripStrength;
            PoseUtils.Lerp(_indexPinchPose, _middlePinchPose, t, ref _pinchPose);
        }
    }
}
