using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FrostyDog.SafeRunningZones;

public sealed class Zone_NatureRunning : Zone
{
    protected override Color NextZoneColor => new(0.15f, 0.65f, 0.95f, 0.09f);

    public override bool IsMultiselectable => true;

    public Zone_NatureRunning()
    {
    }

    public Zone_NatureRunning(ZoneManager zoneManager)
        : base("NatureRunningZone_Label".Translate(), zoneManager)
    {
        string baseName = "NatureRunningZone_Label".Translate();
        label = NextNatureRunningZoneName(zoneManager, baseName);
    }

    public override IEnumerable<Gizmo> GetZoneAddGizmos()
    {
        yield return DesignatorUtility.FindAllowedDesignator<Designator_ZoneAdd_NatureRunning>();
    }

    private static string NextNatureRunningZoneName(ZoneManager zoneManager, string baseName)
    {
        int nextNumber = 1;
        string prefix = baseName + " ";
        foreach (Zone zone in zoneManager.AllZones)
        {
            if (zone is not Zone_NatureRunning || zone.label.NullOrEmpty() || !zone.label.StartsWith(prefix))
            {
                continue;
            }

            string suffix = zone.label.Substring(prefix.Length);
            if (int.TryParse(suffix, out int number) && number >= nextNumber)
            {
                nextNumber = number + 1;
            }
        }

        return prefix + nextNumber;
    }
}
