# ReStory Better Workbench

A quality-of-life mod for the **ReStory** workbench. It organizes the parts on the
table, highlights them, and makes the notepad parts list easier to read.

## Features

- **Organize the bench**: press **F** to pack every loose part into rows against one side of the bench.
- **Part highlights**: press **G** to highlight parts respecting their condition state.
- **Notepad sorted by work left**: the notepad parts list is ordered to group what still needs doing.
- **Collapsible notepad sections**: click a section header in the notepad to collapse its list content.

## Requirements

- **ReStory: Chill Electronics Repairs**
- **BepInEx 5.x** (x64, Unity **Mono** build). Tested against BepInEx 5.4.23.5.

## Installation

1. Install **BepInEx 5.x (x64, Unity Mono)** into your ReStory folder, the one containing
   `Restory.exe`. Get it from [GitHub](https://github.com/BepInEx/BepInEx/releases) and follow the install
   instructions on its page.

   Check it extracted correctly: `BepInEx\core\BepInEx.dll` should now exist. By default that is
   `(YourDrive):\Program Files (x86)\Steam\steamapps\common\Restory\BepInEx\core`.

2. Launch the game once from Steam, then quit. BepInEx creates its `plugins` and `config` folders on first
   run.

3. Extract this mod's archive and move the `ReStoryBetterWorkbench` folder into `BepInEx\plugins\`, so the
   DLL ends up at:

   ```
   ...\Restory\BepInEx\plugins\ReStoryBetterWorkbench\ReStoryBetterWorkbench.dll
   ```

That's all needed, if you want to confirm the mod loaded succesfully you can start the game and check the log at `...\BepInEx\LogOutput.log`. It should contain:

   ```
   [Info   :ReStory Better Workbench] Loaded. Self-check passed.
   ```

### Uninstalling

Delete `BepInEx\plugins\ReStoryBetterWorkbench\` and `BepInEx\config\com.archives.restorybetterworkbench.cfg` as well. 

## Controls

| Key                | Action                                                          |
|--------------------|-----------------------------------------------------------------|
| **F**              | Packs the loose parts on the bench.                             |
| **Shift + F**      | Packs the loose parts against the opposite side.                |
| **G**              | Toggles the outline highlight on every part lying on the bench. |

The notepad parts list is sorted automatically; it has no key.

## Configuration

Settings live in `...\BepInEx\config\com.archives.restorybetterworkbench.cfg`, created the first time you run
the game with the mod installed. **Edit it while the game is closed**, it is read once at startup.

### `[Hotkeys]`

Both take any Unity KeyCode name, e.g. `F`, `G`, `R`, `Tab`, `F6`.

| Setting         | Default | Description                                                                       |
|-----------------|---------|-----------------------------------------------------------------------------------|
| `OrganizeKey`   | `F`     | Packs the loose parts on the bench. Hold Shift to pack against the opposite side. |
| `HighlightsKey` | `G`     | Toggles the outline highlight on every part lying on the bench.                   |

### `[Layout]`

| Setting                 | Default | Range           | Description                                                                                                      |
|-------------------------|---------|-----------------|------------------------------------------------------------------------------------------------------------------|
| `PackAgainstSide`       | `Left`  | `Left`, `Right` | Bench side the parts are packed against.                                                                         |
| `CellGap`               | `0.025` | 0 – 0.1         | Gap between packed parts, in meters.                                                                             |
| `SafetyMargin`          | `0.005` | 0 – 0.05        | Extra collision padding safety around each part spot, in meters.                                                 |
| `ShelfSlack`            | `1`     | 1 – 2.5         | How loosely the packed block spreads over the bench.                                                             |
| `ControlsDisplayOffset` | `0.07`  | 0 – 0.4         | How far the controls UI display position is adjusted when switching sides.                                       |
| `TopMarginLeft`         | `0.05`  | 0 – 0.4         | Margin kept clear along the top edge of the bench. Applies when packing against the Left side.                   |
| `TopMarginRight`        | `0.10`  | 0 – 0.4         | Margin kept clear along the top edge of the bench. Applies when packing against the Right side.                  |
| `SideMarginLeft`        | `0.05`  | 0 – 0.4         | Margin kept clear along both the left and right edges of the bench. Applies when packing against the Left side.  |
| `SideMarginRight`       | `0.00`  | 0 – 0.4         | Margin kept clear along both the left and right edges of the bench. Applies when packing against the Right side. |

## Building from source

Requires the .NET SDK. The project references the game's own assemblies, so set `GameDir` in
`ReStoryBetterWorkbench.csproj` to your ReStory install path if it differs from the default.

```
dotnet build                # Debug   - verbose per-action logging, deploys to the game folder
dotnet build -c Release     # Release - development logging compiled out, deploys to the game folder
```

Both configurations copy the DLL straight into `BepInEx\plugins\ReStoryBetterWorkbench\` and print which
build was deployed.
