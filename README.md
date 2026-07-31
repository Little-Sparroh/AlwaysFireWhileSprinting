# Always Fire While Sprinting

A BepInEx mod for Mycopunk that allows firing weapons while sprinting and sliding, with an optional sprint-to-fire fix
for immediate fire and proper sprint resume.

## Features

- **Fire While Sprinting** — Allows weapons to fire during sprint.
- **Fire While Sliding** — Allows weapons to fire during slide.
- **Sprint To Fire Fix** (optional, off by default) — Removes delay when firing from sprint and restores sprint properly
  when releasing fire.

## Dependencies

- Mycopunk
- [BepInEx Pack for Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/) 5.4.2403 or compatible

## Installation

**Via Thunderstore (recommended)**

1. Install with a Thunderstore mod manager (e.g. r2modman or the Thunderstore App).
2. The mod is placed in the correct directory automatically.

**Manual installation**

1. Install BepInEx for Mycopunk if you have not already.
2. Copy `AlwaysFireWhileSprinting.dll` into `<Mycopunk Directory>/BepInEx/plugins/`.

The mod loads automatically with BepInEx. Check the BepInEx log for a load confirmation message.

## Configuration

Settings are in:

`<Mycopunk Directory>/BepInEx/config/sparroh.alwaysfirewhilesprinting.cfg`

| Setting                  | Default | Description                                                                                                    |
|--------------------------|---------|----------------------------------------------------------------------------------------------------------------|
| Can Fire While Sprinting | `true`  | Allows firing weapons while sprinting.                                                                         |
| Can Fire While Sliding   | `true`  | Allows firing weapons while sliding.                                                                           |
| Sprint To Fire Fix       | `false` | Enables the Sprint-to-Fire fix that allows immediate firing while sprinting and proper sprint resume behavior. |

Config changes are hot-reloaded while the game is running. Edit and save the `.cfg` file and the mod picks up the new
values without a restart. Fire-constraint options are re-applied to equipped weapons live.

## Building

1. Clone this repository.
2. Open the solution in Visual Studio, Rider, or another C# IDE, **or** build from the command line:

```bash
dotnet build --configuration Release
```

3. The output assembly is `bin/Release/netstandard2.1/AlwaysFireWhileSprinting.dll`.

**Build requirements:** .NET SDK (targeting `netstandard2.1`), game assemblies referenced by the project, and
BepInEx/Harmony references.

## Help

- **Mod not loading?** Confirm BepInEx is installed correctly and check the BepInEx log for errors.
- **Still can't fire while sprinting?** Ensure the config options are enabled (changes apply live; no restart needed).

## Authors

- Sparroh

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
