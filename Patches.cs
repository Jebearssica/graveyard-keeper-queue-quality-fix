using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace QueueQualityFix;

internal static class Patches
{
    // The vanilla craft queue only stores the recipe id and a count, so a queued craft starts with
    // multiquality_ids = null. CraftDefinition.GetMultiqualityResult reads that list without a null
    // check, and it runs after the ingredients have already been removed. Remember the star the
    // player picked when queueing and hand it back when that bench starts the craft.
    private static readonly ConditionalWeakTable<CraftComponent, Dictionary<string, List<string>>> QueuedQualityIds = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftComponent), "EnqueueCraft")]
    private static void EnqueueCraft_Prefix(CraftComponent __instance, CraftDefinition craft, List<string> multiquality_ids)
    {
        if (craft == null || string.IsNullOrEmpty(craft.id)) return;
        if (multiquality_ids == null || multiquality_ids.Count == 0) return;
        if (!craft.IsMultiqualityOutput()) return;

        QueuedQualityIds.GetOrCreateValue(__instance)[craft.id] = new List<string>(multiquality_ids);

        if (Plugin.DebugLogging.Value)
        {
            Plugin.Log.LogInfo($"[Enqueue] {craft.id}: remembered star [{string.Join(", ", multiquality_ids)}]");
        }
    }

    // Both targets are named as strings: this plugin builds without a publicizer, and CraftComponent
    // is reduced to a declaration-only stub in the no-game CI build. Missing targets surface loudly
    // as a Harmony error on load, and the real binding is verified against the shipped assembly.
    // CraftReally is protected in the shipped assembly, so a string is required there anyway.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftComponent), "CraftReally")]
    private static void CraftReally_Prefix(CraftComponent __instance, CraftDefinition craft, ref List<string> multiquality_ids)
    {
        if (multiquality_ids != null && multiquality_ids.Count > 0) return;
        if (craft == null || string.IsNullOrEmpty(craft.id)) return;
        if (!craft.IsMultiqualityOutput()) return;

        List<string> remembered = null;
        if (QueuedQualityIds.TryGetValue(__instance, out var byCraft))
        {
            byCraft.TryGetValue(craft.id, out remembered);
        }

        multiquality_ids = BuildQualityIds(craft.needs.Count, remembered);

        if (Plugin.DebugLogging.Value)
        {
            Plugin.Log.LogInfo($"[Craft] {craft.id}: restored star [{string.Join(", ", multiquality_ids)}]");
        }
    }

    // One entry per need: GetMultiqualityResult indexes the list even when a slot is empty, and an
    // empty string is the game's "no particular star" tier-draining path. Never returns null and
    // never returns a list shorter than needCount.
    internal static List<string> BuildQualityIds(int needCount, List<string> remembered)
    {
        var ids = new List<string>(needCount);
        for (var i = 0; i < needCount; i++)
        {
            ids.Add(remembered != null && i < remembered.Count && !string.IsNullOrEmpty(remembered[i])
                ? remembered[i]
                : string.Empty);
        }

        return ids;
    }
}
