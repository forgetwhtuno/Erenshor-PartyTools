using System;
using ErenshorPartyTools;

internal static class PanelPositioningTests
{
    private const float PanelWidth = 310f;
    private const float PanelHeight = 156f;

    private static int Main()
    {
        try
        {
            DefaultPositionIsVisible();
            DefaultPositionAvoidsMinimapBand();
            TypicalDisplaySizesStayVisible();
            NegativeAndOffscreenCoordinatesClamp();
            PartiallyOffscreenSavedPositionRecovers();
            NonFiniteSavedCoordinatesRecover();
            ResolutionShrinkClamps();
            NormalSavedPositionIsUnchanged();
            DragCommitPersistsAndRehydrates();
            FriendAvailabilityTests.Run();
            PartyRollSocialTests.Run();
            Console.WriteLine("PanelPositioningTests: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PanelPositioningTests: FAIL");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void DefaultPositionIsVisible()
    {
        PanelPosition position = PanelPositioning.Resolve(1920f, 1080f, PanelWidth, PanelHeight, 0f, 0f);
        AssertVisible(position, 1920f, 1080f, PanelWidth, PanelHeight, "default position");
    }

    private static void DefaultPositionAvoidsMinimapBand()
    {
        PanelPosition position = PanelPositioning.Resolve(1920f, 1080f, PanelWidth, PanelHeight, 0f, 0f);
        Assert(position.Y >= PanelPositioning.IntendedMinimapBottom + PanelPositioning.ScreenMargin,
            "default panel should start below the intended upper-right minimap band");
    }


    private static void TypicalDisplaySizesStayVisible()
    {
        AssertDefaultVisibleAt(1920f, 1080f, "1920x1080");
        AssertDefaultVisibleAt(2560f, 1440f, "2560x1440");
        AssertDefaultVisibleAt(1280f, 800f, "1280x800 Steam Deck");
    }

    private static void AssertDefaultVisibleAt(float width, float height, string label)
    {
        PanelPosition position = PanelPositioning.Resolve(width, height, PanelWidth, PanelHeight, 0f, 0f);
        AssertVisible(position, width, height, PanelWidth, PanelHeight, label);
        Assert(position.Y >= PanelPositioning.IntendedMinimapBottom + PanelPositioning.ScreenMargin,
            label + " default should remain below the intended minimap band");
    }

    private static void NegativeAndOffscreenCoordinatesClamp()
    {
        PanelPosition upperLeft = PanelPositioning.Resolve(1920f, 1080f, PanelWidth, PanelHeight, 5000f, -5000f);
        AssertNear(PanelPositioning.ScreenMargin, upperLeft.X, "negative X clamps");
        AssertNear(PanelPositioning.ScreenMargin, upperLeft.Y, "negative Y clamps");

        PanelPosition lowerRight = PanelPositioning.Resolve(1920f, 1080f, PanelWidth, PanelHeight, -5000f, 5000f);
        AssertNear(1920f - PanelWidth - PanelPositioning.ScreenMargin, lowerRight.X, "right edge clamps");
        AssertNear(1080f - PanelHeight - PanelPositioning.ScreenMargin, lowerRight.Y, "bottom edge clamps");
    }


    private static void PartiallyOffscreenSavedPositionRecovers()
    {
        PanelPosition expectedOffscreen = new PanelPosition(1100f, 700f);
        PanelOffsets saved = PanelPositioning.ToOffsets(1280f, PanelWidth, expectedOffscreen);
        int saveCount = 0;
        PanelPositionState state = new PanelPositionState(saved.X, saved.Y, delegate { saveCount++; });
        PanelPosition recovered = state.ResolveAndRecover(1280f, 800f, PanelWidth, PanelHeight);
        AssertVisible(recovered, 1280f, 800f, PanelWidth, PanelHeight, "partially offscreen saved position");
        AssertNear(1280f - PanelWidth - PanelPositioning.ScreenMargin, recovered.X, "partial right edge clamp");
        AssertNear(800f - PanelHeight - PanelPositioning.ScreenMargin, recovered.Y, "partial bottom edge clamp");
        Assert(saveCount == 1, "partial offscreen recovery should persist exactly once");
        state.ResolveAndRecover(1280f, 800f, PanelWidth, PanelHeight);
        Assert(saveCount == 1, "recovered partial position should not save every frame");
    }

    private static void NonFiniteSavedCoordinatesRecover()
    {
        AssertNonFiniteRecovery(float.NaN, 0f, "NaN X");
        AssertNonFiniteRecovery(0f, float.NaN, "NaN Y");
        AssertNonFiniteRecovery(float.PositiveInfinity, 0f, "positive infinity X");
        AssertNonFiniteRecovery(0f, float.NegativeInfinity, "negative infinity Y");
    }

    private static void AssertNonFiniteRecovery(float offsetX, float offsetY, string label)
    {
        int saveCount = 0;
        float savedX = float.NaN;
        float savedY = float.NaN;
        PanelPositionState state = new PanelPositionState(offsetX, offsetY, delegate(float x, float y)
        {
            saveCount++;
            savedX = x;
            savedY = y;
        });

        PanelPosition recovered = state.ResolveAndRecover(1280f, 800f, PanelWidth, PanelHeight);
        AssertVisible(recovered, 1280f, 800f, PanelWidth, PanelHeight, label);
        Assert(!float.IsNaN(recovered.X) && !float.IsInfinity(recovered.X), label + " recovered X must be finite");
        Assert(!float.IsNaN(recovered.Y) && !float.IsInfinity(recovered.Y), label + " recovered Y must be finite");
        Assert(saveCount == 1, label + " should persist one corrected position");
        Assert(!float.IsNaN(savedX) && !float.IsInfinity(savedX), label + " persisted X must be finite");
        Assert(!float.IsNaN(savedY) && !float.IsInfinity(savedY), label + " persisted Y must be finite");
        state.ResolveAndRecover(1280f, 800f, PanelWidth, PanelHeight);
        Assert(saveCount == 1, label + " should not save every frame after recovery");
    }

    private static void ResolutionShrinkClamps()
    {
        PanelPosition oldPosition = new PanelPosition(200f, 1150f);
        PanelOffsets saved = PanelPositioning.ToOffsets(2560f, PanelWidth, oldPosition);
        int saveCount = 0;
        PanelPositionState state = new PanelPositionState(saved.X, saved.Y, delegate { saveCount++; });
        PanelPosition recovered = state.ResolveAndRecover(1280f, 720f, PanelWidth, PanelHeight);
        AssertVisible(recovered, 1280f, 720f, PanelWidth, PanelHeight, "resolution-shrunk position");
        AssertNear(PanelPositioning.ScreenMargin, recovered.X, "resolution shrink left clamp");
        AssertNear(720f - PanelHeight - PanelPositioning.ScreenMargin, recovered.Y, "resolution shrink bottom clamp");
        Assert(saveCount == 1, "resolution recovery should persist the corrected position once");
        state.ResolveAndRecover(1280f, 720f, PanelWidth, PanelHeight);
        Assert(saveCount == 1, "stable recovered position should not save every frame");
    }

    private static void NormalSavedPositionIsUnchanged()
    {
        PanelPosition expected = new PanelPosition(900f, 420f);
        PanelOffsets saved = PanelPositioning.ToOffsets(1920f, PanelWidth, expected);
        int saveCount = 0;
        PanelPositionState state = new PanelPositionState(saved.X, saved.Y, delegate { saveCount++; });
        PanelPosition actual = state.ResolveAndRecover(1920f, 1080f, PanelWidth, PanelHeight);
        AssertNear(expected.X, actual.X, "normal saved X unchanged");
        AssertNear(expected.Y, actual.Y, "normal saved Y unchanged");
        Assert(saveCount == 0, "normal in-bounds saved position should not be rewritten");
    }

    private static void DragCommitPersistsAndRehydrates()
    {
        int saveCount = 0;
        float savedX = 0f;
        float savedY = 0f;
        PanelPositionState state = new PanelPositionState(0f, 0f, delegate(float x, float y)
        {
            saveCount++;
            savedX = x;
            savedY = y;
        });

        PanelPosition moved = state.MoveTo(1920f, 1080f, PanelWidth, PanelHeight, 760f, 500f);
        Assert(saveCount == 0, "drag motion must not write config per frame");
        state.CommitIfMoved();
        Assert(saveCount == 1, "drag completion should persist exactly once");
        state.CommitIfMoved();
        Assert(saveCount == 1, "a second commit without movement should not save again");

        PanelPositionState rehydrated = new PanelPositionState(savedX, savedY, null);
        PanelPosition restored = rehydrated.ResolveAndRecover(1920f, 1080f, PanelWidth, PanelHeight);
        AssertNear(moved.X, restored.X, "persisted drag X restores");
        AssertNear(moved.Y, restored.Y, "persisted drag Y restores");
    }

    private static void AssertVisible(PanelPosition position, float screenWidth, float screenHeight, float panelWidth, float panelHeight, string label)
    {
        Assert(position.X >= PanelPositioning.ScreenMargin, label + " X minimum");
        Assert(position.Y >= PanelPositioning.ScreenMargin, label + " Y minimum");
        Assert(position.X + panelWidth <= screenWidth - PanelPositioning.ScreenMargin + 0.01f, label + " right edge");
        Assert(position.Y + panelHeight <= screenHeight - PanelPositioning.ScreenMargin + 0.01f, label + " bottom edge");
    }

    private static void AssertNear(float expected, float actual, string label)
    {
        Assert(Math.Abs(expected - actual) <= 0.01f, label + ": expected " + expected + ", got " + actual);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
