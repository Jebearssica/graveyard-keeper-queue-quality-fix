// Declaration-only stand-in for the three game types this plugin touches. It exists so the
// plugin can be compiled without a game install (CI); it is never shipped and never loaded
// at runtime. Signatures follow Assembly-CSharp 1.407 (Graveyard Keeper 1.407, .NET Framework
// build). The comments record the real shape so a mismatch is easy to spot when the game
// updates.

using System.Collections.Generic;

// Real type: Assembly-CSharp.Item (large class with id/value/quality/multiquality_items/...).
// Only the type identity is needed here: CraftDefinition.needs is a List<Item>.
public class Item
{
}

// Real type: Assembly-CSharp.CraftDefinition.
public class CraftDefinition
{
    public string id = string.Empty;

    public List<Item> needs = new List<Item>();

    public bool IsMultiqualityOutput()
    {
        return false;
    }
}

// Real type: Assembly-CSharp.CraftComponent : WorldGameObjectComponent.
// The plugin only patches it by name (EnqueueCraft is public, CraftReally is protected), so the
// stub deliberately declares no members: a compile-time reference to either method would defeat
// the point of building without the game.
public class CraftComponent
{
}
