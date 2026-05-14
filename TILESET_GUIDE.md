# Tileset & Decor Guide for Claude

This file tells Claude exactly how to build `.tscn` scenes using GandalfHardcore assets —
tile coordinates, encoding rules, decor positions, and ready-to-paste `.tscn` snippets.

---

## 1. Core numbers — memorise these

| Property | Value |
|---|---|
| **Tile size** | **32 × 32 px** |
| Physics polygon (full tile) | `PackedVector2Array(-16,-16, 16,-16, 16,16, -16,16)` |
| Background image size | 1024 × 346 px |
| Background scale in scene | `Vector2(2, 2)` → covers 2048 × 692 world px |
| Background centre position | `Vector2(576, 324)` |
| Zone crossfade boundaries | Zone1End = 576, Zone2End = 1152, FadeSize = 128 |

---

## 2. Floor Tiles1.png atlas

**File:** `res://GandalfHardcore FREE Platformer Assets/Floor Tiles1.png`
**Image size:** 288 × 576 px
**Grid:** 9 columns × 18 rows (each cell = 32 × 32 px)

### Season zones (row ranges)

288px / 32px = 9 cols. 576px / 32px = 18 rows. 18 rows ÷ 3 seasons = **6 rows per season**.

| Season | Atlas rows | World px start |
|---|---|---|
| Normal (green) | 0 – 5 | y = 0 |
| Autumn (orange) | 6 – 11 | y = 192 |
| Winter (blue/snow) | 12 – 17 | y = 384 |

### Key tile atlas coordinates (col : row)

Each season block uses the same relative row offset:

```
season_start:  Normal=0  Autumn=6  Winter=12

Row +0  → TOP SURFACE tiles     (visible grass/snow cap on top of terrain)
Row +2  → UNDERGROUND FILL      (solid dark dirt, used for deep ground layers)
```

**Standard tiles for a basic ground layer:**

| Tile purpose | Atlas col | Atlas row | Encoded int (col + row×65536) |
|---|---|---|---|
| Normal — surface | 0 | 0 | `0` |
| Normal — fill | 0 | 2 | `131072` |
| Autumn — surface | 0 | 6 | `393216` |
| Autumn — fill | 0 | 8 | `524288` |
| Winter — surface | 0 | 12 | `786432` |
| Winter — fill | 0 | 14 | `917504` |

> **Note on existing main_node.tscn:** the current tile_data uses old atlas rows (8, 20, 24, 32)
> that were calculated for 16×16 tiles. Rows 20, 24, 32 fall outside the 18-row grid at 32×32
> and will show blank tiles. Regenerate tile_data with the corrected values above.

### How to register tiles in TileSetAtlasSource

Syntax inside `[sub_resource type="TileSetAtlasSource"]`:

```
col:row/0 = 0
col:row/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16,-16, 16,-16, 16,16, -16,16)
```

Example — register all 6 standard tiles with collision:

```gdscene
[sub_resource type="TileSetAtlasSource" id="TileSetAtlasSource_1"]
texture = ExtResource("floor_tiles_id")
0:0/0 = 0
0:0/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16, -16, 16, -16, 16, 16, -16, 16)
0:2/0 = 0
0:2/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16, -16, 16, -16, 16, 16, -16, 16)
0:6/0 = 0
0:6/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16, -16, 16, -16, 16, 16, -16, 16)
0:8/0 = 0
0:8/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16, -16, 16, -16, 16, 16, -16, 16)
0:12/0 = 0
0:12/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16, -16, 16, -16, 16, 16, -16, 16)
0:14/0 = 0
0:14/0/physics_layer_0/polygon_0/points = PackedVector2Array(-16, -16, 16, -16, 16, 16, -16, 16)

[sub_resource type="TileSet" id="TileSet_1"]
physics_layer_0/collision_layer = 1
sources/0 = SubResource("TileSetAtlasSource_1")
```

---

## 3. TileMap tile_data encoding

Godot 4 `PackedInt32Array` format: **every 3 ints = one tile**

```
[map_coord,  source_id,  atlas_coord]
```

### Encoding formulas

```python
map_coord  = map_x  | (map_y  << 16)   # x,y are signed int16 (−32768..32767)
atlas_coord = atlas_col | (atlas_row << 16)
source_id  = 0                          # always 0 if single source
```

### Decoding formulas

```python
map_x  = map_coord & 0xFFFF            # sign-extend if > 32767
map_y  = (map_coord >> 16) & 0xFFFF
atlas_col = atlas_coord & 0xFFFF
atlas_row = (atlas_coord >> 16) & 0xFFFF
```

### Quick reference — encoding map positions

```
x=0,  y=34 → 34*65536     = 2228224
x=0,  y=35 → 35*65536     = 2293760
x=36, y=34 → 34*65536+36  = 2228260
x=36, y=35 → 35*65536+36  = 2293796
```

### Example: 2-tile-thick ground at map row y=17/18, x from 0 to N-1, Normal season

