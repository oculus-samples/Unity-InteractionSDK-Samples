// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    public class LoopSoundOnCollision : MonoBehaviour
    {
        public string targetTag = "Target";
        public AudioClip soundClip;
        private AudioSource _audioSource;

        void Start()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = soundClip;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0.2f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(targetTag))
            {
                if (!_audioSource.isPlaying)
                {
                    _audioSource.Play();
                }
            }
            else
            {
                _audioSource.Stop();
            }
        }

        void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag(targetTag))
            {
                _audioSource.Stop();
            }
        }
    }
}
