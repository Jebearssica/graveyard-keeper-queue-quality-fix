using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace QueueQualityFix;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("p1xel8ted.gyk.queueeverything")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; }

    internal static ConfigEntry<bool> DebugLogging { get; private set; }

    private void Awake()
    {
        Log = Logger;

        DebugLogging = Config.Bind(
            "Debug",
            "DebugLogging",
            false,
            "Log the star tier restored for every queued quality craft.\n" +
            "为每个排队的品质制作输出被恢复的星级日志。");

        Harmony.CreateAndPatchAll(typeof(Patches), MyPluginInfo.PLUGIN_GUID);

        Logger.LogInfo("Queue Everything: Quality Queue Fix loaded.");
    }
}
