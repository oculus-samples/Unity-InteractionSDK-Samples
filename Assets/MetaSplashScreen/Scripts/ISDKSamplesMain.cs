/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Meta Platform Technologies SDK License Agreement (the "SDK License").
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

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace Oculus.Interaction.Samples
{
    public class ISDKSamplesMain : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _director;

        private SceneLoader _loader;
        private CustomOVRScreenFade _fader;
        private bool _onSplashScreen = true;
        private bool _wasOnSplashScreen = false;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneManagerLoaded;
        }

        void Start()
        {
            Assert.IsNotNull(_director);

            // run the timeline for a couple of frames to get the materials color properties set to nighttime
            StartCoroutine(RunNighttimeFadeInBriefly());
        }

        public void HandlePreLoadScene(string sceneName)
        {
            StartCoroutine(PlayAnimAndLoadScene(sceneName));
        }

        private void FindScreenFader()
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            Assert.IsNotNull(centerEye);
            if (_onSplashScreen || _wasOnSplashScreen)
            {
                _fader = centerEye.GetComponent<CustomOVRScreenFade>();
                if (_fader == null)
                {
                    _fader = centerEye.AddComponent<CustomOVRScreenFade>();
                }

                _fader.fadeTime = .35f;
                _fader.fadeOnStart = false;
                _fader.enabled = false;
                _fader.fadeColor = Color.white;
                // need to update color immediately as it might be magenta for a frame otherwise until the next update cycle
                _fader.SetExplicitFade(_onSplashScreen ? 0 : 1);
                Assert.IsNotNull(_fader);
            }
        }

        IEnumerator RunNighttimeFadeInBriefly()
        {
            // play the nighttime to daytime animation
            _director.Play();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            _director.Pause();
        }

        IEnumerator PlayAnimAndLoadScene(string sceneName)
        {
            _wasOnSplashScreen = _onSplashScreen;

            if (_onSplashScreen)
            {
                _onSplashScreen = false;
                _director.Play();
                yield return new WaitForSeconds((float)(_director.playableAsset.duration));

                // fade scene to white (only for splash screen)
                _fader.enabled = true;
                _fader.FadeOut();
                yield return new WaitForSeconds(_fader.fadeTime);
            }

            // now that we're done with our animation stuff let the loader know that it's okay to proceed with the actual loading of the next scene
            _loader.HandleReadyToLoad(sceneName);
        }

        void HandleSceneManagerLoaded(Scene scene, LoadSceneMode mode)
        {
            _loader = GameObject.FindObjectOfType<SceneLoader>();
            Assert.IsNotNull(_loader);

            // we want to do some fadey stuff and on the title screen we want to animate the examples button panel,
            // so ask the loader to wait for us to do our thing before it loads
            _loader.WhenLoadingScene += HandlePreLoadScene;

            // look for the Interaction SDK info and browser button panel, make it visible for the samples app
            SamplesInfoPanel[] panels = FindObjectsOfType<SamplesInfoPanel>(true);
            if (panels != null)
            {
                panels[0].gameObject.SetActive(true);
            }

            FindScreenFader();

            if (_wasOnSplashScreen && (_fader != null))
            {
                // new scene loaded, fade from white back to normal
                _fader.enabled = true;
                _fader.FadeIn();
            }
        }
    }
}
