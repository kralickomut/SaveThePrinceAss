# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

- **Name**: SaveThePrinceAss
- **Genre**: 2D side-scrolling platformer
- **Engine**: Godot 4.6 (Forward Plus renderer)
- **Language**: C# / .NET 8 (`Godot.NET.Sdk/4.6.0`)
- **Physics**: Jolt Physics
- **Entry scene**: `res://main_node.tscn`

## Building & Running

```bash
# Build C# scripts
dotnet build

# Run (requires Godot 4.6 in PATH)
godot --path .

# Or open in Godot editor and press F5
```

No automated tests exist.

## Scene Structure

### `main_node.tscn`
```
MainNode (Node2D)
├── NormalBG (CanvasLayer, layer=-100)   ← always alpha 1, base background
│   └── Sprites (Node2D)
│       └── Castle, Layer5..Layer1 (Sprite2D × 6)
├── AutumnBG (CanvasLayer, layer=-100)   ← fades in over Normal at zone boundary
│   └── Sprites (Node2D, starts at alpha 0)
│       └── Castle, Layer5..Layer1 (Sprite2D × 6)
├── WinterBG (CanvasLayer, layer=-100)   ← fades in over Autumn at zone boundary
│   └── Sprites (Node2D, starts at alpha 0)
│       └── Castle, Layer5..Layer1 (Sprite2D × 6)
├── TileMap          ← Floor Tiles1.png atlas, 16px tiles, collision layer 1
├── Player           ← instance of character.tscn, spawns at Vector2(48, 539)
├── SlimeEnemy       ← instance of slime.tscn, spawns at Vector2(300, 544)
└── ZoneManager (Node) ← ZoneManager.cs, controls background crossfades
```

### `character.tscn`
```
Player (CharacterBody2D)    ← PlayerController.cs, collision_layer = 2
├── Camera2D                ← zoom = Vector2(6, 6)
├── PlayerAnimator (Node2D) ← PlayerAnimator.cs (empty stub, just a container)
│   ├── AnimationPlayer     ← autoplay = "idle"; animations: idle, move, jump, attack, hit, death, RESET
│   └── Sprite2D            ← knight.png OR attack.png (swapped on attack), 32×32 region, offset (2, -7)
├── CollisionShape2D        ← RectangleShape2D 13.5×19 px, offset (1.25, -4.5)
└── AttackHitbox (Area2D)   ← offset (12, -7); collision_mask = 4 (hits enemies only)
    └── CollisionShape2D    ← RectangleShape2D 12×10 px; disabled by default
```

### `slime.tscn`
```
SlimeEnemy (CharacterBody2D)  ← SlimeEnemy.cs, collision_layer = 4
├── Sprite2D                  ← slime_green.png, 24×24 region, offset (0, -12)
├── CollisionShape2D          ← RectangleShape2D 14×14 px, offset (0, -7)
└── AnimationPlayer           ← animations: move, attack, hit
```

## Scripts

### `IDamageable.cs`
Single-method interface: `void TakeDamage(int damage)`. Every entity that can be hit must implement this. Currently: `PlayerController`, `SlimeEnemy`.

### `PlayerController.cs`
State machine with states: `Idle | Move | Jump | Attack | Hurt | Dead`

Key constants:
```csharp
const float Speed = 100.0f;
const float JumpVelocity = -250.0f;
```

Input actions:
- `ui_accept` — jump (Space/Enter)
- `ui_left` / `ui_right` / `ui_up` / `ui_down` — movement (`Input.GetVector`)
- `attack` — attack (mapped to left mouse button)

Attack flow: `SetState(Attack)` → `attack.png` swapped in, `"attack"` animation plays → animation keyframe enables `AttackHitbox/CollisionShape2D` at t=0.14s and disables it at t=0.42s → `_PhysicsProcess` detects overlapping `IDamageable` bodies (one hit per swing via `_hasDealtDamageThisAttack`) → `OnAnimationFinished` returns to `Idle`.

