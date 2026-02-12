# Klyra's Reach - Project Memory

## Project Overview
**Klyra's Reach** is a lightweight third-person sci-fi game built for WebGL, inspired by Star Citizen. The game focuses on accessible sci-fi gameplay with a modular design that allows for future expansion into multiplayer and advanced features.

**Target Platform**: WebGL (Browser-based)
**Engine**: Unity 6.3
**Performance Target**: 30 fps minimum, 60 fps ideal

---

## Core Technology Stack

### Unity Components
- **Unity Version**: Unity 6.3
- **Character Controller**: Opsive Third Person Controller
- **Art Assets**: Synty POLYGON asset packs
  - POLYGON Sci-Fi Space (spaceships, stations, space environments)
  - POLYGON Sci-Fi City (futuristic cities, urban environments)
  - POLYGON Sci-Fi Worlds (planetary surfaces, sci-fi terrain)
  - Additional Synty packs as needed

### Technical Constraints
- **WebGL Optimization**: All code and assets must be optimized for browser performance
- **File Size**: Keep builds lean for web delivery
- **Mobile Compatibility**: Consider future mobile WebGL deployment

---

## Development Phases

### Phase 1: Foundation (CURRENT)
**Focus**: Basic character movement and control
- [x] Unity project setup
- [x] Folder structure organization
- [ ] Opsive Third Person Controller integration
- [ ] Basic character movement with Synty character model
- [ ] Camera control
- [ ] Input system setup
- [ ] Basic scene with Synty environment assets

### Phase 2: Core Systems
**Focus**: Essential gameplay systems
- [ ] UI/HUD framework
- [ ] Player health system
- [ ] Interaction system (pickup, use, examine)
- [ ] Basic inventory system
- [ ] Save/Load system
- [ ] Settings menu

### Phase 3: Content Foundation
**Focus**: Playable content
- [ ] Quest/Mission system basics
- [ ] Dialogue system
- [ ] NPC framework
- [ ] Environmental storytelling
- [ ] Audio system integration

### Phase 4: Expansion (Future)
**Focus**: Star Citizen-inspired features
- [ ] Hauling/Cargo system
- [ ] Mercenary combat mechanics
- [ ] Ship piloting and combat
- [ ] Economy system
- [ ] Multiple locations/zones
- [ ] Potentially seamless planet landing (TBD)

### Phase 5: Multiplayer (Far Future)
**Focus**: Online always connectivity
- [ ] Multiplayer architecture refactor
- [ ] Server infrastructure
- [ ] Player-to-player interactions
- [ ] Persistent world elements

---

## Code Standards & Practices

### Commenting Requirements
**ALL scripts must include**:
- File header explaining the script's purpose
- Detailed comments for every function/method
- Inline comments explaining complex logic
- Parameter descriptions
- Return value descriptions

**Reasoning**: User has no coding experience - every script should be educational and self-documenting.

### Script Organization
```
Assets/
├── Scripts/
│   ├── Player/          # Character control, stats, abilities
│   ├── Managers/        # Game managers, scene management, system controllers
│   ├── UI/              # All UI-related scripts
│   ├── Utilities/       # Helper classes, extensions, tools
│   └── Systems/         # Quest, dialogue, inventory, save systems
├── Scenes/              # Unity scenes
├── Settings/            # Unity settings
└── [Synty Asset Folders] # Imported Synty packs
```

### Naming Conventions
- **Classes**: PascalCase (e.g., `PlayerController`, `InventoryManager`)
- **Methods**: PascalCase (e.g., `MovePlayer()`, `GetItemById()`)
- **Variables**: camelCase (e.g., `currentHealth`, `moveSpeed`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_INVENTORY_SIZE`)
- **Private fields**: _camelCase with underscore (e.g., `_playerTransform`)

### Unity-Specific Guidelines
- Use SerializeField for private variables that need Inspector access
- Add [Header] and [Tooltip] attributes for designer-friendly Inspector
- Implement MonoBehaviour lifecycle methods in order: Awake, Start, Update, FixedUpdate, LateUpdate
- Cache component references in Awake/Start to avoid GetComponent calls in Update

---

## Team Roles

### User Responsibilities
- Unity interface operations (drag/drop, Inspector setup)
- Scene building and layout with Synty assets
- Asset importing and organization
- Testing gameplay in Unity Editor and WebGL builds
- Design decisions and creative direction
- Providing feedback on features

### Claude Code Responsibilities
- **ALL C# scripting**
- Code architecture and organization
- System design and implementation
- Bug fixing and optimization
- Code documentation
- Technical problem-solving
- WebGL optimization strategies

---

## Current Project State

### Completed
✅ Unity 6.3 project created
✅ Initial folder structure established
✅ Scripts organization (Player, Managers, UI, Utilities, Systems folders)

### In Progress
🔄 Setting up project memory and documentation

### Next Steps
1. Import Opsive Third Person Controller
2. Import first Synty asset pack
3. Create initial test scene
4. Implement basic player movement script
5. Set up camera follow system

---

## Important Notes

### WebGL Considerations
- Avoid threading (not supported in WebGL)
- Minimize file I/O operations
- Optimize texture sizes and compression
- Use object pooling for frequently instantiated objects
- Profile early and often

### Future Multiplayer Prep
While Phase 1-3 are single-player, code should be written with future multiplayer in mind:
- Separate client logic from game logic where possible
- Use events/messaging patterns instead of direct references
- Design systems to be authoritative-server friendly
- Document which systems will need refactoring for multiplayer

### Asset Integration
- Synty assets use specific naming conventions - maintain consistency
- Keep materials and prefabs organized by asset pack
- Document any modifications to Synty prefabs

---

## Version History
- **v0.1** - Project initialization (February 10, 2026)
  - Unity 6.3 project created
  - Folder structure established
  - Project memory created

---

## Quick Reference Links
- Unity 6 Documentation: https://docs.unity3d.com/6000.0/Documentation/Manual/
- Opsive Third Person Controller Docs: https://opsive.com/support/documentation/third-person-controller/
- Synty Store: https://syntystore.com/

---

*Last Updated: February 10, 2026*
