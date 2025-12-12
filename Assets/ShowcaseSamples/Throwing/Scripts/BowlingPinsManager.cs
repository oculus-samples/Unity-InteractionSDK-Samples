// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    public class BowlingPinsManager : MonoBehaviour
    {
        public Transform[] objects;
        private Vector3[] savedPositions;
        private Quaternion[] savedRotations;

        bool isReset = false;

        /// <summary>
        /// Validates that a Vector3 contains no NaN or Infinity values
        /// </summary>
        private static bool IsValidVector3(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
                   !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }

        /// <summary>
        /// Validates that a Quaternion contains no NaN or Infinity values
        /// </summary>
        private static bool IsValidQuaternion(Quaternion q)
        {
            return !float.IsNaN(q.x) && !float.IsNaN(q.y) && !float.IsNaN(q.z) && !float.IsNaN(q.w) &&
                   !float.IsInfinity(q.x) && !float.IsInfinity(q.y) && !float.IsInfinity(q.z) && !float.IsInfinity(q.w);
        }

        void Start()
        {
            savedPositions = new Vector3[objects.Length];
            savedRotations = new Quaternion[objects.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    savedPositions[i] = objects[i].localPosition;
                    savedRotations[i] = objects[i].localRotation;

                    // Validate saved positions/rotations
                    if (!IsValidVector3(savedPositions[i]))
                    {
                        savedPositions[i] = Vector3.zero;
                    }

                    if (!IsValidQuaternion(savedRotations[i]))
                    {
                        savedRotations[i] = Quaternion.identity;
                    }

                    // Move pins off-screen initially using LOCAL position (not world position)
                    objects[i].localPosition = savedPositions[i] + new Vector3(0f, 1000f, 0f);
                }
            }
        }

        void Update()
        {
            if (transform.localScale.magnitude > 0.99f)
            {
                if (!isReset)
                {
                    StartCoroutine(PlaceAtSavedPositionsWithDelay());
                    isReset = true;
                }
            }

            if (transform.localScale.magnitude < 0.99f)
            {
                isReset = false;
            }
        }

        IEnumerator PlaceAtSavedPositionsWithDelay()
        {
            float delay = 0.11f;
            yield return new WaitForSeconds(delay);

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    // Validate that saved position is still valid before applying
                    if (!IsValidVector3(savedPositions[i]))
                    {
                        continue;
                    }

                    if (!IsValidQuaternion(savedRotations[i]))
                    {
                        savedRotations[i] = Quaternion.identity;
                    }

                    Vector3 targetPosition = savedPositions[i] + new Vector3(0f, 0.1f, 0f);

                    // Final validation before setting transform
                    if (!IsValidVector3(targetPosition))
                    {
                        continue;
                    }

                    // Get rigidbody first and prepare it BEFORE moving
                    Rigidbody rb = objects[i].GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.Sleep();
                    }

                    // Now set the transform
                    objects[i].localPosition = targetPosition;
                    objects[i].localRotation = savedRotations[i];

                    // Wake up the rigidbody after positioning
                    if (rb != null)
                    {
                        rb.WakeUp();
                    }

                    // Validate the transform was set correctly
                    if (!IsValidVector3(objects[i].localPosition))
                    {
                        objects[i].localPosition = savedPositions[i];
                        if (rb != null)
                        {
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }

            // Sync transforms after repositioning all pins
            Physics.SyncTransforms();
        }

        public void ResetAllPositionsWithDelay()
        {
            StartCoroutine(PlaceAtSavedPositionsWithDelay());
        }
    }
}
