// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Oculus.Interaction.Samples
{
    public sealed class PassthroughFix : MonoBehaviour
    {
        private bool _lastPassthroughState;

        private void Awake()
        {
            _lastPassthroughState = MRPassthrough.PassThrough.IsPassThroughOn;
        }

        private void Update()
        {
            bool passthroughRequested = MRPassthrough.PassThrough.IsPassThroughOn;
            if (passthroughRequested == _lastPassthroughState)
            {
                return;
            }

            _lastPassthroughState = passthroughRequested;

            if (passthroughRequested)
            {
                Camera mainCamera = OVRManager.FindMainCamera();
                if (mainCamera != null)
                {
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = Color.clear;
                }
            }
        }
    }
}
