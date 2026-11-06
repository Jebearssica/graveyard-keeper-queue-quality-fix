# CI references

`ci/refs` exists so the plugin can be compiled on a machine (or GitHub runner) without a
Graveyard Keeper install.

`ci/refs/Assembly-CSharp` is a declaration-only stub of the three game types the plugin touches:

| Stub type | Members used by the plugin | Real shape |
| --- | --- | --- |
| `CraftDefinition` | `id`, `needs`, `IsMultiqualityOutput()` | `Assembly-CSharp.CraftDefinition` |
| `Item` | type identity only (`List<Item>`) | `Assembly-CSharp.Item` |
| `CraftComponent` | type identity only | `Assembly-CSharp.CraftComponent : WorldGameObjectComponent` |

`CraftComponent` intentionally declares no members. Both Harmony targets are resolved by name at
runtime (`EnqueueCraft` and the `protected` `CraftReally`), so a stub method would only hide a
potential mismatch.

This means the CI build proves **compilation**, not binding. Binding is verified separately against
the real `Assembly-CSharp.dll` from a game install (see the local build instructions in the README);
the plugin ships the exact same source for both builds.

Build without a game:

```powershell
dotnet build -c Release -p:UseGameRefs=false -p:DeployToGame=false -p:BepInExCore="<extracted BepInEx>\core"
```

The `UnityEngine.Modules` NuGet package supplies the Unity assembly references that `BepInEx.dll`
itself needs.
