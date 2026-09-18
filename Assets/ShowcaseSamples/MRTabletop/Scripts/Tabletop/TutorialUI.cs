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
using UnityEngine;
using UnityEngine.UI;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class TutorialUI : MonoBehaviour
    {
        [SerializeField]
        private Toggle _dismissButton;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private float _fadeDuration = 0.3f;


        private void OnEnable()
        {
            _dismissButton.onValueChanged.AddListener(HandleDismissClicked);
        }

        private void OnDisable()
        {
            if (_dismissButton) _dismissButton.onValueChanged.RemoveListener(HandleDismissClicked);
        }

        private void HandleDismissClicked(bool newValue)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            StartCoroutine(DismissRoutine());
        }

        private IEnumerator DismissRoutine()
        {
            var fromAlpha = _canvasGroup.alpha;
            var elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(fromAlpha, 0f, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
