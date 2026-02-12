# AI Character Setup Guide

Quick guide for setting up AI characters using Opsive Ultimate Character Controller.

## Quick Setup (5 minutes)

### Step 1: Prepare Your AI Character

1. Drag your character prefab into the scene (e.g., `SM_Chr_Psionic_01` or `SM_Chr_Cryo_Male_01`)
2. Rename it to something meaningful like "NPC_Guard_01" or "NPC_Scientist_01"

### Step 2: Add the Character AI Controller Script

1. Select your AI character in the Hierarchy
2. Click "Add Component" in the Inspector
3. Search for "Character AI Controller" and add it
4. Set the **Character Name** field (e.g., "Dr. Chen", "Security Guard")
5. Set the **Character Role** field (e.g., "Scientist", "Guard", "Engineer")

### Step 3: Verify Opsive Components

**IMPORTANT**: The AI character MUST have Opsive's UltimateCharacterLocomotion component!

If you used a character prefab (SM_Chr_Cryo_Male_01, etc.), these components are already set up:
- ✅ **UltimateCharacterLocomotion** (required)
- ✅ **UltimateCharacterLocomotionHandler**
- ✅ **CharacterFootEffects**
- ✅ Animator with Opsive animation controller

The CharacterAIController will automatically:
- Find and use the UltimateCharacterLocomotion component
- Use Opsive's animation system (idle, walk, run animations)
- Integrate with Opsive's movement abilities for future behaviors

### Step 4: Set AI Layer (Important!)

