using System;
using System.IO;
using System.Reflection;
using AntiCheat.Core;
using AntiCheat.Detections;
using HarmonyLib;

namespace AntiCheat
{
    public class ModEntry : IModApi
    {
        private const string HarmonyId = "com.ctrlkirk.anticheat";

        public static string ModPath { get; private set; }
        public static string SettingsPath => Path.Combine(ModPath, "Config", "settings.xml");

        public void InitMod(Mod modInstance)
        {
            ModPath = modInstance.Path;
            Settings.Load(SettingsPath);

            ApplyPatches();

            ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
            ModEvents.PlayerDisconnected.RegisterHandler(OnPlayerDisconnected);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            ModLog.Info($"Loaded v{version.ToString(3)} for game {Constants.cVersionInformation.LongString}.");
        }

        private static void ApplyPatches()
        {
            var harmony = new Harmony(HarmonyId);
            var applied = 0;
            var failed = 0;

            foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                {
                    continue;
                }

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    applied++;
                }
                catch (Exception e)
                {
                    failed++;
                    ModLog.Error($"Patch {type.Name} failed, that check is disabled: {e.GetBaseException().Message}");
                }
            }

            ModLog.Info($"Applied {applied} patch(es).");
            if (failed > 0)
            {
                ModLog.Warning($"{failed} patch(es) failed to apply. The game version may not be supported.");
            }
        }

        private static void OnGameUpdate(ref ModEvents.SGameUpdateData data)
        {
            if (!ServerContext.IsServer)
            {
                return;
            }

            MovementMonitor.Tick();
            DetectionService.ProcessQueue();
        }

        private static void OnPlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData data)
        {
            if (ServerContext.IsServer)
            {
                PlayerTracker.Get(data.EntityId).StartGrace();
            }
        }

        private static void OnPlayerDisconnected(ref ModEvents.SPlayerDisconnectedData data)
        {
            if (data.ClientInfo != null)
            {
                PlayerTracker.Remove(data.ClientInfo.entityId);
            }
        }
    }
}
