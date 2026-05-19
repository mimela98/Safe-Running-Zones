using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FrostyDog.NatureRunningZone;

internal static class NatureRunningTargetFinder
{
    internal enum TargetCheckResult
    {
        Valid,
        RemoveFromJobCache,
        TemporaryFail
    }

    private const int MaxZonesToTry = 32;

    public static bool TryFindTarget(Pawn pawn, out LocalTargetInfo target)
    {
        target = LocalTargetInfo.Invalid;
        Map map = pawn.Map;
        if (map == null)
        {
            return false;
        }

        JobRunCache? cache = NatureRunningJobCache.TryGet(pawn);
        if (cache?.CurrentZone != null && TryFindCellInZone(cache, cache.CurrentZone, pawn, out IntVec3 cachedCell))
        {
            target = new LocalTargetInfo(cachedCell);
            return true;
        }

        int zonesTried = 0;
        foreach (Zone_NatureRunning zone in GetZonesByDistance(pawn))
        {
            if (cache?.CurrentZone == zone)
            {
                continue;
            }

            if (++zonesTried > MaxZonesToTry)
            {
                break;
            }

            if (TryFindCellInZone(cache, zone, pawn, out IntVec3 cell))
            {
                if (cache != null)
                {
                    cache.CurrentZone = zone;
                }

                target = new LocalTargetInfo(cell);
                return true;
            }
        }

        return false;
    }

    public static bool AnyNatureRunningZone(Map map)
    {
        return map.zoneManager.AllZones.Any(static zone => zone is Zone_NatureRunning);
    }

    public static AcceptanceReport IsDesignatableCell(IntVec3 c, Map map)
    {
        if (!c.Standable(map))
        {
            return false;
        }

        TerrainDef terrain = c.GetTerrain(map);
        if (terrain == null || terrain.avoidWander)
        {
            return false;
        }

        if (NatureRunningZoneMod.Settings.terrainMode == NatureRunningTerrainMode.NaturalOnly && !terrain.natural)
        {
            return false;
        }

        if (!NatureRunningZoneMod.Settings.allowIndoor && c.Roofed(map))
        {
            return false;
        }

        return true;
    }

    public static ZoneDebugInfo GetZoneDebugInfo(Zone_NatureRunning zone, Pawn pawn)
    {
        ZoneDebugInfo info = new();
        if (zone.Map != pawn.Map)
        {
            info.ZoneFailureReason = "NatureRunningZone_Debug_DifferentMap";
            return info;
        }

        if (zone.cells.NullOrEmpty())
        {
            info.ZoneFailureReason = "NatureRunningZone_Debug_EmptyZone";
            return info;
        }

        if (NatureRunningZoneMod.Settings.allowIndoor && !IsZoneTemperatureComfortable(zone, pawn))
        {
            info.ZoneFailureReason = "NatureRunningZone_Debug_Temperature";
            return info;
        }

        if (!IsZoneVacuumSafe(zone))
        {
            info.ZoneFailureReason = "NatureRunningZone_Debug_Vacuum";
            return info;
        }

        info.ZoneUsable = true;
        CandidateRules rules = CandidateRules.Current;
        bool enjoyableOutside = JoyUtility.EnjoyableOutsideNow(pawn);
        foreach (IntVec3 c in zone.cells)
        {
            TargetCheckResult result = CheckTargetCell(c, pawn, zone.Map, enjoyableOutside, rules);
            if (result == TargetCheckResult.Valid)
            {
                info.ValidCells.Add(c);
            }
            else if (result == TargetCheckResult.TemporaryFail)
            {
                info.TemporaryFailCells.Add(c);
            }
            else
            {
                info.InvalidCells.Add(c);
            }
        }

        if (info.ValidCells.Count < rules.MinValidZoneCells)
        {
            info.ZoneFailureReason = "NatureRunningZone_Debug_NotEnoughValidCells";
        }

        return info;
    }

    private static IEnumerable<Zone_NatureRunning> GetZonesByDistance(Pawn pawn)
    {
        return pawn.Map.zoneManager.AllZones
            .OfType<Zone_NatureRunning>()
            .Where(zone => IsZoneEnvironmentUsable(zone, pawn))
            .OrderBy(zone => zone.Position.DistanceToSquared(pawn.Position));
    }

