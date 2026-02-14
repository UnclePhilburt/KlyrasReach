# Blood Effects Setup Guide

## Overview
Enemies now have TWO blood systems:
1. **Hit Blood** - Small blood spray every time they're shot
2. **Death Blood** - Massive explosion when they die

The enemy body vanishes on death and is replaced by a particle effect.

## Available Blood Effects

You have 3 blood effects from Synty's PolygonParticleFX pack:

### 1. FX_BloodSplat_01 (Recommended for enemies)
**Location:** `Assets/PolygonParticleFX/Prefabs/FX_BloodSplat_01.prefab`
- Large blood splatter
- Blood shoots up and falls down
- Good for normal enemy deaths

### 2. FX_BloodSplat_Small_01
**Location:** `Assets/PolygonParticleFX/Prefabs/FX_BloodSplat_Small_01.prefab`
- Smaller blood spray
- Good for small enemies or weak hits
- Less dramatic

### 3. FX_Explosion_Body_Bloody_01 (Most dramatic)
**Location:** `Assets/PolygonParticleFX/Prefabs/FX_Explosion_Body_Bloody_01.prefab`
- Massive blood explosion
- Gore chunks fly everywhere
- Best for big enemy deaths or explosions
- Very satisfying

## How to Set Up

### Complete Setup (Hit + Death Blood)
1. Open your enemy prefab (e.g., Ravager)
2. Find the **EnemyAIController** component

3. **Death Effects** section:
   - Check "Use Blood Effect"
   - Drag `FX_BloodSplat_01.prefab` (or `FX_Explosion_Body_Bloody_01`) into "Blood Effect Prefab"

4. **Hit Effects** section:
   - Check "Use Hit Blood Effect"
   - Drag `FX_BloodSplat_Small_01.prefab` into "Hit Blood Effect Prefab"
   - Set "Min Damage For Blood" to 1 (shows blood on any hit)

5. Done!

### Option B: Set in RavagerSpawner
You can also set different blood effects for different enemy types:
1. Open your RavagerSpawner in the Inspector
2. Select the enemy prefab
3. Configure blood effect on that prefab

## What Happens When Shot

1. Bullet hits enemy
2. Opsive damage event fires
3. **Small blood spray spawns** at bullet impact point
4. Blood faces away from shooter (spray direction)
5. After 2 seconds, blood cleans up

## What Happens on Death

1. Enemy health reaches 0
2. **Massive blood explosion spawns** at enemy position
3. **Enemy body vanishes** immediately (all renderers disabled)
4. **Health bar hides**
5. Blood particles shoot up and fall down
6. After 5 seconds, blood effect cleans up
7. Enemy returns to object pool for respawn

## Performance Notes

- Blood effects auto-destroy after 3 seconds
- Very lightweight compared to ragdolls
- Works perfectly with object pooling
- No physics calculations (just particles)

## Customization

You can adjust blood amount by editing the prefab:
- More particles = more blood
- Faster velocity = blood shoots higher
- Longer lifetime = blood stays longer

## Recommendations

### Hit Blood (Every Shot)
**Best setup:**
- Use `FX_BloodSplat_Small_01` - small spray, not overwhelming
- Min Damage: 1 (shows on every hit)

**Performance mode (80 enemies):**
- Use `FX_BloodSplat_Small_01` - already lightweight
- Min Damage: 5 (only shows on bigger hits)

### Death Blood (Final Kill)
**For normal zombies/enemies:**
- Use `FX_BloodSplat_01` - good balance

**For boss enemies:**
- Use `FX_Explosion_Body_Bloody_01` - MAXIMUM GORE with flying chunks

**For weak enemies:**
- Use `FX_BloodSplat_Small_01` - subtle

**For robots/mechanical enemies:**
- Disable both blood effects
- Or create sparks/oil effects instead

## Performance Impact

With 80 enemies and hit blood enabled:
- Each bullet spawns 1 small particle effect
- Each death spawns 1 large particle effect
- All auto-cleanup after 2-5 seconds
- **Very lightweight** - just particles, no physics

If you experience lag, increase "Min Damage For Blood" to reduce blood spam.
