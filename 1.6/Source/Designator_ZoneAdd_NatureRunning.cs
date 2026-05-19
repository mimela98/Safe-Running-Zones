using RimWorld;
using UnityEngine;
using Verse;

namespace FrostyDog.NatureRunningZone;

public sealed class Designator_ZoneAdd_NatureRunning : Designator_ZoneAdd
{
    protected override string NewZoneLabel => "NatureRunningZone_Label".Translate();

    public Designator_ZoneAdd_NatureRunning()
    {
        zoneTypeToPlace = typeof(Zone_NatureRunning);
        defaultLabel = "NatureRunningZone_Label".Translate();
        defaultDesc = "NatureRunningZone_Desc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_NatureRunning");
        tutorTag = "ZoneAdd_NatureRunning";
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        AcceptanceReport baseReport = base.CanDesignateCell(c);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        return NatureRunningTargetFinder.IsDesignatableCell(c, Map);
    }

    protected override Zone MakeNewZone()
    {
        return new Zone_NatureRunning(Find.CurrentMap.zoneManager);
    }
}
