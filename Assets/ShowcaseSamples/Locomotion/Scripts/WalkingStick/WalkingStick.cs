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
using UnityEngine.Events;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Works in conjunction with <see cref="WalkingStickLocomotor"/> to move
    /// the player when the stick is pushed against the floor.
    /// Multiple <see cref="WalkingStick"/> can be used at the same time to move.
    /// </summary>
    public class WalkingStick : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The WalkingStickLocomotor to subscribe this WalkingStick to.")]
        private WalkingStickLocomotor _walkingStickLocomotor;
        /// <summary>
        /// The <see cref="WalkingStickLocomotor"/>to subscribe this WalkingStick to.
        /// </summary>
        public WalkingStickLocomotor WalkingStickLocomotor => _walkingStickLocomotor;

        [SerializeField, Optional]
        [Tooltip("When set the WalkingStick is directly under this transform, it will not be used to move the player.")]
        private Transform _shoulder;

        [SerializeField]
        [Tooltip(" Raised when the stick is penetrating the floor.")]
        private UnityEvent _whenSticked = new UnityEvent();
        /// <summary>
        /// Raised when the stick is penetrating the floor.
        /// </summary>
        public UnityEvent WhenSticked => _whenSticked;

        [SerializeField]
        [Tooltip("Raised when the stick is no longer penetrating the floor.")]
        private UnityEvent _whenUnsticked = new UnityEvent();
        /// <summary>
        /// Raised when the stick is no longer penetrating the floor.
        /// </summary>
        public UnityEvent WhenUnsticked => _whenUnsticked;

        private bool _pushing;
        /// <summary>
        /// True if the stick is pushing against the floor.
        /// </summary>
        public bool Pushing
        {
            get => this.isActiveAndEnabled && _pushing;
            internal set => _pushing = value;
        }

        private UniqueIdentifier _identifier;
        /// <summary>
        /// Unique identifier of this stick.
        /// </summary>
        public int Identifier => _identifier.ID;

        private static readonly float _pointingDownTreshold = 0.6f;
        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_walkingStickLocomotor, nameof(_walkingStickLocomotor));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _walkingStickLocomotor.RegisterStick(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _walkingStickLocomotor.UnregisterStick(this);
                _whenUnsticked.Invoke();
            }
        }

        internal void Stick()
        {
            _whenSticked.Invoke();
        }

        internal void Unstick()
        {
            _whenUnsticked.Invoke();
        }

        internal bool IsHighConfidence()
        {
            if (_shoulder != null &&
                Vector3.Dot(this.transform.position - _shoulder.position, Vector3.down) > _pointingDownTreshold)
            {
                return false;
            }
            return true;
        }

        #region Inject

        public void InjectAllWalkingStick(WalkingStickLocomotor walkingStickLocomotor)
        {
            InjectWalkingStickLocomotor(walkingStickLocomotor);
        }

        public void InjectWalkingStickLocomotor(WalkingStickLocomotor walkingStickLocomotor)
        {
            _walkingStickLocomotor = walkingStickLocomotor;
        }

        public void InjectOptionalShoulder(Transform shoulder)
        {
            _shoulder = shoulder;
        }

        #endregion
    }
}
