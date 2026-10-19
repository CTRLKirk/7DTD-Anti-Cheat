namespace AntiCheatExtension.Core
{
    internal static class ModLog
    {
        private const string Prefix = "[AntiCheat] ";

        public static void Info(string message) => Log.Out(Prefix + message);
        public static void Warning(string message) => Log.Warning(Prefix + message);
        public static void Error(string message) => Log.Error(Prefix + message);
    }
}