```python
# Surface row (map y=17), Normal atlas (0,0) → atlas_coord = 0
# Fill row    (map y=18), Normal atlas (0,2) → atlas_coord = 131072

result = []
for x in range(N):
    result += [17*65536 + x, 0, 0]          # surface
    result += [18*65536 + x, 0, 131072]     # fill
```

---

## 4. Building a 3-zone level (Normal → Autumn → Winter)

With 32×32 tiles and ZoneManager boundaries at x=576 and x=1152:
each zone = 576px / 32px = **18 tiles wide**.

| Zone | Map X range (tiles) | World px range | Surface atlas_coord | Fill atlas_coord |
|---|---|---|---|---|
| Normal | 0 – 17 | 0 – 575 | `0` | `131072` |
| Autumn | 18 – 35 | 576 – 1151 | `393216` | `524288` |
| Winter | 36 – 53 | 1152 – 1727 | `786432` | `917504` |

To make wider zones, increase the X range — just keep using the same atlas tile repeatedly.

---

## 5. Background layers setup

Each zone has 6 parallax Sprite2D layers inside a CanvasLayer (layer = −100).
All layers share the same position and scale.

```gdscene
[node name="NormalBG" type="CanvasLayer" parent="."]
layer = -100

[node name="Sprites" type="Node2D" parent="NormalBG"]
# NormalBG/Sprites starts at full alpha (no modulate line needed = default 1)

[node name="Castle" type="Sprite2D" parent="NormalBG/Sprites"]
texture = ExtResource("5_n0")      # Background Castle .png
position = Vector2(576, 324)
scale = Vector2(2, 2)

[node name="Layer5" type="Sprite2D" parent="NormalBG/Sprites"]
texture = ExtResource("6_n5")
position = Vector2(576, 324)
scale = Vector2(2, 2)

# ... repeat for Layer4, Layer3, Layer2, Layer1
```

For Autumn and Winter groups — add `modulate = Color(1, 1, 1, 0)` to `Sprites` node
(ZoneManager fades them in at runtime).

**Background file order (layer number = draw order, higher = in front):**

| Node | File |
|---|---|
| Castle | `Background Castle .png` (or Autumn/Winter variant) |
| Layer5 | `layer 5.png` — furthest back after sky |
| Layer4 | `layer 4.png` |
| Layer3 | `layer 3.png` |
| Layer2 | `layer 2.png` |
| Layer1 | `layer 1.png` — closest to player |

---

## 6. Decor objects (Sprite2D, not TileMap)

**File:** `res://GandalfHardcore FREE Platformer Assets/Decor.png`
**Image size:** 416 × 544 px

Decor items are **not on a regular grid** — they vary in size.
Use `region_enabled = true` + `region_rect = Rect2(x, y, w, h)` to crop a specific item.

### Item atlas map (pixel regions, Rect2(x, y, w, h))

**Row 0 — small props (~32×32 each)**

| Item | region_rect |
|---|---|
| Barrel (left) | `Rect2(0, 0, 32, 32)` |
| Barrel (open) | `Rect2(32, 0, 32, 32)` |
| Barrel (dark) | `Rect2(64, 0, 32, 32)` |
| Sack / bag | `Rect2(96, 0, 32, 32)` |
| Writing stand | `Rect2(128, 0, 32, 32)` |
| Chest | `Rect2(160, 0, 32, 32)` |
| Chair | `Rect2(192, 0, 32, 32)` |
| Apple pile | `Rect2(288, 0, 32, 32)` |

**Row 1-2 — medium structures (~64–96 px)**

| Item | region_rect |
|---|---|
| Tent (large) | `Rect2(0, 32, 96, 96)` |
| Log pile | `Rect2(96, 64, 64, 32)` |
| Wooden gate/table | `Rect2(192, 64, 64, 48)` |
| Tombstone (round) | `Rect2(160, 96, 32, 48)` |
| Tombstone (cross) | `Rect2(192, 96, 32, 48)` |
| Tombstone (broken) | `Rect2(224, 96, 32, 48)` |
| Basket (small) | `Rect2(320, 32, 32, 32)` |
| Fruit baskets | `Rect2(352, 0, 64, 64)` |

**Row 3-4 — ground decorations (thin, ~16–32 px tall)**

| Item | region_rect |
|---|---|
| Grass tuft (green, sm) | `Rect2(0, 160, 32, 16)` |
| Grass tuft (green, lg) | `Rect2(32, 160, 48, 16)` |
| Grass tuft (autumn) | `Rect2(128, 160, 48, 16)` |
| Snowdrift (small) | `Rect2(192, 160, 48, 16)` |
| Hay tuft | `Rect2(256, 160, 48, 32)` |
| Reed cluster | `Rect2(304, 144, 32, 48)` |
| Wooden fence panel | `Rect2(352, 144, 32, 32)` |

**Row 5-6 — rock piles and scarecrow**

