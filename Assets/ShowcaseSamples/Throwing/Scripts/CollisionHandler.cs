// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(AudioSource))]
    public class CollisionHandler : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _bounceAudio;
        [SerializeField] private AudioSource _winAudioSource;
        [SerializeField] private bool _toFreezeOnHit;

        private ScoreManager _scoreManager;
        private AudioSource _bounceAudioSource;
        private PooledThrowable _pooledThrowable;

        private void Awake()
        {
            _bounceAudioSource = GetComponent<AudioSource>();
            _bounceAudioSource.playOnAwake = false;
            _bounceAudioSource.volume = 0.4f;
            _pooledThrowable = GetComponent<PooledThrowable>();
        }

        private void Start()
        {
            _scoreManager = FindObjectOfType<ScoreManager>();

            if (_winAudioSource == null)
            {
                _winAudioSource = _scoreManager.GetComponent<AudioSource>();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Bouncable"))
            {
                PlayRandomBounceSound();
            }
            else if (collision.gameObject.CompareTag("Target"))
            {


                if (collision.GetContact(0).thisCollider.CompareTag("Handle"))
                    return;

                HitTarget(collision.gameObject, collision);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Target"))
            {
                HitTarget(other.gameObject);
            }
        }

        private void PlayRandomBounceSound()
        {
            if (_bounceAudio != null && _bounceAudio.Length > 0)
            {
                AudioClip clip = _bounceAudio[Random.Range(0, _bounceAudio.Length)];
                if (clip != null)
                {
                    _bounceAudioSource.PlayOneShot(clip);
                }
            }
        }

        private void HitTarget(GameObject targetObject, Collision collision = null)
        {
            if (_winAudioSource != null)
            {
                _winAudioSource.Play();
                _scoreManager.AddScore(transform.position);
            }

            if (_toFreezeOnHit)
            {
                if (TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = true;
                }

                transform.SetParent(targetObject.transform, true);

                StartCoroutine(ReturnToPool());
            }
        }

        private IEnumerator ReturnToPool()
        {
            yield return new WaitForSeconds(2f);
            transform.SetParent(null);
            _pooledThrowable.Despawn();
        }
    }
}
