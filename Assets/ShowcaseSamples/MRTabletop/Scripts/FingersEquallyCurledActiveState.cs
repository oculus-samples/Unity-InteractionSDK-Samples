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

using System.Text;
using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    /// <summary>
    /// Active state is true when the angle difference between selected finger curls is within _maxDifference. Excludes thumb.
    /// </summary>
    [MetaCodeSample("ISDK-Tabletop")]
    class FingersEquallyCurledActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _handRef;

        private IHand _hand;

        [SerializeField]
        private HandFingerFlags _fingers;

        [SerializeField, Tooltip("In Degrees")]
        private float _maxDifference = 20f;

        private FingerShapes _fingerShapes;

        public bool Active => AreFingersEquallyCurled();

        private void Start()
        {
            _hand = _handRef as IHand;
            _fingerShapes = new FingerShapes();
        }

        private bool AreFingersEquallyCurled()
        {
            float? angle = null;
            return
                // Compare(ref angle, HandFinger.Thumb) &&
                Compare(ref angle, HandFinger.Index) &&
                Compare(ref angle, HandFinger.Middle) &&
                Compare(ref angle, HandFinger.Ring) &&
                Compare(ref angle, HandFinger.Pinky);
        }

        private bool Compare(ref float? compare, HandFinger finger)
        {
            // fingers not in the mask, no need to compare
            if ((_fingers & HandFingerUtils.ToFlags(finger)) == 0) return true;

            var value = GetValue(finger);

            if (!compare.HasValue)
            {
                // no value to compare against yet
                compare = value;
                return true;
            }
            else
            {
                return Mathf.Abs(compare.Value - value) < _maxDifference;
            }
        }

        private float GetValue(HandFinger finger)
        {
            return _fingerShapes.GetCurlValue(finger, _hand) +
                   _fingerShapes.GetFlexionValue(finger, _hand);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            // sb.AppendLine($"ThumbCurl: {GetValue(HandFinger.Thumb)}");
            sb.AppendLine($"IndexCurl: {GetValue(HandFinger.Index)}");
            sb.AppendLine($"MiddleCurl: {GetValue(HandFinger.Middle)}");
            sb.AppendLine($"RingCurl: {GetValue(HandFinger.Ring)}");
            sb.AppendLine($"PinkyCurl: {GetValue(HandFinger.Pinky)}");
            return sb.ToString();
        }
    }
}
