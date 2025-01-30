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
using TMPro;

public class StepsMove : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI GearText;
    private int gear;
    public bool isMoving;
    [SerializeField] private AudioSource Click;
    [SerializeField] private Material _offMaterial;
    [SerializeField] private Material _onMaterial;
    [SerializeField] private Renderer[] _materialList;
    // Start is called before the first frame update
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
        if (transform.localPosition.x < -0.0375f) gear = 4;
        else if (transform.localPosition.x < -0.0125f) gear = 3;
        else if (transform.localPosition.x < 0.0125f) gear = 2;
        else if (transform.localPosition.x < 0.0375f) gear = 1;
        else gear = 0;

        foreach (var item in _materialList)
        {
            item.material = _offMaterial;
        }
        _materialList[gear].material = _onMaterial;

        if (GearText.text != gear.ToString())
        {
            Click.Play();
            GearText.text = gear.ToString();
        }
    }

    public void SnapToPlace()
    {
        float newLocalPosX = 0f;

        if (gear == 4) newLocalPosX = -0.06f;
        else if (gear == 3) newLocalPosX = -0.025f;
        else if (gear == 2) newLocalPosX = 0f;
        else if (gear == 1) newLocalPosX = 0.025f;
        else newLocalPosX = 0.06f;

        transform.localPosition = new Vector3(newLocalPosX, 0f, 0f);
    }

    public void SetMoving(bool _isMoving)
    {
        isMoving = _isMoving;
    }
}
