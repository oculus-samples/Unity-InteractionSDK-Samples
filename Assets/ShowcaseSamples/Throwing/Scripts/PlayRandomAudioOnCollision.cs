// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    public class PlayRandomAudioOnCollision : MonoBehaviour
    {
        public AudioClip[] audioClips;
        private AudioSource _audioSource;
        private bool _hasPlayed = false;
        public float volume = 0.4f;
        private SwapTargets _swapTargets;

        void Start()
        {
            _swapTargets = FindObjectOfType<SwapTargets>();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.volume = volume;
        }

        void OnCollisionEnter(Collision collision)
        {
            // Check if the audio has not already been played during this collision
            if (!_hasPlayed && audioClips.Length > 0)
            {
                int randomIndex = Random.Range(0, audioClips.Length);
                _audioSource.clip = audioClips[randomIndex];
                if (!_audioSource.isPlaying) _audioSource.Play();

                _hasPlayed = true;
            }
        }

        void OnCollisionExit(Collision collision)
        {
            _hasPlayed = false;
        }
    }
}
