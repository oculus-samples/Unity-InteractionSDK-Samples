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
    /// ClimbingEvent is a decoration for an <see cref="LocomotionEvent"/> that indicates
    /// when the user starts or stops grabbing a <see cref="Climbable"/> to climb on.
    /// </summary>
    public struct ClimbingEvent
    {
        /// <summary>
        /// Enumeration of the types events a <see cref="ClimbingEvent"/> can represent.
        /// </summary>
        public enum ClimbingEventType
        {
            None,
            Start,
            Move,
            End
        }

        /// <summary>
        /// Identifier of the specific event.
        /// Can be used to add additional decorations and track the event.
        /// </summary>
        public ulong EventId { get; }
        /// <summary>
        /// The type of event that occurred.
        /// </summary>
        public ClimbingEventType EventType { get; }
        /// <summary>
        /// The <see cref="Climbable"/> that is sourcing the event.
        /// </summary>
        public Climbable Climbable { get; }

        /// <summary>
        /// Creates a new <see cref="ClimbingEvent"/> of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event</param>
        /// <param name="climbable">The climbable sourcing the event</param>
        public ClimbingEvent(ClimbingEventType eventType, Climbable climbable)
        {
            EventId = ++_nextEventId;
            this.EventType = eventType;
            this.Climbable = climbable;
        }

        /// <summary>
        /// Creates a new <see cref="LocomotionEvent"/> decorated with the specified <see cref="ClimbingEvent"/>.
        /// </summary>
        /// <param name="identifier">The identifier of the source</param>
        /// <param name="climbingEvent">The <see cref="ClimbingEvent"/> that will decorate the <see cref="LocomotionEvent"/> </param>
        /// <param name="pose">The Pose of the climbing event</param>
        /// <param name="context">The context used for storing the decoration</param>
        /// <returns>A new <see cref="LocomotionEvent"/> decorated with a <see cref="ClimbingEvent"/></returns>
        public static LocomotionEvent CreateLocomotionClimbingEvent(int identifier,
            ClimbingEvent climbingEvent, Pose pose, Context context = null)
        {
            LocomotionEvent locomotionEvent = new LocomotionEvent(identifier,
                pose, LocomotionEvent.TranslationType.None, LocomotionEvent.RotationType.None);
            Decorator.GetFromContext(context)
                .AddDecoration(locomotionEvent.EventId, climbingEvent);
            return locomotionEvent;
        }

        /// <summary>
        /// Retrieves the <see cref="ClimbingEvent"/> from the specified <see cref="LocomotionEvent"/>.
        /// </summary>
        /// <param name="locomotionEvent">The locomotion event</param>
        /// <param name="climbingEvent">The climbing event attached</param>
        /// <param name="context">The context used for storing the decoration</param>
        /// <returns>True if the ClimbingEvent was found in the LocomotionEvent</returns>
        public static bool TryGetLocomotionClimbingEvent(LocomotionEvent locomotionEvent,
            out ClimbingEvent climbingEvent, Context context = null)
        {
            if (Decorator.GetFromContext(context)
                .TryGetDecoration(locomotionEvent.EventId, out ClimbingEvent decoration))
            {
                climbingEvent = decoration;
                return true;
            }
            climbingEvent = default;
            return false;
        }

        /// <summary>
        /// Disposes the <see cref="ClimbingEvent"/> from the specified <see cref="LocomotionEvent"/>.
        /// </summary>
        /// <param name="locomotionEvent">The locomotion event</param>
        /// <param name="context">The context used for storing the decoration</param>
        public static void DisposeLocomotionEventClimb(LocomotionEvent locomotionEvent,
            Context context = null)
        {
            Decorator.GetFromContext(context).RemoveDecoration(locomotionEvent.EventId);
        }

        private class Decorator : ValueToValueDecorator<ulong, ClimbingEvent>
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

        private static ulong _nextEventId = 0;
    }
}
