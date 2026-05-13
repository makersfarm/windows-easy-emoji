using WindowsEasyEmoji.Platform.Windows;

namespace WindowsEasyEmoji.Platform.Tests.Windows;

public sealed class OverlayPlacementCalculatorTests
{
    [Fact]
    public void Calculate_places_overlay_below_anchor_in_upper_half()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            anchor: new ScreenRectangle(200, 80, 240, 110),
            overlay: new ScreenSize(420, 420),
            workArea: new ScreenRectangle(0, 0, 1200, 900),
            gap: 10);

        Assert.Equal(OverlayPlacementSide.Below, placement.Side);
        Assert.Equal(120, placement.Top);
    }

    [Fact]
    public void Calculate_places_overlay_above_anchor_in_lower_half()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            anchor: new ScreenRectangle(200, 790, 240, 820),
            overlay: new ScreenSize(420, 420),
            workArea: new ScreenRectangle(0, 0, 1200, 900),
            gap: 10);

        Assert.Equal(OverlayPlacementSide.Above, placement.Side);
        Assert.Equal(360, placement.Top);
    }

    [Fact]
    public void Calculate_clamps_overlay_inside_work_area()
    {
        var placement = OverlayPlacementCalculator.Calculate(
            anchor: new ScreenRectangle(1160, 790, 1190, 820),
            overlay: new ScreenSize(420, 420),
            workArea: new ScreenRectangle(0, 0, 1200, 900),
            gap: 10);

        Assert.Equal(780, placement.Left);
        Assert.Equal(360, placement.Top);
    }
}
