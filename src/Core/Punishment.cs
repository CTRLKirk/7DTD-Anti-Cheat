using System;

namespace AntiCheat.Core
{
    internal static class Punishment
    {
        public static void Kick(ClientInfo client, string detection)
        {
            var reason = $"AntiCheat: {detection}";
            GameUtils.KickPlayerForClientInfo(client,
                new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, reason));
        }

        public static void Ban(ClientInfo client, string detection, int durationHours)
        {
            var reason = $"AntiCheat: {detection}";
            var until = durationHours > 0 ? DateTime.Now.AddHours(durationHours) : DateTime.Now.AddYears(100);

            var blacklist = GameManager.Instance.adminTools?.Blacklist;
            if (blacklist != null)
            {
                blacklist.AddBan(client.playerName, client.PlatformId, until, reason);
                if (client.CrossplatformId != null)
                {
                    blacklist.AddBan(client.playerName, client.CrossplatformId, until, reason);
                }
            }

            GameUtils.KickPlayerForClientInfo(client,
                new GameUtils.KickPlayerData(GameUtils.EKickReason.Banned, 0, until, reason));
        }
    }
}
