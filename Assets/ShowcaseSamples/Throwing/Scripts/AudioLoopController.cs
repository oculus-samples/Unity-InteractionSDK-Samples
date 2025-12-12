// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [DisallowMultipleComponent]
    public class AudioLoopController : MonoBehaviour
    {
        public AudioClip audioClip;
        private AudioSource _audioSource;

        void Start()
        {
            _audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            _audioSource.clip = audioClip;
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0.2f;
        }

        public void PlayAudio()
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        public void StopAudio()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }
    }
}
