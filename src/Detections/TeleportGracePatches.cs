using AntiCheat.Core;
using HarmonyLib;
using UnityEngine;

namespace AntiCheat.Detections
{
    /// <summary>
    /// Server-initiated teleports (admin commands, trader eviction, game events) start a grace period
    /// so they are not reported as teleport cheats.
    /// </summary>
    [HarmonyPatch(typeof(ClientInfo), nameof(ClientInfo.SendPackage))]
    internal static class ClientTeleportPatch
    {
        private static void Prefix(ClientInfo __instance, NetPackage _package)
        {
            if (_package is NetPackageTeleportPlayer && __instance != null && __instance.entityId != -1)
            {
                PlayerTracker.Get(__instance.entityId).StartGrace();
            }
        }
    }

    [HarmonyPatch(typeof(ConnectionManager), nameof(ConnectionManager.SendPackage),
        typeof(NetPackage), typeof(bool), typeof(int), typeof(int), typeof(int), typeof(Vector3?), typeof(int), typeof(bool))]
    internal static class BroadcastTeleportPatch
    {
        private static void Prefix(NetPackage _package, int _attachedToEntityId)
        {
            if (_package is NetPackageTeleportPlayer && _attachedToEntityId != -1)
            {
                PlayerTracker.Get(_attachedToEntityId).StartGrace();
            }
        }
    }
}
