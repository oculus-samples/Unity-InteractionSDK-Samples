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
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class Key : MonoBehaviour
    {
        [SerializeField]
        private SnapInteractor _snap;

        [SerializeField]
        private TagSet _tagSet;

        private Color _prevColor;
        private Door _door;

        [SerializeField]
        [Tooltip(
            "Snap mgiht have movement that takes time. Wait for movement to finish before triggering lock animations.")]
        private VirtualActiveState _insideLock;

        private Coroutine _checkLockRoutine;

        public void SetDoor(Door door)
        {
            _door = door;
            SetColor(door.Color);
        }

        public void HandleFound()
        {
            _snap.SetComputeCandidateOverride(() => _door.LockSnapInteractable, false);
            _snap.SetComputeShouldSelectOverride(() => true, false);
            _snap.SetComputeShouldUnselectOverride(() => false, false);
        }

        private void Update()
        {
            if (_insideLock.Active) return;
            if (_snap.State != InteractorState.Select) return;

            _door.LockSnapInteractable.PoseForInteractor(_snap, out var targetPose);
            if (Vector3.Distance(targetPose.position, transform.position) > 0.01f) return;
            _insideLock.Active = true;
        }

        private void SetColor(Color color)
        {
            _tagSet.RemoveTag(_prevColor.ToString());
            _tagSet.AddTag(color.ToString());

            _prevColor = color;
        }
    }
}
