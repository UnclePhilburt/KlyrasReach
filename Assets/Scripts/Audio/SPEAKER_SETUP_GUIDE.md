# Speaker System Setup Guide

## Quick Setup (5 minutes)

### Step 1: Create Audio Zone Manager

1. In your Unity scene, create an empty GameObject
2. Name it **"Audio Zone - Main Deck"** (or whatever zone name you want)
3. Add the **AudioZoneManager** script to it (`Assets/Scripts/Audio/AudioZoneManager.cs`)
4. In the Inspector:
   - Set the **Playlist** size (e.g., 5 for 5 songs)
   - Drag your music AudioClips into the playlist slots
   - Choose a **Playlist Mode**:
     - **Sequential**: Plays songs in order (1, 2, 3, 4, 5, repeat)
     - **Random**: Picks random song each time
     - **Shuffle**: Shuffles once, plays in shuffled order
     - **Loop**: Loops first song forever

### Step 2: Add Speakers Around the Station

1. Place speaker models around your space station (use any prop - wall panels, greebles, etc.)
   - Suggested props from PolygonSciFiSpace:
     - `SM_Prop_Wall_Panel_Small_01`
     - `SM_Prop_Greeble_Panel_01`
     - Any wall-mounted prop you like!

2. Parent all speaker GameObjects under the "Audio Zone - Main Deck" object you created

3. Add an **AudioSource** component to each speaker GameObject:
   - In Unity, select the speaker
   - Click "Add Component"
   - Search for "Audio Source"
   - Click to add it

4. **That's it!** The AudioZoneManager will automatically configure all child speakers when you press Play

### Step 3: Test It

1. Press Play in Unity
2. Music should start playing from all speakers simultaneously
3. Walk around - speakers closer to you will be louder

## Advanced Settings

### AudioZoneManager Settings

- **Playlist**: Array of AudioClips to play
- **Playlist Mode**:
  - **Sequential**: Play songs in order
  - **Random**: Random song each time
  - **Shuffle**: Shuffle once and play through
  - **Loop**: Loop single song (first in playlist)
- **Volume**: Master volume for all speakers in this zone (0-1)
- **Play On Start**: Auto-play when scene starts?
- **Min Distance**: Distance where audio is at full volume
- **Max Distance**: Distance where audio fades to silent
- **Spatial Blend**:
  - 0 = 2D audio (same volume everywhere)
  - 1 = 3D audio (volume based on distance)

### Playlist Features

- **Auto-advance**: Songs automatically play next when finished (except in Loop mode)
- **Multiple modes**: Sequential, Random, Shuffle, or Loop
- **Runtime control**: Skip forward/back, play specific tracks
- Songs play synchronized across all speakers in the zone

### Multiple Audio Zones

You can have multiple audio zones playing different music:

1. Create **"Audio Zone - Docking Bay"** with its own speakers
2. Create **"Audio Zone - Medical Bay"** with its own speakers
3. Each zone plays different music independently
4. Speakers automatically play the correct zone's music

### Controlling Music at Runtime

```csharp
// Get the AudioZoneManager
AudioZoneManager audioZone = GetComponent<AudioZoneManager>();

// Control playback
audioZone.PlayMusic();
audioZone.StopMusic();
audioZone.PauseMusic();
audioZone.ResumeMusic();

// Playlist navigation
audioZone.PlayNextTrack();        // Skip to next song
audioZone.PlayPreviousTrack();    // Go to previous song
audioZone.PlayTrack(2);           // Play track at index 2

// Get playlist info
string currentSong = audioZone.GetCurrentTrackName();
int currentIndex = audioZone.GetCurrentTrackIndex();
int totalTracks = audioZone.GetPlaylistLength();
Debug.Log($"Playing: {currentSong} ({currentIndex + 1}/{totalTracks})");

// Change volume
audioZone.SetVolume(0.8f); // 0-1

// Modify playlist at runtime
audioZone.ChangePlaylist(newPlaylistArray);
audioZone.AddTrackToPlaylist(newSongClip);
```

## Troubleshooting

**No sound playing?**
- Make sure you assigned an AudioClip to the AudioZoneManager
- Check that speakers are child objects of the AudioZoneManager
- Verify each speaker has an AudioSource component

**Speakers not synchronized?**
- All speakers are started at the exact same time automatically
- If you add speakers at runtime, call `audioZone.RefreshSpeakers()`

**Audio too loud/quiet?**
- Adjust the Volume slider on the AudioZoneManager (affects all speakers)
- Adjust Min/Max Distance for larger/smaller zones

**Want speakers to cover bigger area?**
- Increase the Max Distance value on AudioZoneManager
