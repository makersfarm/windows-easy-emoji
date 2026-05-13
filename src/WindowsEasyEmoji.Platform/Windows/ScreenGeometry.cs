namespace WindowsEasyEmoji.Platform.Windows;

public readonly record struct ScreenSize(double Width, double Height);

public readonly record struct ScreenRectangle(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;

    public double Height => Bottom - Top;

    public double CenterY => Top + (Height / 2);
}

public enum OverlayPlacementSide
{
    Above,
    Below
}

public readonly record struct OverlayPlacement(double Left, double Top, OverlayPlacementSide Side);
