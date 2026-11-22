namespace p3rpc.femc.Components;

internal static class NativeColorCode
{
    internal static string[] Build(ulong storage, string register, bool preserveAlpha, bool bgra = false, bool onlyIfLowByteNonZero = false)
    {
        if (register is not ("eax" or "edi" or "r9d"))
            throw new ArgumentException("Unsupported packed color register", nameof(register));
        // FSprColor is 0xRRGGBBAA; Unreal FColor is 0xAARRGGBB.
        // Mid-function callers may have live flags and volatile registers. Save
        // both flags and our scratch register; do not touch any SIMD registers.
        var instructions = new List<string>
        {
            "use64", "pushfq", "push r11", $"mov r11, 0x{storage:X}"
        };
        if (onlyIfLowByteNonZero)
        {
            instructions.Add($"test {register}, 0xFF");
            instructions.Add("jz .keepOriginalColor");
        }
        if (preserveAlpha)
        {
            instructions.Add($"and {register}, 0x{(bgra ? 0xFF000000u : 0xFFu):X8}");
            instructions.Add($"or {register}, dword [r11]");
        }
        else instructions.Add($"mov {register}, dword [r11]");
        if (onlyIfLowByteNonZero) instructions.Add(".keepOriginalColor:");
        instructions.Add("pop r11");
        instructions.Add("popfq");
        return instructions.ToArray();
    }
}
