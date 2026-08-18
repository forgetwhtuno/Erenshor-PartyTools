using System;

namespace ErenshorPartyTools
{
    internal struct PartyToolsUiRect
    {
        internal float X, Y, Width, Height;
        internal PartyToolsUiRect(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
    }

    internal struct PartyToolsPanelLayout
    {
        internal readonly bool ShowFooter;
        internal readonly float ResultY;
        internal readonly float ViewportBottom;
        internal readonly float ViewportHeight;

        internal PartyToolsPanelLayout(bool showFooter, float resultY, float viewportBottom, float viewportHeight)
        {
            ShowFooter = showFooter; ResultY = resultY; ViewportBottom = viewportBottom; ViewportHeight = viewportHeight;
        }
    }

    internal static class PartyToolsPanelLayoutPolicy
    {
        internal static PartyToolsPanelLayout Resolve(float panelHeight)
        {
            float resultY = panelHeight - 216f;
            bool showFooter = panelHeight >= 360f;
            float bottom = showFooter ? 66f : 12f;
            float height = Math.Max(30f, resultY - bottom - 6f);
            return new PartyToolsPanelLayout(showFooter, resultY, bottom, height);
        }

        internal static bool NoResultOverlap(float panelHeight)
        {
            PartyToolsPanelLayout layout = Resolve(panelHeight);
            return layout.ViewportBottom + layout.ViewportHeight <= layout.ResultY - 5.9f;
        }
    }

    internal static class PartyToolsUiGeometry
    {
        internal const float Unset = -1f;
        internal const float Margin = 10f;
        internal const float PanelWidth = 370f;
        internal const float PanelHeight = 455f;
        internal const float HeaderHeight = 32f;
        internal const float CollapsedHeight = HeaderHeight;
        internal const float LauncherWidth = 154f;
        internal const float LauncherHeight = 32f;

        internal static float Interpret(float value) { return Finite(value) && value >= 0f && value <= 1f ? value : Unset; }

        internal static PartyToolsUiRect ResolvePanel(float x, float y, float sw, float sh)
        {
            x = Interpret(x); y = Interpret(y);
            PartyToolsUiRect r = x == Unset || y == Unset
                ? new PartyToolsUiRect(Math.Max(Margin, sw - PanelWidth - 18f), Math.Max(Margin, sh - PanelHeight - 190f), PanelWidth, PanelHeight)
                : new PartyToolsUiRect(x * sw, y * sh, PanelWidth, PanelHeight);
            return Clamp(r, sw, sh);
        }

        internal static PartyToolsUiRect ResolveLauncher(float x, float y, float sw, float sh)
        {
            x = Interpret(x); y = Interpret(y);
            PartyToolsUiRect r = x == Unset || y == Unset
                ? new PartyToolsUiRect(Math.Max(Margin, sw - LauncherWidth - 18f), Math.Max(Margin, sh - LauncherHeight - 152f), LauncherWidth, LauncherHeight)
                : new PartyToolsUiRect(x * sw, y * sh, LauncherWidth, LauncherHeight);
            return Clamp(r, sw, sh);
        }

        internal static PartyToolsUiRect Clamp(PartyToolsUiRect r, float sw, float sh)
        {
            r.Width = Math.Min(r.Width, Math.Max(80f, sw - 2f * Margin));
            r.Height = Math.Min(r.Height, Math.Max(32f, sh - 2f * Margin));
            r.X = ClampValue(Finite(r.X) ? r.X : Margin, Margin, Math.Max(Margin, sw - r.Width - Margin));
            r.Y = ClampValue(Finite(r.Y) ? r.Y : Margin, Margin, Math.Max(Margin, sh - r.Height - Margin));
            return r;
        }

        internal static PartyToolsUiRect CollapseFromExpanded(PartyToolsUiRect expanded, float sw, float sh)
        {
            PartyToolsUiRect value = new PartyToolsUiRect(expanded.X,
                expanded.Y + expanded.Height - CollapsedHeight,
                expanded.Width, CollapsedHeight);
            return Clamp(value, sw, sh);
        }

        internal static PartyToolsUiRect ExpandFromCollapsed(PartyToolsUiRect collapsed, float expandedHeight, float sw, float sh)
        {
            float height = Math.Max(HeaderHeight, expandedHeight);
            PartyToolsUiRect value = new PartyToolsUiRect(collapsed.X,
                collapsed.Y + collapsed.Height - height,
                collapsed.Width, height);
            return Clamp(value, sw, sh);
        }

        internal static void Normalize(PartyToolsUiRect r, float sw, float sh, out float x, out float y)
        {
            x = sw <= 0f ? 0f : ClampValue(r.X / sw, 0f, 1f);
            y = sh <= 0f ? 0f : ClampValue(r.Y / sh, 0f, 1f);
        }

        internal static string RunSelfTests()
        {
            if (Interpret(float.NaN) != Unset || Interpret(250f) != Unset) return "FAIL partytools legacy position rejection";
            if (Math.Abs(Interpret(.5f) - .5f) > .0001f) return "FAIL partytools normalized position";
            PartyToolsUiRect r = ResolvePanel(Unset, Unset, 1920, 1080);
            if (r.X < Margin || r.Y < Margin || r.X + r.Width > 1920 || r.Y + r.Height > 1080) return "FAIL partytools panel clamp";
            PartyToolsUiRect l = ResolveLauncher(1f, 1f, 640, 360);
            if (l.X + l.Width > 640.1f || l.Y + l.Height > 360.1f) return "FAIL partytools launcher clamp";
            PartyToolsUiRect expanded = new PartyToolsUiRect(100f, 100f, PanelWidth, PanelHeight);
            PartyToolsUiRect collapsed = CollapseFromExpanded(expanded, 1920f, 1080f);
            if (Math.Abs(collapsed.Height - HeaderHeight) > .001f) return "FAIL partytools collapsed height";
            if (Math.Abs((expanded.Y + expanded.Height) - (collapsed.Y + collapsed.Height)) > .001f) return "FAIL partytools collapse top preservation";
            PartyToolsUiRect restored = ExpandFromCollapsed(collapsed, expanded.Height, 1920f, 1080f);
            if (Math.Abs(restored.Y - expanded.Y) > .001f || Math.Abs(restored.Height - expanded.Height) > .001f)
                return "FAIL partytools expand restoration";
            if (HeaderHeight != 32f) return "FAIL partytools canonical header height";
            return "PASS partytools retained ui geometry";
        }

        private static float ClampValue(float v, float min, float max) { return v < min ? min : v > max ? max : v; }
        private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
    }
}
