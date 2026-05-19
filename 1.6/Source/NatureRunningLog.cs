using Verse;

namespace FrostyDog.NatureRunningZone;

internal static class NatureRunningLog
{
    public static void Message(string message)
    {
        if (NatureRunningZoneMod.Settings.verboseLogging)
        {
            Log.Message("[Nature Running Zone] " + message);
        }
    }
}
