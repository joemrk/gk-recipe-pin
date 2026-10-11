using BepInEx.Logging;

namespace MultiRecipePin
{
    internal static class Log
    {
        private static ManualLogSource _source;

        public static void Init(ManualLogSource source)
        {
            _source = source;
        }

        public static void Info(string msg)
        {
            _source?.LogInfo(msg);
        }

        public static void Warn(string msg)
        {
            _source?.LogWarning(msg);
        }

        public static void Error(string msg)
        {
            _source?.LogError(msg);
        }

        // Detailed log, only with config flag Debug.
        public static void Dbg(string msg)
        {
            if (Plugin.DebugLog != null && Plugin.DebugLog.Value)
            {
                _source?.LogInfo("[dbg] " + msg);
            }
        }
    }
}
