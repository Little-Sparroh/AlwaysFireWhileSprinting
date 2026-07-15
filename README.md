# Always Fire While Sprinting

A BepInEx mod for Mycopunk that allows firing weapons while sprinting and sliding, with a sprint-to-fire fix.

## Features

- **Fire While Sprinting**: Allows weapons to fire during sprint.
- **Fire While Sliding**: Allows weapons to fire during slide.
- **Sprint To Fire Fix**: Removes delay when firing from sprint and restores sprint properly when releasing fire.

## Getting Started

### Dependencies

* Mycopunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8
* [HarmonyLib](https://github.com/pardeike/Harmony) (included via NuGet)

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode to generate the .dll file

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Via Thunderstore (Recommended)**:
1. Download and install via Thunderstore Mod Manager
2. The mod will be automatically installed to the correct directory

**Manual Installation**:
1. Place the built `AlwaysFireWhileSprinting.dll` in your `<Mycopunk Directory>/BepInEx/plugins/` folder

### Executing program

The mod loads automatically through BepInEx when the game starts. Check the BepInEx console for loading confirmation messages.

## Configuration

Access mod settings through the BepInEx configuration file at `<Mycopunk Directory>/BepInEx/config/sparroh.alwaysfirewhilesprinting.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| Can Fire While Sprinting | `true` | Allows firing weapons while sprinting. |
| Can Fire While Sliding | `true` | Allows firing weapons while sliding. |
| Sprint To Fire Fix | `true` | Enables immediate firing while sprinting and proper sprint resume. |

Config changes are hot-reloaded while the game is running. Edit and save the `.cfg` file and the mod will pick up the new values without a restart.

## Help

* **Mod not loading?** Verify BepInEx is installed correctly and check console logs for errors
* **Still can't fire while sprinting?** Ensure the config options are enabled (changes apply live; no restart needed)


## Authors

- Sparroh

## License

This project is licensed under the MIT License - see the LICENSE file for details
