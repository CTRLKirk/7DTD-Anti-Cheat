using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntiCheat.Core
{
    public sealed class DetectionRecord
    {
        public DateTime Time { get; set; }
        public string Detection { get; set; }
        public string PlayerName { get; set; }
        public string PlayerId { get; set; }
        public int EntityId { get; set; }
        public string Details { get; set; }
        public DetectionAction Action { get; set; }

        public override string ToString()
        {
            return $"{Time:yyyy-MM-dd HH:mm:ss} {Detection} {Action} player=\"{PlayerName}\" id={PlayerId} entity={EntityId} {Details}";
        }
    }

    public static class DetectionService
    {
        private const int RecentCapacity = 100;

        private static readonly ConcurrentQueue<KeyValuePair<int, KeyValuePair<string, string>>> Pending =
            new ConcurrentQueue<KeyValuePair<int, KeyValuePair<string, string>>>();

        private static readonly LinkedList<DetectionRecord> RecentRecords = new LinkedList<DetectionRecord>();
        private static readonly Dictionary<string, DateTime> LastAlert = new Dictionary<string, DateTime>();
        private static readonly object FileLock = new object();

        public static IReadOnlyList<DetectionRecord> Recent
        {
            get
            {
                lock (RecentRecords)
                {
                    return RecentRecords.ToList();
                }
            }
        }

        public static int TotalDetections { get; private set; }

        /// <summary>
        /// Queues a detection. Safe to call from any thread; handled on the next game update.
        /// </summary>
        public static void Report(int entityId, string detection, string details)
        {
            if (!Settings.Current.Get(detection).Enabled)
            {
                return;
            }

            Pending.Enqueue(new KeyValuePair<int, KeyValuePair<string, string>>(
                entityId, new KeyValuePair<string, string>(detection, details)));
        }

        public static void ProcessQueue()
        {
            while (Pending.TryDequeue(out var item))
            {
                try
                {
                    Handle(item.Key, item.Value.Key, item.Value.Value);
                }
                catch (Exception e)
                {
                    ModLog.Error($"Failed to handle {item.Value.Key} detection: {e}");
                }
            }
        }

        private static void Handle(int entityId, string detection, string details)
        {
            var settings = Settings.Current;
            var action = settings.Get(detection).Action;
            if (action == DetectionAction.Off)
            {
                return;
            }

            var client = ServerContext.ClientFor(entityId);
            if (client == null || client.disconnecting || ServerContext.IsExempt(client))
            {
                return;
            }

            var cooldownKey = entityId + ":" + detection;
            var now = DateTime.UtcNow;
            if (LastAlert.TryGetValue(cooldownKey, out var last) &&
                (now - last).TotalSeconds < settings.AlertCooldownSeconds)
            {
                return;
            }
            LastAlert[cooldownKey] = now;

            var record = new DetectionRecord
            {
                Time = DateTime.Now,
                Detection = detection,
                PlayerName = client.playerName,
                PlayerId = client.InternalId?.CombinedString ?? "unknown",
                EntityId = entityId,
                Details = details,
                Action = action
            };

            Remember(record);
            ModLog.Warning(record.ToString());
            WriteToFile(record);
            Notifier.NotifyAdmins(record);
            Notifier.SendWebhook(record);

            switch (action)
            {
                case DetectionAction.Kick:
                    Punishment.Kick(client, detection);
                    break;
                case DetectionAction.Ban:
                    Punishment.Ban(client, detection, settings.BanDurationHours);
                    break;
            }
        }

        private static void Remember(DetectionRecord record)
        {
            lock (RecentRecords)
            {
                TotalDetections++;
                RecentRecords.AddFirst(record);
                while (RecentRecords.Count > RecentCapacity)
                {
                    RecentRecords.RemoveLast();
                }
            }
        }

        private static void WriteToFile(DetectionRecord record)
        {
            try
            {
                var directory = Path.Combine(ModEntry.ModPath, "Logs");
                Directory.CreateDirectory(directory);
                var file = Path.Combine(directory, $"detections-{record.Time:yyyy-MM-dd}.log");
                lock (FileLock)
                {
                    File.AppendAllText(file, record + Environment.NewLine);
                }
            }
            catch (Exception e)
            {
                ModLog.Error($"Could not write detection log: {e.Message}");
            }
        }
    }
}
