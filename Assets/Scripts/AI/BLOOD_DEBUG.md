# Blood Effect Debug Guide

## Quick Checklist

If blood isn't showing, check these things in order:

### 1. Is the Blood Prefab Assigned?
- Open your enemy prefab in Inspector
- Find **EnemyAIController** component
- Under **Death Effects** section:
  - [ ] "Use Blood Effect" is **CHECKED**
  - [ ] "Blood Effect Prefab" has **FX_BloodSplat_01** assigned

**Path to blood prefab:**
`Assets/PolygonParticleFX/Prefabs/FX_BloodSplat_01.prefab`

### 2. Check Console for Debug Messages

When an enemy dies, you should see these messages:

```
[EnemyAI] 'EnemyName' detected death! Health: 0
[EnemyAI] 'EnemyName' OnOpsiveDeath event received!
[EnemyAI] 'EnemyName' OnDeath() executing! UseBloodEffect: True, BloodPrefab: SET
[EnemyAI] 'EnemyName' spawning blood effect at (x, y, z)
```

**What each message means:**

#### If you see "detected death":
✓ Health system working
✓ Enemy AI detecting death

#### If you see "OnOpsiveDeath event received":
✓ Opsive death event firing correctly

#### If you see "blood effect enabled but prefab is NULL":
❌ **Problem:** Blood prefab not assigned
**Fix:** Drag `FX_BloodSplat_01.prefab` into the slot

#### If you see "blood effect disabled":
❌ **Problem:** "Use Blood Effect" is unchecked
**Fix:** Check the checkbox in Inspector

#### If you don't see any messages:
❌ **Problem:** Enemy isn't dying or AI script not running
**Fix:**
1. Make sure enemy has `EnemyAIController` component
2. Make sure enemy has `AttributeManager` with Health attribute
3. Try shooting the enemy to reduce health

### 3. Test Blood Effect Manually

To verify the blood effect works:

1. Open scene
2. Drag `FX_BloodSplat_01.prefab` into scene
3. Hit Play
4. You should see blood particles

If blood shows in scene but not on death:
- Check that "Use Blood Effect" is enabled
- Check that prefab is assigned
- Check console for error messages

### 4. Common Issues

**Issue:** Blood spawns but instantly disappears
- Check that blood prefab has ParticleSystem component
- Check particle system has emission enabled
- Check particle lifetime is > 0

**Issue:** Blood spawns at wrong location
- Check enemy pivot point
- Blood spawns at `transform.position` of enemy

**Issue:** Multiple blood effects spawn
- OnDeath() might be called multiple times
- Check console for multiple "OnDeath() executing" messages

### 5. Alternative Blood Effects

Try these other blood effects if FX_BloodSplat_01 doesn't work:

**Massive gore explosion:**
`Assets/PolygonParticleFX/Prefabs/FX_Explosion_Body_Bloody_01.prefab`

**Small blood spray:**
`Assets/PolygonParticleFX/Prefabs/FX_BloodSplat_Small_01.prefab`

### 6. Force Death Test

Add this to test death manually:

1. Select enemy in Hierarchy during Play mode
2. Find `EnemyAIController` component
3. Right-click component
4. Select "Debug"
5. Find `OnDeath()` method
6. Click to manually trigger

OR use Console command:
```csharp
FindObjectOfType<EnemyAIController>().OnDeath();
```

## Still Not Working?

Check these files exist:
- `Assets/PolygonParticleFX/Prefabs/FX_BloodSplat_01.prefab`
- `Assets/PolygonParticleFX/Materials/PolygonParticleFX_Blood.mat`

If files are missing, reimport the PolygonParticleFX package.
