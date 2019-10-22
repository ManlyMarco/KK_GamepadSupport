# Gamepad and keyboard support for Koikatsu!
Mod that adds support for using XInput-compatible gamepads (aka Xbox 360 controllers) with the entire main game (menus, school mode, H scenes, maker, etc.). It also adds support for navigating the UI with keyboard (Arrow keys and Enter). Studio is not supported at all.

## Installation
1. Make sure your game is updated and has [BepInEx v4.1](https://github.com/BepInEx/BepInEx), installed.
2. Download the latest release from [releases](https://github.com/ManlyMarco/KK_GamepadSupport/releases).
3. Extract contents of the archive directly into your game's directory.
4. Start Koikatu.exe. Once in main menu try pressing arrow keys on your keyboard and Dpad on your controller.

## Controls
List of controls for different game modes. General controls apply to most of other modes.

### General
Most of the graphical interface (buttons, toggles, etc.) can be navigated and controlled as follows:
- Left stick X/Y and Dpad - Select control (currently selected control is makred with a pointer icon). If a slider or scrollbar is selected then either X or Y axis will control its value. 
- A - Click or otherwise activate currently selected control (also accept text field input in case it eats your inputs)
- B - Cancel (same as mouse right-click)

Global hotkeys:
- Start - Show tutorial if available.
- Back - Show settings.
- Guide(Home) - Exit the game.

### ADV / Visual Novel UI
- B - Next.
- X - Skip.
- Right stick Click - Enter/Exit backlog.
- Right stick X/Y - Scroll backlog contents.

### Main Game
- B - Switch between roaming and menu mode.

Controls in roaming mode:
- Left stick X/Y and Dpad - Move your character.
- Right stick X/Y - Control the camera.
- A - Interact.
- X - Switch between FPS/TPS camera.
- Y - Open quick travel window.
- Left shoulder - Turn camera 180 degrees.
- Right shoulder - Crouch.
- Right stick Click - Reset camera.
- Left trigger / Right trigger - Zoom the camera.

There are no special controls in menu mode.

### H Scene
- X - Change animation.
- Y - Toggle auto.
- Left shoulder / Right shoulder - Change speed (hold in auto mode) / Manual action (press both in manual mode).
- Right stick X/Y - Control the camera.
- Left trigger / Right trigger - Change how Right stick controls the camera.

### Mouse emulation mode
Some parts of the game (e.g. touching in H) and mods (mod interfaces) might be impossible to use without mouse. To solve this issue a mouse cursor emulation mode is included. To enter/exit the mouse mode press both Left Trigger and Right Trigger at the same time. You can then use the following:
- Left stick Y - Scroll mouse wheel Up/Down.
- Right stick X/Y - Move cursor horizontally/vertically.
- Left Trigger - Right mouse click and hold.
- Right Trigger - Left mouse click and hold.
