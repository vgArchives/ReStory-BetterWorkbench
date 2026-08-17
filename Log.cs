#if MELONLOADER
using MelonLoader;
#else
using BepInEx.Logging;
#endif

namespace ReStoryBetterWorkbench;

internal static class Log
{
#if MELONLOADER
    internal static MelonLogger.Instance Source;

    internal static void Info(string message) => Source.Msg(message);

    internal static void Warning(string message) => Source.Warning(message);

    internal static void Error(string message) => Source.Error(message);
#else
    internal static ManualLogSource Source;

    internal static void Info(string message) => Source.LogInfo(message);

    internal static void Warning(string message) => Source.LogWarning(message);

    internal static void Error(string message) => Source.LogError(message);
#endif

    [System.Diagnostics.Conditional("DEBUG")]
    internal static void Debug(string message) => Info(message);
}
