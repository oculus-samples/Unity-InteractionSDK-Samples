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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Visual class for a <see cref="WalkingStick"/> that draws
    /// a cane and highlights it based on the current state.
    /// </summary>
    public class WalkingStickVisual : MonoBehaviour
    {
        [SerializeField]
        private WalkingStick _walkingStick;
        [SerializeField]
        private TubeRenderer _tube;
        [SerializeField]
        private MaterialPropertyBlockEditor _materialBlock;

        [SerializeField]
        [Tooltip("Height of the handle of the visual cane")]
        private float _handleHeight = 0.08f;
        /// <summary>
        /// Height of the handle of the visual cane
        /// </summary>
        public float HandleHeight
        {
            get => _handleHeight;
            set => _handleHeight = value;
        }

        [SerializeField]
        [Tooltip("Color to use for the non-walking state")]
        private Color _idleColor = Color.gray;
        /// <summary>
        /// Color to use for the non-walking state
        /// </summary>
        public Color IdleColor
        {
            get => _idleColor;
            set => _idleColor = value;
        }

        [SerializeField]
        [Tooltip("Color to use for the walking state")]
        private Color _selectedColor = Color.white;
        /// <summary>
        /// Color to use for the walking state
        /// </summary>
        public Color SelectedColor
        {
            get => _selectedColor;
            set => _selectedColor = value;
        }

        private TubePoint[] _tubePoints = new TubePoint[5];
        private static readonly int _colorShaderPropertyID = Shader.PropertyToID("_Color");

        protected bool _started;
        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_walkingStick, nameof(_walkingStick));
            this.AssertField(_tube, nameof(_tube));
            this.AssertField(_materialBlock, nameof(_materialBlock));
            this.EndStart(ref _started);
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _tube.Hide();
            }
        }

        protected virtual void LateUpdate()
        {
            if (_walkingStick.isActiveAndEnabled)
            {
                UpdatePoints();
            }
            else
            {
                _tube.Hide();
            }
        }

        private void UpdatePoints()
        {
            Pose origin = this.transform.GetPose();
            bool highlight = _walkingStick.Pushing;

            Vector3 handleDir = -origin.up;
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, -origin.up).normalized;

            Vector3 hinge = origin.position
                + handleDir * _handleHeight * 0.5f;

            _tubePoints[0].rotation = Quaternion.LookRotation(handleDir, slopeDir);
            _tubePoints[0].position = origin.position
                - handleDir * _handleHeight * 0.5f;
            _tubePoints[0].relativeLength = 0f;

            _tubePoints[^1].rotation = Quaternion.LookRotation(Vector3.down, origin.up);
            _tubePoints[^1].position = hinge
                + Vector3.down * _walkingStick.WalkingStickLocomotor.StickLength;
            _tubePoints[^1].relativeLength = 1f;

            float div = 1f / (_tubePoints.Length - 2f);
            for (int i = 1; i < _tubePoints.Length - 1; i++)
            {
                float t = (i - 1) * div;

                Quaternion bevel = Quaternion.Slerp(_tubePoints[0].rotation, _tubePoints[^1].rotation, t);
                _tubePoints[i].rotation = bevel;
                _tubePoints[i].position = hinge;
                _tubePoints[i].relativeLength = 0f;
            }

            _tube.RenderTube(_tubePoints, Space.World);
            _materialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, highlight ? _selectedColor : _idleColor);
            _materialBlock.UpdateMaterialPropertyBlock();
        }

        #region Inject

        public void InjectAllWalkingStickVisual(WalkingStick walkingStick,
            TubeRenderer tube, MaterialPropertyBlockEditor materialBlock)
        {
            InjectWalkingStick(walkingStick);
            InjectTube(tube);
            InjectMaterialBlock(materialBlock);
        }

        public void InjectWalkingStick(WalkingStick walkingStick)
        {
            _walkingStick = walkingStick;
        }

        public void InjectTube(TubeRenderer tube)
        {
            _tube = tube;
        }

        public void InjectMaterialBlock(MaterialPropertyBlockEditor materialBlock)
        {
            _materialBlock = materialBlock;
        }

        #endregion
    }
}
