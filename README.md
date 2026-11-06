# Queue Everything: Quality Queue Fix

A tiny [BepInEx 5](https://github.com/BepInEx/BepInEx) companion plugin for **Graveyard Keeper 1** that fixes a
crash-and-lose-your-materials bug in [Queue Everything](https://github.com/p1xel8ted/Graveyard-Keeper-Mods/tree/main/src/QueueEverything)
when queued crafts produce quality (starred) items.

给 Queue Everything 打的一个运行时补丁：开启「允许品质物品批量制作」后，排队的品质制作会**保留你选择的星级**，
不再出现「材料被扣掉、东西没做出来」的情况。

> **Credits.** Queue Everything is by **[p1xel8ted](https://github.com/p1xel8ted)** —
> [source](https://github.com/p1xel8ted/Graveyard-Keeper-Mods/tree/main/src/QueueEverything) ·
> [Nexus mod 57](https://www.nexusmods.com/graveyardkeeper/mods/57) (GPL-3.0).
> This repository is **not** a fork and contains **no code from Queue Everything**; it only patches the game methods
> that Queue Everything drives. It is licensed MIT (see [LICENSE](LICENSE)).

## What it fixes

The vanilla craft queue stores only the recipe id and a count:

```csharp
public class CraftQueueItem
{
    public string id = "";
    public int n;
    public bool infinite;
    public bool is_gratitude_points_craft;
}
```

The star the player picked in the craft window (`CraftItemGUI._multiquality_ids`) is not stored, so when the queue
starts a craft it passes `null`:

```csharp
// CraftComponent.TryStartCraftFromQueue
this.CraftReally(craftQueueItem.craft, null, null, null, false, 1, ...);
//                                 ^^^^ multiquality_ids = null
```

and the quality calculation reads that list without a null check:

```csharp
// CraftDefinition.GetMultiqualityResult
if ((i < multiquality_ids.Count && this.needs[i].is_multiquality) || this.needs[i].definition.quality >= 0.5f)
```

`CraftReally` runs in this order (IL offsets from `Assembly-CSharp.dll`, Graveyard Keeper 1.407):

| Step | IL offset |
| --- | --- |
| `MultiInventory.RemoveItems(...)` — ingredients leave the inventory | 1523 / 1543 |
| `wgo.data.RemoveItems(needs_from_wgo)` — fuel leaves the bench | after that |
| `_multiquality_craft_result = craft.GetMultiqualityResult(multiquality_ids)` | **2056** |
| `is_crafting = true` | **2113** |

So the failure mode is not "you get a lower star" — it is:

1. ingredients and fuel are **already gone**;
2. `GetMultiqualityResult(null)` throws `NullReferenceException`;
3. `is_crafting` is never set, so the craft **never starts**;
4. the queue entry was already decremented/removed before the call, so the request is **silently swallowed**.

Vanilla never reaches this path because `CraftDefinition.CanCraftMultiple()` and `CanEnqueue()` both return `false`
for multi-quality output. Queue Everything opens those gates on purpose (`force_multi_craft`,
`enqueue_type = CanEnqueue`), which is what exposes the queue path.

The analysis above was cross-checked against the game assembly's IL and the public decompilation
[Kupie/GYK_DECOMP](https://github.com/Kupie/GYK_DECOMP).

## Install

1. Install **BepInEx 5** and **Queue Everything** (which itself requires *Rest In Patches*).
2. Download `QueueQualityFix.dll` from the [latest release](../../releases/latest) (or build it from source) and drop it
   into `BepInEx/plugins/QueueQualityFix/`.
3. Start the game. `BepInEx/LogOutput.log` should contain `Queue Everything: Quality Queue Fix loaded.`

Queue Everything must be installed — the plugin declares a hard `BepInDependency` on it and BepInEx will skip it
otherwise. It does not touch Queue Everything's DLL, its config, or your save.

## What changes

- Queued quality crafts are started with **the star you picked when queueing**, so the ingredients removed are the
  ones the craft-window preview used, and the roll uses the probabilities that preview showed.
- Every finished item still rolls its own quality with the normal vanilla logic (material quality + perks − difficulty,
  then `Random.value` against the three tier probabilities). Nothing about the probability formula is changed.
- If nothing was remembered (a queue that was saved and reloaded, or a queue row cloned by another mod), the plugin
  falls back to a correctly sized list of empty strings — vanilla's "no particular star" tier-draining path — instead
  of handing `null` to the game. That path cannot throw.

## Configuration

`BepInEx/config/gk.queuequalityfix.cfg`

| Key | Default | Meaning |
|---|---|---|
| `DebugLogging` | `false` | Log `[Enqueue] ... remembered star [...]` / `[Craft] ... restored star [...]` for each queued quality craft |

## How it works

Two Harmony prefixes, no transpilers, no changes to Queue Everything:

- **`CraftComponent.EnqueueCraft`** — when the recipe has a multi-quality output, remember the chosen
  `multiquality_ids` per `(CraftComponent, craft.id)` in a `ConditionalWeakTable`. The table is keyed by the bench
  object, so nothing outlives a save being unloaded.
- **`CraftComponent.CraftReally`** — when the call arrives with an empty star list and the recipe has a multi-quality
  output, restore the remembered list. `CraftReally` is `protected` in the shipped assembly, so the target is named as
  a string; the plugin is built against the real, unpublicized game DLL.

Everything else in the game (including Queue Everything's own patches) is untouched.

## Known limitations

- A queue entry that was written to a save before this plugin was installed has no remembered star; it uses the
  tier-draining fallback above.
- If the selected tier runs out part-way through a batch, the remaining entries cannot start (`CraftReally` returns
  `false` before removing anything). The materials are not consumed, but the queue entry is already gone — this is the
  vanilla queue's behaviour for any craft that fails to start.
- Queueing the same recipe twice with different tiers makes the earlier entries use the most recent selection.
- Only tested against Graveyard Keeper 1.407 (Steam) and Queue Everything 2.2.1+.

## Building from source

Against a real install (the default):

```powershell
dotnet build -c Release
```

The game directory is detected when the project sits inside it (for example `<game>/mods-work/QueueQualityFix`),
otherwise the default Steam location is used; override it with `-p:GameDir="D:\Games\Graveyard Keeper"`. The build
copies the DLL into `BepInEx/plugins/QueueQualityFix/` when BepInEx is installed.

Without the game, using the declaration-only stub in `ci/refs` (this is what CI does):

```powershell
dotnet build -c Release -p:UseGameRefs=false -p:DeployToGame=false -p:BepInExCore="<extracted BepInEx>\core"
```

See [ci/README.md](ci/README.md). Release builds use `DebugType=none` and a `PathMap`, so the assembly contains no
local file paths.

## Licence

Plugin code: **MIT** — see [LICENSE](LICENSE).

Queue Everything and Graveyard Keeper are not part of this repository. Queue Everything is © p1xel8ted, GPL-3.0;
Graveyard Keeper is © Lazy Bear Games. No game or mod assemblies are redistributed here.
