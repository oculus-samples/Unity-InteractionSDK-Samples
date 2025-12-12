// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Throw;
using Object = UnityEngine.Object;

namespace Meta.XR.InteractionSDK.Samples
{
    [RequireComponent(typeof(Rigidbody), typeof(Grabbable))]
    public sealed class PooledThrowable : MonoBehaviour
    {
        [SerializeField]
        private float _killPlaneY = -1f;
        [SerializeField]
        private float _sleepSecondsBeforeDespawn = 3f;

        private static readonly Dictionary<GameObject, ObjectPool<PooledThrowable>> s_pools = new();

        /// <summary>
        /// Reset static state when domain reloads (important for IL2CPP builds)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_pools.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            // Clear stale pool references.
            s_pools.Clear();
        }

        private ThrowTuner _tuner;
        private ObjectPool<PooledThrowable> _pool;
        private Rigidbody _rigidbody;
        private Grabbable _grabbable;
        private float _sleepTimer;
        private bool _started = false;

        private void Awake()
        {
            TryGetComponent(out _rigidbody);
            TryGetComponent(out _grabbable);
        }

        private void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_rigidbody, nameof(_rigidbody));
            this.AssertField(_grabbable, nameof(_grabbable));
            this.AssertField(_grabbable.VelocityThrow, nameof(_grabbable.VelocityThrow));
            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            // Enable physics before ThrowTuner applies throw profile (ThrowTuner has DefaultExecutionOrder=100)
            if (_started)
            {
                _grabbable.VelocityThrow.WhenThrown += HandleThrow;
            }
        }

        private void OnDisable()
        {
            if (_started)
            {
                _grabbable.VelocityThrow.WhenThrown -= HandleThrow;
            }
        }

        private void HandleThrow(Vector3 velocity, Vector3 torque)
        {
            // Set non-kinematic to enable physics
            _rigidbody.isKinematic = false;
        }

        public static PooledThrowable Get(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (!s_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<PooledThrowable>(
                    createFunc: () =>
                    {
                        var go = Instantiate(prefab);
                        // strip duplicate broadcasters once, across all children:
                        var bcs = go.GetComponentsInChildren<InteractableTriggerBroadcaster>(true);
                        for (int i = 1; i < bcs.Length; i++)
                            DestroyImmediate(bcs[i]);

                        var pt = go.GetComponent<PooledThrowable>() ?? go.AddComponent<PooledThrowable>();

                        pt._pool = pool;
                        pt._rigidbody = go.GetComponent<Rigidbody>();
                        pt._grabbable = go.GetComponent<Grabbable>();
                        pt._tuner = go.GetComponent<ThrowTuner>();

                        return pt;
                    },
                    actionOnGet: pt =>
                    {
                        pt.transform.SetPositionAndRotation(pos, rot);
                        pt.gameObject.SetActive(true);

                        // Reset the RigidbodyKinematicLocker counter if it exists
                        // This prevents corrupted kinematic state when pooled objects are reused
                        if (pt._rigidbody.TryGetComponent<RigidbodyKinematicLocker>(out var locker))
                        {
                            Object.Destroy(locker);
                        }

                        // Must set non-kinematic BEFORE clearing velocities, then set kinematic
                        pt._rigidbody.isKinematic = false;
                        pt._rigidbody.velocity = Vector3.zero;
                        pt._rigidbody.angularVelocity = Vector3.zero;
                        pt._rigidbody.isKinematic = true;
                        pt._rigidbody.WakeUp();
                        pt._sleepTimer = 0f;

                        // Force sync since autoSyncTransforms is disabled in project settings
                        Physics.SyncTransforms();
                    },
                    actionOnRelease: pt =>
                    {
                        // Reset the RigidbodyKinematicLocker counter if it exists
                        // This prevents corrupted kinematic state when pooled objects are reused
                        if (pt._rigidbody.TryGetComponent<RigidbodyKinematicLocker>(out var locker))
                        {
                            Object.Destroy(locker);
                        }

                        // Must set non-kinematic BEFORE clearing velocities, then set kinematic
                        pt._rigidbody.isKinematic = false;
                        pt._rigidbody.velocity = Vector3.zero;
                        pt._rigidbody.angularVelocity = Vector3.zero;
                        pt._rigidbody.isKinematic = true;
                        pt._rigidbody.Sleep();

                        pt.transform.SetParent(null, false);
                        pt.transform.localScale = Vector3.one;
                        pt.gameObject.SetActive(false);
                        pt._sleepTimer = 0f;
                    },
                    actionOnDestroy: pt => Destroy(pt.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 2,
                    maxSize: 6
                );
                s_pools[prefab] = pool;
            }
            return pool.Get();
        }

        public void Despawn()
        {
            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (_grabbable && _grabbable.SelectingPointsCount > 0)
            {
                _sleepTimer = 0f;
                return;
            }

            if (transform.position.y <= _killPlaneY)
            {
                Despawn();
                return;
            }

            if (_tuner && _tuner.InFlight)
            {
                _sleepTimer += Time.fixedDeltaTime;
            }
            else
            {
                _sleepTimer = 0f;
            }

            if (_sleepTimer > _sleepSecondsBeforeDespawn)
            {
                Despawn();
            }

        }
    }
}
