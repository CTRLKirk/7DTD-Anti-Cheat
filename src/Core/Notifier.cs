using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AntiCheatExtension.Core
{
    internal static class Notifier
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        static Notifier()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static void NotifyAdmins(DetectionRecord record)
        {
            var connectionManager = SingletonMonoBehaviour<ConnectionManager>.Instance;
            if (connectionManager == null)
            {
                return;
            }

            var recipients = new List<int>();
            foreach (var client in connectionManager.Clients.List)
            {
                if (client != null && client.entityId != -1 && !client.disconnecting &&
                    ServerContext.PermissionLevel(client) <= Settings.Current.NotifyPermissionLevel)
                {
                    recipients.Add(client.entityId);
                }
            }

            if (recipients.Count == 0)
            {
                return;
            }

            var message = $"[FF5555]AntiCheat[-] {record.PlayerName}: {record.Detection} ({record.Details}) -> {record.Action}";
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, message, recipients, EMessageSender.Server);
        }

        public static void SendWebhook(DetectionRecord record)
        {
            var url = Settings.Current.DiscordWebhookUrl;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            var content = $"**{record.Detection}** ({record.Action}) - {record.PlayerName} `{record.PlayerId}`\n{record.Details}";
            var json = "{\"username\":\"AntiCheat\",\"content\":\"" + EscapeJson(content) + "\"}";

            Task.Run(async () =>
            {
                try
                {
                    using (var body = new StringContent(json, Encoding.UTF8, "application/json"))
                    using (var response = await Http.PostAsync(url, body).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            ModLog.Warning($"Discord webhook returned {(int)response.StatusCode}.");
                        }
                    }
                }
                catch (Exception e)
                {
                    ModLog.Warning($"Discord webhook failed: {e.GetBaseException().Message}");
                }
            });
        }

        private static string EscapeJson(string value)
        {
            const char Backslash = (char)92;
            var builder = new StringBuilder(value.Length + 16);
            foreach (var c in value)
            {
                if (c == '"' || c == Backslash)
                {
                    builder.Append(Backslash).Append(c);
                }
                else if (c == (char)10)
                {
                    builder.Append(Backslash).Append('n');
                }
                else if (c < 0x20)
                {
                    builder.Append(Backslash).Append('u').Append(((int)c).ToString("x4"));
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
