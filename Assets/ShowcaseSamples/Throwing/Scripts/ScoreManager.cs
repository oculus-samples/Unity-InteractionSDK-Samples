// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;
using TMPro;

namespace Meta.XR.InteractionSDK.Samples
{
    public class ScoreManager : MonoBehaviour
    {
        public TextMeshProUGUI ButtonText;

        private int _score = 0;

        private void Start()
        {
            ButtonText.text = _score.ToString();
        }

        public void AddScore(Vector3 position)
        {
            _score += 1;
            ButtonText.text = _score.ToString();
        }
    }
}
