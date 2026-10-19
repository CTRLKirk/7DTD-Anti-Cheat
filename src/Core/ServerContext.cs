namespace AntiCheat.Core
{
    internal static class ServerContext
    {
        public static bool IsServer
        {
            get
            {
                var connectionManager = SingletonMonoBehaviour<ConnectionManager>.Instance;
                return connectionManager != null && connectionManager.IsServer;
            }
        }

        public static World World => GameManager.Instance?.World;

        public static ClientInfo ClientFor(int entityId)
        {
            return SingletonMonoBehaviour<ConnectionManager>.Instance?.Clients?.ForEntityId(entityId);
        }

        public static int PermissionLevel(ClientInfo client)
        {
            var admin = GameManager.Instance?.adminTools;
            return admin == null || client == null ? 1000 : admin.Users.GetUserPermissionLevel(client);
        }

        public static bool IsExempt(ClientInfo client)
        {
            return client == null || PermissionLevel(client) <= Settings.Current.ExemptPermissionLevel || IsListedExempt(client);
        }

        public static bool HasExemptBuff(EntityAlive entity)
        {
            var buffs = Settings.Current.ExemptBuffs;
            if (buffs.Count == 0 || entity?.Buffs == null)
            {
                return false;
            }

            foreach (var buff in buffs)
            {
                if (entity.Buffs.HasBuff(buff))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsListedExempt(ClientInfo client)
        {
            var players = Settings.Current.ExemptPlayers;
            return players.Count > 0 &&
                   (players.Contains(client.PlatformId?.CombinedString ?? string.Empty) ||
                    players.Contains(client.CrossplatformId?.CombinedString ?? string.Empty) ||
                    players.Contains(client.playerName ?? string.Empty));
        }

        public static bool IsExempt(int entityId)
        {
            return IsExempt(ClientFor(entityId));
        }
    }
}
