using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace FrostyDog.NatureRunningZone;

internal static class NatureRunningJobCache
{
    private static readonly ConditionalWeakTable<Pawn, JobRunCache> CachesByPawn = new();

    public static JobRunCache? TryGet(Pawn pawn)
    {
        Job? curJob = pawn.CurJob;
        if (curJob == null || curJob.def == null || curJob.def.defName != "NatureRunning")
        {
            return null;
        }

        JobRunCache cache = CachesByPawn.GetValue(pawn, static _ => new JobRunCache());
        if (cache.Job != curJob)
        {
            cache.Reset(curJob);
        }

        return cache;
    }
}

internal sealed class JobRunCache
{
    public Job? Job;
    public Zone_NatureRunning? CurrentZone;
    public readonly Dictionary<Zone_NatureRunning, List<IntVec3>> ZoneCandidates = new();

    public void Reset(Job job)
    {
        Job = job;
        CurrentZone = null;
        ZoneCandidates.Clear();
    }
}
