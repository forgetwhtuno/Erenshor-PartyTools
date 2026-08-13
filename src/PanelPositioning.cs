using System;

namespace ErenshorPartyTools
{
    internal struct PanelPosition
    {
        internal readonly float X;
        internal readonly float Y;

        internal PanelPosition(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    internal struct PanelOffsets
    {
        internal readonly float X;
        internal readonly float Y;

        internal PanelOffsets(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    internal static class PanelPositioning
    {
        internal const float ScreenMargin = 8f;
        internal const float RightMargin = 18f;
        internal const float DefaultTop = 336f;
        internal const float IntendedMinimapBottom = 320f;
        private const float PositionEpsilon = 0.01f;

        internal static PanelPosition Resolve(
            float screenWidth,
            float screenHeight,
            float panelWidth,
            float panelHeight,
            float offsetX,
            float offsetY)
        {
            offsetX = FiniteOrDefault(offsetX, 0f);
            offsetY = FiniteOrDefault(offsetY, 0f);
            float desiredX = screenWidth - panelWidth - RightMargin - offsetX;
            float desiredY = DefaultTop + offsetY;
            return Clamp(screenWidth, screenHeight, panelWidth, panelHeight, desiredX, desiredY);
        }

        internal static PanelPosition Clamp(
            float screenWidth,
            float screenHeight,
            float panelWidth,
            float panelHeight,
            float desiredX,
            float desiredY)
        {
            float maxX = Math.Max(ScreenMargin, screenWidth - panelWidth - ScreenMargin);
            float maxY = Math.Max(ScreenMargin, screenHeight - panelHeight - ScreenMargin);
            return new PanelPosition(
                ClampValue(desiredX, ScreenMargin, maxX),
                ClampValue(desiredY, ScreenMargin, maxY));
        }

        internal static PanelOffsets ToOffsets(float screenWidth, float panelWidth, PanelPosition position)
        {
            return new PanelOffsets(
                screenWidth - panelWidth - RightMargin - position.X,
                position.Y - DefaultTop);
        }

        internal static bool NearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) <= PositionEpsilon;
        }

        private static float ClampValue(float value, float minimum, float maximum)
        {
            if (!IsFinite(value)) return minimum;
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }

        private static float FiniteOrDefault(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal sealed class PanelPositionState
    {
        private readonly Action<float, float> _persist;
        private float _offsetX;
        private float _offsetY;
        private bool _dirty;

        internal PanelPositionState(float offsetX, float offsetY, Action<float, float> persist)
        {
            _offsetX = offsetX;
            _offsetY = offsetY;
            _persist = persist;
        }

        internal float OffsetX { get { return _offsetX; } }
        internal float OffsetY { get { return _offsetY; } }

        internal PanelPosition ResolveAndRecover(
            float screenWidth,
            float screenHeight,
            float panelWidth,
            float panelHeight)
        {
            PanelPosition position = PanelPositioning.Resolve(
                screenWidth, screenHeight, panelWidth, panelHeight, _offsetX, _offsetY);
            PanelOffsets normalized = PanelPositioning.ToOffsets(screenWidth, panelWidth, position);
            if (SetOffsets(normalized.X, normalized.Y))
            {
                _dirty = false;
                Persist();
            }
            return position;
        }

        internal PanelPosition MoveTo(
            float screenWidth,
            float screenHeight,
            float panelWidth,
            float panelHeight,
            float desiredX,
            float desiredY)
        {
            PanelPosition position = PanelPositioning.Clamp(
                screenWidth, screenHeight, panelWidth, panelHeight, desiredX, desiredY);
            PanelOffsets offsets = PanelPositioning.ToOffsets(screenWidth, panelWidth, position);
            if (SetOffsets(offsets.X, offsets.Y)) _dirty = true;
            return position;
        }

        internal void Reset()
        {
            if (!SetOffsets(0f, 0f)) return;
            _dirty = false;
            Persist();
        }

        internal void CommitIfMoved()
        {
            if (!_dirty) return;
            _dirty = false;
            Persist();
        }

        private bool SetOffsets(float offsetX, float offsetY)
        {
            if (PanelPositioning.NearlyEqual(_offsetX, offsetX) &&
                PanelPositioning.NearlyEqual(_offsetY, offsetY))
                return false;

            _offsetX = offsetX;
            _offsetY = offsetY;
            return true;
        }

        private void Persist()
        {
            if (_persist != null) _persist(_offsetX, _offsetY);
        }
    }
}
