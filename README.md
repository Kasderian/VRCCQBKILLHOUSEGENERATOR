# CQB Kill House Generator

A procedural room generation system for VRChat, allowing players to create randomized Close Quarters Battle (CQB) training environments.

Built with Unity and UdonSharp, this project features procedural room layouts, interactive gameplay elements, network-synchronized systems, and customizable generation settings.

---

## Screenshots


![World Thumbnail](CQB-Training-Random-Generator/Assets/Thumbnail/900px-World_Thumbnail_Template.png)
![World Screenshot](ReadMeImages/VRChat_2026-10-01_22-03-58.736_1920x1080.png)

---

## Overview

CQB Kill House Generator is a VRChat world designed around the concept of randomized CQB training environments.

Instead of navigating the same fixed layout every time, players can generate new room configurations, creating different layouts and combat scenarios.

The project focuses on procedural generation, multiplayer synchronization, and interactive gameplay systems within the limitations of VRChat's Udon environment.

---

## Features

- **Procedural Room Generation** – Generate randomized room layouts using a Binary Space Partitioning (BSP) algorithm.
- **Customizable Generation Settings** – Configure Kill House by setting room size (width and length) and seed.
- **Interactive Firearms** – Pick up and use weapons throughout the environment.
- **Target System** – Spawn and manage targets for shooting practice.
- **Multiplayer Synchronization** – Network-synchronized gameplay elements.
- **Interactive UI** – Configure and control generation settings through an in-world interface.
- **Reusable Object Systems** – Manage gameplay objects using predefined object pools where applicable.

---

## Procedural Generation

The room generation system uses **Binary Space Partitioning (BSP)** to divide a larger area into smaller sections.

These sections are used to determine room placement and layout, producing randomized CQB environments.

The generation process includes:

1. Initialize the generation area.
2. Recursively divide the area into smaller partitions.
3. Determine room placement within the partitions.
4. Create connections between rooms.
5. Generate the resulting room layout.


---

## Gameplay Systems

### Firearms

Interactive firearms can be picked up and used by players.

### Targets

A target management system handles target activation, positioning, and network synchronization.

### Multiplayer

Gameplay systems are designed to support multiple players, with network ownership and synchronization handled through VRChat networking.

---

## Technical Details

| Component | Technology |
|---|---|
| Game Engine | Unity |
| Platform | VRChat |
| Scripting | UdonSharp / C# |
| Networking | VRChat Udon Networking |
| UI | Unity UI / TextMeshPro |
| Room Generation | Binary Space Partitioning (BSP) |

---

## Project Structure

<!-- Update this section to match the actual repository structure -->

```text
Assets/
├── Scripts/
│   ├── Generation/
│   ├── Gameplay/
│   ├── Networking/
│   └── UI/
├── Prefabs/
├── Materials/
├── Models/
└── Scenes/
```

---

## Installation

1. Clone or download this repository.
2. Open the project using the compatible Unity version.
3. Ensure the required VRChat SDK and UdonSharp packages are installed.
4. Open the main world scene.
5. Build and test using the VRChat SDK.

Check the project configuration and package dependencies before opening the project.

---

## Configuration

Generation parameters can be adjusted through the exposed Unity Inspector settings.

Examples include:

- Maximum number of rooms.
- Minimum room dimensions.
- Generation parameters.
- Target count and gameplay settings.

---

## Future Updates

- [ ] Further improve procedural room generation.
- [ ] Add more door variations. Current door is 180 door. P

---

## Known Issues

<!-- Document known bugs, limitations, or compatibility issues -->

---

## Credits

Developed by Kasder [Kasderian].

Built using Unity, UdonSharp, and the VRChat SDK.

With the help of Claude, ChatGPT and Gemini.

---

## License

<!-- Specify your chosen license here -->