1. Select your AI character
2. In the Inspector, change the **Layer** from "Player" to something else like:
   - "Default" (if you don't have custom layers)
   - "AI" (if you create a custom AI layer)

This prevents the AI from being detected as the player.

### Step 5: Remove Player-Only Components

Remove these components if present (they're only for the player):
- **UnityInput** component
- **CameraController** component
- Any UI-related components

### Step 6: (Optional) Add Dialogue/Voice Lines

1. Add the **NPCDialogue** component to your AI character
2. In the Inspector, set the **Dialogue Lines** size (e.g., 3 for 3 different voice lines)
3. For each dialogue line:
   - Drag an **Audio Clip** into the slot
   - Type what they say in the **Subtitle Text** box
4. Adjust settings:
   - **Interaction Range**: How close you need to be (default: 3 meters)
   - **Talk Key**: Which key to press (default: F)
   - **Voice Volume**: How loud the voice line is
   - **Spatial Audio**: Enable for 3D positional audio

### Step 7: Test It

1. Press Play
2. The AI should stand idle in place (default behavior is "Idle")
3. Walk up to the AI and look at them - their name should appear above their head!
4. If you added dialogue, walk close and you'll see "Press F to Talk"
5. Press F - they'll say a random voice line with subtitles at the bottom!
6. Check the Console - you should see: `[CharacterAI] 'Character Name' (Role) initialized. Behavior: Idle`

## AI Behaviors

The CharacterAIController integrates with Opsive and supports multiple behavior states:

- **Idle**: Character stands still - Opsive automatically plays idle animations
- **Patrol**: Character walks between waypoints - uses Opsive's PathfindingMovement ability
- **Follow** (Future): Character follows player or target - will use PathfindingMovement
- **Wander** (Future): Character randomly walks around - will use PathfindingMovement
- **Guard** (Future): Character guards position and looks around

**How Animations Work:**
- Opsive's UltimateCharacterLocomotion automatically handles ALL animations
- When AI is idle → plays idle animations
- When AI moves → automatically transitions to walk/run animations
- You don't need to write any animation code!

## Setting Up Patrol Routes

To make an AI character patrol between waypoints:

### Step 1: Create Waypoint Objects

1. In the Hierarchy, create empty GameObjects for waypoints
2. Right-click → Create Empty
3. Name them logically like "Waypoint_1", "Waypoint_2", "Waypoint_3"
4. Position them where you want the AI to walk (you can move them in the Scene view)
5. **Tip**: Group them under a parent object like "Guard_Patrol_Route" to keep organized

### Step 2: Assign Waypoints to AI

1. Select your AI character in the Hierarchy
2. Find the **CharacterAIController** component in the Inspector
3. Under **Patrol Settings**:
   - Set **Patrol Waypoints** size to how many waypoints you have (e.g., 3)
   - Drag each waypoint GameObject into the slots
   - **Loop Patrol**: Enable to go 1→2→3→1→2... (loop), Disable for 1→2→3→2→1... (ping-pong)
   - **Waypoint Reached Distance**: How close to get before moving to next (default: 1m)
   - **Wait Time At Waypoint**: How long to pause at each point (default: 2 seconds)

### Step 3: Set Behavior to Patrol

1. Still in the CharacterAIController component
2. Under **AI Behavior**, change **Current Behavior** from "Idle" to "Patrol"

### Step 4: Test It

1. Press Play
2. The AI should walk to waypoint 1, wait 2 seconds, walk to waypoint 2, wait, etc.
3. Check the Console for patrol logs if Debug Mode is enabled
4. In the Scene view while playing, you'll see:
   - Green spheres at each waypoint
   - Yellow lines connecting waypoints
   - Red line from AI to their current target waypoint
   - Cyan line showing the loop connection (if Loop Patrol is enabled)

**Tips:**
- Use empty GameObjects for waypoints - they're lightweight and easy to position
- Waypoints can be at different heights - AI will navigate stairs/ramps automatically
- You can add as many waypoints as you want (3-10 is typical for a patrol route)
- Change Wait Time to 0 if you want continuous walking with no pauses
- The AI uses Opsive's pathfinding, so they'll avoid obstacles automatically!

## Tips

### Multiple AI Characters

- Just duplicate your configured AI character
- Change the Character Name and Role for each one
- Position them around your scene

### AI Settings

**Show Name Tag**: Enabled by default - shows NPC's name when you look at them
- Name tag appears above the NPC's head
- Automatically pulls name and role from CharacterAIController
- Uses raycasting to detect when player is looking at the NPC
- Maximum display distance: 10 meters (configurable on NPCNameTag component)

**Look At Player**: Enable this to make the AI look at the player when nearby!
- The AI will automatically turn their head/eyes to look at the player
- Set **Look Distance** to control how close the player needs to be
- Uses Opsive's LocalLookSource component (added automatically)
- Works great for NPCs who should acknowledge the player

**Debug Mode**: Enable to see detailed AI logs in the console

### Name Tag Customization

If you want to customize the name tag appearance:
1. The NPCNameTag component is added automatically
2. Select the AI character and find the NPCNameTag component
3. You can adjust:
   - **Max Display Distance**: How far away you can see the name (default: 10m)
   - **Height Offset**: How high above head the name appears
   - **Name Font Size**: Size of the character name (default: 18)
   - **Role Font Size**: Size of the role text (default: 14)
   - **Name Color**: Color of the name text
   - **Role Color**: Color of the role text

### Troubleshooting

**Name tag not appearing?**

FIRST: Enable debug mode to see what's happening!
1. Select the AI character
2. Find the **NPCNameTag** component (added automatically)
3. Enable **Debug Mode** checkbox
4. Press Play and look at the Console

The debug logs will show:
- Distance to player
- Angle from camera
- When the name tag should be visible

Common issues:
- **Not close enough**: Default max distance is 10 meters - get closer!
- **Not looking directly at them**: Must be within 30° view angle (looking at center of NPC)
- **No Player tag**: Make sure your player character has the "Player" tag
- **No camera**: NPCNameTag needs Camera.main to work
- **Component not added**: Check Console for "[CharacterAI] added NPCNameTag component"

Quick test:
1. Stand right in front of the NPC (2-3 meters away)
2. Look directly at their chest/head
3. Name should appear immediately
4. Check Console logs if still not working

**AI character slides around?**
- Make sure they have a Rigidbody component
- Check that "Use Gravity" is enabled
- Verify the character is on the ground

**AI character follows player camera?**
- You forgot to remove the CameraController component
- Remove it from the AI character

**AI character responds to player input?**
- Remove the UnityInput component from the AI

**Console errors about missing components?**
- Make sure you're using an Opsive character prefab as your base
- Don't create AI from scratch - always start with a working character prefab

## NPC Dialogue System

The NPCDialogue component lets NPCs speak to the player:

**Features:**
- **Multiple voice lines**: Add as many as you want, it picks randomly
- **Subtitles**: Automatically shows what they're saying at the bottom of screen
- **Press F to Talk**: Shows prompt when you're close enough
- **Audio**: Supports 3D spatial audio (louder when closer)
- **Character name**: Automatically uses name from CharacterAIController in subtitles

**How Subtitles Work:**
- Format: `"[Character Name]: [Subtitle Text]"`
- Appears at bottom center of screen
- Has black background and outline for readability
- Stays visible for 1 second after audio finishes

**Tips:**
- You can have different NPCs with different voice lines
- Great for guards ("Move along, citizen"), shopkeepers ("Welcome!"), etc.
- If you have multiple lines, each F press picks a random one
- Subtitles can be multiple lines if needed (use the text area)

## Next Steps

When you're ready to add more AI behaviors, the CharacterAIController integrates with Opsive's system:

**Easy to Implement:**
- **Patrol routes**: Use Opsive's PathfindingMovement ability with waypoints
- **Follow player**: Use PathfindingMovement.SetDestination() (like Opsive's FollowAgent example)
- **Wander**: Pick random points and use PathfindingMovement
- **Look at objects**: Use Opsive's LocalLookSource component

**Future Expansion:**
- **Player interaction**: Dialogue, trading, quest giving
- **Combat AI**: Use Opsive's Use ability for attacks (like MeleeAgent example)
- **Animations**: All handled automatically by Opsive!
- **Group behaviors**: Coordinate multiple AI characters

Just let me know which behavior you want to add and I can implement it using Opsive's abilities!
