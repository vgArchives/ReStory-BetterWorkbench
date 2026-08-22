# ReStory Better Workbench

A quality-of-life mod for the **ReStory** workbench. It organizes the parts on the
table, highlights them, and makes the notepad parts list easier to read.

## Features

- **Organize the bench**: press **F** to pack every loose part into rows against one side of the bench.
- **Part highlights**: press **G** to highlight parts respecting their condition state.
- **Notepad sorted by work left**: the notepad parts list is ordered to group what still needs doing.
- **Collapsible notepad sections**: click a section header in the notepad to collapse its list content.
- **Workbench clock**: the game clock shows on screen while you work at the bench.
- **Package tooltips**: hovering a package shows the services it was ordered with, plus the day it is on for email orders.

## Requirements

  - **MelonLoader** latest, or
  - **BepInEx 5.4.23.5**.

## Installation (MelonLoader)

1. Download **`MelonLoader.Installer.exe`** from the
   [MelonLoader releases page](https://github.com/LavaGang/MelonLoader/releases/latest) and run it. Click
   **Select** and point it at your `Restory.exe`, leave the latest version selected, then hit **Install**.
   By default the game is at
   `(YourDrive):\Program Files (x86)\Steam\steamapps\common\Restory\Restory.exe`.

2. Launch the game once, then quit. MelonLoader creates its `Mods` and `UserData` folders on first run.

3. Extract this mod's archive and move `ReStoryBetterWorkbench.dll` into `Mods\`, so it ends up at:

   ```
   ...\Restory\Mods\ReStoryBetterWorkbench.dll
   ```

## Installation (BepInEx)

1. Download **`BepInEx_win_x64_5.4.23.5.zip`** from the
   [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases) and extract it into your ReStory
   folder, the one containing `Restory.exe`. Any newer 5.4.x release works too — just take the file with
   `win_x64` in its name.

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

## Controls

| Key                | Action                                                          |
|--------------------|-----------------------------------------------------------------|
| **F**              | Packs the loose parts on the bench.                             |
| **Shift + F**      | Packs the loose parts against the opposite side.                |
| **G**              | Toggles the outline highlight on every part lying on the bench. |


## Configuration

Settings live in `...\BepInEx\config\com.archives.restorybetterworkbench.cfg`, created the first time you run
the game with the mod installed. **Edit it while the game is closed**, it is read once at startup.

On MelonLoader the same settings live in `...\UserData\MelonPreferences.cfg`, shared with your other mods,
under `[ReStoryBetterWorkbenchGeneral]`, `[ReStoryBetterWorkbenchHotkeys]` and
`[ReStoryBetterWorkbenchLayout]` instead of the three sections below.

### `[General]`

| Setting       | Default | Description                                                                          |
|---------------|---------|--------------------------------------------------------------------------------------|
| `UpdateCheck` | `true`  | Checks once at startup whether a newer release exists. |

The check reads the latest release tag from `api.github.com`. Set it to `false` to keep the mod completely offline.

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
dotnet build                            # Debug   - verbose per-action logging
dotnet build -c Release                 # Release - development logging compiled out
dotnet build -c Release -p:Loader=Melon # the MelonLoader build of the same source
```

The BepInEx build lands in `BepInEx\plugins\ReStoryBetterWorkbench\`, the MelonLoader one in `Mods\`.
Neither loader needs to be installed to build: BepInEx, MelonLoader and Harmony all come from NuGet.

Loader-specific code lives in `Log.cs` and the two `BetterWorkbenchPlugin.*.cs` partials, picked by the
`MELONLOADER` symbol. **Build both before releasing** — only the switched-on one gets compiled.
