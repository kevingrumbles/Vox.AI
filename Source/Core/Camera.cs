// Architecture: First-person camera that owns the View and Projection matrices.
// Rotation is computed from Yaw (horizontal) and Pitch (vertical) using Matrix multiplication,
// which avoids sign-convention pitfalls with raw trigonometry.
// Forward and Right vectors are derived from the rotation and exposed for use by PlayerController.

using Microsoft.Xna.Framework;

namespace Vox.AI.Core;

public sealed class Camera
{
    // --- State ------------------------------------------------------------
    public Vector3 Position;
    public float   Yaw;    // Horizontal rotation in radians — increases when turning right
    public float   Pitch;  // Vertical   rotation in radians — increases when looking up

    // --- Constants --------------------------------------------------------
    public const float FieldOfView = MathHelper.PiOver4;  // 45 °
    public const float NearPlane   = 0.1f;
    public const float FarPlane    = 512f;

    // --- Derived (updated every frame) ------------------------------------
    public Vector3 Forward { get; private set; }
    public Vector3 Right   { get; private set; }
    public Matrix  View       { get; private set; }
    public Matrix  Projection { get; private set; }

    public Camera(Vector3 startPosition, float aspectRatio)
    {
        Position   = startPosition;
        Projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, NearPlane, FarPlane);
    }

    /// <summary>Recomputes Forward, Right, View from current Position/Yaw/Pitch. Call once per frame.</summary>
    public void Update()
    {
        // Clamp pitch so the camera cannot flip over
        Pitch = MathHelper.Clamp(Pitch,
            -MathHelper.PiOver2 + 0.01f,
             MathHelper.PiOver2 - 0.01f);

        // Build rotation matrix: tilt up/down first, then spin left/right
        var rotation = Matrix.CreateRotationX(-Pitch) * Matrix.CreateRotationY(-Yaw);

        // Vector3.Forward = (0, 0, -1) and Vector3.Right = (1, 0, 0) in MonoGame
        Forward = Vector3.Transform(Vector3.Forward, rotation);
        Right   = Vector3.Transform(Vector3.Right,   rotation);

        View = Matrix.CreateLookAt(Position, Position + Forward, Vector3.Up);
    }
}
