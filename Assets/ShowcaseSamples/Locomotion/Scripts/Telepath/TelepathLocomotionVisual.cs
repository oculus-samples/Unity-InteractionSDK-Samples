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

using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This component draws a sequence of dots connecting the path that the <see cref="TelepathLocomotor"/>
    /// will use to reach the destination.
    /// </summary>
    public class TelepathLocomotionVisual : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The TelepathLocomotor whose path this component will visualize")]
        private TelepathLocomotor _telepathLocomotor;

        [SerializeField]
        [Tooltip("Color of the dots during hover")]
        private Color _hoverColor = Color.gray;

        [SerializeField]
        [Tooltip("Color of the dots during travel")]
        private Color _selectColor = Color.white;

        [SerializeField]
        [Tooltip("The size of the dots to be drawn")]
        private float _radius = 0.03f;

        [SerializeField]
        [Tooltip("The gap between the dots to be drawn")]
        private float _step = 0.5f;

        [SerializeField]
        [Tooltip("The material to use for drawing the dots")]
        private Material _material;

        [SerializeField, Range(2, 1023)]
        [Tooltip("The maximum number of dots that will be drawn")]
        private int _maxDots = 100;

        protected bool _started;
        private Mesh _mesh;
        private Matrix4x4[] _matrices;
        private Material _materialInstance;

        private static int _materialColor = Shader.PropertyToID("_Color");

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_telepathLocomotor, nameof(_telepathLocomotor));
            this.AssertField(_material, nameof(_material));

            _materialInstance = new Material(_material);
            _mesh = CreateFlatQuad(_radius);
            _matrices = new Matrix4x4[_maxDots];

            this.EndStart(ref _started);
        }

        protected virtual void LateUpdate()
        {
            if (_telepathLocomotor.PathCornersCount > 0)
            {
                _materialInstance.SetColor(_materialColor, _telepathLocomotor.HasTarget ? _selectColor : _hoverColor);
                DrawPath(_telepathLocomotor.PathCorners, _telepathLocomotor.PathCornersCount);
            }
        }

        private void DrawPath(Vector3[] corners, int cornersCount)
        {
            float sqrStep = _step * _step;
            int dotsCount = 0;
            float gap = _step;
            for (int i = 1; i < cornersCount; i++)
            {
                gap = RegisterSegment(corners[i - 1], corners[i], gap);
            }

            Graphics.DrawMeshInstanced(mesh: _mesh,
                submeshIndex: 0,
                material: _materialInstance,
                matrices: _matrices,
                count: dotsCount,
                properties: null,
                castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows: false,
                layer: gameObject.layer);


            float RegisterSegment(Vector3 start, Vector3 end, float gap)
            {
                Vector3 segment = end - start;
                float sqrDistance = segment.sqrMagnitude;
                Vector3 direction = segment.normalized;
                Vector3 current = start + direction * gap;
                Quaternion rotation = Quaternion.LookRotation(direction);
                Matrix4x4 matrix = Matrix4x4.TRS(current, rotation, Vector3.one);
                while ((current - start).sqrMagnitude <= sqrDistance && dotsCount < _maxDots)
                {
                    matrix.m03 = current.x;
                    matrix.m13 = current.y;
                    matrix.m23 = current.z;
                    _matrices[dotsCount++] = matrix;
                    current += direction * _step;
                }

                return (end - current).magnitude;
            }
        }

        private static Mesh CreateFlatQuad(float size = 0.5f)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-size, 0, -size),
                new Vector3( size, 0, -size),
                new Vector3( size, 0,  size),
                new Vector3(-size, 0,  size),
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
            };

            mesh.triangles = new int[]
            {
                0, 2, 1,
                0, 3, 2
            };

            mesh.RecalculateNormals();

            return mesh;
        }

        #region Inject
        public void InjectAllTelepathLocomotionVisual(TelepathLocomotor locomotionBroadcaster,
            Material material, float radius, float step, int maxDots)
        {
            InjectTelepathLocomotionBroadcaster(locomotionBroadcaster);
            InjectMaterial(material);
            InjectRadius(radius);
            InjectStep(step);
            InjectMaxDots(maxDots);
        }

        public void InjectTelepathLocomotionBroadcaster(TelepathLocomotor locomotionBroadcaster)
        {
            _telepathLocomotor = locomotionBroadcaster;
        }

        public void InjectMaterial(Material material)
        {
            _material = material;
        }

        public void InjectRadius(float radius)
        {
            _radius = radius;
        }

        public void InjectStep(float step)
        {
            _step = step;
        }

        public void InjectMaxDots(int maxDots)
        {
            _maxDots = Mathf.Clamp(maxDots, 2, 1023);
        }

        #endregion
    }
}
