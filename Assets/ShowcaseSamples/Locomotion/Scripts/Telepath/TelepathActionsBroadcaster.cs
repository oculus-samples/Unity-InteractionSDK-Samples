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
    /// This component creates decorated <see cref="LocomotionEvent"/> from a <see cref="TeleportInteractor"/> in order
    /// to signify the start and end of a teleportation action that can be used by the <see cref="TelepathLocomotor"/>.
    /// </summary>
    public class TelepathActionsBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {
        public enum TelepathAction
        {
            Halt,
            Hover,
            Unhover,
        }

        [SerializeField]
        private TeleportInteractor _interactor;

        [SerializeField]
        [Tooltip("If enabled, it will send a Halt event when the interactor is disabled")]
        private bool _broadcastHalt = true;
        /// <summary>
        /// If enabled, it will send a Halt event when the interactor is disabled
        /// </summary>
        public bool BroadcastHalt
        {
            get => _broadcastHalt;
            set => _broadcastHalt = value;
        }

        [SerializeField, Optional]
        private Context _context;

        protected bool _started;
        private bool _isPostprocessResgistered;

        private Action<LocomotionEvent> _whenLocomotionPerformed = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add => _whenLocomotionPerformed += value;
            remove => _whenLocomotionPerformed -= value;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_interactor, nameof(_interactor));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _interactor.WhenStateChanged += InteractorHandleStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_isPostprocessResgistered)
                {
                    UnregisterPostprocess();
                }
                if (_broadcastHalt)
                {
                    BroadcastHaltAction();
                }
                _interactor.WhenStateChanged -= InteractorHandleStateChanged;
            }
        }

        private void InteractorHandleStateChanged(InteractorStateChangeArgs stateChange)
        {
            if (stateChange.NewState == InteractorState.Hover)
            {
                RegisterPostproces();
            }
            else if (stateChange.PreviousState == InteractorState.Hover)
            {
                UnregisterPostprocess();
            }
            else if (stateChange.NewState == InteractorState.Disabled && _broadcastHalt)
            {
                BroadcastHaltAction();
            }
        }

        private void RegisterPostproces()
        {
            if (!_isPostprocessResgistered)
            {
                _isPostprocessResgistered = true;
                _interactor.WhenPostprocessed += HandleHoverPostprocessed;
            }
        }

        private void UnregisterPostprocess()
        {
            if (_isPostprocessResgistered)
            {
                _interactor.WhenPostprocessed -= HandleHoverPostprocessed;
                _isPostprocessResgistered = false;
            }
        }

        private void HandleHoverPostprocessed()
        {
            if (_interactor.HasValidDestination())
            {
                BroadcastHoverAction();
            }
            else
            {
                BroadcastUnhoverAction();
            }
        }

        /// <summary>
        /// Broadcasts a TelepathAction.Hover event
        /// </summary>
        public void BroadcastHoverAction()
        {
            Pose target = _interactor.TeleportTarget;
            LocomotionEvent locomotionEvent = CreateTelepathAction(
                _interactor.Identifier, TelepathAction.Hover, target, _context);
            _whenLocomotionPerformed.Invoke(locomotionEvent);
            DisposeTelepathAction(locomotionEvent);
        }

        /// <summary>
        /// Broadcasts a TelepathAction.Unhover event
        /// </summary>
        public void BroadcastUnhoverAction()
        {
            Pose target = _interactor.TeleportTarget;
            LocomotionEvent locomotionEvent = CreateTelepathAction(
                _interactor.Identifier, TelepathAction.Unhover, target, _context);
            _whenLocomotionPerformed.Invoke(locomotionEvent);
            DisposeTelepathAction(locomotionEvent);
        }

        /// <summary>
        /// Broadcasts a TelepathAction.Halt event
        /// </summary>
        public void BroadcastHaltAction()
        {
            LocomotionEvent locomotionEvent = CreateTelepathAction(
                _interactor.Identifier, TelepathAction.Halt, Pose.identity, _context);
            _whenLocomotionPerformed.Invoke(locomotionEvent);
            DisposeTelepathAction(locomotionEvent);
        }

        /// <summary>
        /// This utility method allows creating TelepathAction decorations
        /// </summary>
        /// <param name="identifier">The identifier of the sender</param>
        /// <param name="action">The action to send</param>
        /// <param name="pose">The pose to be sent in the LocomotionEvent, note that this won't have any Translation or Rotation applied</param>
        /// <param name="context">The context used for storing the decoration</param>
        /// <returns>The LocomotionEvent decorated with the action</returns>
        public static LocomotionEvent CreateTelepathAction(int identifier, TelepathAction action, Pose pose = default, Context context = null)
        {
            LocomotionEvent locomotionEvent = new LocomotionEvent(identifier,
                pose, LocomotionEvent.TranslationType.None, LocomotionEvent.RotationType.None);
            Decorator.GetFromContext(context).AddDecoration(locomotionEvent.EventId, action);
            return locomotionEvent;
        }

        /// <summary>
        /// This utility methods allows retrieving the TelepathAction from a given LocomotionEvent, if it contains one.
        /// </summary>
        /// <param name="locomotionEvent">The LocomotionEvent potentially containing the TelepathAction</param>
        /// <param name="action">The TelepathAction that it contained</param>
        /// <param name="context">The context used for storing the decoration</param>
        /// <returns>True if the LocomotionEvent contained a valid TelepathAction</returns>
        public static bool TryGetTelepathAction(LocomotionEvent locomotionEvent, out TelepathAction action, Context context = null)
        {
            if (Decorator.GetFromContext(context).TryGetDecoration(locomotionEvent.EventId, out TelepathAction decoration))
            {
                action = decoration;
                return true;
            }
            action = default;
            return false;
        }

        /// <summary>
        /// This utility method removes the TelepathAction decoration from the LocomotionEvent.
        /// These decorations are not automatically removed, so it is important to manually call this method
        /// to avoid filling the memory with obsolete decorations
        /// </summary>
        /// <param name="locomotionEvent">The event containing the TelepathAction decoration.</param>
        /// <param name="context">The context storing the decoration</param>
        public static void DisposeTelepathAction(LocomotionEvent locomotionEvent, Context context = null)
        {
            Decorator.GetFromContext(context).RemoveDecoration(locomotionEvent.EventId);
        }

        private class Decorator : ValueToValueDecorator<ulong, TelepathAction>
        {
            private Decorator() { }

            public static Decorator GetFromContext(Context context = null)
            {
                if (context == null)
                {
                    context = Context.Global.GetInstance();
                }
                return context.GetOrCreateSingleton<Decorator>(() => new());
            }
        }

        #region Injects

        public void InjectAllTelepathActionBroadcaster(TeleportInteractor interactor)
        {
            InjectInteractor(interactor);
        }

        public void InjectInteractor(TeleportInteractor interactor)
        {
            _interactor = interactor;
        }

        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion
    }
}
