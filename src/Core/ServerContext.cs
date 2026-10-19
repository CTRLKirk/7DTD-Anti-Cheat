namespace AntiCheatExtension.Core
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
            return client == null || PermissionLevel(client) <= Settings.Current.ExemptPermissionLevel;
        }

        public static bool IsExempt(int entityId)
        {
            return IsExempt(ClientFor(entityId));
        }
    }
}
