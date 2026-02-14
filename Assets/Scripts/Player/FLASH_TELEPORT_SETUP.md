# Flash Teleport Ability Setup Guide

## Overview
The Flash Teleport is a sci-fi teleport ability where your character **completely disappears** and **reappears** at a new location with cool portal effects!

**How it works:**
1. Player presses teleport key
2. Portal effect spawns at current position
3. Character becomes **invisible**
4. Character teleports forward 8 meters
5. Portal effect spawns at new position
6. Character becomes **visible** again
7. Ability goes on cooldown

## Quick Setup

### 1. Add Ability to Player

1. Open your **Player** prefab in the Inspector
2. Find the **UltimateCharacterLocomotion** component
3. Scroll down to **Abilities**
4. Click the **+** button to add a new ability
5. Select **KlyrasReach.Player.FlashTeleport** from the dropdown

### 2. Configure Keybind

1. Still in **UltimateCharacterLocomotion** > **Abilities**
2. Find the **FlashTeleport** ability you just added
3. Set **Input Name** to something like:
   - `"Teleport"` (then map this in Unity's Input Manager)
   - Or use a specific key like `"LeftShift"` or `"Space"`

### 3. Assign Portal Effects

The best portal effects from your PolygonParticleFX pack are:

**Recommended Setup:**
1. In the **FlashTeleport** ability settings, expand **Visual Effects**
2. Assign these prefabs:
   - **Teleport Out Effect Prefab**: `FX_Portal_Sphere_01` (or `FX_Portal_Round_01`)
   - **Teleport In Effect Prefab**: `FX_Portal_Sphere_01` (or `FX_Portal_Round_01`)
   - **Extra Sparkle Effect**: `FX_Sparkles_Small_01` (optional but cool!)

**Where to find them:**
- Location: `Assets/PolygonParticleFX/Prefabs/`
- Drag them from the Project window into the Inspector slots

### 4. Adjust Settings (Optional)

**Teleport Settings:**
- **Teleport Distance**: 8 (how far to teleport in meters)
- **Teleport Duration**: 0.3 seconds (how long the teleport animation takes)
- **Cooldown**: 2 seconds (time between teleports)
- **Invulnerable During Teleport**: ✓ Checked (player can't take damage while teleporting)

## Available Portal Effects

You have several cool portal effects to choose from:

### FX_Portal_Sphere_01 (Recommended)
- Spherical energy portal
- Looks like character enters/exits a portal sphere
- Very sci-fi!

### FX_Portal_Round_01 (Also Great)
- Circular flat portal
- Character appears to go through a disc
- Stargate-style

### FX_Portal_Thin_01
- Thin vertical portal
- More subtle effect
- Good for stealth characters

### Extra Effects
- **FX_Sparkles_Small_01** - Add sparkles for extra flair
- **FX_Glow_Blue_01** - Blue glow during teleport
- **FX_Trail_Blue_01** - Leave a blue trail

## How to Test

1. **Play the game**
2. **Press your teleport key** (e.g., Left Shift)
3. You should see:
   - Portal effect spawns at current location
   - Character disappears completely
   - Character reappears 8 meters forward
   - Portal effect spawns at new location
   - Sparkles appear (if enabled)

## Troubleshooting

### "Character doesn't disappear, just moves"
- Make sure you're using the **FlashTeleport** script, not a dash ability
- Check that **Teleport Duration** is > 0

### "No portal effects appearing"
- Make sure you've assigned the effect prefabs in the Inspector
- Check that the prefabs are from `Assets/PolygonParticleFX/Prefabs/`
- Look in the console for errors

### "Teleport doesn't activate"
- Check that you're **grounded** (can't teleport in mid-air)
- Make sure ability is **off cooldown** (wait 2 seconds between teleports)
- Verify your **Input Name** matches Unity's Input Manager

### "Character stays invisible"
- This means the ability was interrupted
- Try stopping and restarting play mode
- Check console for errors in the TeleportSequence coroutine

### "Teleporting through walls"
- The ability has wall detection built-in
- If it's not working, check that walls have colliders
- Make sure walls are on the correct collision layer

## Advanced Customization

### Teleport to Mouse Cursor
Currently teleports in movement direction (or forward if standing still).
To teleport toward mouse cursor, you'd need to modify the teleport direction calculation.

### Different Effects Per Character
You can have different portal colors for different characters:
- Blue portals for one character
- Red portals for another
- Just assign different FX prefabs to each character's FlashTeleport ability

### UI Cooldown Indicator
The script has helper methods for UI:
```csharp
GetCooldownPercent() // Returns 0-1, where 1 = ready
IsOnCooldown() // Returns true if still on cooldown
```

Use these to create a cooldown timer UI element.

## Performance Notes

**Very Lightweight!**
- Only spawns 2-3 particle effects per teleport
- Effects auto-destroy after 2-3 seconds
- No physics calculations
- Character is invulnerable during teleport (no damage checks needed)

Works great even with 80 enemies active!

## Recommended Settings by Playstyle

### Fast Combat (Overwatch Tracer Style)
- **Teleport Distance**: 5 meters (shorter, more frequent)
- **Cooldown**: 1 second
- **Duration**: 0.2 seconds (very snappy)

### Tactical Positioning
- **Teleport Distance**: 10 meters (longer range)
- **Cooldown**: 3 seconds
- **Duration**: 0.4 seconds (more dramatic)

### Escape Ability
- **Teleport Distance**: 15 meters (far escape)
- **Cooldown**: 5 seconds
- **Duration**: 0.3 seconds

### Balanced (Default)
- **Teleport Distance**: 8 meters
- **Cooldown**: 2 seconds
- **Duration**: 0.3 seconds
