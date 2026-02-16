# Ship Pilot System - Setup Guide

## Overview
This system connects your stationary ship interior to a flyable pilot ship. Players spawn in the interior and can pilot the ship by pressing F at the pilot seat. The ship is controlled in third-person view while the player stays hidden.

## What You Created
- **PilotSeatInteraction.cs** - Handles the F key interaction in the interior
- **ShipPilotController.cs** - Manages hiding/showing the pilot and exiting
- **ShipInteriorManager.cs** - Handles hiding/showing the interior (optional)

Your **TransportShipPilot** and **TransportShipInterior** prefabs already have most of what you need!

---

## Setup Instructions

### Step 1: Setup the Ship Interior (TransportShipInterior)

1. **Select TransportShipInterior** in your scene or prefab
2. **Optionally add ShipInteriorManager component:**
   - This is only needed if you want to hide the interior visually
   - Click "Add Component" → "ShipInteriorManager"
   - ✅ Check "Hide Interior On Start"

### Step 2: Setup the Pilot Seat (Inside the Interior)

1. **Find or create the pilot seat GameObject** in your ship interior
2. **Add a Collider component** if it doesn't have one:
   - Click "Add Component" → "Box Collider" (or any collider)
   - ✅ Check "Is Trigger"
   - Adjust the size so it covers the area where players can interact
3. **Add the PilotSeatInteraction component:**
   - Click "Add Component" → "PilotSeatInteraction"
4. **Configure the settings:**
   - **Pilot Ship:** Drag your **TransportShipPilot** GameObject here (the one with ShipController)
   - **Interaction Distance:** Leave at 3

### Step 3: Setup the Pilot Ship (TransportShipPilot)

Your TransportShipPilot already has a **ShipController** component! You just need to add one more component:

1. **Select TransportShipPilot** in your scene
2. **Add the ShipPilotController component:**
   - Click "Add Component" → "ShipPilotController"
3. **Configure the settings:**
   - **Ship Interior:** Drag your **TransportShipInterior** GameObject here
   - **Exit Spawn Point:** Create an empty GameObject inside the interior where players return when exiting (see Step 4)
   - **Exit Key:** Set to "E" (or whatever key you want for exiting)

### Step 4: Create Exit Spawn Point

You only need ONE spawn point - where players return to when exiting:

**Exit Spawn Point** (in the ship interior):
1. In the scene, right-click **TransportShipInterior** → Create Empty
2. Name it "ExitSpawnPoint"
3. Position it where players should return when exiting (near the pilot seat is good)
4. Drag this to the "Exit Spawn Point" field in ShipPilotController (on the pilot ship)

---

## How It Works

### For Players:
1. Player spawns in the ship interior (optionally hidden)
2. Player walks to the pilot seat
3. A prompt appears: "Press F to Pilot Ship"
4. Player presses F
5. **Player becomes invisible** and the camera switches to third-person view of the pilot ship
6. Player can now fly the ship using WASD, mouse, Space/Ctrl
7. Other players stay in the stationary interior
8. Press **E** to exit - player reappears back in the interior

### Behind the Scenes:
- Your **ShipController** already handles all the flying controls!
- The ship interior stays stationary (doesn't move)
- The pilot ship moves and flies based on player input
- Player character is hidden while piloting (becomes invisible)
- Camera follows the pilot ship in third-person view
- Works with Photon multiplayer

---

## Optional: Add UI Prompt

To show a "Press F to Pilot Ship" message:

1. Create a UI Canvas → Text element
2. Position it where you want the prompt to appear
3. Disable it by default (uncheck the box at the top of Inspector)
4. Drag this UI element to the "Prompt UI" field in PilotSeatInteraction

---

## Testing

1. Start the game
2. Your player should spawn in the interior
3. Walk to the pilot seat
4. Press **F** - your player should become invisible and camera should switch to the pilot ship
5. Try flying with **WASD**, **mouse**, **Space/Ctrl**
6. Press **E** - you should reappear back in the interior

---

## Troubleshooting

**Problem: Nothing happens when I press F**
- Make sure the pilot seat has a Collider with "Is Trigger" checked
- Make sure **Pilot Ship** is assigned in PilotSeatInteraction (drag TransportShipPilot)
- Make sure TransportShipPilot has both **ShipController** AND **ShipPilotController** components
- Check the Console for error messages

**Problem: Ship doesn't move when I press keys**
- Check that **ShipController** is enabled on TransportShipPilot
- Look in the Inspector at ShipController settings - make sure max speed is > 0
- Check Console for "[ShipController] INPUT DETECTED" messages

**Problem: Can't exit the pilot ship**
- Make sure **Exit Spawn Point** is assigned in ShipPilotController
- Make sure it's inside/near the ship interior
- Try pressing **E** (or whatever exit key you set)

**Problem: Player is still visible while flying**
- This could be an issue with how player renderers are being found
- Check if your player has renderers on child objects
- You may need to adjust the SetPlayerVisible() method in the scripts

**Problem: Other players can't see the pilot flying**
- The pilot ship should be visible to everyone
- Make sure TransportShipPilot has a **PhotonView** and **PhotonTransformView** (it should already)
- The pilot's character is hidden, but the ship itself should be visible

---

## Customization

You can customize these scripts:
- Change the interaction key from F to something else
- Change the exit key from E to something else
- Add sound effects when entering/exiting
- Add animations when teleporting
- Make the interior visible instead of hidden

Let me know if you need help with any customizations!
