﻿
public static class Global
{
    public static List<string> audioFiles = new List<string>();
    public static int currentTrackIndex = 0;

    public enum CameraMode
    {
        Basic,
        Advanced,
        Hybrid
 
    }

    public static CameraMode currentCameraMode = CameraMode.Hybrid;

    // Текущее окружение
#if DEBUG
    public const int consoleWidth = 250;
    public const int consoleHeight = 100;
#else 
    public const int consoleWidth = 120;
    public const int consoleHeight = 30;
#endif
    public const int defaultWidth = 250;
    public const int defaultHeight = 200;
}

