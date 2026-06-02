// Architecture: Handles all player input in one place: mouse look, WASD + Space movement,
// and block interaction via a simple DDA-style raycast.
// Movement is always horizontal-relative to camera yaw so the player doesn't fly when looking up.
// Tab toggles mouse capture (useful for debugging without restarting).
// Keys 1/2/3 select the block type to place.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vox.AI.Blocks;
using Vox.AI.Core;

namespace Vox.AI.Player;

public sealed class PlayerController
{
    private readonly Camera              _camera;
    private readonly Vox.AI.World.World  _world;

    public const float MoveSpeed        = 8f;
    public const float MouseSensitivity = 0.003f;
    public const float RaycastDistance  = 6f;
    public const float RaycastStep      = 0.05f;

    private MouseState    _prevMouse;
    private KeyboardState _prevKeyboard;
    private bool          _mouseCaptured = true;

    /// <summary>Block type placed on right-click. Change with keys 1–3.</summary>
    public byte PlaceBlockId { get; set; } = BlockId.Dirt;

    public PlayerController(Camera camera, Vox.AI.World.World world)
    {
        _camera = camera;
        _world  = world;
    }

    public void Update(GameTime gameTime, Game game)
    {
        float         dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState kb = Keyboard.GetState();
        MouseState    ms = Mouse.GetState();

        HandleMouseCapture(kb, game);
        HandleBlockTypeSelection(kb);

        if (_mouseCaptured)
        {
            HandleMouseLook(ms, game.GraphicsDevice.Viewport);
            HandleMovement(kb, dt);
            HandleBlockInteraction(ms);
        }

        _prevMouse    = ms;
        _prevKeyboard = kb;
    }

    // -----------------------------------------------------------------------
    // Mouse capture
    // -----------------------------------------------------------------------

    private void HandleMouseCapture(KeyboardState kb, Game game)
    {
        // Tab toggles cursor visibility / mouse capture
        if (kb.IsKeyDown(Keys.Tab) && !_prevKeyboard.IsKeyDown(Keys.Tab))
        {
            _mouseCaptured      = !_mouseCaptured;
            game.IsMouseVisible = !_mouseCaptured;
        }
    }

    // -----------------------------------------------------------------------
    // Mouse look
    // -----------------------------------------------------------------------

    private void HandleMouseLook(MouseState ms, Viewport viewport)
    {
        int cx = viewport.Width  / 2;
        int cy = viewport.Height / 2;

        int dx = ms.X - cx;
        int dy = ms.Y - cy;

        _camera.Yaw   += dx * MouseSensitivity;   // mouse right → turn right
        _camera.Pitch -= -dy * MouseSensitivity;   // mouse up (dy<0) → look up

        Mouse.SetPosition(cx, cy);
    }

    // -----------------------------------------------------------------------
    // Movement
    // -----------------------------------------------------------------------

    private void HandleMovement(KeyboardState kb, float dt)
    {
        // Flatten Forward onto the XZ plane so walking doesn't drift up/down
        var flatForward = new Vector3(_camera.Forward.X, 0, _camera.Forward.Z);
        if (flatForward.LengthSquared() > 0.001f)
            flatForward = Vector3.Normalize(flatForward);

        var flatRight = new Vector3(_camera.Right.X, 0, _camera.Right.Z);
        if (flatRight.LengthSquared() > 0.001f)
            flatRight = Vector3.Normalize(flatRight);

        var move = Vector3.Zero;

        if (kb.IsKeyDown(Keys.W))          move += flatForward;
        if (kb.IsKeyDown(Keys.S))          move -= flatForward;
        if (kb.IsKeyDown(Keys.D))          move += flatRight;
        if (kb.IsKeyDown(Keys.A))          move -= flatRight;
        if (kb.IsKeyDown(Keys.Space))      move += Vector3.Up;
        if (kb.IsKeyDown(Keys.LeftShift))  move -= Vector3.Up;

        if (move.LengthSquared() > 0.001f)
            move = Vector3.Normalize(move);

        _camera.Position += move * MoveSpeed * dt;
    }

    // -----------------------------------------------------------------------
    // Block selection
    // -----------------------------------------------------------------------

    private void HandleBlockTypeSelection(KeyboardState kb)
    {
        if (kb.IsKeyDown(Keys.D1)) PlaceBlockId = BlockId.Grass;
        if (kb.IsKeyDown(Keys.D2)) PlaceBlockId = BlockId.Dirt;
        if (kb.IsKeyDown(Keys.D3)) PlaceBlockId = BlockId.Stone;
    }

    // -----------------------------------------------------------------------
    // Block interaction — fixed-step raycast
    // -----------------------------------------------------------------------

    private void HandleBlockInteraction(MouseState ms)
    {
        bool leftClick  = ms.LeftButton  == ButtonState.Pressed && _prevMouse.LeftButton  != ButtonState.Pressed;
        bool rightClick = ms.RightButton == ButtonState.Pressed && _prevMouse.RightButton != ButtonState.Pressed;

        if (!leftClick && !rightClick) return;

        Vector3 rayPos = _camera.Position;
        Vector3 rayDir = _camera.Forward;

        // Track the last air cell so we know where to place a new block
        int  lastAirX = 0, lastAirY = 0, lastAirZ = 0;
        bool hasLastAir = false;

        for (float t = 0f; t <= RaycastDistance; t += RaycastStep)
        {
            Vector3 p = rayPos + rayDir * t;

            int bx = (int)MathF.Floor(p.X);
            int by = (int)MathF.Floor(p.Y);
            int bz = (int)MathF.Floor(p.Z);

            if (BlockRegistry.IsSolid(_world.GetBlock(bx, by, bz)))
            {
                if (leftClick)
                    _world.SetBlock(bx, by, bz, BlockId.Air);
                else if (rightClick && hasLastAir)
                    _world.SetBlock(lastAirX, lastAirY, lastAirZ, PlaceBlockId);
                return;
            }

            lastAirX    = bx;
            lastAirY    = by;
            lastAirZ    = bz;
            hasLastAir  = true;
        }
    }
}
