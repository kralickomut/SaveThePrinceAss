# Save The Princess — Project Context for Claude

This file exists to give Claude full project context at the start of a session, so no codebase exploration is needed upfront.

---

## Project Overview

- **Name**: Save The Princess
- **Genre**: 2D side-scrolling action platformer / slasher
- **Engine**: Godot 4.6 (Forward Plus renderer)
- **Language**: C#
- **Physics**: Jolt Physics
- **Renderer backend**: Direct3D 12 (Windows)
- **Entry scene**: `res://main_node.tscn`
- **Target resolution**: 1920×1080
- **Deadline**: 2026-05-20

---

## Game Concept (from `idea.md`)

A medieval castle platformer. The Prince returns home to find his father — the King — has gone mad, turned tyrant, and imprisoned the Princess. The Prince fights through castle floors to rescue his sister and uncover the truth.

**Hidden scrolls** scattered around levels (inside bookshelves, barrels, pots) drive the story — lore, hints, and servant humor ("Cabbage for dinner again").

### Floors

| Floor | Location        | Atmosphere                                              |
|-------|-----------------|---------------------------------------------------------|
| 1     | Castle entrance | Bright, intro — weak enemies                            |
| 2     | Living quarters | Normal lighting — medium enemies, most scrolls          |
| 3     | Dungeon/Prison  | **Dark — player carries a torch** (`PointLight2D`), rescue Princess |

### Enemies (planned)

| Enemy  | HP    | Speed  | Notes                                        |
|--------|-------|--------|----------------------------------------------|
| Slime (green)  | Low   | Slow   | Alchemical experiments; floors 1–2 |
| Slime (purple) | Med   | Slow   | Stronger variant; floors 1–2       |
| Guard  | Med   | Med    | Sword attack; floors 1–2                     |
| Knight | High  | Slow   | Armored, heavy hits; floors 2–3              |

### Weapons (no shop, no upgrades — found in levels)

1. Fists (start)
2. Short sword (floor 1)
3. Broadsword (floor 2)
4. Magic sword — optional reward (floor 3)

### Out of scope (v1)

Boss fights, inventory system, dialog system, save system.

---

## Git History

| Date       | Commit  | Message                                                   |
|------------|---------|-----------------------------------------------------------|
| 2026-03-02 | 59bf1c0 | Test (initial commit)                                     |
| 2026-03-02 | 33220d1 | Update .gitignore for Godot project                       |
| 2026-03-02 | 6165b53 | Delete hello.txt                                          |
| 2026-03-09 | 69b5dc5 | First real stuff                                          |
| 2026-03-10 | 32d8ad1 | Fresh day                                                 |
| 2026-03-24 | ab09f86 | Add attack animation, hitbox and 1st enemy                |
| 2026-05-?? | 1826b8b | Add 3-zone world with background crossfades and GandalfHardcore floor tiles |
| 2026-05-14 | —       | Fix TileMap 32×32, camera Y-lock, sprites ×2, damage cooldowns, Gandalf slime variants |

---

## Scene Structure

### `main_node.tscn`

```
MainNode (Node2D)
├── NormalBG (CanvasLayer, layer=-100)
│   └── Sprites (Node2D)              ← alpha 1 always; 6 Sprite2D layers
├── AutumnBG (CanvasLayer, layer=-100)
│   └── Sprites (Node2D)              ← alpha 0 at start; faded in by ZoneManager
├── WinterBG (CanvasLayer, layer=-100)
│   └── Sprites (Node2D)              ← alpha 0 at start; faded in by ZoneManager
├── TileMap                           ← Floor Tiles1.png atlas, 32×32, 162 tiles wide
├── Player                            ← instance of character.tscn, pos Vector2(96, 571)
├── SlimeEnemy                        ← instance of slime.tscn, green Gandalf slime, pos Vector2(400, 576)
├── SlimeEnemyRed                     ← instance of slime_purple.tscn, red Gandalf slime, pos Vector2(700, 576)
├── SlimeEnemyBlue                    ← instance of slime_blue.tscn, blue Gandalf slime, pos Vector2(1000, 576)
└── ZoneManager                       ← Node with ZoneManager.cs script
```

