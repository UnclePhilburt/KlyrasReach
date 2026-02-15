# Multiplayer Setup Guide - PUN 2 Integration

This guide will walk you through setting up your character for multiplayer using Opsive's PUN 2 Integration.

## Part 1: Prepare Your Player Prefab

### Step 1: Locate Your Player Prefab
1. Find your player prefab: `Assets/Characters/SM_Chr_Psionic_01.prefab`
2. Open it in Prefab mode (double-click in Project window)

### Step 2: Add Required Network Components

**Add these components to the ROOT GameObject:**

1. **PhotonView** (Photon → PhotonView)
   - This is the core networking component
   - Leave settings as default for now

2. **PunCharacter** (Add Component → Opsive → Ultimate Character Controller → Add-Ons → Multiplayer → PhotonPUN → Character → Pun Character)
   - Handles character RPCs and networked state
   - This will auto-configure most settings

3. **PunCharacterTransformMonitor** (Add Component → Opsive → Ultimate Character Controller → Add-Ons → Multiplayer → PhotonPUN → Character → Pun Character Transform Monitor)
   - Syncs position and rotation across network
   - Set **Network Transform** to the character's Transform
   - Set **Update Mode** to "Interpolate" for smooth movement

4. **PunCharacterAnimatorMonitor** (OPTIONAL - if you use animations)
   - Add Component → Opsive → Ultimate Character Controller → Add-Ons → Multiplayer → PhotonPUN → Character → Pun Character Animator Monitor
   - Syncs animations across network

### Step 3: Configure PhotonView

After adding all components:

1. Select the **PhotonView** component
2. Click **"Add Observed Component"** multiple times
3. Add these to the Observed Components list:
   - **PunCharacter**
   - **PunCharacterTransformMonitor**
   - **PunCharacterAnimatorMonitor** (if you added it)

4. Set **Ownership Transfer**: "Request"
5. Set **Synchronization**: "Unreliable On Change"

### Step 4: Move Prefab to Resources Folder

**IMPORTANT:** For PhotonNetwork.Instantiate() to work, the prefab MUST be in a Resources folder:

1. Create folder if it doesn't exist: `Assets/Resources/`
2. Create subfolder: `Assets/Resources/Characters/`
3. **Move (not copy)** your `SM_Chr_Psionic_01.prefab` to:
   - `Assets/Resources/Characters/SM_Chr_Psionic_01.prefab`

### Step 5: Update PlayerSpawnPoint Reference

Since you moved the prefab:

1. Find any **PlayerSpawnPoint** GameObjects in your scenes
2. Re-assign the **Player Prefab** field to point to the new location
3. The path in code will now be: `"Characters/SM_Chr_Psionic_01"` (without .prefab extension)

---

## Part 2: Create Multiplayer Connection Scene

### Step 6: Create Lobby Scene

1. Create new scene: **File → New Scene**
2. Save as: `Assets/Scenes/MultiplayerLobby.scene`

3. Create a **Canvas** for UI:
   - Right-click Hierarchy → UI → Canvas
   - This creates Canvas + EventSystem

4. Add connection buttons:
   - Right-click Canvas → UI → Button - TextMeshPro
   - Rename to "ConnectButton"
   - Change text to "Connect to Photon"

### Step 7: Create Connection Manager Script

I'll create this script for you - it will handle:
- Connecting to Photon servers
- Creating/joining rooms
- Loading game scene when connected

---

## Part 3: Update Game Scenes for Multiplayer

### Step 8: Add Spawn Manager to Scenes

For each gameplay scene (planet surfaces, etc.):

1. Create empty GameObject, name it **"Network Manager"**

2. Add Component: **SingleCharacterSpawnManager**
   - Component → Opsive → Ultimate Character Controller → Add-Ons → Multiplayer → PhotonPUN → Game → Single Character Spawn Manager

3. Configure it:
   - **Character Prefab**: `Characters/SM_Chr_Psionic_01` (path in Resources)
   - **Camera**: Assign your Main Camera prefab (or leave empty)
   - **Spawn Points**: Assign your PlayerSpawnPoint GameObject

4. **Remove/Disable** the old PlayerSpawnPoint script's "Spawn On Start"
   - The SpawnManager will handle spawning now

### Step 9: Test Workflow

**Single Player Testing:**
1. Start in **MultiplayerLobby** scene
2. Click "Connect to Photon"
3. Game creates room and loads your game scene
4. Character spawns via PUN
5. Camera attaches, crosshair works

**Multiplayer Testing (2 instances):**
1. Build your game: File → Build Settings → Build
2. Run the built exe
3. Also run Play mode in Unity Editor
4. Both connect to same room
5. You should see each other's characters!

---

## Part 4: What Still Needs Networking

These systems will need additional work:

- ❌ **Ships** - Need PhotonView components
- ❌ **NPCs/Enemies** - Only host spawns them
- ❌ **Weapons/Combat** - Needs damage syncing
- ❌ **Inventory** - Opsive handles basic sync, but custom items need work
- ❌ **Quests/Progression** - Need custom networking

But for basic multiplayer (players walking around, seeing each other), the above steps are enough!

---

## Next Steps

After this guide is complete, I'll create the connection manager script and update your existing scripts to work with multiplayer spawning.

Ready to start? Open your player prefab and let me know when you reach Step 2!
