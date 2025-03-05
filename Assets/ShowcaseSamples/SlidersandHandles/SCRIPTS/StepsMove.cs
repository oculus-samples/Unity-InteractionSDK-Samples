/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Meta Platform Technologies SDK License Agreement (the “SDK License”).
 * You may not use the MPT SDK except in compliance with the SDK License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the SDK License at
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the MPT SDK
 * distributed under the SDK License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the SDK License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using TMPro;

namespace Meta.XR.InteractionSDK.Samples
{
    public class StepsMove : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gearText;
        [SerializeField] private AudioSource _click;
        [SerializeField] private Material _offMaterial;
        [SerializeField] private Material _onMaterial;
        [SerializeField] private Renderer[] _materialList;

        private bool _isMoving;
        private int _gear;

        void Start()
        {
            CheckStatus();
        }

        private void Update()
        {
            if (_isMoving)
            {
                CheckStatus();
            }
            else
            {
                SnapToPlace();
            }
        }

        private void CheckStatus()
        {
            if (transform.localPosition.x < -0.0375f) _gear = 4;
            else if (transform.localPosition.x < -0.0125f) _gear = 3;
            else if (transform.localPosition.x < 0.0125f) _gear = 2;
            else if (transform.localPosition.x < 0.0375f) _gear = 1;
            else _gear = 0;

            foreach (var item in _materialList)
            {
                item.material = _offMaterial;
            }

            _materialList[_gear].material = _onMaterial;

            if (_gearText.text != _gear.ToString())
            {
                _click.Play();
                _gearText.text = _gear.ToString();
            }
        }

        private void SnapToPlace()
        {
            float newLocalPosX = 0f;

            switch (_gear)
            {
                case 4:
                    newLocalPosX = -0.06f;
                    break;
                case 3:
                    newLocalPosX = -0.025f;
                    break;
                case 2:
                    newLocalPosX = 0f;
                    break;
                case 1:
                    newLocalPosX = 0.025f;
                    break;
                default:
                    newLocalPosX = 0.06f;
                    break;
            }

            transform.localPosition = new Vector3(newLocalPosX, 0f, 0f);
        }

        public void SetMoving(bool setMoving)
        {
            _isMoving = setMoving;
        }
    }
}
