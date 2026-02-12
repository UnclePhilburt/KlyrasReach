# Ship Flight System Setup Guide

## Quick Setup (5 Steps)

### Step 1: Prepare Your Ship
1. Drag a Synty ship model into your scene
2. Make sure it has a collider (use the Collider Adder tool if needed)
3. Position it where you want it in your hangar

### Step 2: Add Ship Scripts
1. Select the ship in Hierarchy
2. Add Component → Search "Ship Controller" → Add it
3. Add Component → Search "Ship Entry Point" → Add it

### Step 3: Configure Ship Controller
In the Inspector, set these values:

**Flight Characteristics:**
- **Max Speed**: `50` (how fast it goes forward)
- **Max Reverse Speed**: `20` (backward speed)
- **Acceleration**: `10` (how quickly it speeds up)
- **Deceleration**: `5` (how quickly it slows down)
- **Strafe Speed**: `25` (left/right speed)
- **Vertical Speed**: `20` (up/down speed)

**Turn Rate:**
- **Turn Speed**: `2` (how responsive turning is)
- **Mouse Sensitivity**: `3` (mouse look speed)

**Camera:**
- **Ship Camera**: Drag your Main Camera here
- **Camera Offset**: `(0, 2, -10)` (camera position behind ship)
- **Camera Smoothing**: `5` (how smoothly camera follows)

**Ship State:**
- **Is Active**: Leave UNCHECKED (player starts on foot)

### Step 4: Configure Ship Entry Point
**Entry Settings:**
- **Entry Range**: `3` (how close to walk to enter)
- **Interact Key**: `F`
- **Player Tag**: `Player`

**Exit Settings:**
- **Exit Offset**: `(2, 0, 0)` (where player appears when exiting)

**UI:**
- **Show Prompt**: Check this to show "Press F to Enter Ship"

### Step 5: Test It!
1. Make sure your player has the "Player" tag
2. Press Play
3. Walk up to the ship (within 3 meters)
4. You should see "Press F to Enter Ship"
5. Press F - you should enter the ship!

## Flight Controls

Once in the ship:
- **W** - Forward
- **S** - Backward
- **A** - Strafe Left
- **D** - Strafe Right
- **Space** - Up
- **Ctrl** - Down
- **Mouse** - Look/Aim
- **F** - Exit Ship

---

## Creating Different Ship Types

You can make each ship fly differently by adjusting the values!

### Example: "Sally" - Shitty Ship
Make a ship that flies poorly:
- Max Speed: `30` (slow)
- Acceleration: `3` (sluggish)
- Turn Speed: `0.8` (poor handling)
- Mouse Sensitivity: `1.5` (unresponsive)

### Example: "Raptor" - Fighter Ship
Make a fast, agile ship:
- Max Speed: `80` (very fast)
- Acceleration: `20` (quick)
- Turn Speed: `5` (very responsive)
- Strafe Speed: `40` (nimble)

### Example: "Hauler" - Cargo Ship
Make a slow but stable ship:
- Max Speed: `40` (moderate)
- Acceleration: `5` (heavy feel)
- Turn Speed: `1` (slow turning)
- Vertical Speed: `10` (struggles to climb)

---

## Common Issues

### Issue: Ship doesn't respond to input
**Solution:** Make sure "Is Active" is UNCHECKED in Ship Controller. Player needs to enter first.

### Issue: Can't enter ship
**Possible causes:**
1. Player doesn't have "Player" tag
2. Entry Range too small - increase it
3. Ship Entry Point script not added

### Issue: Camera doesn't follow ship
**Solution:** Make sure you dragged Main Camera into "Ship Camera" field

### Issue: Ship rotates weirdly
**Solution:**
- Check that the ship model's forward direction is correct
- The ship's blue arrow (in Scene view) should point forward
- If not, rotate the ship model to face the right way

### Issue: Player falls through ship when exiting
**Solution:**
- Make sure ship has colliders
- Adjust "Exit Offset" to spawn player further away from ship

### Issue: Can't exit ship
**Solution:** Press F while piloting

---

## Advanced: Making Ship Prefabs

Once you have a ship configured perfectly:

1. Drag the ship from Hierarchy to Project window
2. Now you have a reusable prefab!
3. Any time you want that ship type, just drag the prefab into your scene
4. All settings are saved in the prefab

**Example Workflow:**
- Create "Sally_Prefab" with poor flight stats
- Create "Raptor_Prefab" with good flight stats
- Create "Hauler_Prefab" with cargo ship stats
- Now you can spawn multiple ships of each type easily!

---

## Tips

1. **Test flight feel** - Adjust Max Speed and Acceleration until it feels good
2. **Camera position** - Change Camera Offset Y value for higher/lower view
3. **Turn speed** - Lower = more realistic, Higher = more arcade-style
4. **Entry range** - Make it bigger if players have trouble finding the prompt

---

## Next Features (Coming Later)

- Ship weapons/combat
- Ship health/damage
- HUD for ship (speed, altitude, etc.)
- Boost ability
- Landing gear
- Multiple camera views (first-person cockpit)
- Ship-specific sound effects

---

*Last Updated: February 10, 2026*
