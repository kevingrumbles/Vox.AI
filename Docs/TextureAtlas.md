# Texture Atlas

The voxel renderer uses a single shared texture atlas — one `Texture2D` bound once per frame.
All block faces sample from this atlas using pre-computed UV coordinates.
No per-chunk or per-block texture loads occur at runtime.

---

## Atlas Specification

| Property        | Value                          |
|-----------------|--------------------------------|
| Grid            | 4 columns × 4 rows (16 tiles)  |
| Default tile size | 16 × 16 px                   |
| Total size (procedural) | 64 × 64 px             |
| Format          | PNG (RGBA)                     |
| File path       | `Art/Vox.AI.Atlas.png` (relative to executable) |

The file path and grid dimensions are defined in code:

| Constant                         | File                          | Value                  |
|----------------------------------|-------------------------------|------------------------|
| `TextureAtlas.Columns`           | `Rendering/TextureAtlas.cs`   | `4`                    |
| `TextureAtlas.Rows`              | `Rendering/TextureAtlas.cs`   | `4`                    |
| `TextureGenerator.AtlasFilePath` | `Rendering/TextureGenerator.cs` | `Art/Vox.AI.Atlas.png` |

---

## Tile Layout

Tiles are addressed by a flat **tile index**:

```
tileIndex = column + row * 4
```

```
		Col 0        Col 1        Col 2        Col 3
Row 0 [  0 GrassTop  |  1 GrassSide |  2 Dirt      |  3 Stone    ]
Row 1 [  4 Sand      |  5 Water     |  6 WoodTop   |  7 WoodSide ]
Row 2 [  8 Leaves    |  9 (unused)  | 10 (unused)  | 11 (unused) ]
Row 3 [ 12 (unused)  | 13 (unused)  | 14 (unused)  | 15 (unused) ]
```

Named constants for every tile live in `Blocks/AtlasTile.cs`:

```csharp
AtlasTile.GrassTop   // 0
AtlasTile.GrassSide  // 1
AtlasTile.Dirt       // 2
AtlasTile.Stone      // 3
AtlasTile.Sand       // 4
AtlasTile.Water      // 5
AtlasTile.WoodTop    // 6
AtlasTile.WoodSide   // 7
AtlasTile.Leaves     // 8
// 9–15 reserved
```

---

## UV Coordinate System

UVs are normalised (0.0 – 1.0), origin top-left, computed at mesh-build time.

Each tile occupies:
- Width: `1.0 / Columns` = 0.25
- Height: `1.0 / Rows` = 0.25

An **inset of 0.001** is applied to all four edges of every tile to prevent texture
bleeding caused by bilinear filtering sampling across tile borders:

```
U0 =  col      * 0.25 + 0.001
V0 =  row      * 0.25 + 0.001
U1 = (col + 1) * 0.25 - 0.001
V1 = (row + 1) * 0.25 - 0.001
```

The inset value is `TextureAtlas.AtlasInset` (`Rendering/TextureAtlas.cs`).

### UV API (`Rendering/TextureAtlas.cs`)

```csharp
// Top-left UV corner of a tile
Vector2 uv = TextureAtlas.GetUV(tileIndex);

// Full inset rectangle (U0, V0, U1, V1)
var (u0, v0, u1, v1) = TextureAtlas.GetUVRect(tileIndex);

// All four corners in ChunkMesh vertex order (TL, TR, BR, BL)
var (tl, tr, br, bl) = TextureAtlas.GetUVs(tileIndex);
```

---

## Atlas Loading

`TextureGenerator.LoadAtlas(GraphicsDevice)` is called once in `VoxGame.LoadContent()`.

**Priority order:**

1. **File on disk** — if `Art/Vox.AI.Atlas.png` exists next to the executable, it is loaded with `Texture2D.FromStream`.
2. **Procedural fallback** — if the file is absent, `TextureGenerator.CreateAtlas()` generates a 64 × 64 atlas in memory using simple painted patterns.

The procedural fallback is always available; no asset pipeline setup is required to run the game.

---

## Creating an Artist Atlas File

### Canvas setup

