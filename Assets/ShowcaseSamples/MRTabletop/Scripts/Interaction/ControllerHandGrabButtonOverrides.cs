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
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(HandGrabInteractor))]
    [MetaCodeSample("ISDK-Tabletop")]
    public class ControllerHandGrabButtonOverrides : MonoBehaviour
    {
        private HandGrabInteractor _grab;
        private IController _controller;
        private ControllerButtonUsage _usage;
        private void Awake()
        {
            _grab = GetComponent<HandGrabInteractor>();
            _controller = GetComponentInParent<IController>();

            if (_controller == null)
            {
                Debug.LogAssertion(
                    "An IController could not be found in any parent. Ensure it exists for this script to work.",
                    this);
                enabled = false;
            }

            if (_grab.SupportedGrabTypes.HasFlag(GrabTypeFlags.Palm)) _usage |= ControllerButtonUsage.GripButton;
            if (_grab.SupportedGrabTypes.HasFlag(GrabTypeFlags.Pinch)) _usage |= ControllerButtonUsage.TriggerButton;
        }

        private void OnEnable()
        {
            _grab.SetComputeShouldSelectOverride(() => _controller.IsButtonUsageAnyActive(_usage) && !_grab.HasSelectedInteractable, false);
            _grab.SetComputeShouldUnselectOverride(() => !_controller.IsButtonUsageAnyActive(_usage) && _grab.HasSelectedInteractable, false);
        }

        private void OnDisable()
        {
            _grab.ClearComputeShouldSelectOverride();
            _grab.ClearComputeShouldUnselectOverride();
        }
    }
}
