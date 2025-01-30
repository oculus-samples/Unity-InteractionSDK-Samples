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

using UnityEngine;

public class ToggleMove : MonoBehaviour
{
    [SerializeField] private GameObject OnLabel;
    [SerializeField] private GameObject OffLabel;
    private bool isOn;
    public bool isMoving;
    [SerializeField] private AudioSource Click;
    [SerializeField] private GameObject _toggleHandle;

    void Start()
    {
        CheckStatus();
    }

    private void Update()
    {
        if (isMoving)
        {
            CheckStatus();
        }
        else
        {
            SnapToPlace();
        }
    }

    public void CheckStatus()
    {
        if (_toggleHandle.transform.localPosition.x > 0)
        {
            if (!isOn)
            {
                isOn = true;
                OnLabel.SetActive(true);
                OffLabel.SetActive(false);
                Click.Play();
            }
        }
        else
        {
            if (isOn)
            {
                isOn = false;
                OnLabel.SetActive(false);
                OffLabel.SetActive(true);
                Click.Play();
            }
        }
    }

    public void SnapToPlace()
    {

        if (isOn)
        {
            _toggleHandle.transform.localPosition = new Vector3(0.06f, 0, 0);
            OnLabel.SetActive(true);
            OffLabel.SetActive(false);
        }
        else
        {
            _toggleHandle.transform.localPosition = new Vector3(-0.06f, 0, 0);
            OnLabel.SetActive(false);
            OffLabel.SetActive(true);
        }
    }

    public void SetMoving(bool _isMoving)
    {
        isMoving = _isMoving;
    }
}
