# Third-Party Notices

The MIT license in `LICENSE` applies only to the original code and project data
of this repository. The items below are separate works with their own terms.
Anything not listed as original code in `LICENSE` keeps its original license.

## Art

### Gem Hunter Match sample art
- Location: `Assets/GemHunterMatch/`
- Contents: gameplay piece sprites and UI sprites.
- Origin: Unity Technologies' "Gem Hunter Match" sample project.
- Terms: remain with the original owner, not covered by this repository's MIT
  license. Check the license of the original project before reusing, forking
  or redistributing these files, and replace them if your use is not allowed.

### CC0 asset packs
- Location: `Assets/FreeAssets_CC0/`
  - `Tiles_PuzzlePack`, `UI_Pack`, `Audio_Interface`, `Audio_Jingles`,
    `VFX_Particles`
- Terms: released into the public domain under CC0 1.0 Universal
  (https://creativecommons.org/publicdomain/zero/1.0/). Attribution is not
  required, but the authors are credited here for convenience.

### Sound effects
- Location: `Assets/Audio/SFX/`
- Generated procedurally for this project.

## Packages and SDKs

These are not stored in the repository unless noted. They are restored by
Unity Package Manager from `Packages/manifest.json`, or by the vendor's
importer, and are subject to their own licenses.

| Component | License / terms |
|---|---|
| Unity Engine and Unity packages (URP, Input System, uGUI, 2D, Test Framework, Timeline, ...) | Unity Companion License / Unity Terms of Service |
| PrimeTween (`com.kyrylokuzyk.primetween`) | Asset Store EULA / vendor license |
| TextMeshPro (`LiberationSans SDF` font) | SIL Open Font License 1.1 (Liberation fonts) |
| Firebase Unity SDK (App, Crashlytics) in `Assets/Firebase` | Apache License 2.0 |
| External Dependency Manager in `Assets/ExternalDependencyManager` | Apache License 2.0, see `Assets/ExternalDependencyManager/Editor/LICENSE` |

## Not included

- `google-services.json` and other Firebase configuration files. Create your
  own Firebase project and add them locally (see `README.md`).
- Signing keystores.
