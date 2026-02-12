# Landing Light System Setup Guide

Creates synchronized flashing lights on landing pads - just like airport runway lights!

## Quick Setup (5 minutes)

### Step 1: Set Up Your Landing Pad

1. In your scene, select your landing pad GameObject (e.g., `SM_Bld_Landing_Platform_01`)
2. Add the **LandingLightController** script to it

### Step 2: Add Light GameObjects

1. Create empty child GameObjects for each light position on the pad
   - Right-click landing pad → Create Empty
   - Name them "Landing Light 1", "Landing Light 2", etc.
   - Position them around the edge of the landing pad

2. Add a **Light** component to each empty GameObject:
   - Select the light GameObject
   - Add Component → Rendering → Light
   - Set Light Type to **Point** or **Spot**
   - Adjust Range (e.g., 10-20 for landing pads)

**Tip:** Typical airport runway has 4-8 lights per side, evenly spaced.

### Step 3: Configure Flash Pattern

In the LandingLightController Inspector:

- **Flash Pattern**: Choose your pattern:
  - **Simultaneous**: All lights flash together (classic)
  - **Sequential**: Lights flash one-by-one (runway style)
  - **Alternating Pairs**: Left-right pattern
  - **Wave**: Smooth wave across the pad
  - **Pulse**: Smooth breathing effect
  - **Random Flicker**: Damaged/emergency look

- **Flash Interval**: Time between flashes (1 second = steady)
- **Flash Duration**: How long each flash lasts (0.2s = quick blink)
- **On Intensity**: Brightness when ON (2.0 = bright)
- **Flash Color**: Color of the lights (white/cyan for landing, red for danger)

### Step 4: Press Play!

That's it! The lights will flash in perfect sync.

## Flash Patterns Explained

### Simultaneous
```
All lights: ▓▓ ░░ ▓▓ ░░ ▓▓
Best for: Standard landing pads, simple beacon
```

### Sequential
```
Light 1: ▓░░░░░
Light 2: ░▓░░░░
Light 3: ░░▓░░░
Light 4: ░░░▓░░
Best for: Runway direction indicator, guiding ships in
```

### Alternating Pairs
```
Lights 1,3,5: ▓▓ ░░ ▓▓ ░░
Lights 2,4,6: ░░ ▓▓ ░░ ▓▓
Best for: Left-right directional, edge marking
```

### Wave
```
Smooth sine wave flowing across all lights
Best for: Fancy futuristic look, "active pad" indicator
```

### Pulse
```
All lights breathe together: bright → dim → bright
Best for: Calm ambient lighting, standby mode
```

### Random Flicker
```
Random chaos: ▓░▓▓░░▓░▓
Best for: Damaged pads, emergency situations, horror scenes
```

## Advanced Tips

### Multiple Landing Pads

Each landing pad can have its own pattern:
- **Pad 1**: Sequential (green lights) - active landing zone
- **Pad 2**: Pulse (blue lights) - standby
- **Pad 3**: Random Flicker (red lights) - damaged/offline

### Creating Edge Lighting

Place lights in a line along the edge:
```
[Light] --- [Light] --- [Light] --- [Light]
```
Use **Sequential** pattern to create a "runway" effect.

### Creating Corner Beacons

Place 4 bright lights at corners, use **Simultaneous** for strong visibility.

### Controlling at Runtime

```csharp
// Get the controller
LandingLightController controller = GetComponent<LandingLightController>();

// Control lights
controller.StartFlashing();
controller.StopFlashing();

// Change pattern dynamically
controller.SetFlashPattern(FlashPattern.Sequential);
controller.SetFlashInterval(0.5f); // Faster flashing
```

### Example: Landing Sequence Script

```csharp
// When ship approaches
controller.SetFlashPattern(FlashPattern.Sequential); // Guide them in
controller.SetFlashInterval(0.3f); // Fast flashing

// When ship lands
controller.SetFlashPattern(FlashPattern.Pulse); // Calm pulse
controller.SetFlashInterval(2f); // Slow breathing
```

## Recommended Settings

### Standard Landing Pad (Safe)
- Pattern: **Simultaneous**
- Interval: 1.0s
- Duration: 0.2s
- Color: Cyan or White
- Intensity: 2.0

### Active Runway (Guiding Ships)
- Pattern: **Sequential**
- Interval: 0.5s
- Duration: 0.15s
- Color: Green
- Intensity: 3.0

### Emergency/Danger Pad
- Pattern: **Alternating Pairs**
- Interval: 0.3s
- Duration: 0.2s
- Color: Red
- Intensity: 4.0

### Standby Mode
- Pattern: **Pulse**
- Pulse Speed: 1.5
- Color: Blue
- Intensity: 1.5

## Troubleshooting

**Lights not flashing?**
- Make sure Auto Start is checked
- Verify child GameObjects have Light components
- Check that lights are not disabled

**Lights out of sync?**
- They should always be in sync automatically
- If you add lights at runtime, save and reload the scene

**Want bigger/smaller light range?**
- Select each Light GameObject
- Adjust the **Range** property on the Light component

**Want different colors per light?**
- The system uses one color for all lights
- For multi-color, use multiple LandingLightControllers with different light groups
