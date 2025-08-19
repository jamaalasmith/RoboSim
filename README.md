# RoboSim

A simple Unity-based rover simulation with WASD controls.

## Setup

1. Open the project in Unity Editor (Unity 2022.3 LTS or later)
2. Open the main scene or create a new scene
3. Add the RoverController script to a GameObject with a Rigidbody
4. Use WASD keys to control the rover:
   - W/S: Forward/Backward movement
   - A/D: Left/Right rotation

## Features

- Physics-based rover movement
- Battery system with drainage
- Collision detection
- Real-time battery monitoring

## Controls

- **W**: Move forward
- **S**: Move backward  
- **A**: Rotate left
- **D**: Rotate right

The rover will stop when the battery is depleted. Use `RechargeBattery()` method to restore power.