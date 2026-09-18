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

using System.Collections.Generic;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class Door : MonoBehaviour
    {
        [SerializeField]
        private SnapInteractable _lockSnap;

        [SerializeField]
        private Key _key;

        [SerializeField]
        private TagSetFilter _filter;

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private List<Renderer> _renderers;

        [SerializeField]
        private string _colorProperty = "_EmissionColor";

        private MaterialPropertyBlock _mpb;

        private Color _prevColor;
        public Color Color => _color;
        public SnapInteractable LockSnapInteractable => _lockSnap;

        private void OnEnable()
        {
            SetColor(_color);
        }

        private void OnDisable()
        {
            if (_key) _key.gameObject.SetActive(false);
        }

        private void SetColor(Color color)
        {
            _filter.RemoveRequireTag(_prevColor.ToString());
            _filter.AddRequireTag(color.ToString());

            if (_renderers != null && _renderers.Count > 0)
            {
                _mpb ??= new MaterialPropertyBlock();

                foreach (var renderer in _renderers)
                {
                    if (!renderer) continue;
                    renderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor(_colorProperty, color);
                    renderer.SetPropertyBlock(_mpb);
                }
            }

            _prevColor = color;

            _key.SetDoor(this);
        }

        private void OnValidate()
        {
            if (!_key || !_filter) return;
            if (_prevColor != _color)
            {
                SetColor(_color);
            }
        }
    }
}
