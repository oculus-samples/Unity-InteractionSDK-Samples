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
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class InteractableStateScaleVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private MonoBehaviour _interactable;

        [SerializeField]
        private float _lerpSpeed = 10f;

        [SerializeField]
        private StateVisual<Vector3> _localScale = new(Vector3.one);

        [Header("Dynamic Scale")]
        [Tooltip(
            "Will scale the transform based on distance to the player. Additional to the _localScale defined by the state.")]
        [SerializeField]
        private bool _useDynamicScaling = false;

        [SerializeField]
        private float _minDistance = 1f, _maxDistance = 5f;

        [SerializeField]
        private Vector3 _minScaleMult = Vector3.one * 0.1f;

        [SerializeField]
        private Vector3 _maxScaleMult = Vector3.one;

        [SerializeField]
        private AnimationCurve _scaleCurve;

        private float _dynamicScalingInterval = 0.5f;
        private float _timeSinceLastDynamicUpdate;

        private IInteractableView Interactable;
        private Vector3 _currentScale, _targetScale;
        private Vector3 _currentMult = Vector3.one;
        private Camera _cam;

        private void Awake()
        {
            Interactable = _interactable as IInteractableView;
        }

        private void OnEnable()
        {
            if (Interactable == null)
            {
                Debug.Log($"Interactable not setup correctly. Disabling component.");
                enabled = false;
                return;
            }

            Interactable.WhenStateChanged += HandleStateChange;
            _currentScale = _targetScale = _localScale.GetVisualForState(Interactable.State);

            if (_useDynamicScaling) _timeSinceLastDynamicUpdate = Random.Range(0f, _dynamicScalingInterval);
            _requireUpdate = true;
            UpdateVisual();
        }

        private void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.WhenStateChanged -= HandleStateChange;
            }
        }

        private void HandleStateChange(InteractableStateChangeArgs args)
        {
            _targetScale = _localScale.GetVisualForState(Interactable.State);
        }


        private bool _requireUpdate;
        private void Update() => UpdateVisual();

        private void UpdateVisual()
        {
            if (_targetScale != _currentScale)
            {
                _currentScale = Vector3.Lerp(_currentScale, _targetScale, Time.deltaTime * _lerpSpeed);
                if ((_targetScale - _currentScale).sqrMagnitude < 0.0001f)
                {
                    _currentScale = _targetScale;
                }

                _requireUpdate = true;

            }

            if (UpdateDynamicScale()) _requireUpdate = true;

            if (_requireUpdate)
            {
                transform.localScale = Vector3.Scale(_currentScale, _currentMult);
            }
        }


        private bool UpdateDynamicScale()
        {
            if (!_useDynamicScaling) return false;

            _timeSinceLastDynamicUpdate += Time.deltaTime;
            if (_timeSinceLastDynamicUpdate < _dynamicScalingInterval) return false;
            _timeSinceLastDynamicUpdate = 0f;

            if (!_cam) _cam = Camera.main;
            if (!_cam) return false;

            var diff = Vector3.Distance(_cam.transform.position, transform.position);
            var t = Mathf.Clamp01(Mathf.InverseLerp(_minDistance, _maxDistance, diff));
            _currentMult = Vector3.Lerp(_minScaleMult, _maxScaleMult, _scaleCurve.Evaluate(t));

            return true;
        }


        [Serializable]
        [MetaCodeSample("ISDKSamples-ShowcaseSamples")]
        private class StateVisual<T>
        {
            public T Normal, Hover, Select, Disable;

            public T GetVisualForState(InteractableState state)
            {
                return state switch
                {
                    InteractableState.Normal => Normal,
                    InteractableState.Hover => Hover,
                    InteractableState.Select => Select,
                    InteractableState.Disabled => Disable,
                    _ => Normal
                };
            }

            public StateVisual(T defaultValue)
            {
                Normal = Hover = Select = Disable = defaultValue;
            }
        }
    }
}
