# Switch to Opsive CrosshairsMonitor - Setup Guide

This guide will help you replace the custom AdaptiveCrosshair with Opsive's built-in CrosshairsMonitor system.

## Step 1: Remove Old Crosshair Component

1. Open the player prefab: `Assets/Characters/SM_Chr_Psionic_01.prefab`
2. Select the root GameObject
3. In the Inspector, find the **AdaptiveCrosshair** component
4. Click the three dots (⋮) and select **Remove Component**

## Step 2: Create Canvas and UI Structure

Since you don't have a Canvas yet, create one:

1. In Hierarchy, right-click → **UI → Canvas**
2. This creates:
   - Canvas
   - EventSystem (automatically)

3. Select the Canvas and set:
   - Render Mode: **Screen Space - Overlay**
   - Canvas Scaler: **Scale with Screen Size** (Reference Resolution: 1920x1080)

## Step 3: Create Crosshair GameObject

1. Right-click on Canvas → **Create Empty**
2. Rename it to **"Crosshairs"**
3. Add component: **Rect Transform**
4. Set position to center:
   - Anchors: **Center-Center**
   - Position X: **0**
   - Position Y: **0**

## Step 4: Create Crosshair Images

You can choose between two styles:

### Option A: Simple Single Dot (Recommended for now)

1. Right-click Crosshairs → **UI → Image**
2. Rename to **"Center"**
3. Set:
   - Width: **4** (or your preferred size)
   - Height: **4**
   - Color: **White**
   - You can leave Sprite as None (it will show as a white square)

### Option B: Full 5-Part Crosshair (For spread effect)

Create 5 images as children of "Crosshairs":
1. **Center** - middle dot
2. **Left** - left line/dot
3. **Top** - top line/dot
4. **Right** - right line/dot
5. **Bottom** - bottom line/dot

For each, set appropriate positions and sizes.

## Step 5: Add CrosshairsMonitor Component

1. Select the **Crosshairs** GameObject
2. Add Component → **Opsive → Ultimate Character Controller → UI → Crosshairs Monitor**

3. Configure settings:
   - **Character GameObject**: Drag your player character here (or leave empty to auto-find)
   - **Default Sprite**: (Optional - leave None for solid color square)
   - **Default Color**: White
   - **Target Color**: Red (changes when aiming at enemy)
   - **Center Crosshairs Image**: Drag the "Center" image here
   - **Movement Spread**: 0 (or 5-20 if you want spread when moving)
   - **Collision Radius**: 0.05
   - **Disable On Death**: ✓ Checked

4. If using 5-part crosshair:
   - Also assign Left, Top, Right, Bottom Image references

## Step 6: Test

1. Enter Play Mode
2. The crosshair should appear in center of screen
3. Look at an enemy - it should turn red
4. Move around - if Movement Spread > 0, the crosshair will expand

## Benefits Over AdaptiveCrosshair

- ✅ No performance cost (no Camera.Render() or ReadPixels)
- ✅ Built-in target detection
- ✅ Weapon-specific crosshairs
- ✅ Movement spread animation
- ✅ Auto color change on target
- ✅ Fully integrated with Opsive system

## Optional: Create Simple Crosshair Sprite

If you want a custom crosshair image:
1. Create a small PNG (32x32 or 64x64)
2. Draw a simple crosshair or dot
3. Import to Unity
4. Set Texture Type to **Sprite (2D and UI)**
5. Assign to **Default Sprite** in CrosshairsMonitor

## Troubleshooting

**Crosshair not showing:**
- Make sure Canvas is in **Screen Space - Overlay** mode
- Check that the Image component has a color with alpha > 0
- Verify CrosshairsMonitor is enabled

**Not changing color on enemies:**
- Enemies must be on Layer 26 (Enemy layer)
- Check Collision Radius setting
- Verify Character GameObject is assigned

**Not moving with cursor:**
- Set **Move With Cursor** to true in CrosshairsMonitor
- Adjust Controller Sensitivity if needed
