using AntiCheatExtension.Core;
using HarmonyLib;

namespace AntiCheatExtension.Detections
{
    [HarmonyPatch(typeof(NetPackagePlayerStats), nameof(NetPackagePlayerStats.ProcessPackage))]
    internal static class ProgressionPatch
    {
        private static void Prefix(NetPackagePlayerStats __instance)
        {
            var sender = __instance.Sender;
            var stats = __instance.entityNetworkStats;
            if (sender == null || stats == null || !ServerContext.IsServer || __instance.entityId != sender.entityId)
            {
                return;
            }

            var tracker = PlayerTracker.Get(sender.entityId);
            var level = stats.level;
            var previous = tracker.LastLevel;
            tracker.LastLevel = level;

            var progression = Settings.Current.Get(DetectionNames.Progression);
            if (previous < 0 || !progression.Enabled || ServerContext.IsExempt(sender))
            {
                return;
            }

            var limit = progression.GetInt("maxLevelGain", 3);
            if (level - previous > limit)
            {
                DetectionService.Report(sender.entityId, DetectionNames.Progression,
                    $"level jumped from {previous} to {level}");
            }
        }
    }
}