The hitbox enable/disable is driven by **animation keyframes**, not by code — do not manually toggle it in C#.

`UpdateMovementState()` only transitions between `Idle / Move / Jump`; it returns early when state is `Attack`, `Hurt`, or `Dead`.

### `SlimeEnemy.cs`
State machine with states: `Patrol | Chase | Attack | Hurt | Dead`

Key exports (tunable in editor):
```csharp
int MaxHealth = 30;   int AttackDamage = 5;
float MoveSpeed = 40; float DetectRange = 120; float AttackRange = 14;
```

AI loop (runs in `_PhysicsProcess`):
1. Distance to player → if `< AttackRange`: attack; if `< DetectRange`: chase; else: patrol
2. Patrol reverses direction on wall collision or after `PatrolTime = 2.5s`
3. Attack damage is applied via `_attackDamageTimer = 0.24s` delay (mid-animation), not via a hitbox

Player is located with `GetTree().GetFirstNodeInGroup("player")` — the player must stay in group `"player"`.

On death: `QueueFree()` immediately removes the node.

### `ZoneManager.cs`
Attached to the `ZoneManager` node in `main_node.tscn`. Reads player X each frame and crossfades between the three background `CanvasLayer` sets.

Key constants (adjust to change zone layout):
```csharp
const float Zone1End = 576f;   // world-x centre of zone 1→2 crossfade
const float Zone2End = 1152f;  // world-x centre of zone 2→3 crossfade
const float FadeSize = 128f;   // total crossfade width in game pixels
```

Alpha formula: `NormalBG` always stays at 1 (base layer); `AutumnBG = t12*(1-t23)`; `WinterBG = t23`. This gives a correct linear crossfade when layers are stacked (Autumn over Normal, Winter over Autumn).

### `PlayerAnimator.cs` / `PlayerAnimation.cs`
Both are empty stubs. `PlayerAnimator` is the scene container for the player's `AnimationPlayer`+`Sprite2D`. `PlayerAnimation.cs` at the repo root is unused legacy code.

## Collision Layers

| Layer | Bit | Used by |
|---|---|---|
| World | 1 | TileMap physics |
| Player | 2 | `character.tscn` CharacterBody2D |
| Enemy | 4 | `slime.tscn` CharacterBody2D |

`AttackHitbox` has `collision_mask = 4` → overlaps enemies only. No enemy has an `AttackHitbox`; SlimeEnemy uses a distance check instead.

## TileMap & Ground

- **Tileset texture**: `res://GandalfHardcore FREE Platformer Assets/Floor Tiles1.png` (288×576 px, 18×36 tiles at 16px)
- **Tile size**: 16×16 px (Godot default — do NOT set `tile_size` or `texture_region_size` explicitly in .tscn; overriding these breaks world coordinates)
- **Ground rows**: y=34 (surface) and y=35 (fill) → world_y = 544 and 560
- **World width**: 108 tiles = 1728 px
- **Physics polygon per tile**: `(-8,-8, 8,-8, 8,8, -8,8)` (full 16px tile)

### Atlas tile selection (col 0 of each zone section)

| Zone | Surface atlas | Fill atlas |
|---|---|---|
| Normal (rows 0–11) | `(0, 0)` | `(0, 8)` |
| Autumn (rows 12–23) | `(0, 12)` | `(0, 20)` |
| Winter (rows 24–35) | `(0, 24)` | `(0, 32)` |

Column 0 was chosen as a safe default. To use a different visual tile, select tiles in the Godot editor's TileMap inspector — all physics-defined tiles in col 0 already have collision, so changing to another col just changes the visual.

### Spawn positions
- **Player**: `Vector2(48, 539)` — feet land at world_y 544 (= top of ground row 34)
- **SlimeEnemy**: `Vector2(300, 544)` — feet land at world_y 544

## Zone System & Backgrounds

The world is divided into 3 horizontal zones (each 36 tiles / 576 px wide):

