// Architecture: Top-level game class that owns and wires all subsystems.
// Update loop:  PlayerController → Camera → WorldRenderer (rebuild dirty meshes)
// Draw loop:    apply BasicEffect matrices → WorldRenderer.Draw
//
// Controls:
//   WASD       — move              Space / LeftShift — fly up/down
//   Mouse      — look              Left click        — break block
//   Right click — place block      1/2/3             — select block type
//   F3         — toggle wireframe  Tab               — release mouse cursor
//   Escape     — quit

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vox.AI.Core;
using Vox.AI.Player;
using Vox.AI.Rendering;

namespace Vox.AI;

public class VoxGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private Vox.AI.World.World _world;
    private Camera _camera;
    private WorldRenderer _worldRenderer;
    private PlayerController _player;

    private BasicEffect _effect;
    private Texture2D _atlas;

    // Cached rasterizer states — created once to avoid per-frame allocations
    private RasterizerState _solidState;
    private RasterizerState _wireState;
    private bool            _wireframe;

    private KeyboardState _prevKeyboard;

    public VoxGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth  = 1280,
            PreferredBackBufferHeight = 720,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible        = false;
    }

    protected override void Initialize()
    {
        Window.Title = "Vox.AI — Voxel Prototype";

        float aspect = (float)_graphics.PreferredBackBufferWidth
                             / _graphics.PreferredBackBufferHeight;

        _world  = new Vox.AI.World.World();

        // Spawn above the terrain centre; terrain height is roughly 6–14 blocks
        _camera = new Camera(new Vector3(8f, 22f, 8f), aspect);

        _worldRenderer = new WorldRenderer(_world);
        _player        = new PlayerController(_camera, _world);

        _solidState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.Solid };
        _wireState  = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.WireFrame };

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _atlas = TextureGenerator.CreateAtlas(GraphicsDevice);

        _effect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled     = true,
            VertexColorEnabled = false,
            LightingEnabled    = false,
            Texture            = _atlas,
        };
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();

        if (kb.IsKeyDown(Keys.Escape))
            Exit();

        // F3 toggles wireframe rendering
        if (kb.IsKeyDown(Keys.F3) && !_prevKeyboard.IsKeyDown(Keys.F3))
            _wireframe = !_wireframe;

        _player!.Update(gameTime, this);
        _camera!.Update();

        // Rebuild meshes for any chunks dirtied by block edits this frame
        _worldRenderer!.Update(GraphicsDevice);

        _prevKeyboard = kb;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(135, 206, 235));  // sky blue

        GraphicsDevice.RasterizerState   = _wireframe ? _wireState! : _solidState!;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.SamplerStates[0]  = SamplerState.PointClamp;  // crisp pixel art look

        _effect!.View       = _camera!.View;
        _effect.Projection  = _camera.Projection;
        _effect.World       = Matrix.Identity;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _worldRenderer!.Draw(GraphicsDevice);
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _worldRenderer?.Dispose();
        _atlas?.Dispose();
        _effect?.Dispose();
        _solidState?.Dispose();
        _wireState?.Dispose();
        base.UnloadContent();
    }
}