    private static bool TryFindCellInZone(JobRunCache? cache, Zone_NatureRunning zone, Pawn pawn, out IntVec3 cell)
    {
        cell = IntVec3.Invalid;
        if (!IsZoneEnvironmentUsable(zone, pawn))
        {
            return false;
        }

        CandidateRules rules = CandidateRules.Current;
        bool enjoyableOutside = JoyUtility.EnjoyableOutsideNow(pawn);
        List<IntVec3> candidates = GetCandidatesForZone(cache, zone, pawn, enjoyableOutside, rules);
        if (candidates.Count < rules.MinValidZoneCells)
        {
            return false;
        }

        int attempts = candidates.Count;
        for (int i = 0; i < attempts && candidates.Count > 0; i++)
        {
            int index = Rand.Range(0, candidates.Count);
            IntVec3 candidate = candidates[index];
            TargetCheckResult result = CheckTargetCell(candidate, pawn, zone.Map, enjoyableOutside, rules);
            if (result == TargetCheckResult.Valid)
            {
                cell = candidate;
                return true;
            }

            if (result == TargetCheckResult.RemoveFromJobCache)
            {
                candidates.RemoveAt(index);
            }
        }

        return false;
    }

    private static List<IntVec3> GetCandidatesForZone(JobRunCache? cache, Zone_NatureRunning zone, Pawn pawn, bool enjoyableOutside, CandidateRules rules)
    {
        if (cache != null && cache.ZoneCandidates.TryGetValue(zone, out List<IntVec3> cached))
        {
            return cached;
        }

        List<IntVec3> candidates = new();
        Map map = zone.Map;
        foreach (IntVec3 c in zone.cells)
        {
            if (IsLowCostCandidate(c, map, enjoyableOutside, rules))
            {
                candidates.Add(c);
            }
        }

        if (cache != null)
        {
            cache.ZoneCandidates[zone] = candidates;
        }

        return candidates;
    }

    private static bool IsLowCostCandidate(IntVec3 c, Map map, bool enjoyableOutside, CandidateRules rules)
    {
        if (!c.InBounds(map) || c.Fogged(map) || !c.Standable(map) || c.GetEdifice(map) != null)
        {
            return false;
        }

        TerrainDef terrain = c.GetTerrain(map);
        if (terrain == null || terrain.avoidWander)
        {
            return false;
        }

        if (rules.TerrainMode == NatureRunningTerrainMode.NaturalOnly && !terrain.natural)
        {
            return false;
        }

        if (!rules.AllowIndoor)
        {
            return enjoyableOutside && !c.Roofed(map);
        }

        return enjoyableOutside || c.Roofed(map);
    }

    private static TargetCheckResult CheckTargetCell(IntVec3 c, Pawn pawn, Map map, bool enjoyableOutside, CandidateRules rules)
    {
        if (!IsLowCostCandidate(c, map, enjoyableOutside, rules))
        {
            return TargetCheckResult.RemoveFromJobCache;
        }

        if (c.IsForbidden(pawn))
        {
            return TargetCheckResult.RemoveFromJobCache;
        }

        if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Some))
        {
            return TargetCheckResult.RemoveFromJobCache;
        }

        int minDistance = rules.MinDistanceFromChild;
        if (minDistance > 0 && c.DistanceTo(pawn.Position) < minDistance)
        {
            return TargetCheckResult.TemporaryFail;
        }

        return TargetCheckResult.Valid;
    }

    private static bool IsZoneEnvironmentUsable(Zone_NatureRunning zone, Pawn pawn)
    {
        if (zone.Map != pawn.Map || zone.cells.NullOrEmpty())
        {
            return false;
        }

        if (NatureRunningZoneMod.Settings.allowIndoor && !IsZoneTemperatureComfortable(zone, pawn))
        {
            return false;
        }

        return IsZoneVacuumSafe(zone);
    }

    private static bool IsZoneTemperatureComfortable(Zone_NatureRunning zone, Pawn pawn)
    {
        IntVec3 sample = zone.cells[0];
        float temperature = sample.GetTemperature(zone.Map);
        return pawn.ComfortableTemperatureRange().Includes(temperature);
    }

    private static bool IsZoneVacuumSafe(Zone_NatureRunning zone)
    {
        Map map = zone.Map;
        if (!ModsConfig.OdysseyActive || !map.Biome.inVacuum)
        {
            return true;
        }

        IntVec3 sample = zone.cells[0];
        return sample.GetVacuum(map) < VacuumUtility.MinVacuumForDamage;
    }
}

internal sealed class ZoneDebugInfo
{
    public bool ZoneUsable;
    public string? ZoneFailureReason;
    public readonly List<IntVec3> ValidCells = new();
    public readonly List<IntVec3> TemporaryFailCells = new();
    public readonly List<IntVec3> InvalidCells = new();
}

internal readonly struct CandidateRules
{
    public readonly NatureRunningTerrainMode TerrainMode;
    public readonly bool AllowIndoor;
    public readonly int MinDistanceFromChild;
    public readonly int MinValidZoneCells;

    private CandidateRules(NatureRunningZoneSettings settings)
    {
        TerrainMode = settings.terrainMode;
        AllowIndoor = settings.allowIndoor;
        MinDistanceFromChild = settings.minDistanceFromChild;
        MinValidZoneCells = settings.minValidZoneCells;
    }

    public static CandidateRules Current => new(NatureRunningZoneMod.Settings);
}
