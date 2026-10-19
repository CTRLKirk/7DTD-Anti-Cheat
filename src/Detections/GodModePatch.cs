using AntiCheat.Core;
using HarmonyLib;

namespace AntiCheat.Detections
{
    [HarmonyPatch(typeof(NetPackageEntityAliveFlags), nameof(NetPackageEntityAliveFlags.ProcessPackage))]
    internal static class GodModePatch
    {
        private const ushort GodModeFlag = 0x80;

        private static void Prefix(NetPackageEntityAliveFlags __instance)
        {
            var sender = __instance.Sender;
            if (sender == null || !ServerContext.IsServer || (__instance.flags & GodModeFlag) == 0 ||
                __instance.entityId != sender.entityId)
            {
                return;
            }

            if (!GameStats.GetBool(EnumGameStats.IsPlayerDamageEnabled) || ServerContext.IsExempt(sender))
            {
                return;
            }

            DetectionService.Report(sender.entityId, DetectionNames.GodMode, "client reported god mode enabled");
        }
    }
}
