namespace p3rpc.femc.Components;

// Installed only after KeyHelp verifies these offsets against native code.
internal static unsafe class KeyHelpColors
{
    internal const int SpriteColor = 0x50;
    internal const int SpriteStride = 0x68;
    internal const int SpriteCapacity = 4;
    internal const int TextColor = 0x1F0;
    internal const int SpriteCount = 0x260;
    internal const int ActivationState = 0x590;
    internal const int MovieMainColor = 0x41C;
    internal const int MoviePulseColor = 0x4DC;

    private static void Recolor(nint self, int offset, uint rgb)
    {
        var color = (uint*)(self + offset);
        // Retain each native fade alpha, including disabled/transition states.
        *color = (*color & 0xFFu) | (rgb & 0xFFFFFF00u);
    }

    private static void ApplyText(nint self, uint rgb)
    {
        int count = *(int*)(self + SpriteCount);
        // The four inline sprite layouts end where the text layout begins.
        // Never trust an invalid count to drive writes beyond that array.
        if ((uint)count > SpriteCapacity) return;
        Recolor(self, TextColor, rgb);
        for (int i = 0; i < count; i++)
            Recolor(self, SpriteColor + i * SpriteStride, rgb);
    }

    internal static void ApplyFastForward(nint self, uint rgb)
    {
        if (self == 0) return;
        int state = *(int*)(self + ActivationState);
        if (state is 1 or 2) ApplyText(self, rgb);
    }

    internal static void ApplyMovie(nint self, uint rgb)
    {
        if (self == 0) return;
        ApplyText(self, rgb);
        Recolor(self, MovieMainColor, rgb);
        Recolor(self, MoviePulseColor, rgb);
    }
}
