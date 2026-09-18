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
using UnityEngine.UI;
using Meta.XR.Samples;
using static Oculus.Interaction.Locomotion.LocomotionSettingsUIUtilities;

namespace Oculus.Interaction.Locomotion
{
    [MetaCodeSample("ISDK-Locomotion")]
    public class LocomotionSettingsUIControllerWalkingStick : MonoBehaviour
    {
        [SerializeField]
        private TeleportMovementSetting _teleportSetting;
        [SerializeField]
        private MovingHandWalkingStickSetting _movingHandSetting;
        [SerializeField]
        private MovingControllerWalkingStickSetting _movingControllerSetting;
        [SerializeField]
        private TurningSetting _turningSetting;
        [SerializeField]
        private MovementAimingSetting _aimingSetting;
        [SerializeField]
        private StandingSetting _standingSetting;
        [SerializeField]
        private ComfortVignetteSetting _comfortMovingSetting;
        [SerializeField]
        private ComfortVignetteSetting _comfortTurningSetting;

        [Space]
        [Header("Teleport Style")]
        [SerializeField]
        private Toggle _teleportStyleBlink;
        [SerializeField]
        private Toggle _teleportStyleTelepath;

        [Header("Hand Movement Style")]
        [SerializeField]
        private Toggle _movementStyleHandTeleport;
        [SerializeField]
        private Toggle _movementStyleHandWalkingStick;

        [Header("Controller Movement Style")]
        [SerializeField]
        private Toggle _movementStyleControllerSlide;
        [SerializeField]
        private Toggle _movementStyleControllerTeleport;
        [SerializeField]
        private Toggle _movementStyleControllerWalkingStick;

        [Header("Slide Direction")]
        [SerializeField]
        private Toggle _slideDirectionFacing;
        [SerializeField]
        private Toggle _slideDirectionHand;

        [Header("Rotation Angle")]
        [SerializeField]
        private UnityEngine.UI.Slider _turnVelocity;
        [SerializeField]
        private List<RotationSliderStep> _turnSteps;
        [SerializeField]
        private Toggle _rotationSnap;
        [SerializeField]
        private Toggle _rotationSmooth;

        [Header("Comfort Turning")]
        [SerializeField]
        private Toggle _comfortAssistanceTurningOff;
        [SerializeField]
        private Toggle _comfortAssistanceTurningLow;
        [SerializeField]
        private Toggle _comfortAssistanceTurningMedium;
        [SerializeField]
        private Toggle _comfortAssistanceTurningHigh;

        [Header("Comfort Moving")]
        [SerializeField]
        private Toggle _comfortAssistanceMovingOff;
        [SerializeField]
        private Toggle _comfortAssistanceMovingLow;
        [SerializeField]
        private Toggle _comfortAssistanceMovingMedium;
        [SerializeField]
        private Toggle _comfortAssistanceMovingHigh;

        [Header("Seating")]
        [SerializeField]
        private Toggle _seating;
        [SerializeField]
        private Toggle _standing;

        protected bool _started;
        private List<ISettingsUIBinding> _toggleBindings = null;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            _toggleBindings = new List<ISettingsUIBinding>
                {
                    new ToggleEnumBinding<TeleportMovementSetting.TeleportStyle>(_teleportSetting.TeleportMovement,
                        (_teleportStyleBlink, TeleportMovementSetting.TeleportStyle.Blink),
                        (_teleportStyleTelepath, TeleportMovementSetting.TeleportStyle.Telepath)),
                    new ToggleEnumBinding<MovingHandWalkingStickSetting.HandMovementStyle>(_movingHandSetting.HandMovement,
                        (_movementStyleHandTeleport, MovingHandWalkingStickSetting.HandMovementStyle.Teleport),
                        (_movementStyleHandWalkingStick, MovingHandWalkingStickSetting.HandMovementStyle.WalkingStick)),
                    new ToggleEnumBinding<MovingControllerWalkingStickSetting.MovementStyle>(_movingControllerSetting.ControllerMovement,
                        (_movementStyleControllerSlide, MovingControllerWalkingStickSetting.MovementStyle.Slide),
                        (_movementStyleControllerTeleport, MovingControllerWalkingStickSetting.MovementStyle.Teleport),
                        (_movementStyleControllerWalkingStick, MovingControllerWalkingStickSetting.MovementStyle.WalkingStick)),
                    new ToggleEnumBinding<TurningSetting.RotationStyle>(_turningSetting.ControllerTurn,
                        (_rotationSnap, TurningSetting.RotationStyle.Snap),
                        (_rotationSmooth, TurningSetting.RotationStyle.Smooth)),
                    new ToggleEnumBinding<MovementAimingSetting.AimingStyle>(_aimingSetting.Aiming,
                        (_slideDirectionHand, MovementAimingSetting.AimingStyle.Hand),
                        (_slideDirectionFacing, MovementAimingSetting.AimingStyle.Facing)),
                    new ToggleEnumBinding<ComfortVignetteSetting.ComfortAssistance>(_comfortMovingSetting.ComfortLevel,
                        (_comfortAssistanceMovingOff, ComfortVignetteSetting.ComfortAssistance.Off),
                        (_comfortAssistanceMovingLow, ComfortVignetteSetting.ComfortAssistance.Low),
                        (_comfortAssistanceMovingMedium, ComfortVignetteSetting.ComfortAssistance.Medium),
                        (_comfortAssistanceMovingHigh, ComfortVignetteSetting.ComfortAssistance.High)),
                    new ToggleEnumBinding<ComfortVignetteSetting.ComfortAssistance>(_comfortTurningSetting.ComfortLevel,
                        (_comfortAssistanceTurningOff, ComfortVignetteSetting.ComfortAssistance.Off),
                        (_comfortAssistanceTurningLow, ComfortVignetteSetting.ComfortAssistance.Low),
                        (_comfortAssistanceTurningMedium, ComfortVignetteSetting.ComfortAssistance.Medium),
                        (_comfortAssistanceTurningHigh, ComfortVignetteSetting.ComfortAssistance.High)),
                    new ToggleEnumBinding<StandingSetting.StandingMode>(_standingSetting.Standing,
                        (_standing, StandingSetting.StandingMode.Standing),
                        (_seating, StandingSetting.StandingMode.Seating)),
                    new TurnSettingsBinding(_turnVelocity, _turningSetting.RotationSnapAngle, _turningSetting.RotationSmoothVelocity, _turnSteps)
                };

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                foreach (var binding in _toggleBindings)
                {
                    binding.Register();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (var binding in _toggleBindings)
                {
                    binding.Unregister();
                }
            }
        }

    }
}
