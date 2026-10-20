using System.Collections.Generic;
using System.Globalization;
using AntiCheat.Core;

namespace AntiCheat.Commands
{
    public class ConsoleCmdAntiCheat : ConsoleCmdAbstract
    {
        public override int DefaultPermissionLevel => 0;

        public override string[] getCommands() => new[] { "anticheat", "ac" };

        public override string getDescription() => "AntiCheat: status, recent detections, reload settings.";

        public override string getHelp()
        {
            return "Usage:\n" +
                   "  ac status         - show each detection and its action\n" +
                   "  ac recent [count] - show the most recent detections (default 10)\n" +
                   "  ac reload         - reload Config/settings.xml";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            var sub = _params.Count > 0 ? _params[0].ToLowerInvariant() : "status";
            switch (sub)
            {
                case "status":
                    PrintStatus();
                    break;
                case "recent":
                    var count = 10;
                    if (_params.Count > 1 && !int.TryParse(_params[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                    {
                        Output("Count must be a number.");
                        return;
                    }
                    PrintRecent(count);
                    break;
                case "reload":
                    Settings.Load(ModEntry.SettingsPath);
                    Output("Settings reloaded.");
                    PrintStatus();
                    break;
                default:
                    Output(getHelp());
                    break;
            }
        }

        private static void PrintStatus()
        {
            var settings = Settings.Current;
            Output($"AntiCheat - {DetectionService.TotalDetections} detection(s) since start");
            Output($"  Exempt permission level: <= {settings.ExemptPermissionLevel}, alerts to: <= {settings.NotifyPermissionLevel}");
            Output($"  Discord webhook: {(string.IsNullOrEmpty(settings.DiscordWebhookUrl) ? "off" : "on")}");
            Output($"  Modded: speed x{settings.SpeedMultiplier:0.##}, reach x{settings.ReachMultiplier:0.##}, damage x{settings.DamageMultiplier:0.##}, " +
                   $"exempt buffs: {settings.ExemptBuffs.Count}, exempt players: {settings.ExemptPlayers.Count}");
            foreach (var detection in settings.Detections)
            {
                Output($"  {detection.Name,-12} {detection.Action}");
            }
        }

        private static void PrintRecent(int count)
        {
            var recent = DetectionService.Recent;
            if (recent.Count == 0)
            {
                Output("No detections yet.");
                return;
            }

            for (var i = 0; i < recent.Count && i < count; i++)
            {
                Output(recent[i].ToString());
            }
        }

        private static void Output(string line) => SingletonMonoBehaviour<SdtdConsole>.Instance.Output(line);
    }
}
