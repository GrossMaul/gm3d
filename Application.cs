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
    private const float CIRCLE_STEP_SIZE = MathF.PI / 16;

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

    private Matrix4x4 _circleRotationMatrix = Matrix4x4.CreateRotationX(MathF.PI / 2);

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

    private void RenderCircleGround(Renderer renderer, Vector3 position, float radius)
    {
        List<SDL.Vertex> points = new();

        SDL.Vertex? firstVertex = GetCircleVertex(position, radius, 0);
        if (firstVertex == null) return;

        points.Add((SDL.Vertex)firstVertex);

        SDL.Vertex? centerVertex = GetCircleVertex(position, 0, 0);
        if (centerVertex == null) return;

        for (float i = 0; i < MathF.PI * 2; i += CIRCLE_STEP_SIZE)
        {
            points.Add((SDL.Vertex)centerVertex);

            SDL.Vertex? vertex = GetCircleVertex(position, radius, i);
            if (vertex == null) return;
            points.Add((SDL.Vertex)vertex);
            points.Add((SDL.Vertex)vertex);
        }

        points.Add((SDL.Vertex)centerVertex);
        points.Add((SDL.Vertex)firstVertex);

        SDL.SetRenderDrawColor(renderer.Handle, 255, 255, 255, 255);
        SDL.RenderGeometry(renderer.Handle, 0, points.ToArray(), points.Count, 0, 0);
    }

    //private Texture2D GetOrCreateCircleTexture(Renderer renderer, float radius)
    //{
    //    if (_circleTextures.TryGetValue(radius, out Texture2D? cachedTexture))
    //    {
    //        return cachedTexture;
    //    }

    //    nint texture = SDL.CreateTexture(renderer.Handle, SDL.PixelFormat.ABGR8888, SDL.TextureAccess.Target, (int)radius * 2, (int)radius * 2);
    //    SDL.SetRenderTarget(renderer.Handle, texture);

    //    List<SDL.Vertex> points = new();

    //    Vector2 centerPosition = new Vector2(radius);
    //    Vector2 firstVertexPosition = centerPosition + new Vector2(MathF.Cos(0), MathF.Sin(0)) * radius;

    //    SDL.Vertex firstVertex = new SDL.Vertex
    //    {
    //        Position = new SDL.FPoint { X = firstVertexPosition.X, Y = firstVertexPosition.Y },
    //        Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1 }
    //    };

    //    SDL.Vertex centerVertex = new SDL.Vertex
    //    {
    //        Position = new SDL.FPoint { X = centerPosition.X, Y = centerPosition.Y },
    //        Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1 }
    //    };

    //    points.Add(firstVertex);

    //    for (float i = 0; i < MathF.PI * 2; i += CIRCLE_STEP_SIZE)
    //    {
    //        points.Add(centerVertex);

    //        Vector2 vertexPosition = centerPosition + new Vector2(MathF.Cos(i), MathF.Sin(i)) * radius;
    //        SDL.Vertex vertex = new SDL.Vertex
    //        {
    //            Position = new SDL.FPoint { X = vertexPosition.X, Y = vertexPosition.Y },
    //            Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1 }
    //        };

    //        points.AddRange(vertex, vertex);
    //    }

    //    points.AddRange(centerVertex, firstVertex);

    //    SDL.SetRenderDrawColor(renderer.Handle, 255, 255, 255, 255);
    //    SDL.RenderGeometry(renderer.Handle, 0, points.ToArray(), points.Count, 0, 0);

    //    SDL.SetRenderTarget(renderer.Handle, IntPtr.Zero);

    //    Texture2D circleTexture = new Texture2D(texture, "circle");
    //    _circleTextures[radius] = circleTexture;
    //    return circleTexture;
    //}

    private SDL.Vertex? GetCircleVertex(Vector3 position, float radius, float radians)
    {
        Vector3 pos = Vector3.Transform(position + Vector3.Transform(new Vector3(MathF.Cos(radians), MathF.Sin(radians), 0), _circleRotationMatrix) * radius - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
        float cameraDistance = Vector3.Distance(_cameraPos, position);

        Vector2 screenPos = Screen(Project(pos));
        if (pos.Z < 0 || 1.1f - (cameraDistance / 100f) <= 0)
        {
            return null;
        }
        else
        {
            return new SDL.Vertex { Position = new SDL.FPoint { X = screenPos.X, Y = screenPos.Y }, Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1.1f - (cameraDistance / 100f) } };
        }
    }

    //private void RenderStar(Renderer renderer, Vector3 position, float rotation)
    //{
    //    Matrix4x4 rotationY = Matrix4x4.CreateRotationY(rotation);

    //    Vector3[] rotated = new Vector3[Star.Vertices.Length];
    //    for (int i = 0; i < rotated.Length; i++)
    //    {
    //        rotated[i] = Vector3.Transform(Star.Vertices[i], rotationY);
    //    }

    //    Vector3 topWorld = Vector3.Transform(rotated[0] + position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 bottomRightWorld = Vector3.Transform(rotated[1] + position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 topLeftWorld = Vector3.Transform(rotated[2] + position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 topRightWorld = Vector3.Transform(rotated[3] + position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 bottomLeftWorld = Vector3.Transform(rotated[4] + position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);

    //    if (topWorld.Z < 0 ||
    //        bottomRightWorld.Z < 0 ||
    //        topLeftWorld.Z < 0 ||
    //        topRightWorld.Z < 0 ||
    //        bottomLeftWorld.Z < 0) return;

    //    Vector2 top = Screen(Project(topWorld));
    //    Vector2 bottomRight = Screen(Project(bottomRightWorld));
    //    Vector2 topLeft = Screen(Project(topLeftWorld));
    //    Vector2 topRight = Screen(Project(topRightWorld));
    //    Vector2 bottomLeft = Screen(Project(bottomLeftWorld));

    //    List<Vector2> points = [
    //        top,
    //        bottomRight,
    //        topLeft,
    //        topRight,
    //        bottomLeft,
    //        top
    //    ];

    //    SDL.SetRenderDrawColor(renderer.Handle, 255, 255, 255, 255);
    //    SDL.RenderLines(renderer.Handle, Vector2ToFPoint(points), points.Count);
    //}

    //private void RenderCube(Renderer renderer, Cube cube)
    //{
    //    //Vector2 topLeft = Screen(Project(cube.Position - _cameraPos));
    //    //Vector2 topRight = Screen(Project(cube.Position + new Vector3(cube.Width, 0, 0) - _cameraPos));
    //    //Vector2 bottomLeft = Screen(Project(cube.Position + new Vector3(0, cube.Height, 0) - _cameraPos));
    //    //Vector2 bottomRight = Screen(Project(cube.Position + new Vector3(cube.Width, cube.Height, 0) - _cameraPos));

    //    //Vector2 backTopLeft = Screen(Project(cube.Position + new Vector3(0, 0, cube.Depth) - _cameraPos));
    //    //Vector2 backTopRight = Screen(Project(cube.Position + new Vector3(cube.Width, 0, cube.Depth) - _cameraPos));
    //    //Vector2 backBottomLeft = Screen(Project(cube.Position + new Vector3(0, cube.Height, cube.Depth) - _cameraPos));
    //    //Vector2 backBottomRight = Screen(Project(cube.Position + new Vector3(cube.Width, cube.Height, cube.Depth) - _cameraPos));

    //    Matrix4x4 rotationY = Matrix4x4.CreateRotationY(cube.Rotation);
    //    Matrix4x4 rotationX = Matrix4x4.CreateRotationX(cube.Rotation);

    //    Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(cube.Scale);

    //    Vector3[] rotatedCube = new Vector3[Cube.Vertices.Length];
    //    for (int i = 0; i < rotatedCube.Length; i++)
    //    {
    //        rotatedCube[i] = Vector3.Transform(Cube.Vertices[i], rotationY * rotationX * scaleMatrix);
    //    }

    //    //Vector3 topLeftWorld = Vector3.Transform(rotatedCube[0] + cube.Position, camRotationY) - _cameraPos;
    //    //Vector3 topRightWorld = Vector3.Transform(rotatedCube[1] + cube.Position, camRotationY) - _cameraPos;
    //    //Vector3 bottomLeftWorld = Vector3.Transform(rotatedCube[2] + cube.Position, camRotationY) - _cameraPos;
    //    //Vector3 bottomRightWorld = Vector3.Transform(rotatedCube[3] + cube.Position, camRotationY) - _cameraPos;

    //    //Vector3 backTopLeftWorld =  Vector3.Transform(rotatedCube[4] + cube.Position, camRotationY) - _cameraPos;
    //    //Vector3 backTopRightWorld = Vector3.Transform(rotatedCube[5] + cube.Position, camRotationY) - _cameraPos;
    //    //Vector3 backBottomLeftWorld = Vector3.Transform(rotatedCube[6] + cube.Position, camRotationY) - _cameraPos;
    //    //Vector3 backBottomRightWorld = Vector3.Transform(rotatedCube[7] + cube.Position, camRotationY) - _cameraPos;

    //    Vector3 topLeftWorld = Vector3.Transform(rotatedCube[0] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 topRightWorld = Vector3.Transform(rotatedCube[1] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 bottomLeftWorld = Vector3.Transform(rotatedCube[2] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 bottomRightWorld = Vector3.Transform(rotatedCube[3] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);

    //    Vector3 backTopLeftWorld = Vector3.Transform(rotatedCube[4] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 backTopRightWorld = Vector3.Transform(rotatedCube[5] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 backBottomLeftWorld = Vector3.Transform(rotatedCube[6] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);
    //    Vector3 backBottomRightWorld = Vector3.Transform(rotatedCube[7] + cube.Position - _cameraPos, _camRotationMatrixY * _camRotationMatrixX);

    //    if (topLeftWorld.Z < 0 ||
    //        topRightWorld.Z < 0 ||
    //        bottomLeftWorld.Z < 0 ||
    //        bottomRightWorld.Z < 0 ||
    //        backTopLeftWorld.Z < 0 ||
    //        backTopRightWorld.Z < 0 ||
    //        backBottomLeftWorld.Z < 0 ||
    //        backBottomRightWorld.Z < 0) return;

    //    SDL.FPoint topLeft = ScreenPoint(Project(topLeftWorld));
    //    SDL.FPoint topRight = ScreenPoint(Project(topRightWorld));
    //    SDL.FPoint bottomLeft = ScreenPoint(Project(bottomLeftWorld));
    //    SDL.FPoint bottomRight = ScreenPoint(Project(bottomRightWorld));

    //    SDL.FPoint backTopLeft = ScreenPoint(Project(backTopLeftWorld));
    //    SDL.FPoint backTopRight = ScreenPoint(Project(backTopRightWorld));
    //    SDL.FPoint backBottomLeft = ScreenPoint(Project(backBottomLeftWorld));
    //    SDL.FPoint backBottomRight = ScreenPoint(Project(backBottomRightWorld));

    //    SDL.FPoint[] points = new SDL.FPoint[16];

    //    //Front
    //    //renderer.RenderLine(topLeft, topRight, Color.White);
    //    //renderer.RenderLine(topLeft, bottomLeft, Color.White);
    //    //renderer.RenderLine(topRight, bottomRight, Color.White);
    //    //renderer.RenderLine(bottomLeft, bottomRight, Color.White);

    //    //points.AddRange(topLeft, topRight,
    //    //                topLeft, bottomLeft,
    //    //                topRight, bottomRight,
    //    //                bottomLeft, bottomRight);

    //    //Left
    //    //renderer.RenderLine(topLeft, backTopLeft, Color.White);
    //    //renderer.RenderLine(bottomLeft, backBottomLeft, Color.White);
    //    //renderer.RenderLine(backTopLeft, backBottomLeft, Color.White);

    //    //points.AddRange(topLeft, backTopLeft,
    //    //                bottomLeft, backBottomLeft,
    //    //                backTopLeft, backBottomLeft);

    //    //Right
    //    //renderer.RenderLine(topRight, backTopRight, Color.White);
    //    //renderer.RenderLine(bottomRight, backBottomRight, Color.White);
    //    //renderer.RenderLine(backTopRight, backBottomRight, Color.White);

    //    //points.AddRange(topRight, backTopRight,
    //    //                bottomRight, backBottomRight,
    //    //                backTopRight, backBottomRight);

    //    //Back
    //    //renderer.RenderLine(backTopLeft, backTopRight, Color.White);
    //    //renderer.RenderLine(backBottomLeft, backBottomRight, Color.White);

    //    //points.AddRange(backTopLeft, backTopRight,
    //    //                backBottomLeft, backBottomRight);

    //    points[0] = topLeft;
    //    points[1] = topRight;
    //    points[2] = bottomRight;
    //    points[3] = bottomLeft; 
    //    points[4] = topLeft;
    //    points[5] = backTopLeft;
    //    points[6] = backBottomLeft;
    //    points[7] = bottomLeft;
    //    points[8] = backBottomLeft;
    //    points[9] = backBottomRight;
    //    points[10] = backTopRight;
    //    points[11] = backTopLeft;
    //    points[12] = backTopRight;
    //    points[13] = topRight;
    //    points[14] = bottomRight;
    //    points[15] = backBottomRight;

    //    SDL.SetRenderDrawColor(renderer.Handle, cube.Color.R, cube.Color.G, cube.Color.B, 255);
    //    SDL.RenderLines(renderer.Handle, points, 16);
    //    //SDL.RenderGeometry(renderer.Handle, IntPtr.Zero, Vector2ToFPoint(points.ToArray()), points.Count, 0, 0);
    //}

    private SDL.FPoint[] Vector2ToFPoint(List<Vector2> vector2s)
    {
        List<SDL.FPoint> points = new();
        foreach (Vector2 vector2 in vector2s)
        {
            points.Add(new SDL.FPoint { X = vector2.X, Y = vector2.Y });
        }

        return points.ToArray();
    }

    private SDL.FPoint[] Vector2ToFPoint(Vector2[] vector2s)
    {
        List<SDL.FPoint> points = new();
        foreach (Vector2 vector2 in vector2s)
        {
            points.Add(new SDL.FPoint { X = vector2.X, Y = vector2.Y });
        }

        return points.ToArray();
    }

    private SDL.Vertex[] Vector2ToVertex(List<Vector2> vector2s)
    {
        List<SDL.Vertex> points = new();
        foreach (Vector2 vector2 in vector2s)
        {
            points.Add(new SDL.Vertex { Position = new SDL.FPoint { X = vector2.X, Y = vector2.Y }, Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1 } });
        }

        return points.ToArray();
    }

    private SDL.Vertex[] Vector2ToVertex(Vector2[] vector2s)
    {
        List<SDL.Vertex> points = new();

        foreach (Vector2 vector2 in vector2s)
        {
            points.Add(new SDL.Vertex { Position = new SDL.FPoint { X = vector2.X, Y = vector2.Y }, Color = new SDL.FColor { R = 1, G = 1, B = 1, A = 1 } });
        }

        return points.ToArray();
    }

    private (Vector3[], int[]) OBJToVector3(string obj)
    {
        List<Vector3> vertices = new();
        List<int> indices = new();

        string[] lines = obj.Split("\n");
        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                string e = line.Remove(0, 2);

                StringBuilder sb = new();
                List<float> floats = new();

                for (int i = 0; i < e.Length; i++)
                {
                    if (e[i] != ' ') sb.Append(e[i]);
                    else
                    {
                        floats.Add(float.Parse(sb.ToString(), CultureInfo.InvariantCulture.NumberFormat));
                        sb.Clear();
                    }
                }

                floats.Add(float.Parse(sb.ToString(), CultureInfo.InvariantCulture.NumberFormat));
                sb.Clear();

                vertices.Add(new Vector3(floats[0], floats[1], floats[2]));
            }

            //if (line.StartsWith("f "))
            //{
            //    string e = line.Remove(0, 2);

            //    string[] blocks = e.Split(" ");

            //    StringBuilder sb = new();
            //    List<int> lineIndices = new();

            //    for (int i = 0; i < blocks.Length; i++)
            //    {
            //        for (int j = 0; j < blocks[i].Length; j++)
            //        {
            //            if (blocks[i][j] != '/')
            //            {
            //                sb.Append(blocks[i][j]);
            //            }
            //            else
            //            {
            //                lineIndices.Add(int.Parse(sb.ToString(), CultureInfo.InvariantCulture.NumberFormat) - 1);
            //                sb.Clear();
            //                break;
            //            }
            //        }
            //    }

            //    indices.AddRange(lineIndices);
            //}
        }

        return (vertices.ToArray(), indices.ToArray());
    }
}
