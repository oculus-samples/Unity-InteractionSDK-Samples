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

using System.Collections.Generic;
using UnityEngine;
using Meta.XR.Samples;

namespace Oculus.Interaction.Locomotion
{
    [MetaCodeSample("ISDK-Locomotion")]
    public class MovingHandWalkingStickSetting : MonoBehaviour
    {
        public enum HandMovementStyle
        {
            Teleport = 0,
            WalkingStick = 1,
        }

        [SerializeField]
        [LocomotionSetting]
        private ReactiveValue<HandMovementStyle> _handMovement;
        public ReactiveValue<HandMovementStyle> HandMovement => _handMovement;

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<GameObject> _teleportGameObjects;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<MonoBehaviour> _teleportComponents;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<GameObject> _walkingStickGameObjects;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<MonoBehaviour> _walkingStickComponents;

        protected bool _started = false;


        protected void Start()
        {
            this.BeginStart(ref _started);
            if (_teleportGameObjects != null)
            {
                this.AssertCollectionItems(_teleportGameObjects, nameof(_teleportGameObjects));
            }
            if (_teleportComponents != null)
            {
                this.AssertCollectionItems(_teleportComponents, nameof(_teleportComponents));
            }
            if (_walkingStickGameObjects != null)
            {
                this.AssertCollectionItems(_walkingStickGameObjects, nameof(_walkingStickGameObjects));
            }
            if (_walkingStickComponents != null)
            {
                this.AssertCollectionItems(_walkingStickComponents, nameof(_walkingStickComponents));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                HandMovement.WhenChanged += HandleMovingChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                HandMovement.WhenChanged -= HandleMovingChanged;
            }
        }

        private void HandleMovingChanged(HandMovementStyle handMovement)
        {
            //First disable all irrelevant, then enable relevant
            //This avoids undesired deactivations if a gameobject is in multiple lists
            switch (handMovement)
            {
                case HandMovementStyle.Teleport:
                    _walkingStickComponents?.ForEach(c => c.enabled = false);
                    _walkingStickGameObjects?.ForEach(c => c.SetActive(false));
                    _teleportComponents?.ForEach(c => c.enabled = true);
                    _teleportGameObjects?.ForEach(c => c.SetActive(true));
                    break;
                case HandMovementStyle.WalkingStick:
                    _teleportComponents?.ForEach(c => c.enabled = false);
                    _teleportGameObjects?.ForEach(c => c.SetActive(false));
                    _walkingStickComponents?.ForEach(c => c.enabled = true);
                    _walkingStickGameObjects?.ForEach(c => c.SetActive(true));
                    break;
            }
        }
    }
}
