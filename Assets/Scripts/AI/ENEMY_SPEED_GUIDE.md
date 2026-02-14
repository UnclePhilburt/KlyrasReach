# Enemy Speed Setup Guide

## Problem: Enemies Too Fast!

The AI was forcing enemies to SPRINT by using Opsive's SpeedChange ability. Now it's fixed!

## Quick Fix

### For Slow Walking Zombies (Recommended):

1. Open Ravager prefab
2. Find **EnemyAIController** component
3. Under **Chase Settings**:
   - **Uncheck "Sprint When Chasing"** (default is now unchecked)

Done! Now they'll just walk normally instead of sprinting.

### For Fast Running Enemies:

1. Open enemy prefab
2. Find **EnemyAIController** component
3. Under **Chase Settings**:
   - **Check "Sprint When Chasing"**

Now they'll sprint like before.

## Adjusting Walk Speed (Opsive Settings)

If enemies are still too fast/slow when walking:

1. Select the enemy prefab
2. Find **UltimateCharacterLocomotion** component
3. Expand **Movement Types** > **Default**
4. Adjust these values:

**For slower zombies:**
- **Move Forward Speed**: 1.5 (default is usually 2-3)
- **Move Backward Speed**: 1.0
- **Move Strafe Speed**: 1.0

**For faster enemies:**
- **Move Forward Speed**: 3.0
- **Move Backward Speed**: 2.0
- **Move Strafe Speed**: 2.5

## Sprint Speed (Only if "Sprint When Chasing" is checked)

1. Select enemy prefab
2. Find **SpeedChange** ability in **UltimateCharacterLocomotion**
3. Expand **SpeedChange** ability
4. Adjust **Speed Change Multiplier**:
   - 1.5 = slow jog
   - 2.0 = normal sprint (default)
   - 3.0 = fast sprint

## Recommended Settings by Enemy Type

### Slow Zombies (Recommended for Ravagers):
- **Sprint When Chasing**: ✗ Unchecked
- **Move Forward Speed**: 1.5
- Shambling, slow, scary

### Fast Zombies (28 Days Later style):
- **Sprint When Chasing**: ✓ Checked
- **Speed Change Multiplier**: 2.5
- Terrifying sprinters

### Normal Enemies:
- **Sprint When Chasing**: ✗ Unchecked
- **Move Forward Speed**: 2.5
- Walks briskly toward player

### Boss Enemies:
- **Sprint When Chasing**: ✓ Checked
- **Speed Change Multiplier**: 1.5
- Slow but unstoppable charge

## Testing Speed

1. Play the game
2. Stand far from an enemy
3. Let them detect you
4. Watch them chase
5. Adjust settings until speed feels right

## Performance Note

**Sprint disabled = Better performance!**
- No SpeedChange ability activation
- Less Opsive ability processing
- Smoother with 80 enemies

For best performance with large hordes, keep "Sprint When Chasing" unchecked.
