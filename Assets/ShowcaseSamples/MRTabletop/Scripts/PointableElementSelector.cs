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
using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class PointableElementSelector : PointableElement
    {
        [SerializeField]
        private Selection[] _selections;

        [SerializeField]
        private PointableElement _defaultElement;

        private int _selectedIndex = -1;
        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (var selection in _selections)
            {
                selection.Initialize();
            }

            Assert.IsFalse(_defaultElement.Equals(this), "DefaultElement cannot reference ourselves.");
            Assert.IsNull(ForwardElement, "Cannot have forward element on this selector.");
        }

        public override void ProcessPointerEvent(PointerEvent evt)
        {
            base.ProcessPointerEvent(evt);  //Updates points lists.

            var prevIndex = _selectedIndex;
            ProcessEvent(evt, prevIndex);

            _selectedIndex = Array.FindIndex(_selections, x => x.ActiveState.Active);
            if (_selectedIndex != prevIndex)
            {
                HandleNewSelection(prevIndex, _selectedIndex);
            }
        }

        private IPointableElement GetElement(int index)
        {
            return index < 0 ? _defaultElement : _selections[index].PointableElement;
        }

        private void ProcessEvent(PointerEvent evt, int index)
        {
            GetElement(index).ProcessPointerEvent(evt);
        }

        private void HandleNewSelection(int prevIndex, int newIndex)
        {
            var points = new List<int>(_pointIds); //Is this needed anymore? Can just directly reference them?
            var poses = new List<Pose>(_points);
            var selectingPoints = new List<int>(_selectingPointIds);
            var selectingPoses = new List<Pose>(_selectingPoints);

            //Cancel interaction on prevPointable.
            Assert.IsTrue(points.Count == poses.Count, "Points and Poses should be matching.");

            for (var i = 0; i < selectingPoints.Count; i++)
            {
                var unselectEvent = new PointerEvent(selectingPoints[i], PointerEventType.Unselect, selectingPoses[i]);
                ProcessEvent(unselectEvent, prevIndex);
            }

            for (var i = 0; i < points.Count; i++)
            {
                var unhoverEvent = new PointerEvent(points[i], PointerEventType.Unhover, poses[i]);
                ProcessEvent(unhoverEvent, prevIndex);
            }

            //Hover points that need hovering on newPointable
            for (var i = 0; i < points.Count; i++)
            {
                var hoverEvent = new PointerEvent(points[i], PointerEventType.Hover, poses[i]);
                ProcessEvent(hoverEvent, newIndex);
            }

            Assert.IsTrue(selectingPoints.Count == selectingPoses.Count, "Selecting Points and Poses should be matching.");
            for (var i = 0; i < selectingPoints.Count; i++)
            {
                var selectEvent = new PointerEvent(selectingPoints[i], PointerEventType.Select, selectingPoses[i]);
                ProcessEvent(selectEvent, newIndex);
            }
        }

        [Serializable]
        [MetaCodeSample("ISDKSamples-ShowcaseSamples")]
        private class Selection
        {
            [SerializeField, Interface(typeof(IActiveState))]
            private UnityEngine.Object _activeState;

            public IActiveState ActiveState;

            [SerializeField, Interface(typeof(IPointableElement))]
            private UnityEngine.Object _pointableElement;

            public IPointableElement PointableElement;

            public void Initialize()
            {
                ActiveState = _activeState as IActiveState;
                PointableElement = _pointableElement as IPointableElement;

                Assert.IsNotNull(ActiveState, "Selection must have a valid ActiveState.");
                Assert.IsNotNull(PointableElement, "Selection must have a valid PointableElement.");
            }
        }
    }
}
