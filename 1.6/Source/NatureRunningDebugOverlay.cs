using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FrostyDog.NatureRunningZone;

internal static class NatureRunningDebugOverlay
{
    private sealed class ZoneOverlayInfo
    {
        public Zone_NatureRunning Zone = null!;
        public ZoneDebugInfo DebugInfo = null!;
    }

    private static readonly Color ValidColor = new(0.2f, 1f, 0.2f, 0.85f);
    private static readonly Color TemporaryFailColor = new(1f, 0.85f, 0.15f, 0.85f);
    private static readonly Color InvalidColor = new(1f, 0.2f, 0.2f, 0.85f);
    private static readonly Color DisabledColor = new(0.75f, 0.75f, 0.75f, 0.85f);

    private static bool enabled;
    private static int cachedFrame = -1;
    private static Pawn? cachedPawn;
    private static readonly List<ZoneOverlayInfo> cachedZones = new();

    public static bool Active
    {
        get
        {
            return enabled && Prefs.DevMode;
        }
    }

    [DebugAction("Nature Running Zone", "Toggle selected pawn overlay", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ToggleSelectedPawnOverlay()
    {
        enabled = !enabled;
        string state = enabled ? "NatureRunningZone_Debug_OverlayOn".Translate() : "NatureRunningZone_Debug_OverlayOff".Translate();
        Messages.Message("NatureRunningZone_Debug_OverlayToggled".Translate(state), MessageTypeDefOf.NeutralEvent, historical: false);
    }

    public static void DrawMapOverlay()
    {
        Pawn? pawn = SelectedPawn();
        if (!Active || pawn?.Map == null || pawn.Map != Find.CurrentMap)
        {
            return;
        }

        foreach (ZoneOverlayInfo overlayInfo in GetOverlayInfo(pawn))
        {
            ZoneDebugInfo info = overlayInfo.DebugInfo;
            if (!info.ZoneUsable)
            {
                GenDraw.DrawFieldEdges(overlayInfo.Zone.cells, DisabledColor);
                continue;
            }

            GenDraw.DrawFieldEdges(info.ValidCells, ValidColor);
            GenDraw.DrawFieldEdges(info.TemporaryFailCells, TemporaryFailColor);
            GenDraw.DrawFieldEdges(info.InvalidCells, InvalidColor);
        }
    }

    public static void DrawLabels()
    {
        Pawn? pawn = SelectedPawn();
        if (!Active || pawn?.Map == null || pawn.Map != Find.CurrentMap)
        {
            return;
        }

        DrawScreenLabel(pawn);
        foreach (ZoneOverlayInfo overlayInfo in GetOverlayInfo(pawn))
        {
            Zone_NatureRunning zone = overlayInfo.Zone;
            if (zone.cells.NullOrEmpty())
            {
                continue;
            }

            ZoneDebugInfo info = overlayInfo.DebugInfo;
            string text;
            Color color;
            if (!info.ZoneUsable)
            {
                text = "NatureRunningZone_Debug_ZoneBlocked".Translate(zone.label, FailureReasonText(info.ZoneFailureReason));
                color = DisabledColor;
            }
            else if (info.ValidCells.Count < NatureRunningZoneMod.Settings.minValidZoneCells)
            {
                text = "NatureRunningZone_Debug_ZoneTooFewCells".Translate(zone.label, info.ValidCells.Count, NatureRunningZoneMod.Settings.minValidZoneCells, FailureReasonText(info.ZoneFailureReason));
                color = TemporaryFailColor;
            }
            else
            {
                text = "NatureRunningZone_Debug_ZoneValid".Translate(zone.label, info.ValidCells.Count);
                color = ValidColor;
            }

            GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(zone.Position), text, color);
        }
    }

    private static Pawn? SelectedPawn()
    {
        return Find.Selector?.SingleSelectedThing as Pawn;
    }

    private static List<ZoneOverlayInfo> GetOverlayInfo(Pawn pawn)
    {
        if (cachedFrame == Time.frameCount && cachedPawn == pawn)
        {
            return cachedZones;
        }

        cachedFrame = Time.frameCount;
        cachedPawn = pawn;
        cachedZones.Clear();

        foreach (Zone_NatureRunning zone in pawn.Map.zoneManager.AllZones.OfType<Zone_NatureRunning>())
        {
            cachedZones.Add(new ZoneOverlayInfo
            {
                Zone = zone,
                DebugInfo = NatureRunningTargetFinder.GetZoneDebugInfo(zone, pawn)
            });
        }

        return cachedZones;
    }

    private static void DrawScreenLabel(Pawn pawn)
    {
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        Widgets.Label(new Rect(12f, 120f, 520f, 48f), "NatureRunningZone_Debug_OverlayHeader".Translate(pawn.LabelShortCap));
        Text.Font = GameFont.Small;
    }

    private static string FailureReasonText(string? key)
    {
        return key.NullOrEmpty() ? "NatureRunningZone_Debug_Unknown".Translate() : key.Translate();
    }
}

[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate))]
internal static class Patch_MapInterface_MapInterfaceUpdate_NatureRunningOverlay
{
    private static void Postfix()
    {
        NatureRunningDebugOverlay.DrawMapOverlay();
    }
}

[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs))]
internal static class Patch_MapInterface_MapInterfaceOnGUI_AfterMainTabs_NatureRunningOverlay
{
    private static void Postfix()
    {
        NatureRunningDebugOverlay.DrawLabels();
    }
}
