using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VSLineRenderer
{
    public class Line : IDisposable
    {
        private ICoreClientAPI _capi;

        private List<Vec3d> _points;
        private EnumLineDrawMode _drawMode;

        private float _width;
        private Vec4f _color;

        private Matrixf _modelMat = new Matrixf();
        private MeshRef _segmentRef;

        public Line(ICoreClientAPI capi, List<Vec3d> points, float width, Vec4f color, EnumLineDrawMode drawMode)
        {
            _capi = capi;
            _points = points;

            _width = width;
            _color = color;
            
            _drawMode = drawMode;

            var segmentMesh = QuadMeshUtil.GetQuad();
            segmentMesh.SetXyz(
                new[]
                {
                    0.0f, -0.5f, 0.0f,
                    0.0f, -0.5f, 1.0f,
                    0.0f, 0.5f, 1.0f,
                    0.0f, 0.5f, 0.0f
                }
            );

            segmentMesh.CustomFloats = new CustomMeshDataPartFloat(_points.Count * 3)
            {
                Instanced = true,
                InterleaveOffsets = new[] {0, 12},
                InterleaveSizes = new[] {3, 3},
                InterleaveStride = _drawMode == EnumLineDrawMode.Lines ? 24 : 12,
                StaticDraw = false,
                Count = _points.Count * 3,
            };

            var index = 0;
            foreach (var offset in _points.Select(point => point - _points[0]))
            {
                segmentMesh.CustomFloats.Values[index++] = (float) offset.X;
                segmentMesh.CustomFloats.Values[index++] = (float) offset.Y;
                segmentMesh.CustomFloats.Values[index++] = (float) offset.Z;
            }

            _segmentRef = _capi.Render.UploadMesh(segmentMesh);
        }

        public void Render(float deltaTime, EnumRenderStage stage)
        {
            var shader = _capi.Render.CurrentActiveShader;
            var cameraPos = _capi.World.Player.Entity.CameraPos;
            var origin = (_points[0] - cameraPos).ToVec3f();

            _modelMat.Set(_capi.Render.CameraMatrixOriginf).Translate(origin.X, origin.Y, origin.Z).ReverseMul(_capi.Render.CurrentProjectionMatrix);
            var resolution = new Vec2f(_capi.Settings.Int["screenWidth"], _capi.Settings.Int["screenHeight"]);

            shader.UniformMatrix("modelViewProjection", _modelMat.Values);
            shader.Uniform("resolution", resolution);
            
            shader.Uniform("width", _width);
            shader.Uniform("color", _color);

            var segmentCount = _drawMode == EnumLineDrawMode.Lines ? _points.Count / 2 : _points.Count - 1;
            _capi.Render.RenderMeshInstanced(_segmentRef, segmentCount);
        }

        public void Dispose()
        {
            _segmentRef?.Dispose();
        }
    }
}