**TileMap layout:**
- 3 zones × 54 tiles wide = **162 tiles total = 5184 px**
- Ground surface: map_y=18 (world y=576 px), fill: map_y=19
- Normal zone: x=0..53, Autumn: x=54..107, Winter: x=108..161
- Atlas tiles registered: `(1,0)` Normal surface, `(1,1)` Normal fill, `(3,6)` Autumn surface, `(3,7)` Autumn fill, `(1,12)` Winter surface, `(1,13)` Winter fill

**Background sprites:** all 18 sprites (6 per zone) positioned at `Vector2(960, 540)`, `scale = Vector2(3.5, 3.5)` → covers 1920×1080 screen.

### `character.tscn`

```
Player (CharacterBody2D)              ← script: PlayerController.cs
│   collision_layer=2
├── Camera2D                          ← zoom=Vector2(6,6), offset=(0,10), limit_top=429, limit_bottom=609
├── PlayerAnimator (Node2D)           ← script: PlayerAnimator.cs (stub)
│   ├── AnimationPlayer               ← autoplay="idle", root_node=Player
│   └── Sprite2D                      ← GandalfHardcore Warrior.png, 80×64 frames, offset (0,-5)
├── CollisionShape2D                  ← RectangleShape2D 28×38 px, offset (1,-2)
└── AttackHitbox (Area2D)             ← offset (40,-9), collision_mask=4
    └── CollisionShape2D              ← RectangleShape2D 54×30 px, disabled by default
```

**Camera Y-lock:** `limit_top=429`, `limit_bottom=609` clamps camera to world y≈519, so vertical camera scroll is disabled — background stays still when player jumps.

### `slime.tscn`

```
SlimeEnemy (CharacterBody2D)          ← script: SlimeEnemy.cs, collision_layer=4, collision_mask=1
├── Sprite2D                          ← scale=Vector2(2,2), Gandalf Slime Enemy/Slime green.png, 32×32 frames, offset (0,-32)
├── CollisionShape2D                  ← RectangleShape2D 42×24 px, offset (0,-12)
├── AnimationPlayer                   ← idle, move, attack, hit, death
└── HealthBar                         ← world-space bar above slime
```

### `slime_purple.tscn`

```
SlimeEnemy (CharacterBody2D)          ← script: SlimeEnemy.cs, collision_layer=4, collision_mask=1
├── Sprite2D                          ← scale=Vector2(2,2), Gandalf Slime Enemy/Slime red.png, 32×32 frames, offset (0,-32)
├── CollisionShape2D                  ← RectangleShape2D 42×24 px, offset (0,-12)
├── AnimationPlayer                   ← idle, move, attack, hit, death
└── HealthBar                         ← world-space bar above slime
```

Filename is legacy; this scene now represents the red slime. Same animations and AI as green slime. Stats can be overridden via `[Export]` in the scene inspector.

### `slime_blue.tscn`

Standalone copy of the same slime setup using `GandalfHardcore Slime Enemy/Slime blue.png`. It is intentionally not inherited from `slime.tscn`, so editor collider tweaks are less fragile.

---

## Scripts

### `scripts/PlayerController.cs`

Inherits `CharacterBody2D`, implements `IDamageable`.

**Exported / constants:**
```csharp
const float Speed = 100.0f;
const float JumpVelocity = -250.0f;
const float InvincibleDuration = 1.2f;  // seconds of i-frames after taking damage
[Export] int MaxHealth = 100;
[Export] int AttackDamage = 10;
```

**State machine:**
```
Idle → Move → Jump → Attack → Hurt → Dead
```
- Input locked during `Attack`, `Hurt`, `Dead`
- `Attack` calls `UpdateFacingDirection()` to move `AttackHitbox.Position` left/right; hitbox enabled/disabled by animation keyframes
- One damage instance per attack swing (`_hasDealtDamageThisAttack` flag)
- **Damage cooldowns:** source-aware damage uses `TakeDamageFrom(attacker, damage)` and tracks a separate 1.2 s cooldown per attacker. Slimes use this path, so multiple slimes can each damage the player during their own attack cycles while a single slime is still rate-limited. Generic `TakeDamage()` still uses the shared `_invincibleTimer`.
- `Heal(int amount)` restores HP up to `MaxHealth`, updates the HUD health bar, and returns `true` only when HP changed.
- Death: plays "death" animation, stops all processing
- Player is in group `"player"` (enemies use this to locate it)
- Uses a single 80×64 warrior spritesheet for all player states; no attack texture swap

