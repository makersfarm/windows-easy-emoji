namespace WindowsEasyEmoji.Platform.Windows;

public static class OverlayPlacementCalculator
{
    public static OverlayPlacement Calculate(
        ScreenRectangle anchor,
        ScreenSize overlay,
        ScreenRectangle workArea,
        double gap)
    {
        var workCenterY = workArea.Top + (workArea.Height / 2);
        var prefersBelow = anchor.CenterY < workCenterY;
        var spaceAbove = Math.Max(0, anchor.Top - workArea.Top - gap);
        var spaceBelow = Math.Max(0, workArea.Bottom - anchor.Bottom - gap);

        var side = ChooseSide(prefersBelow, spaceAbove, spaceBelow, overlay.Height);
        var desiredTop = side == OverlayPlacementSide.Below
            ? anchor.Bottom + gap
            : anchor.Top - gap - overlay.Height;
        var desiredLeft = anchor.Left;

        return new OverlayPlacement(
            Left: Clamp(desiredLeft, workArea.Left, workArea.Right - overlay.Width),
            Top: Clamp(desiredTop, workArea.Top, workArea.Bottom - overlay.Height),
            Side: side);
    }

    private static OverlayPlacementSide ChooseSide(
        bool prefersBelow,
        double spaceAbove,
        double spaceBelow,
        double overlayHeight)
    {
        if (prefersBelow)
        {
            if (spaceBelow >= overlayHeight || spaceBelow >= spaceAbove)
            {
                return OverlayPlacementSide.Below;
            }

            return OverlayPlacementSide.Above;
        }

        if (spaceAbove >= overlayHeight || spaceAbove >= spaceBelow)
        {
            return OverlayPlacementSide.Above;
        }

        return OverlayPlacementSide.Below;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }
}