| Item | region_rect |
|---|---|
| Scarecrow | `Rect2(256, 192, 48, 64)` |
| Pumpkin | `Rect2(352, 192, 32, 32)` |
| Rock pile (sm) | `Rect2(0, 192, 48, 32)` |
| Rock pile (md green) | `Rect2(64, 192, 64, 32)` |
| Snow pile (sm) | `Rect2(128, 192, 64, 32)` |
| Stone statue | `Rect2(384, 160, 32, 64)` |

**Row 7 — large boulder piles (~96×64 px)**

| Item | region_rect |
|---|---|
| Boulder group (stones) | `Rect2(0, 272, 96, 64)` |
| Boulder group (large) | `Rect2(96, 272, 96, 64)` |
| Boulder group (fire) | `Rect2(192, 272, 96, 64)` |
| Snow boulder pile | `Rect2(288, 272, 96, 64)` |

**Row 8 — clothesline**

| Item | region_rect |
|---|---|
| Clothesline with laundry | `Rect2(0, 352, 128, 80)` |

**Row 8 — cloud/bush decorations**

| Item | region_rect |
|---|---|
| Autumn leaf cloud (lg) | `Rect2(160, 352, 96, 48)` |
| Autumn leaf cloud (sm) | `Rect2(160, 400, 64, 32)` |
| Snow cloud (lg) | `Rect2(256, 352, 96, 48)` |
| Snow cloud (sm) | `Rect2(256, 400, 64, 32)` |

**Row 9 — ground bushes**

| Item | region_rect |
|---|---|
| Bush (green, lg) | `Rect2(0, 448, 64, 48)` |
| Bush (green, sm) | `Rect2(64, 448, 64, 48)` |
| Bush (autumn, lg) | `Rect2(0, 496, 64, 48)` |
| Bush (autumn, sm) | `Rect2(64, 496, 64, 48)` |
| Snow bush | `Rect2(0, 512, 64, 32)` |

> **Note:** These pixel positions are approximate — verify in Godot's SpriteFrames editor
> or with the inspector's region_rect picker before finalising.

### How to place a decor Sprite2D in .tscn

```gdscene
[node name="Barrel" type="Sprite2D" parent="Decor"]
position = Vector2(240, 530)    # world position
texture = ExtResource("decor_id")
region_enabled = true
region_rect = Rect2(0, 0, 32, 32)
```

For interactive objects (scrolls, pickups) attach an `Area2D` child with a `CollisionShape2D`.

---

## 7. Other tile sheets

### Other Tiles1.png / Other Tiles2.png (288 × 224 px, 9 × 7 tiles @ 32px)

Both files appear identical. Contents:
- **Rows 0–1**: Slope tiles (cliff left/right, grass-capped)
- **Rows 2–3**: Hazard strips (lava/spike variants in red, blue, white)
- **Rows 4–5**: Thin platform slabs (1-tile-high platforms)
- **Row 6**: Short wooden board strips (bridge/board tiles)

Register and use these the same way as Floor Tiles1.png (same tile size = 32×32).

### House Tiles.png (448 × 224 px)

Contains 2 full house illustrations side-by-side (~224 × 224 each).
**Use as a large static Sprite2D, NOT as a TileMap.**

```gdscene
[node name="House" type="Sprite2D" parent="."]
texture = ExtResource("house_id")
region_enabled = true
region_rect = Rect2(0, 0, 224, 224)   # left house
position = Vector2(300, 400)
```

---

## 8. Torch animation (Sprite2D + AnimationPlayer)

**File:** `res://GandalfHardcore FREE Platformer Assets/Torch.png`
**Image size:** 192 × 128 px
**Frame size:** 32 × 32 px → 6 columns × 4 rows = 24 frames

Animation: cycle through all 24 frames for a flickering torch effect.

```gdscene
[node name="Torch" type="Sprite2D" parent="."]
texture = ExtResource("torch_id")
region_enabled = true
region_rect = Rect2(0, 0, 32, 32)    # starts at frame 0
position = Vector2(200, 520)
```

For animated torch, use `AnimationPlayer` stepping through `region_rect`:
- Frame step: 0.08–0.1s
- Frame coords: Rect2(col*32, row*32, 32, 32) for col in 0–5, row in 0–3

---

## 9. Workflow for Claude to build a level in .tscn

1. **Declare ext_resources** — one per texture file used
2. **Create TileSetAtlasSource** — register needed atlas coords with physics polygons
3. **Wrap in TileSet** — set `physics_layer_0/collision_layer = 1`
4. **Add TileMap node** with `tile_set = SubResource(...)` and `format = 2`
5. **Write tile_data** — use encoding formula: `[x + y*65536,  0,  atlas_col + atlas_row*65536]` per tile
6. **Add background CanvasLayers** (NormalBG / AutumnBG / WinterBG) with 6 Sprite2D each
7. **Add ZoneManager node** with the ZoneManager script
8. **Place decor** as Sprite2D children of a `Decor` Node2D
9. **Instance Player** from `character.tscn`
10. **Instance enemies** from `slime.tscn` at desired positions
