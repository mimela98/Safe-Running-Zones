using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FrostyDog.SafeRunningZones;

public sealed class NatureRunningZoneMod : Mod
{
    internal static NatureRunningZoneSettings Settings = new();

    public NatureRunningZoneMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<NatureRunningZoneSettings>();
        new Harmony("frostydog.saferunningzones").PatchAll();
    }

    public override string SettingsCategory()
    {
        return "NatureRunningZone_SettingsCategory".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();
        listing.Begin(inRect);

        listing.Label("NatureRunningZone_Settings_TerrainMode".Translate());
        if (listing.RadioButton("NatureRunningZone_Settings_NaturalOnly".Translate(), Settings.terrainMode == NatureRunningTerrainMode.NaturalOnly))
        {
            Settings.terrainMode = NatureRunningTerrainMode.NaturalOnly;
        }
        if (listing.RadioButton("NatureRunningZone_Settings_AllTerrain".Translate(), Settings.terrainMode == NatureRunningTerrainMode.AllTerrain))
        {
            Settings.terrainMode = NatureRunningTerrainMode.AllTerrain;
        }

        listing.GapLine();
        listing.CheckboxLabeled("NatureRunningZone_Settings_AllowIndoor".Translate(), ref Settings.allowIndoor);
        listing.CheckboxLabeled("NatureRunningZone_Settings_FallbackNoZone".Translate(), ref Settings.fallbackWhenNoZone);
        listing.CheckboxLabeled("NatureRunningZone_Settings_FallbackZoneFails".Translate(), ref Settings.fallbackWhenZoneFails);
        listing.CheckboxLabeled("NatureRunningZone_Settings_VerboseLogging".Translate(), ref Settings.verboseLogging);

        listing.GapLine();
        listing.Label("NatureRunningZone_Settings_MinDistance".Translate(Settings.minDistanceFromChild));
        Settings.minDistanceFromChild = RoundToInt(listing.Slider(Settings.minDistanceFromChild, 0f, 30f));
        listing.Label("NatureRunningZone_Settings_MinCells".Translate(Settings.minValidZoneCells));
        Settings.minValidZoneCells = RoundToInt(listing.Slider(Settings.minValidZoneCells, 1f, 100f));

        listing.End();
    }

    public override void WriteSettings()
    {
        Settings.Clamp();
        base.WriteSettings();
    }

    private static int RoundToInt(float value)
    {
        return Mathf.RoundToInt(value);
    }
}

public sealed class NatureRunningZoneSettings : ModSettings
{
    public bool fallbackWhenNoZone;
    public bool fallbackWhenZoneFails;
    public NatureRunningTerrainMode terrainMode = NatureRunningTerrainMode.NaturalOnly;
    public int minDistanceFromChild = 10;
    public bool allowIndoor;
    public int minValidZoneCells = 25;
    public bool verboseLogging;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref fallbackWhenNoZone, "fallbackWhenNoZone", false);
        Scribe_Values.Look(ref fallbackWhenZoneFails, "fallbackWhenZoneFails", false);
        Scribe_Values.Look(ref terrainMode, "terrainMode", NatureRunningTerrainMode.NaturalOnly);
        Scribe_Values.Look(ref minDistanceFromChild, "minDistanceFromChild", 10);
        Scribe_Values.Look(ref allowIndoor, "allowIndoor", false);
        Scribe_Values.Look(ref minValidZoneCells, "minValidZoneCells", 25);
        Scribe_Values.Look(ref verboseLogging, "verboseLogging", false);
        Clamp();
    }

    public void Clamp()
    {
        minDistanceFromChild = ClampInt(minDistanceFromChild, 0, 30);
        minValidZoneCells = ClampInt(minValidZoneCells, 1, 100);
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }
}

public enum NatureRunningTerrainMode
{
    NaturalOnly,
    AllTerrain
}
