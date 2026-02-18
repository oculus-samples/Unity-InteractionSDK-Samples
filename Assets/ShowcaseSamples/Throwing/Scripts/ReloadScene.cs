// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Throwing")]
    public class ReloadScene : MonoBehaviour
    {
        public void ReloadCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
