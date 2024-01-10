using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VSLineRenderer
{
    public class VSLineRenderer : ModSystem, IRenderer
    {
        private ICoreClientAPI _capi;

        private IShaderProgram _shader;

        private List<Line> _lines = new List<Line>();

        public double RenderOrder
        {
            get { return 0.5f; }
        }

        public int RenderRange
        {
            get { return 1; }
        }


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            _capi = api;

            _capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition);
            _capi.Event.ReloadShader += OnReloadShader;

            _capi.Event.LevelFinalize += OnLevelFinalize;
        }

        private void OnLevelFinalize()
        {
            var startPos = _capi.World.Player.Entity.Pos.XYZ;

            #region Cube Geometry

            var vertices = new []
            {
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 1.0f,
                1.0f, 1.0f, 1.0f,
                1.0f, 1.0f, 0.0f
            };

            var indices = new []
            {
                0, 1,
                1, 2,
                2, 3,
                3, 0,
                4, 5,
                5, 6,
                6, 7,
                7, 4,
                0, 4,
                1, 5,
                2, 6,
                3, 7
            };
            #endregion

            List<Vec3d> points = new List<Vec3d>();

            foreach (var p in indices)
            {
                var index = p * 3;
                var point = new Vec3d(vertices[index] * 10, vertices[index + 1] * 10, vertices[index + 2] * 10);
                    
                points.Add(point.Add(startPos));
            }
            
            _lines.Add(new Line(_capi, points, 50.0f, new Vec4f(1.0f, 0.0f, 0.0f, 1.0f), EnumLineDrawMode.Lines));
        }

        private bool OnReloadShader()
        {
            _shader?.Dispose();

            _shader = _capi.Shader.NewShaderProgram();
            
            _shader.VertexShader = _capi.Shader.NewShader(EnumShaderType.VertexShader);
            _shader.VertexShader.Code = _capi.Assets.Get(new AssetLocation(Mod.Info.ModID, "shaders/_lines.vsh")).ToText();

            _shader.FragmentShader = _capi.Shader.NewShader(EnumShaderType.FragmentShader);
            _shader.FragmentShader.Code = _capi.Assets.Get(new AssetLocation(Mod.Info.ModID, "shaders/_lines.fsh")).ToText();

            _capi.Shader.RegisterMemoryShaderProgram("_lines", _shader);

            return _shader.Compile();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (_lines.Count == 0) return;
            
            var activeShader = _capi.Render.CurrentActiveShader;
            activeShader?.Stop();

            _shader.Use();
            foreach (var line in _lines) line.Render(deltaTime, stage);
            _shader.Stop();

            activeShader?.Use();
        }

        public override void Dispose()
        {
            _capi.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
            _capi.Event.ReloadShader -= OnReloadShader;

            foreach (var line in _lines) line.Dispose();
        }
    }
}