**Collision layers:**
- Player body: layer 2, mask 1 (hits world)
- Attack hitbox Area2D: layer 0, mask 4 (hits enemies on layer 4)

### `scripts/SlimeEnemy.cs`

Inherits `CharacterBody2D`, implements `IDamageable`. Shared by the green, red, and blue Gandalf slime scenes.

**Exported:**
```csharp
[Export] int MaxHealth = 30;
[Export] int AttackDamage = 5;
[Export] float MoveSpeed = 40.0f;
[Export] float DetectRange = 120.0f;
[Export] float AttackRange = 52.0f;
```

**State machine:**
```
Patrol → Chase → Attack → Hurt → Dead (QueueFree)
```
- Patrol: moves at `MoveSpeed * 0.5f`, reverses on wall or every 2.5s
- Chase: moves toward player at `MoveSpeed`
- Attack: stops, plays "attack" anim, deals damage after `0.24s` delay
- Dead: `QueueFree()` immediately

**Collision layer:** 4 (matched by player's AttackHitbox mask=4)

### `scripts/ZoneManager.cs`

Node that crossfades three background layers based on player X position.

```
Zone 1 (Normal)   x < 1600         → NormalBG fully visible
Crossfade 1→2     x ≈ 1600–1856    → AutumnBG fades in
Zone 2 (Autumn)   1856 < x < 3328  → AutumnBG fully visible
Crossfade 2→3     x ≈ 3328–3584    → WinterBG fades in
Zone 3 (Winter)   x > 3584         → WinterBG fully visible
```

Boundaries: `Zone1End = 1728`, `Zone2End = 3456`, `FadeSize = 256`.
Modifies `Modulate.A` on `NormalBG/Sprites`, `AutumnBG/Sprites`, `WinterBG/Sprites` nodes each frame.

### `scripts/IDamageable.cs`

```csharp
public interface IDamageable { void TakeDamage(int damage); }
```

### `scripts/PlayerAnimator.cs` / `PlayerAnimation.cs`

Both are empty stubs (auto-generated Node2D scripts, no logic).

---

## Animations

### Player (`character.tscn` → `AnimationPlayer`)

| Name     | Length | Frames | Loop | Notes                                          |
|----------|--------|--------|------|------------------------------------------------|
| `RESET`  | 0.001s | 1      | No   | Internal Godot reset                           |
| `idle`   | 1.0s   | 5      | Yes | Warrior sheet row 7, 80×64 frames |
| `move`   | 0.8s   | 8      | Yes | Warrior sheet row 8 |
| `run`    | 0.64s  | 8      | Yes | Warrior sheet row 9; hold Shift |
| `jump`   | 0.4s   | 4      | No  | Warrior sheet row 11 |
| `fall`   | 0.4s   | 4      | No  | Warrior sheet row 12 |
| `attack` | 0.48s  | 6      | No  | Warrior sheet row 14 slash frames; hitbox enabled frames 0.16–0.40 |
| `hit`    | 0.4s   | 4      | No  | Reuses warrior sheet row 12 |
| `death`  | 1.0s   | 10     | No  | Warrior sheet row 15 |

### Slime (`slime.tscn` / `slime_purple.tscn` / `slime_blue.tscn` → `AnimationPlayer`)

| Name     | Length | Frames | Loop | Notes                    |
|----------|--------|--------|------|--------------------------|
| `idle`   | 0.6s   | 5      | Yes  | Row 0 of Gandalf slime sheet |
| `move`   | 0.64s  | 8      | Yes  | Row 1 movement/jump frames |
| `attack` | 0.48s  | 6      | No   | Row 1 movement/jump frames reused for attack wind-up |
| `hit`    | 0.24s  | 2      | No   | Quick row 0 flinch       |
| `death`  | 0.72s  | 6      | No   | Row 2 death dissolve; `QueueFree()` after animation |

---

## Assets

| File                              | Status    | Notes                                             |
|-----------------------------------|-----------|---------------------------------------------------|
| `GandalfHardcore FREE Warrior/GandalfHardcore Warrior.png` | In use | Player spritesheet, 80×64 px frames |
| `sprites/knight.png`, `sprites/attack.png` | Legacy | Old player spritesheets, no longer referenced by `character.tscn` |
| `GandalfHardcore Slime Enemy/Slime green.png` | In use | Green slime, 32×32 px frames |
| `GandalfHardcore Slime Enemy/Slime red.png`   | In use | Red slime, 32×32 px frames |
| `GandalfHardcore Slime Enemy/Slime blue.png`  | In use | Blue slime, 32×32 px frames |
| `sprites/slime_green.png`, `sprites/slime_purple.png` | Legacy | Old slime spritesheets, no longer referenced by slime scenes |
| `GandalfHardcore FREE Platformer Assets/Floor Tiles1.png` | In use | TileMap atlas, 288×576 px, 32×32 tiles |
| `GandalfHardcore FREE Platformer Assets/GandalfHardcore Background layers/` | In use | 3 zone BG sets (Normal/Autumn/Winter), 6 layers each |
| `sprites/coin.png`                | Unused    | Collectible — not implemented                     |
| `sprites/fruit.png`               | Unused    | Collectible — not implemented                     |
| `sprites/platforms.png`           | Unused    | Alternative platform tiles                        |

---

## What Is and Isn't Implemented

### Done
- [x] Horizontal movement with smooth deceleration
- [x] Jump (floor check) — `JumpVelocity = -250`
- [x] Gravity
- [x] Full player state machine (Idle / Move / Jump / Attack / Hurt / Dead)
- [x] All player animations keyed (idle, move, run, jump, fall, attack, hit, death)
- [x] Attack hitbox — enabled by animation, directional flip, one-hit-per-swing guard
- [x] Player health + `TakeDamage()` via `IDamageable`
- [x] Gandalf HP bar visuals: red player HP/orb, yellow sprint meter, compact enemy health bars
- [x] Sprint stamina — Shift sprint drains stamina, then constantly recharges
- [x] **Damage cooldowns** — per-attacker cooldown for slime attacks, shared i-frame timer for generic damage
- [x] Camera following player (6× zoom, Y-axis locked so background doesn't jump)
- [x] `IDamageable` interface
- [x] Green/red/blue slime enemies — full AI (Patrol / Chase / Attack / Hurt / Dead)
- [x] `ZoneManager` — 3-zone background crossfade (Normal / Autumn / Winter)
- [x] TileMap with collision — 32×32 tiles, 162 tiles wide (3 zones × 54 tiles)
- [x] Background layers — all sprites scaled to fill 1920×1080 (scale 3.5)
- [x] Sprite scaling — player and slimes rendered at 2× visual scale

### Not Done
- [ ] `PlayerAnimator.cs` / `PlayerAnimation.cs` — empty stubs, no logic
- [ ] Guard enemy (planned for floor 1–2)
- [ ] Knight enemy (planned for floor 2–3)
- [ ] Weapons system (short sword / broadsword / magic sword pickups)
- [ ] Scroll / lore system (interactive objects)
- [ ] Torch mechanic for floor 3 (`PointLight2D`)
- [ ] Full level design (3 floors with platforms, hazards, rooms)
- [ ] Coin/score collectibles
- [ ] Death / respawn flow (death anim plays but no scene restart)
- [ ] UI / HUD (health bar, score)
- [ ] Audio

---

## Known Issues / Technical Notes

- `PlayerAnimator.cs` is attached to the `PlayerAnimator` node but does nothing — its `_Process` is empty
- `PlayerAnimation.cs` exists at project root (not in `scripts/`) — likely leftover or misplaced
- Slime scenes use `collision_mask = 1`, so they collide with world geometry but do not physically push the player. They stay on enemy layer 4 so the player's `AttackHitbox` can still overlap them.
- Camera Y-lock is hardcoded to world y≈519. If ground level changes, update `limit_top` and `limit_bottom` in `character.tscn` Camera2D (`limit_top = cameraY - 90`, `limit_bottom = cameraY + 90` at zoom=6, viewport height=180)
- `ZoneManager` expects exactly `NormalBG/Sprites`, `AutumnBG/Sprites`, `WinterBG/Sprites` node paths relative to its parent

---

## Suggested Next Steps (by deadline 2026-05-20)

1. Design floor 1 in TileMap — add platforms, gaps, rooms (user places tiles manually)
2. Implement Guard enemy scene + script
3. Add death/respawn logic (restart scene or checkpoint)
4. Add HUD — health bar
5. Add scroll pickup mechanic (lore items)
6. Implement torch `PointLight2D` for floor 3
7. Add collision mask to slime scenes if enemies need to walk on platforms
