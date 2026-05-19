using HarmonyLib;
using RimWorld;
using Verse;

namespace FrostyDog.NatureRunningZone;

[HarmonyPatch(typeof(NatureRunningUtility), nameof(NatureRunningUtility.TryFindNatureInterestTarget))]
internal static class Patch_NatureRunningUtility_TryFindNatureInterestTarget
{
    private static bool Prefix(Pawn searcher, ref LocalTargetInfo interestTarget, ref bool __result)
    {
        if (searcher?.Map == null)
        {
            interestTarget = LocalTargetInfo.Invalid;
            return true;
        }

        NatureRunningLog.Message($"TryFindNatureInterestTarget pawn={searcher.LabelShortCap} map={searcher.Map}");
        bool anyZone = NatureRunningTargetFinder.AnyNatureRunningZone(searcher.Map);
        if (NatureRunningTargetFinder.TryFindTarget(searcher, out LocalTargetInfo zoneTarget))
        {
            interestTarget = zoneTarget;
            __result = true;
            NatureRunningLog.Message($"Zone target success pawn={searcher.LabelShortCap} target={zoneTarget.Cell}");
            return false;
        }

        if (!anyZone && NatureRunningZoneMod.Settings.fallbackWhenNoZone)
        {
            NatureRunningLog.Message($"No nature running zones; using vanilla fallback pawn={searcher.LabelShortCap}");
            return true;
        }

        if (anyZone && NatureRunningZoneMod.Settings.fallbackWhenZoneFails)
        {
            NatureRunningLog.Message($"Nature running zones failed; using vanilla fallback pawn={searcher.LabelShortCap}");
            return true;
        }

        interestTarget = LocalTargetInfo.Invalid;
        __result = false;
        NatureRunningLog.Message($"Nature running failed without fallback pawn={searcher.LabelShortCap} anyZone={anyZone}");
        return false;
    }
}
