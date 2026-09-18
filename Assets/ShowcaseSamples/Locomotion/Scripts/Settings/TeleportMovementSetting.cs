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
using UnityEngine.Serialization;
using Meta.XR.Samples;

namespace Oculus.Interaction.Locomotion
{
    [MetaCodeSample("ISDK-Locomotion")]
    public class TeleportMovementSetting : MonoBehaviour
    {
        public enum TeleportStyle
        {
            Blink = 0,
            Telepath = 1,
        }

        [SerializeField]
        [LocomotionSetting]
        private ReactiveValue<TeleportStyle> _teleportMovement;
        public ReactiveValue<TeleportStyle> TeleportMovement => _teleportMovement;

        [Space]
        [SerializeField]
        private MovingControllerWalkingStickSetting _controllerMoving;
        [SerializeField]
        private MovingHandWalkingStickSetting _handMoving;

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private GameObject _blinkLocomotor;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<GameObject> _blinkHandGameObjects;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<GameObject> _blinkControllerGameObjects;

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private GameObject _telepathLocomotor;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<GameObject> _telepathHandGameObjects;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private List<GameObject> _telepathControllerGameObjects;

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);
            if (_blinkHandGameObjects != null)
            {
                this.AssertField(_blinkLocomotor, nameof(_blinkLocomotor));
                this.AssertCollectionItems(_blinkHandGameObjects, nameof(_blinkHandGameObjects));
                this.AssertCollectionItems(_blinkControllerGameObjects, nameof(_blinkControllerGameObjects));
            }
            if (_telepathHandGameObjects != null)
            {
                this.AssertField(_telepathHandGameObjects, nameof(_telepathHandGameObjects));
                this.AssertCollectionItems(_telepathHandGameObjects, nameof(_telepathHandGameObjects));
                this.AssertCollectionItems(_telepathControllerGameObjects, nameof(_telepathControllerGameObjects));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                TeleportMovement.WhenChanged += HandleMovingChanged;
                _controllerMoving.ControllerMovement.WhenChanged += HandleControllerMovementChanged;
                _handMoving.HandMovement.WhenChanged += HandleHandMovementChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                TeleportMovement.WhenChanged -= HandleMovingChanged;
                _controllerMoving.ControllerMovement.WhenChanged -= HandleControllerMovementChanged;
                _handMoving.HandMovement.WhenChanged -= HandleHandMovementChanged;
            }
        }

        private void HandleControllerMovementChanged(MovingControllerWalkingStickSetting.MovementStyle controllerMovement)
        {
            UpdateTeleportMode();
        }

        private void HandleHandMovementChanged(MovingHandWalkingStickSetting.HandMovementStyle handMovement)
        {
            UpdateTeleportMode();
        }

        private void HandleMovingChanged(TeleportStyle teleportMovement)
        {
            UpdateTeleportMode();
        }

        private void UpdateTeleportMode()
        {
            TeleportStyle teleportMovement = TeleportMovement.Value;
            bool controllerTeleport = _controllerMoving.ControllerMovement.Value == MovingControllerWalkingStickSetting.MovementStyle.Teleport;
            bool handTeleport = _handMoving.HandMovement.Value == MovingHandWalkingStickSetting.HandMovementStyle.Teleport;

            //First disable all irrelevant, then enable relevant
            //This avoids undesired deactivations if a gameobject is in multiple lists
            switch (teleportMovement)
            {
                case TeleportStyle.Blink:
                    _telepathLocomotor.SetActive(false);
                    _blinkLocomotor.SetActive(true);
                    _telepathHandGameObjects?.ForEach(c => c.SetActive(false));
                    _telepathControllerGameObjects?.ForEach(c => c.SetActive(false));
                    _blinkHandGameObjects?.ForEach(c => c.SetActive(handTeleport));
                    _blinkControllerGameObjects?.ForEach(c => c.SetActive(controllerTeleport));
                    break;
                case TeleportStyle.Telepath:
                    _blinkLocomotor.SetActive(false);
                    _telepathLocomotor.SetActive(true);
                    _blinkHandGameObjects?.ForEach(c => c.SetActive(false));
                    _blinkControllerGameObjects?.ForEach(c => c.SetActive(false));
                    _telepathHandGameObjects?.ForEach(c => c.SetActive(handTeleport));
                    _telepathControllerGameObjects?.ForEach(c => c.SetActive(controllerTeleport));
                    break;
            }
        }
    }
}