| Zone | X range | Background theme | Floor Tiles1.png rows |
|---|---|---|---|
| 1 – Normal | 0 – 576 | Green castle forest | 0–11 |
| 2 – Autumn | 576 – 1152 | Autumn castle | 12–23 |
| 3 – Winter | 1152 – 1728 | Winter castle | 24–35 |

Each zone's background is a `CanvasLayer` (layer=−100) containing a `Sprites` Node2D with 6 `Sprite2D` children (castle image + layers 5→1). All background images are from `GandalfHardcore FREE Platformer Assets/GandalfHardcore Background layers/`. Sprites are centred at `Vector2(576, 324)` with `scale = Vector2(2, 2)` — sized for the default 1152×648 viewport.

## Assets

| File | Location | Status |
|---|---|---|
| `knight.png` | `sprites/` | Player (idle/move/jump/hit/death frames) |
| `attack.png` | `sprites/` | Player attack frames (texture-swapped during attack state) |
| `Floor Tiles1.png` | `GandalfHardcore FREE Platformer Assets/` | Active ground tileset |
| `slime_green.png` | `sprites/` | SlimeEnemy sprite |
| Background layers (×18) | `GandalfHardcore FREE Platformer Assets/GandalfHardcore Background layers/` | Zone backgrounds (Normal/Autumn/Winter, 6 layers each) |
| `world_tileset.png` | `sprites/` | No longer used (replaced by Floor Tiles1.png) |
| `slime_purple.png` | `sprites/` | Unused — intended for second slime variant |
| `coin.png`, `fruit.png` | `sprites/` | Unused — collectibles |
| `platforms.png` | `sprites/` | Unused |

Duplicate PNG copies at the repo root are legacy leftovers — scenes reference `res://sprites/…` or `res://GandalfHardcore FREE Platformer Assets/…`.

## Known Technical Notes

- **`move` animation loop**: requires `loop_mode = 1` in `character.tscn`. If the walk cycle freezes after one pass, this property is missing on the `Animation_wfnr8` sub_resource.
- **Editor cache**: Godot stores the last-selected AnimationPlayer path in `.godot/editor/*.cfg`. If the node `PlayerAnimator` was previously named `AgentAnimator`, these files contain stale paths and produce "Node not found" warnings on scene load. The cache files have been patched.
- **TileMap tile_size**: Do not set `tile_size = Vector2i(32, 32)` on the TileSet sub_resource in .tscn — Godot 4.6 may not apply it from the file, causing the tile grid to default to 16px and placing ground tiles at world_y=272 instead of 544 (off-screen).
- **CanvasLayer modulate**: `CanvasLayer` does not extend `CanvasItem` so it has no `modulate` property. The fade targets are the `Sprites` Node2D children (which do have `modulate`).

## What Is and Isn't Implemented

### Done
- [x] Horizontal movement with smooth deceleration
- [x] Jump + gravity
- [x] All player animations: idle (loop), move (loop), jump, attack, hit, death
- [x] Player state machine (Idle / Move / Jump / Attack / Hurt / Dead)
- [x] Attack hitbox with one-hit-per-swing guard (animation-driven)
- [x] `IDamageable` interface + health/damage on player and slime
- [x] SlimeEnemy with Patrol / Chase / Attack / Hurt / Dead AI
- [x] Camera following player (6× zoom)
- [x] 108-tile horizontal world with collision (1728 px)
- [x] 3-zone horizontal world with smooth background crossfades (Normal → Autumn → Winter)
- [x] Zone-appropriate floor tiles from `Floor Tiles1.png`

### Not Done
- [ ] Respawn / game-over logic (player dies but nothing restarts)
- [ ] Collectibles (coin, fruit sprites exist; no pickup logic)
- [ ] UI / HUD (health bar, score)
- [ ] Audio
- [ ] Proper level design (flat single-height platform only)
- [ ] Second slime variant (purple sprite exists, no scene/script)
- [ ] Parallax scrolling on backgrounds (backgrounds are currently static in CanvasLayer)
