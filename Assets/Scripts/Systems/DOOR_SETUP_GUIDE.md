# Automatic Sliding Door Setup Guide

## SUPER SIMPLE METHOD (Recommended!)

Use **SimpleSlidingDoor.cs** - just attach directly to each door panel!

### Setup (3 Steps):

**Step 1:** Select your door object in the Hierarchy (the actual Synty door model)

**Step 2:** Click "Add Component" in Inspector → Search for "Simple Sliding Door" → Add it

**Step 3:** Configure the settings:
- **Slide Direction X**: `-1` for left, `+1` for right
- **Slide Distance**: `2` (how far it slides)
- **Slide Speed**: `2` (how fast)
- **Detection Range**: `3` (how close player needs to be)
- **Player Tag**: `Player`

**Done!** Press Play and walk near it!

---

## ADVANCED METHOD (For Complex Setups)

Use **AutomaticSlidingDoor.cs** - for paired doors or special configurations.

### Step 1: Create Door Parent Object
1. In your Hierarchy, **Right-click → Create Empty**
2. Name it `SlidingDoor_Entrance` (or whatever makes sense)
3. Position it where you want the door to be

### Step 2: Create Door Panels
You need visual door objects (from Synty assets). Two options:

**Option A - Double Doors (Left & Right):**
1. Drag a Synty door model into the scene TWICE
2. Make both children of your SlidingDoor_Entrance parent
3. Name them `LeftPanel` and `RightPanel`
4. Position them to form a closed doorway

**Option B - Single Door:**
1. Drag ONE Synty door model into the scene
2. Make it a child of SlidingDoor_Entrance
3. Name it `LeftPanel` (we still call it left even if it's alone)

### Step 3: Add the Script
1. Select your `SlidingDoor_Entrance` parent object
2. In the Inspector, click **Add Component**
3. Search for `Automatic Sliding Door`
4. Click it to add the script

### Step 4: Configure the Script
In the Inspector, you'll see settings. Fill them out:

**Door Panel References:**
- **Left Door Panel**: Drag your `LeftPanel` object here
- **Right Door Panel**: Drag your `RightPanel` here (if you have one, leave empty if single door)

**Door Behavior Settings:**
- **Slide Distance**: `2` (how far the door slides - adjust to fit your doorway)
- **Slide Speed**: `2` (how fast - higher = faster)
- **Detection Range**: `3` (how close player needs to be)
- **Slide Direction**:
  - For doors that slide sideways: `X=1, Y=0, Z=0` (left/right)
  - For doors that slide up: `X=0, Y=1, Z=0` (up/down)

**Player Detection:**
- **Player Tag**: `Player` (make sure your player GameObject has the "Player" tag!)

**Audio (Optional):**
- Leave empty for now, or add door sound effects later

### Step 5: Test It!
1. Press Play in Unity
2. Walk your character toward the door
3. Doors should slide open automatically!
4. Walk away - doors should close

---

## Common Issues & Solutions

### Issue: "No door panel assigned!" error
**Solution:** You forgot Step 4 - drag your door panel objects into the script's Inspector fields

### Issue: Doors don't open when I approach
**Possible causes:**
1. **Player tag not set** - Select your player GameObject, at top of Inspector set Tag to "Player"
2. **Detection range too small** - Increase "Detection Range" in the door script
3. **Player has no collider** - Player needs a CharacterController or Collider to trigger the door

### Issue: Doors slide the wrong direction
**Solution:** Change the "Slide Direction" values:
- Left/Right: `(1, 0, 0)` or `(-1, 0, 0)`
- Up/Down: `(0, 1, 0)` or `(0, -1, 0)`
- Forward/Back: `(0, 0, 1)` or `(0, 0, -1)`

### Issue: Doors open too slow/fast
**Solution:** Adjust "Slide Speed" (higher = faster, lower = slower)

### Issue: Doors don't slide far enough
**Solution:** Increase "Slide Distance" value

### Issue: Doors open when I'm too far away
**Solution:** Decrease "Detection Range" value

---

## Visual Guide

```
Scene Hierarchy Should Look Like This:

SlidingDoor_Entrance (Empty GameObject with AutomaticSlidingDoor script)
├── LeftPanel (Synty door model)
└── RightPanel (Synty door model) [optional]
```

---

## Advanced Tips

### Making Door Prefabs
Once you have a door working perfectly:
1. Drag the entire `SlidingDoor_Entrance` from Hierarchy to Project window
2. Now you have a prefab you can reuse anywhere!
3. Just drag the prefab into your scene wherever you need doors

### Adding Sound Effects
1. Find/import door sound effects (open and close sounds)
2. Drag them into the "Open Sound" and "Close Sound" fields in the Inspector
3. Doors will now play sounds when opening/closing!

### Adjusting Trigger Zone Size
The script automatically creates an invisible trigger zone. If you want to see it:
1. Select your door in the Hierarchy
2. Look in Scene view (not Game view)
3. You'll see a green wireframe box - that's the trigger zone
4. Adjust "Detection Range" to change its size

---

## Next Steps

Once you have basic doors working:
- Create multiple door prefabs for different areas
- Add door sound effects from a sci-fi sound pack
- Experiment with slide directions (up, diagonal, etc.)
- Create locked doors (we'll add this system later!)

---

*Last Updated: February 10, 2026*
