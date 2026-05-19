using Verse;

namespace FrostyDog.SafeRunningZones;

internal static class NatureRunningLog
{
    public static void Message(string message)
    {
        if (NatureRunningZoneMod.Settings.verboseLogging)
        {
            Log.Message("[Safe Running Zones] " + message);
        }
    }
}
