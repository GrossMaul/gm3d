using System.Globalization;
using System.Numerics;
using System.Text;
using SDL3;
using Smash;
using Smash.Graphics;
using Smash.Input;
using Color = System.Drawing.Color;

public class App : Application
{
    private Window _window;
    private Renderer _renderer;

    private float _aspect => _window.Width / _window.Height;

    private Dictionary<Vector3, Cube> _cubes = new();
    private Vector3 _cameraPos = new Vector3(0, 10, -100);

    private float _yaw;
    private float _pitch;

    private float _elapsedTime;
    private double _fps;

    private Matrix4x4 _camRotationMatrixY;
    private Matrix4x4 _camRotationMatrixX;

    private readonly Texture2D TextureAtlas;

    private Networker _networker;

    public App()
    {
        CreateWindowAndRenderer("Katzi plant", 1080, 720, out _window, out _renderer);
        _window.SetWindowResizable(true);

        AssetManager.SetAssetRootDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
        TextureAtlas = AssetManager.LoadTexture("TextureAtlas.png", _renderer);

        AssetManager.AddTextureRegion("Dirt", new TextureRegion("TextureAtlas", 0, 0, 16, 16));

        _renderer.SetVSyncEnabled(true);
        _renderer.SetRenderBlendMode(BlendMode.Blend);

        _camRotationMatrixY = Matrix4x4.CreateRotationY(_yaw);
        _camRotationMatrixX = Matrix4x4.CreateRotationY(_pitch);

        for (int x = 0; x < 50; x++)
        {
            for (int z = 0; z < 50; z++)
            {
                _cubes.Add(new Vector3(x, -1, z), new Cube(AssetManager.GetTexture("Dirt")));
            }
        }

        string ip = Environment.GetEnvironmentVariable("GM_SERVER_ADDRESS")!;
        _networker = new("Aarono", ip, 4389);
    }

    public override void Update(double deltaTime)
    {
        _elapsedTime += (float)deltaTime;
        if (_elapsedTime > 0.5f)
        {
            _fps = 1f / deltaTime;
            _elapsedTime = 0;
        }

        float cameraSpeed = InputHandler.IsKeyDown(SDL.Keycode.LShift) ? 150 : 20;

        Vector3 movementVector = new();

        Vector3 forward = new Vector3(
            MathF.Cos(_pitch) * MathF.Sin(-_yaw),
            MathF.Sin(_pitch),
            MathF.Cos(_pitch) * MathF.Cos(-_yaw)
        );

        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

        if (InputHandler.IsKeyDown(SDL.Keycode.W))
            movementVector += forward;

        if (InputHandler.IsKeyDown(SDL.Keycode.S))
            movementVector -= forward;

        if (InputHandler.IsKeyDown(SDL.Keycode.A))
            movementVector += right;

        if (InputHandler.IsKeyDown(SDL.Keycode.D))
            movementVector -= right;

        if (InputHandler.IsKeyDown(SDL.Keycode.Space))
            movementVector += new Vector3(0, 1, 0);

        if (InputHandler.IsKeyDown(SDL.Keycode.LCtrl))
            movementVector -= new Vector3(0, 1, 0);

        _cameraPos += movementVector * cameraSpeed * (float)deltaTime;

        if (InputHandler.IsKeyDown(SDL.Keycode.Left))
        {
            _yaw += (float)deltaTime * 1.5f;
            _camRotationMatrixY = Matrix4x4.CreateRotationY(_yaw);
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.Right))
        {
            _yaw -= (float)deltaTime * 1.5f;
            _camRotationMatrixY = Matrix4x4.CreateRotationY(_yaw);
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.Up))
        {
            _pitch += (float)deltaTime * 1.5f;
            _camRotationMatrixX = Matrix4x4.CreateRotationX(_pitch);
        }

        if (InputHandler.IsKeyDown(SDL.Keycode.Down))
        {
            _pitch -= (float)deltaTime * 1.5f;
            _camRotationMatrixX = Matrix4x4.CreateRotationX(_pitch);
        }
    }

    public override void Render()
    {
        _renderer.Clear(Color.Black);

        foreach (var kvp in _cubes)
        {
            RenderCube(_renderer, kvp.Key, kvp.Value.Texture);
        }

        _renderer.RenderDebugText(new Vector2(20), $"{(int)_fps}", Color.White);
        _renderer.RenderDebugText(new Vector2(20, 40), $"Position: {_cameraPos}", Color.White);

        Vector3 forward = new Vector3(
                MathF.Cos(_pitch) * MathF.Sin(-_yaw),
                MathF.Sin(_pitch),
                MathF.Cos(_pitch) * MathF.Cos(-_yaw)
        );

        _renderer.RenderDebugText(new Vector2(20, 60), $"Forward: {forward}", Color.White);
        _renderer.RenderDebugText(new Vector2(20, 80), $"Cubes amount: {_cubes.Count}", Color.White);

        _renderer.RenderPresent();
    }

    public override void End()
    {
        _networker.CloseConnection();
        _window.Dispose();
        _renderer.Dispose();
        AssetManager.Dispose();
    }

    private Vector2 Project(Vector3 point)
    {
        return new Vector2(point.X / point.Z / _aspect, point.Y / point.Z);
    }

    private Vector2 Screen(Vector2 point)
    {
        return new(
            (point.X + 1) / 2 * _window.Width,
            (1 - (point.Y + 1) / 2) * _window.Height
        );
    }

    private void RenderCube(Renderer renderer, Vector3 position, Texture2D texture)
    {
        SDL.Vertex[] vertices = new SDL.Vertex[Cube.Vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexPosition = Vector3.Transform(Cube.Vertices[i] + position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
            if (vertexPosition.Z < 0) return;

            Vector2 screenVertexPosition = Screen(Project(vertexPosition));

            vertices[i] = new SDL.Vertex()
            {
                Position = new SDL.FPoint { X = screenVertexPosition.X, Y = screenVertexPosition.Y },
                Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1 },
                TexCoord = new SDL.FPoint {  }
            };
        }

        SDL.RenderGeometry(renderer.Handle, texture.Handle, vertices, vertices.Length, Cube.Indices, Cube.Indices.Length);
    }
}
