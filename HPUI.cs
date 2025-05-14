using UnityEngine;
using Verse;
using System;

public static class HPUI
{
    // Basic UI Elements
    public static bool DrawButton(Rect rect, string label)
    {
        return Widgets.ButtonText(rect, label);
    }

    public static void DrawLabel(Rect rect, string label)
    {
        Widgets.Label(rect, label);
    }

    public static float DrawSlider(Rect rect, float value, float min, float max)
    {
        return Widgets.HorizontalSlider(rect, value, min, max);
    }

    public static string DrawTextField(Rect rect, string text)
    {
        return Widgets.TextField(rect, text);
    }

    // Window Management
    public static void OpenWindow(Window window)
    {
        Find.WindowStack.Add(window);
    }

    // Utility Functions
    public static Vector2 GetMousePosition()
    {
        return UI.MousePositionOnUIInverted;
    }

    public static bool IsMouseOver(Rect rect)
    {
        return Mouse.IsOver(rect);
    }

    public static bool IsKeyPressed(KeyCode key)
    {
        return Input.GetKey(key);
    }

    // Image Loading
    public static Texture2D LoadTexture(string path)
    {
        try
        {
            return ContentFinder<Texture2D>.Get(path, true);
        }
        catch (Exception ex)
        {
            Log.Error($"[HPUI] Failed to load texture at path '{path}': {ex.Message}");
            return null;
        }
    }

    public static Graphic LoadGraphic(string path, Shader shader = null, Vector2 drawSize = default(Vector2), Color color = default(Color))
    {
        if (shader == null)
        {
            shader = ShaderDatabase.Cutout; // Basic shader for cutout-style textures
        }
        if (drawSize == default(Vector2))
        {
            drawSize = Vector2.one; // 1x1 default size
        }
        if (color == default(Color))
        {
            color = Color.white;
        }

        try
        {
            return GraphicDatabase.Get<Graphic_Single>(path, shader, drawSize, color);
        }
        catch (Exception ex)
        {
            Log.Error($"[HPUI] Failed to load graphic at path '{path}': {ex.Message}");
            return null;
        }
    }
     public static void ShowArchitectMenu()
    {
        // opens the Architect tab group
        Find.DesignationManager.Select(null);            // clear any tool
        Find.MainButtonsRoot.OpenTab(MainButtonDefOf.Architect);
    }
    public static void CloseArchitectMenu()
    {
        Find.MainButtonsRoot.CloseOpenTab();
    }
    public static bool IsArchitectMenuOpen()
    {
        return Find.MainButtonsRoot.OpenTab == MainButtonDefOf.Architect;
    }

    // 2) Inspect pane (bottom-left when you click something)
    public static bool IsInspectPaneVisible()
    {
        return Find.MainTabsRoot.OpenTab == MainButtonDefOf.Inspect;
    }
    public static void ShowInspectPane()
    {
        Find.MainButtonsRoot.OpenTab(MainButtonDefOf.Inspect);
    }
    public static void CloseInspectPane()
    {
        Find.MainButtonsRoot.CloseOpenTab();
    }

    // 3) Cell info is just the default when nothing else is open—no toggle needed
    //    but you can query it:
    public static string GetCellInfo()
    {
        var cell = UI.MouseCell();
        return GenGeo.GetCellInfo(cell);  // uses RimWorld’s built-in cell info formatter
    }

    // 4) Resource list (top-left) – toggles “categorized mode”
    public static bool IsResourceCategorized()
    {
        return Find.PlaySettings.usePlayMenuDialog; // actually backs the categorized toggle
    }
    public static void ToggleResourceCategorized()
    {
        Find.PlaySettings.usePlayMenuDialog = !Find.PlaySettings.usePlayMenuDialog;
    }

    // 5) Zone visibility (bottom-right toggles)
    public static bool IsZoneVisibilityOn()
    {
        return Find.PlaySettings.showZone;
    }
    public static void ToggleZoneVisibility()
    {
        Find.PlaySettings.showZone = !Find.PlaySettings.showZone;
    }

    // 6) Time speed controls (pause / normal / fast / faster)
    public static void SetTimeSpeed(TimeSpeed speed)
    {
        Find.TickManager.CurTimeSpeed = speed;
    }
    public static TimeSpeed GetTimeSpeed()
    {
        return Find.TickManager.CurTimeSpeed;
    }

    // 7) Current in-game time string
    public static string GetCurrentTimeString()
    {
        return GenDate.DateFullStringAt(GenTicks.TicksAbs, Find.CurrentMap);
    }

    // 8) Dev mode toolbar (top-center)
    public static bool IsDevMode()
    {
        return Prefs.DevMode;
    }
    public static void ToggleDevMode()
    {
        Prefs.DevMode = !Prefs.DevMode;
    }

    // 9) Learning helper (top-right hints)
    public static bool IsTutorialMode()
    {
        return TutorialState.activeInt.tutorialMode;
    }
    public static void ToggleTutorialMode()
    {
        var ts = TutorialState.activeInt;
        ts.tutorialMode = !ts.tutorialMode;
    }

    // 10) Colonist bar (top)
    public static bool IsColonistBarVisible()
    {
        return Find.ColonistBar.IsVisible;
    }
    public static void ToggleColonistBar()
    {
        Find.ColonistBar.ToggleVisibility();
    }
}
