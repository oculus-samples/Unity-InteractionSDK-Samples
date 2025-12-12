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
using UnityEngine.AI;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This component transforms incoming Absolute (Teleport) locomotion events into Velocity based events,
    /// following a path in the specified NavMesh.
    /// In case the target is unreachable, it will create an Absolute translation event to the target.
    /// </summary>
    public class TelepathLocomotor : MonoBehaviour,
        ILocomotionEventHandler, ILocomotionEventBroadcaster
    {
        [Header("Character")]
        [SerializeField]
        [Tooltip("The character's feet transform")]
        private Transform _characterFeet;

        [Header("NavMesh")]
        [SerializeField]
        [Tooltip("The min distance to consider that the player has reached a corner in the path")]
        private float _cornerThreshold = 0.1f;
        /// <summary>
        /// The min distance to consider that the player has reached a corner in the path
        /// </summary>
        public float CornerThreshold
        {
            get => _cornerThreshold;
            set => _cornerThreshold = value;
        }

        [SerializeField]
        [Tooltip("The index of the agent in Navigation settings to be used for the path calculation")]
        private int _agentIndex = 0;

        [SerializeField]
        [Tooltip("The area mask used for the path calculation")]
        private int _areasID = NavMesh.AllAreas;

        [SerializeField, Min(2)]
        [Tooltip("The max number of corners that the path can have")]
        private int _maxNavMeshCornersCount = 30;

        [SerializeField, Optional]
        private Context _context;

        public event Action<LocomotionEvent> WhenLocomotionPerformed = delegate { };

        protected Action<LocomotionEvent, Pose> _whenLocomotionEventHandled = delegate { };
        public event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled
        {
            add
            {
                _whenLocomotionEventHandled += value;
            }
            remove
            {
                _whenLocomotionEventHandled -= value;
            }
        }

        public bool HasTarget => _target.HasValue;

        private Vector3[] _pathCorners;
        private int _cornersCount = 0;
        private int _currentCorner = 0;

        public Vector3[] PathCorners => _pathCorners;
        public int PathCornersCount => _cornersCount;
        public int CurrentPathCorner => _currentCorner;

        private NavMeshPath _navMeshPath;
        private NavMeshQueryFilter _navMeshQuery;
        private LocomotionEvent? _target;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            _pathCorners = new Vector3[_maxNavMeshCornersCount];
            _navMeshPath = new NavMeshPath();

            this.AssertField(_characterFeet, nameof(_characterFeet));

            _navMeshQuery = new NavMeshQueryFilter()
            {
                agentTypeID = NavMesh.GetSettingsByIndex(_agentIndex).agentTypeID,
                areaMask = _areasID
            };

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                EndTravel();
            }
        }

        protected virtual void Update()
        {
            if (_target.HasValue)
            {
                HomingTarget(_target.Value);
            }
        }

        #region Locomotion Events Handling

        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            if (locomotionEvent.Translation != LocomotionEvent.TranslationType.None)
            {
                EndTravel();
            }

            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute
                || locomotionEvent.Translation == LocomotionEvent.TranslationType.AbsoluteEyeLevel)
            {
                if (NavMesh.CalculatePath(_characterFeet.position, locomotionEvent.Pose.position, _navMeshQuery, _navMeshPath))
                {
                    _target = locomotionEvent;
                    _cornersCount = _navMeshPath.GetCornersNonAlloc(_pathCorners);
                    _currentCorner = 1;
                    _whenLocomotionEventHandled.Invoke(locomotionEvent, locomotionEvent.Pose);
                }
                else
                {
                    WhenLocomotionPerformed.Invoke(locomotionEvent);
                }
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None
                && locomotionEvent.Rotation == LocomotionEvent.RotationType.None
                && TryPerformTelepathAction(locomotionEvent))
            {
                _whenLocomotionEventHandled.Invoke(locomotionEvent, locomotionEvent.Pose);
            }
            else
            {
                WhenLocomotionPerformed.Invoke(locomotionEvent);
            }
        }

        #endregion Locomotion Events Handling

        private bool TryPerformTelepathAction(in LocomotionEvent locomotionEvent)
        {
            if (!TelepathActionsBroadcaster.TryGetTelepathAction(locomotionEvent,
                out TelepathActionsBroadcaster.TelepathAction action, _context))
            {
                return false;
            }

            switch (action)
            {
                case TelepathActionsBroadcaster.TelepathAction.Halt:
                    EndTravel();
                    return true;
                case TelepathActionsBroadcaster.TelepathAction.Hover:
                    HoverPath(locomotionEvent.Pose);
                    return true;
                case TelepathActionsBroadcaster.TelepathAction.Unhover:
                    UnhoverPath();
                    return true;
                default: return false;
            }
        }

        private void HoverPath(Pose pose)
        {
            if (_target.HasValue)
            {
                return;
            }

            if (NavMesh.CalculatePath(_characterFeet.position, pose.position, _navMeshQuery, _navMeshPath))
            {
                _cornersCount = _navMeshPath.GetCornersNonAlloc(_pathCorners);
            }
            else
            {
                _cornersCount = 0;
            }
        }

        private void UnhoverPath()
        {
            if (_target.HasValue)
            {
                return;
            }

            _cornersCount = 0;
        }

        private void HomingTarget(LocomotionEvent target)
        {
            Vector3 characterFeet = _characterFeet.position;

            float sqrThreshold = _cornerThreshold * _cornerThreshold;
            if (!TryCalculateNextCorner(characterFeet, out Vector3 targetPos))
            {
                EndTravel();
                WhenLocomotionPerformed.Invoke(target);
                return;
            }

            Vector3 delta = targetPos - characterFeet;
            Vector3 flatDelta = Vector3.ProjectOnPlane(delta.normalized, Vector3.up);
            Pose pose = new Pose(flatDelta,
                Quaternion.LookRotation(flatDelta.normalized));
            WhenLocomotionPerformed.Invoke(new LocomotionEvent(target.Identifier, pose,
                LocomotionEvent.TranslationType.Velocity, LocomotionEvent.RotationType.None));
        }

        private bool TryCalculateNextCorner(Vector3 currentPosition, out Vector3 nextCorner)
        {
            Vector3 start = _pathCorners[_currentCorner - 1];
            Vector3 end = _pathCorners[_currentCorner];
            Vector3 segment = end - start;
            Vector3 projected = Vector3.Project(currentPosition - start, segment);

            if (projected.sqrMagnitude - segment.sqrMagnitude > -(_cornerThreshold * _cornerThreshold))
            {
                if (_currentCorner >= _cornersCount - 1)
                {
                    nextCorner = end;
                    return false;
                }
                _currentCorner++;
                nextCorner = _pathCorners[_currentCorner];
                return true;
            }
            else
            {
                nextCorner = end;
                return true;
            }
        }

        private void EndTravel()
        {
            _currentCorner = 0;
            _cornersCount = 0;
            _target = null;
        }

        #region  Injects

        public void InjectAllTelepathLocomotor(Transform characterFeet)
        {
            InjectCharacterFeet(characterFeet);
        }

        public void InjectCharacterFeet(Transform characterFeet)
        {
            _characterFeet = characterFeet;
        }

        public void InjectOptionalAgentIndex(int agentIndex)
        {
            _agentIndex = agentIndex;
        }

        public void InjectOptionalMaxNavMeshCorners(int maxNavMeshCornersCount)
        {
            _maxNavMeshCornersCount = maxNavMeshCornersCount;
        }

        public void InjectOptionalAreasID(int areasID)
        {
            _areasID = areasID;
        }

        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion


    }
}
