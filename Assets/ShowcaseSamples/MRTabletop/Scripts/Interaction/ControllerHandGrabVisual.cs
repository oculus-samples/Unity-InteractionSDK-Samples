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

using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
using Meta.XR.Samples;

namespace Meta.XR.InteractionSDK.Samples
{
    /// <summary>
    /// Reads an <see cref="IHandGrabState"/> and applies a fixed finger shape to a
    /// <see cref="SyntheticHand"/> while grabbing. Unlike <see cref="HandGrabStateVisual"/> this does
    /// not constrain the wrist (the hand is positioned by the controller); it only overrides the finger
    /// pose, choosing between a pinch pose (index/trigger) and a palm pose (middle/grip).
    /// The authored handedness of the poses is irrelevant: they are mirrored to match the synthetic hand.
    /// </summary>
    [MetaCodeSample("ISDK-Tabletop")]
    public class ControllerHandGrabVisual : MonoBehaviour
    {
        [SerializeField]
        [Interface(typeof(IHandGrabState))]
        private UnityEngine.Object _handGrabState;

        private IHandGrabState HandGrabState;

        [SerializeField]
        private SyntheticHand _syntheticHand;

        [SerializeField]
        [Tooltip("Finger shape applied when grabbing with the index finger (trigger).")]
        private HandPose _pinchPose;

        [SerializeField]
        [Tooltip("Finger shape applied when grabbing with the middle finger (grip).")]
        private HandPose _palmPose;

        private HandPose _pinchPoseMatched;
        private HandPose _palmPoseMatched;
        private bool _posesMatched;

        private bool _areFingersFree = true;
        private bool _wereFingersFree = true;

        protected bool _started = false;

        protected virtual void Awake()
        {
            if (HandGrabState == null)
            {
                HandGrabState = _handGrabState as IHandGrabState;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(HandGrabState, nameof(HandGrabState));
            this.AssertField(_syntheticHand, nameof(_syntheticHand));
            this.EndStart(ref _started);
        }

        private void LateUpdate()
        {
            EnsurePosesMatched();

            HandPose handPose = SelectHandPose(HandGrabState, out HandFingerFlags grabbingFingers, out float strength);
            if (handPose != null && strength > 0f)
            {
                UpdateFingers(handPose, grabbingFingers, strength);
                _areFingersFree = false;
            }
            else
            {
                FreeFingers();
            }

            if (!_areFingersFree
                || (_areFingersFree && !_wereFingersFree))
            {
                _syntheticHand.MarkInputDataRequiresUpdate();
            }
            _wereFingersFree = _areFingersFree;
        }

        /// <summary>
        /// Picks the finger shape to apply based on which fingers are grabbing. A middle-finger (grip)
        /// grab maps to the palm pose, an index-finger (trigger) grab maps to the pinch pose. Palm takes
        /// priority since a full grip also curls the index finger.
        /// </summary>
        private HandPose SelectHandPose(IHandGrabState grabSource, out HandFingerFlags grabbingFingers, out float strength)
        {
            grabbingFingers = HandFingerFlags.None;
            strength = 0f;

            HandGrabTarget grabData = grabSource.HandGrabTarget;
            if (grabData == null || !grabSource.IsGrabbing)
            {
                return null;
            }

            grabbingFingers = grabSource.GrabbingFingers();
            strength = grabSource.FingersStrength;

            if ((grabbingFingers & HandFingerFlags.Middle) != 0)
            {
                return _palmPoseMatched;
            }
            if ((grabbingFingers & HandFingerFlags.Index) != 0)
            {
                return _pinchPoseMatched;
            }
            return null;
        }

        /// <summary>
        /// Writes the desired rotation values for each joint and locks any constrained finger that is
        /// actively grabbing, mirroring the behavior of <see cref="HandGrabStateVisual"/>.
        /// </summary>
        private void UpdateFingers(HandPose handPose, HandFingerFlags grabbingFingers, float strength)
        {
            Quaternion[] desiredRotations = handPose.JointRotations;
            _syntheticHand.OverrideAllJoints(desiredRotations, strength);

            for (int fingerIndex = 0; fingerIndex < Constants.NUM_FINGERS; fingerIndex++)
            {
                int fingerFlag = 1 << fingerIndex;
                JointFreedom fingerFreedom = handPose.FingersFreedom[fingerIndex];
                if (fingerFreedom == JointFreedom.Constrained
                    && ((int)grabbingFingers & fingerFlag) != 0)
                {
                    fingerFreedom = JointFreedom.Locked;
                }
                _syntheticHand.SetFingerFreedom((HandFinger)fingerIndex, fingerFreedom);
            }
        }

        private bool FreeFingers()
        {
            if (!_areFingersFree)
            {
                _syntheticHand.FreeAllJoints();
                _areFingersFree = true;
                return true;
            }
            return false;
        }

        private void EnsurePosesMatched()
        {
            if (_posesMatched)
            {
                return;
            }
            Handedness handedness = _syntheticHand.Handedness;
            _pinchPoseMatched = MatchHandedness(_pinchPose, handedness);
            _palmPoseMatched = MatchHandedness(_palmPose, handedness);
            _posesMatched = true;
        }

        /// <summary>
        /// Returns a copy of the pose whose handedness matches the target, mirroring the joint rotations
        /// when the authored handedness differs so a pose authored for either hand can drive this hand.
        /// </summary>
        private static HandPose MatchHandedness(HandPose source, Handedness targetHandedness)
        {
            if (source == null)
            {
                return null;
            }
            if (source.Handedness == targetHandedness)
            {
                return source;
            }

            HandPose matched = new HandPose(source);
            matched.Handedness = targetHandedness;
            Quaternion[] rotations = matched.JointRotations;
            for (int i = 0; i < rotations.Length; i++)
            {
                rotations[i] = HandMirroring.Mirror(rotations[i]);
            }
            return matched;
        }

        #region Inject

        public void InjectAllControllerHandGrabVisual(IHandGrabState handGrabState, SyntheticHand syntheticHand,
            HandPose pinchPose, HandPose palmPose)
        {
            InjectHandGrabState(handGrabState);
            InjectSyntheticHand(syntheticHand);
            InjectPinchPose(pinchPose);
            InjectPalmPose(palmPose);
        }

        public void InjectHandGrabState(IHandGrabState handGrabState)
        {
            HandGrabState = handGrabState;
            _handGrabState = handGrabState as UnityEngine.Object;
        }

        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        public void InjectPinchPose(HandPose pinchPose)
        {
            _pinchPose = pinchPose;
            _posesMatched = false;
        }

        public void InjectPalmPose(HandPose palmPose)
        {
            _palmPose = palmPose;
            _posesMatched = false;
        }

        #endregion
    }
}
