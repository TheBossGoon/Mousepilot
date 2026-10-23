# MousePilot

A small Windows WPF utility for placing the mouse cursor, moving it with global keyboard controls, and running a configurable autoclicker.

## Build and run

Open `MousePilot.csproj` in Visual Studio 2022 with the .NET 8 desktop development workload, or build it with the .NET 8 SDK on Windows. The first launch creates `setting.txt` next to the executable if it is missing. Keep that file beside the executable to customize the global movement keys.

## Controls

- Enter integer screen coordinates and choose **Move cursor** to position the cursor.
- Arrow keys move the cursor globally, including while another window is active. The default step is 10 pixels.
- The autoclicker supports left, right, and middle clicks, rates from 0.1 to 200 clicks per second, and a chosen screen position.
- `Alt+F4` closes MousePilot globally.

## Key settings

`setting.txt` is reloaded once per second. Supported keys are letters, digits, `Left`, `Right`, `Up`, `Down`, `Space`, and `F1` through `F12`.

```ini
MoveStep=10
LeftKey=Left
RightKey=Right
UpKey=Up
DownKey=Down
```

Global arrow key handling consumes the configured keys while MousePilot is running. Close the app to restore normal key behavior.
