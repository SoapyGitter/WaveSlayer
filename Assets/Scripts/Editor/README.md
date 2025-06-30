# Wave Slayer

A 2.5D top-down samurai action game where you battle waves of demons. Play as a skilled samurai warrior using fluid movement and devastating attacks to defend against hordes of enemies.

## Game Features

- Fast-paced 2.5D top-down combat
- Responsive player controls
- Variety of enemy types with unique behaviors
- Melee combat system with strategic elements
- Special abilities like dash attacks
- Wave-based enemy spawning system
- Score tracking and difficulty progression

## Controls

### Mobile:
- Touch joystick for movement
- Double-tap anywhere to dash attack to nearest enemy
- Tap buttons for attacking and other abilities

### Keyboard/Mouse:
- WASD / Arrow Keys for movement
- Left mouse button for primary attack
- Space bar / Right mouse button for dash attack

## Player Abilities

### Basic Movement
The player character moves smoothly with responsive controls, allowing for precise positioning during combat encounters.

### Dash Attack
The samurai can perform a quick dash attack toward nearby enemies:
- Automatically targets the closest enemy within range
- Deals damage on contact
- Brief invulnerability during dash
- Short cooldown between uses
- Dash trail effect for visual feedback

## Enemy Types

### Basic Demon
- Standard melee attacker
- Moderate health and speed
- Approaches player and attacks when in range

### Ranged Demon
- Attacks from a distance
- Lower health but higher damage
- Maintains distance from player

## Development

The game is built in Unity with C# scripts handling the core gameplay mechanics:

- `PlayerBase`: Handles basic player movement and input
- `PlayerDashAttack`: Manages the dash ability targeting and execution
- `PlayerHealth`: Controls player health, damage, and death
- `DemonController`: Manages enemy AI, movement, and attacks
- `DemonModel`: Scriptable object defining enemy properties
- `DemonSpawner`: Controls enemy wave spawning logic
- `GameManager`: Oversees game state, score, and progression

## Future Additions

- Additional enemy types
- More player abilities and attack combos
- Level progression system
- Powerups and collectibles
- Boss encounters 