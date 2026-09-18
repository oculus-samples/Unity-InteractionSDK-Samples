/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Meta.XR.InteractionSDK.Samples
{
    [MetaCodeSample("ISDK-Tabletop")]
    public class ConstrainedTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField, Interface(typeof(ITransformer))]
        private MonoBehaviour _transformer;

        [SerializeField]
        private float maxDistance = 3;

        [SerializeField]
        [Tooltip("One Euro filter tuning for position. Decrease min cutoff to remove jitter, increase beta to reduce lag.")]
        private OneEuroFilterPropertyBlock _positionFilterProperties = new OneEuroFilterPropertyBlock(2f, 3f);

        [SerializeField]
        [Tooltip("One Euro filter tuning for rotation.")]
        private OneEuroFilterPropertyBlock _rotationFilterProperties = new OneEuroFilterPropertyBlock(2f, 3f);

        [SerializeField]
        [Tooltip("One Euro filter tuning for scale.")]
        private OneEuroFilterPropertyBlock _scaleFilterProperties = new OneEuroFilterPropertyBlock(2f, 3f);

        private ITransformer Transformer;
        private IGrabbable _grabbable;

        private IOneEuroFilter<Vector3> _positionFilter;
        private IOneEuroFilter<Quaternion> _rotationFilter;
        private IOneEuroFilter<Vector3> _scaleFilter;

        public void Initialize(IGrabbable grabbable)
        {
            Transformer = _transformer as ITransformer;
            _grabbable = grabbable;
            _positionFilter = OneEuroFilter.CreateVector3();
            _rotationFilter = OneEuroFilter.CreateQuaternion();
            _scaleFilter = OneEuroFilter.CreateVector3();
            Transformer.Initialize(grabbable);
        }

        public void BeginTransform()
        {
            _positionFilter.Reset();
            _rotationFilter.Reset();
            _scaleFilter.Reset();
            Transformer.BeginTransform();
        }

        public void EndTransform()
        {
            Transformer.EndTransform();
        }


        public void UpdateTransform()
        {
            Transformer.UpdateTransform();
            var transform = _grabbable.Transform;
            var deltaTime = Time.deltaTime;

            _positionFilter.SetProperties(_positionFilterProperties);
            _rotationFilter.SetProperties(_rotationFilterProperties);
            _scaleFilter.SetProperties(_scaleFilterProperties);

            transform.localScale = _scaleFilter.Step(transform.localScale, deltaTime);

            var position = _positionFilter.Step(transform.localPosition, deltaTime);
            transform.localPosition = Vector3.MoveTowards(Vector3.zero, position,
                maxDistance * transform.localScale.x * transform.localScale.x);

            var rotation = transform.localRotation;
            if (Quaternion.Dot(_rotationFilter.Value, rotation) < 0f)
            {
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
            }
            transform.localRotation = _rotationFilter.Step(rotation, deltaTime);
        }
    }
}
