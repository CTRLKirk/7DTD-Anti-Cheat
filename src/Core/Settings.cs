using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace AntiCheatExtension.Core
{
    public sealed class Settings
    {
        private static readonly Dictionary<string, DetectionAction> DefaultActions =
            new Dictionary<string, DetectionAction>(StringComparer.OrdinalIgnoreCase)
            {
                { DetectionNames.GodMode, DetectionAction.Kick },
                { DetectionNames.Teleport, DetectionAction.Kick },
                { DetectionNames.Speed, DetectionAction.Log },
                { DetectionNames.Fly, DetectionAction.Log },
                { DetectionNames.NoClip, DetectionAction.Log },
                { DetectionNames.IllegalItem, DetectionAction.Kick },
                { DetectionNames.StackSize, DetectionAction.Log },
                { DetectionNames.Damage, DetectionAction.Kick },
                { DetectionNames.Reach, DetectionAction.Log },
                { DetectionNames.Progression, DetectionAction.Log }
            };

        private readonly Dictionary<string, DetectionSettings> detections =
            new Dictionary<string, DetectionSettings>(StringComparer.OrdinalIgnoreCase);

        public static Settings Current { get; private set; } = new Settings();

        public int ExemptPermissionLevel { get; private set; } = 1;
        public int NotifyPermissionLevel { get; private set; } = 1;
        public int BanDurationHours { get; private set; }
        public float AlertCooldownSeconds { get; private set; } = 15f;
        public string DiscordWebhookUrl { get; private set; } = string.Empty;

        public IEnumerable<DetectionSettings> Detections => detections.Values;

        public DetectionSettings Get(string name)
        {
            if (detections.TryGetValue(name, out var settings))
            {
                return settings;
            }

            DefaultActions.TryGetValue(name, out var action);
            return new DetectionSettings(name, action, new Dictionary<string, string>());
        }

        public static void Load(string path)
        {
            var settings = new Settings();

            if (!File.Exists(path))
            {
                ModLog.Warning($"Settings file not found at {path}, using defaults.");
            }
            else
            {
                try
                {
                    settings.Parse(XDocument.Load(path));
                }
                catch (Exception e)
                {
                    ModLog.Error($"Failed to read {path}, using defaults: {e.Message}");
                    settings = new Settings();
                }
            }

            foreach (var name in DefaultActions.Keys)
            {
                if (!settings.detections.ContainsKey(name))
                {
                    settings.detections[name] = settings.Get(name);
                }
            }

            Current = settings;
        }

        private void Parse(XDocument document)
        {
            var root = document.Root;
            if (root == null)
            {
                return;
            }

            foreach (var element in root.Elements("Setting"))
            {
                ApplySetting((string)element.Attribute("name"), (string)element.Attribute("value") ?? string.Empty);
            }

            foreach (var element in root.Elements("Detection"))
            {
                var name = (string)element.Attribute("name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                DefaultActions.TryGetValue(name, out var action);
                var rawAction = (string)element.Attribute("action");
                if (rawAction != null && !Enum.TryParse(rawAction, true, out action))
                {
                    ModLog.Warning($"Unknown action '{rawAction}' for detection {name}, using Log.");
                    action = DetectionAction.Log;
                }

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attribute in element.Attributes())
                {
                    values[attribute.Name.LocalName] = attribute.Value;
                }

                detections[name] = new DetectionSettings(name, action, values);
            }
        }

        private void ApplySetting(string name, string value)
        {
            switch (name)
            {
                case "ExemptPermissionLevel":
                    ExemptPermissionLevel = ParseInt(value, ExemptPermissionLevel);
                    break;
                case "NotifyPermissionLevel":
                    NotifyPermissionLevel = ParseInt(value, NotifyPermissionLevel);
                    break;
                case "BanDurationHours":
                    BanDurationHours = ParseInt(value, BanDurationHours);
                    break;
                case "AlertCooldownSeconds":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cooldown))
                    {
                        AlertCooldownSeconds = cooldown;
                    }
                    break;
                case "DiscordWebhookUrl":
                    DiscordWebhookUrl = value.Trim();
                    break;
                default:
                    ModLog.Warning($"Unknown setting '{name}' ignored.");
                    break;
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }
    }
}