| Setting      | Value                          |
|--------------|--------------------------------|
| Width        | 64 px (or any multiple: 128, 256, 512 …) |
| Height       | 64 px (same multiple)          |
| Colour mode  | RGBA 32-bit                    |
| Format       | PNG                            |

> If you use a larger canvas (e.g. 256 × 256) each tile becomes 64 × 64 px.
> The UV math is resolution-independent — only the **grid** (4 × 4) matters.

### Tile placement

Draw each tile at its grid position. Using 16 px tiles as an example:

| Tile name   | Index | Pixel X | Pixel Y | Size      |
|-------------|-------|---------|---------|-----------|
| GrassTop    | 0     | 0       | 0       | 16 × 16   |
| GrassSide   | 1     | 16      | 0       | 16 × 16   |
| Dirt        | 2     | 32      | 0       | 16 × 16   |
| Stone       | 3     | 48      | 0       | 16 × 16   |
| Sand        | 4     | 0       | 16      | 16 × 16   |
| Water       | 5     | 16      | 16      | 16 × 16   |
| WoodTop     | 6     | 32      | 16      | 16 × 16   |
| WoodSide    | 7     | 48      | 16      | 16 × 16   |
| Leaves      | 8     | 0       | 32      | 16 × 16   |
| (reserved)  | 9–15  | —       | —       | —         |

General formula for pixel coordinates:

```
pixelX = (tileIndex % 4) * tileSize
pixelY = (tileIndex / 4) * tileSize
```

### Bleeding prevention

Leave a 1-pixel transparent padding around each tile **or** rely on the built-in
`AtlasInset = 0.001f` UV shrink that is already applied in code.
For low-resolution tiles (16 px), the code inset alone is usually sufficient.
For higher resolutions, padding is less critical but still recommended.

### Saving the file

Export as **PNG with alpha channel** (RGBA). Do not use JPG — lossy compression
creates colour fringing at tile borders.

---

## Deploying the Atlas File

1. Export the PNG from your image editor.
2. Place it at:
   ```
   <executable directory>/Art/Vox.AI.Atlas.png
   ```
   For a typical Debug build in this solution that is:
   ```
   bin/Debug/net9.0/Art/Vox.AI.Atlas.png
   ```
3. Launch the game. `TextureGenerator.LoadAtlas` will detect the file and load it automatically. No code changes are required.

To revert to the procedural atlas, delete or rename the PNG file.

---

## Adding a New Texture / Block Type

1. **Add a tile** to the atlas PNG at the next unused index (9, 10, …).
2. **Register the constant** in `Blocks/AtlasTile.cs`:
   ```csharp
   public const int MyNewTile = 9;
   ```
3. **Add a block ID** in `Blocks/BlockId.cs`:
   ```csharp
   public const byte MyBlock = 7;
   ```
4. **Register a definition** in `Blocks/BlockRegistry.cs`:
   ```csharp
   Definitions[BlockId.MyBlock] = new BlockDefinition
   {
	   Id            = BlockId.MyBlock,
	   Name          = "MyBlock",
	   IsSolid       = true,
	   IsTransparent = false,
	   TopTexture    = AtlasTile.MyNewTile,
	   BottomTexture = AtlasTile.MyNewTile,
	   SideTexture   = AtlasTile.MyNewTile,
   };
   ```
5. Expand the `Definitions` array size in `BlockRegistry` to cover the new ID.

No changes are required in `ChunkMesh`, `TextureAtlas`, or any renderer code.

---

## Relevant Source Files

| File | Role |
|------|------|
| `Rendering/TextureAtlas.cs` | UV calculation, grid constants, inset value |
| `Rendering/TextureGenerator.cs` | File loading, procedural fallback, atlas path constant |
| `Blocks/AtlasTile.cs` | Named tile index constants |
| `Blocks/BlockDefinition.cs` | Per-block face texture assignments |
| `Blocks/BlockRegistry.cs` | Block registration and `GetTextureForFace` routing |
| `Rendering/ChunkMesh.cs` | Mesh builder — consumes UVs, never sets them directly |
| `VoxGame.cs` | Calls `TextureGenerator.LoadAtlas` once in `LoadContent` |
