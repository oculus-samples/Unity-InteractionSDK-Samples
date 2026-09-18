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
using System.Collections.Generic;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class FirstSelectTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField]
        private List<Option> _options;

        private List<Option> _activeSelections = new();
        private ITransformer _activeTransformer;
        private bool _isTransforming;

        private void OnEnable()
        {
            foreach (var option in _options)
            {
                option.Initialize(this);
            }
        }

        private void OnDisable()
        {
            foreach (var option in _options)
            {
                option.Teardown();
            }

            _activeSelections.Clear();
            _activeTransformer = null;
        }

        private void HandleOptionSelected(Option option)
        {
            if (!_activeSelections.Contains(option))
            {
                _activeSelections.Add(option);
                UpdateActiveTransformer();
            }
        }

        private void HandleOptionUnselected(Option option)
        {
            if (_activeSelections.Contains(option))
            {
                _activeSelections.Remove(option);
                UpdateActiveTransformer();
            }
        }

        private void UpdateActiveTransformer()
        {
            var targetTransformer = _activeSelections.Count > 0 ? _activeSelections[0].Transformer : null;
            if (_activeTransformer == targetTransformer) return;

            if (_activeTransformer != null && _isTransforming)
                _activeTransformer.EndTransform();

            _activeTransformer = targetTransformer;

            if (_activeTransformer != null && _isTransforming)
            {
                _activeTransformer.BeginTransform();
                _activeTransformer.UpdateTransform();
            }
        }

        public void Initialize(IGrabbable grabbable)
        {
            foreach (var option in _options)
            {
                option.Transformer?.Initialize(grabbable);
            }
        }

        public void BeginTransform()
        {
            _isTransforming = true;
            _activeTransformer?.BeginTransform();
        }

        public void UpdateTransform()
        {
            _activeTransformer?.UpdateTransform();
        }

        public void EndTransform()
        {
            _isTransforming = false;
            _activeTransformer?.EndTransform();
        }



        [Serializable]
        [MetaCodeSample("ISDKSamples-ShowcaseSamples")]
        public class Option
        {
            [SerializeField, Interface(typeof(IInteractableView))]
            private MonoBehaviour _interactable;

            [SerializeField, Interface(typeof(ITransformer))]
            private MonoBehaviour _transformer;

            public IInteractableView Interactable => _interactable as IInteractableView;
            public ITransformer Transformer => _transformer as ITransformer;


            private FirstSelectTransformer _owner;

            public void Initialize(FirstSelectTransformer owner)
            {
                _owner = owner;
                Interactable.WhenStateChanged += HandleStateChanged;
            }

            public void Teardown()
            {
                Interactable.WhenStateChanged -= HandleStateChanged;
            }

            private void HandleStateChanged(InteractableStateChangeArgs args)
            {
                if (args.NewState == InteractableState.Select)
                {
                    _owner.HandleOptionSelected(this);
                }
                else if (args.PreviousState == InteractableState.Select)
                {
                    _owner.HandleOptionUnselected(this);
                }
            }
        }
    }
}
