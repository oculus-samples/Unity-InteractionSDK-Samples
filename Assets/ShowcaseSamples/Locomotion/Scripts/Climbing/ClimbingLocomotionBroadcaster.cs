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

using System;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This component acts as a decoration for an Interactor indicating that the
    /// decorated interactor can be used for climbing.
    /// <see cref="Climbable"/> will use it to route <see cref="ClimbingEvent"/> from
    /// the interactable side to the Rig.
    /// </summary>
    public class ClimbingLocomotionBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {
        [SerializeField, Interface(typeof(IInteractorView))]
        [Tooltip("The interactor that can generate climbing events")]
        private UnityEngine.Object _interactor;
        private IInteractorView Interactor { get; set; }

        [SerializeField, Optional]
        [Tooltip("Context to be used for Decorations of the LocomotionEvent and the Interactor")]
        private Context _context;

        private Action<LocomotionEvent> _whenLocomotionEventPerformed = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add => _whenLocomotionEventPerformed += value;
            remove => _whenLocomotionEventPerformed -= value;
        }

        protected bool _started = false;

        protected virtual void Awake()
        {
            Interactor = _interactor as IInteractorView;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(Interactor, nameof(_interactor));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Decorator.GetFromContext(_context)
                    .AddDecoration(Interactor.Identifier, this);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Decorator.GetFromContext(_context)
                    .RemoveDecoration(Interactor.Identifier);
            }
        }

        /// <summary>
        /// Broadcasts a <see cref="LocomotionEvent"/> decorated with the <see cref="ClimbingEvent"/> to any listeners.
        /// </summary>
        /// <param name="climbingEvent">ClimbingEvent to broadcast via the Locomotion pipeline</param>
        /// <param name="pose">Pose of the Climbing</param>
        public void BroadcastClimbingEvent(ClimbingEvent climbingEvent, Pose pose)
        {
            LocomotionEvent locomotionEvent = ClimbingEvent.CreateLocomotionClimbingEvent(
                Interactor.Identifier, climbingEvent, pose);
            _whenLocomotionEventPerformed.Invoke(locomotionEvent);
            ClimbingEvent.DisposeLocomotionEventClimb(locomotionEvent);
        }

        /// <summary>
        /// Returns the <see cref="ClimbingLocomotionBroadcaster"/> associated with the <see cref="IInteractorView"/>
        /// </summary>
        /// <param name="identifier">Identifier of the Interactor</param>
        /// <param name="broadcaster">The <see cref="ClimbingLocomotionBroadcaster"/>, if found</param>
        /// <param name="context">Context used for decorating the Interactor</param>
        /// <returns></returns>
        public static bool TryGetClimbingLocomotionBroadcaster(int identifier, out ClimbingLocomotionBroadcaster broadcaster, Context context = null)
        {
            return Decorator.GetFromContext(context)
                .TryGetDecoration(identifier, out broadcaster);
        }

        private class Decorator : ValueToClassDecorator<int, ClimbingLocomotionBroadcaster>
        {
            private Decorator() { }

            public static Decorator GetFromContext(Context context)
            {
                if (context == null)
                {
                    context = Context.Global.GetInstance();
                }
                return context.GetOrCreateSingleton<Decorator>(() => new());
            }
        }

        #region Inject

        public void InjectAllClimbingLocomotionBroadcaster(IInteractorView interactor)
        {
            InjectInteractor(interactor);
        }

        public void InjectInteractor(IInteractorView interactor)
        {
            _interactor = interactor as UnityEngine.Object;
            Interactor = interactor;
        }

        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion
    }
}
