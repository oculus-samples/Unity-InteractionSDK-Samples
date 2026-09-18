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
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(AudioSource))]
    [MetaCodeSample("ISDK-Tabletop")]
    public class AutoAudioSourceScaler : MonoBehaviour
    {
        private AudioSource _source;

        private float _setupMinDistance;
        private float _setupMaxDistance;

        private float _prevScale;

        [SerializeField]
        private bool _useNonLinearScaling;

        [SerializeField]
        private float _minScale = 0f;

        [SerializeField]
        private float _maxScale = 100f;

        [SerializeField]
        private AnimationCurve _scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private ScaleChangedNotifier _scaleChangedNotifier;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (!_source.spatialize)
            {
                enabled = false;
                return;
            }

            _setupMinDistance = _source.minDistance;
            _setupMaxDistance = _source.maxDistance;

            _scaleChangedNotifier = GetComponentInParent<ScaleChangedNotifier>();
            if (!_scaleChangedNotifier)
            {
                Debug.LogWarning($"{nameof(AutoAudioSourceScaler)} requires a {nameof(ScaleChangedNotifier)} in its parents to function. Disabling component.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_scaleChangedNotifier != null)
            {
                _scaleChangedNotifier.WhenScaleChanged += UpdateScale;
            }

            UpdateScale();
        }

        private void OnDisable()
        {
            if (_scaleChangedNotifier != null)
            {
                _scaleChangedNotifier.WhenScaleChanged -= UpdateScale;
            }
        }

        private void UpdateScale()
        {
            var currentScale = transform.lossyScale.x;
            if (Mathf.Approximately(currentScale, _prevScale)) return;

            ApplyScale(currentScale);
        }

        private void ApplyScale(float scale)
        {
            _prevScale = scale;
            if (scale <= 0f) return;

            if (_useNonLinearScaling)
            {
                var t = Mathf.InverseLerp(_minScale, _maxScale, Mathf.Clamp(scale, _minScale, _maxScale));
                scale = Mathf.Lerp(_minScale, _maxScale, _scaleCurve.Evaluate(t));
            }

            _source.minDistance = _setupMinDistance * scale;
            _source.maxDistance = _setupMaxDistance * scale;
        }
    }
}
