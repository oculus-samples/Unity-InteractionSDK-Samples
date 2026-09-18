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

using System.Collections;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class TabletopRoomFogVisibility : MonoBehaviour
    {
        [SerializeField, Interface(typeof(ISelector)), Optional]
        private Object _showSelector;

        private ISelector ShowSelector { get; set; }

        [SerializeField]
        private TabletopFogSettings _fog;

        [SerializeField]
        private TabletopCullingGroup _cullingGroup;


        private LightController[] _lights;
        private AudioSource[] _audioSources;

        private const int _chunkSize = 15;
        private Coroutine _transitionRoutine;

        private bool _wasShown;

        private void Awake()
        {
            if (!_fog || !_cullingGroup)
            {
                enabled = false;
                return;
            }

            _lights = GetComponentsInChildren<LightController>(true);
            _audioSources = GetComponentsInChildren<AudioSource>(true);

            ShowSelector = _showSelector as ISelector;
        }

        private void Start()
        {
            _wasShown = true;
            ShowRoom(false, true);
        }

        private void OnEnable()
        {
            if (ShowSelector != null)
            {
                ShowSelector.WhenSelected += ShowRoom;
                ShowSelector.WhenUnselected += HideRoom;
            }
        }

        private void OnDisable()
        {
            if (ShowSelector != null)
            {
                ShowSelector.WhenSelected -= ShowRoom;
                ShowSelector.WhenUnselected -= HideRoom;
            }
        }

        public void ShowRoom() => ShowRoom(true);
        public void HideRoom() => ShowRoom(false);

        public void ShowRoom(bool showRoom, bool instant = false)
        {
            if (showRoom == _wasShown) return;
            _wasShown = showRoom;

            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionRoutine(showRoom, instant));
        }

        private IEnumerator TransitionRoutine(bool showRoom, bool instant = false)
        {
            if (!showRoom)
            {
                if (instant)
                {
                    _fog.Strength = 1f;
                    _fog.Height = -1f;
                }
                else
                {
                    yield return FadeFogRoutine(1f, -1f);
                }
            }

            _cullingGroup.SetMasterVisibility(showRoom);
            yield return ShowRoutine(showRoom, instant);

            if (showRoom)
            {
                if (instant)
                {
                    _fog.Strength = 0f;
                    _fog.Height = -3f;
                }
                else
                {
                    yield return FadeFogRoutine(0f, -3f);
                }
            }

        }

        private IEnumerator FadeFogRoutine(float targetStrength, float targetHeight)
        {
            if (_fog == null) yield break;

            while (!Mathf.Approximately(_fog.Strength, targetStrength))
            {
                _fog.Strength = Mathf.MoveTowards(_fog.Strength, targetStrength, Time.deltaTime * 5);
                _fog.Height = Mathf.MoveTowards(_fog.Height, targetHeight, Time.deltaTime * 5 * 2f);
                yield return null;
            }

            _fog.Strength = targetStrength;
            _fog.Height = targetHeight;
        }

        private IEnumerator ShowRoutine(bool show, bool instant)
        {
            int itemsProcessed = 0;

            foreach (var light in _lights)
            {
                if (light) light.SetActive(show);
                if (!instant && ++itemsProcessed >= _chunkSize)
                {
                    itemsProcessed = 0;
                    yield return null;
                }
            }

            foreach (var source in _audioSources)
            {
                if (source) source.mute = !show;
                if (!instant && ++itemsProcessed >= _chunkSize)
                {
                    itemsProcessed = 0;
                    yield return null;
                }
            }
        }
    }
}
