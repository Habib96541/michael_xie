# Bubble Pop (Cyber Orbs)

A modern arcade bubble shooter rebuilt for Unity 6 with a cyber-esports visual direction and responsive mobile layout.

## Overview
- **Core Mechanics**: 10x10 hexagonal grid, 6 orb colors, match-3+ popping, floating cluster detachment, row drop pressure cycles, and responsive aiming controls.
- **Visuals & Audio**: Procedural cyber-themed particle effects, custom audio synthesis, and dynamic HUD.
- **Scenes**:
  - `Assets/Scenes/Boot.unity`: Platform initialization and safe-area bootstrapper.
  - `Assets/Scenes/MainMenu.unity`: Start screen and splash UI.
  - `Assets/Scenes/Bust a Move.unity`: Main gameplay scene.

## Project Structure
- `Assets/`: Scenes, scripts, shaders, audio synthesizers, UI components, and editor tools.
- `Packages/`: Unity package manifest and dependency lockfiles.
- `ProjectSettings/`: Complete project and player configuration.
- `Docs/`: Architecture, reskin design specifications, and documentation.
- `Tools/`: Asset generation and build automation scripts.

## Getting Started
1. Open the project in Unity 6000.4.9f1.
2. Load `Assets/Scenes/Boot.unity` or `Assets/Scenes/Bust a Move.unity`.
3. Enter Play Mode to test gameplay, responsive UI, and controls.
