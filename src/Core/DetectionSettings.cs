using System;
using System.Collections.Generic;
using System.Globalization;

namespace AntiCheatExtension.Core
{
    public enum DetectionAction
    {
        Off,
        Log,
        Kick,
        Ban
    }

    public sealed class DetectionSettings
    {
        private readonly Dictionary<string, string> values;

        public DetectionSettings(string name, DetectionAction action, Dictionary<string, string> values)
        {
            Name = name;
            Action = action;
            this.values = values;
        }

        public string Name { get; }
        public DetectionAction Action { get; }
        public bool Enabled => Action != DetectionAction.Off;

        public float GetFloat(string key, float fallback)
        {
            return values.TryGetValue(key, out var raw) &&
                   float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public int GetInt(string key, int fallback)
        {
            return values.TryGetValue(key, out var raw) &&
                   int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public string GetString(string key, string fallback)
        {
            return values.TryGetValue(key, out var raw) ? raw : fallback;
        }

        public HashSet<string> GetList(string key)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in GetString(key, string.Empty).Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    set.Add(trimmed);
                }
            }
            return set;
        }
    }
}
