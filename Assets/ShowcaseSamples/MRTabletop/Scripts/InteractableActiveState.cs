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
using System.Linq;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class InteractableActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private MonoBehaviour _interactableView;

        private IInteractableView InteractableView;

        public int MinPointsCount = -1;
        public int MaxPointsCount = -1;
        public int MinSelectingPointsCount = 1;
        public int MaxSelectingPointsCount = -1;

        void Awake()
        {
            InteractableView = _interactableView as IInteractableView;
        }

        void Start()
        {
            InteractableView.WhenInteractorViewAdded += HandleInteractor;
            InteractableView.WhenInteractorViewRemoved += HandleInteractor;
            InteractableView.WhenSelectingInteractorViewAdded += HandleInteractor;
            InteractableView.WhenSelectingInteractorViewRemoved += HandleInteractor;
            HandleInteractor(null);
        }

        void OnDestroy()
        {
            InteractableView.WhenInteractorViewAdded -= HandleInteractor;
            InteractableView.WhenInteractorViewRemoved -= HandleInteractor;
            InteractableView.WhenSelectingInteractorViewAdded -= HandleInteractor;
            InteractableView.WhenSelectingInteractorViewRemoved -= HandleInteractor;
        }

        private void HandleInteractor(IInteractorView _)
        {
            var pointCount = InteractableView.InteractorViews.Count();
            var selectingPointCount = InteractableView.SelectingInteractorViews.Count();
            Active =
                (MinPointsCount < 0 || pointCount >= MinPointsCount) &&
                (MaxPointsCount < 0 || pointCount <= MaxPointsCount) &&
                (MinSelectingPointsCount < 0 || selectingPointCount >= MinSelectingPointsCount) &&
                (MaxSelectingPointsCount < 0 || selectingPointCount <= MaxSelectingPointsCount);
        }

        public bool Active { get; private set; }
    }
}
