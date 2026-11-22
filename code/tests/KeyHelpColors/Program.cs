using p3rpc.femc.Components;

unsafe
{
    const int guard = 32;
    const uint rgb = 0xFF586A00;
    // Include sentinels before and after an entire fast-forward object.
    byte[] buffer = new byte[guard + 0x598 + guard];
    int cases = 0;
    foreach (bool movie in new[] { false, true })
    foreach (int state in new[] { 0, 1, 2, 3 })
    foreach (int count in new[] { int.MinValue, -1, 0, 1, 2, 3, 4, 5, 65536, int.MaxValue })
    for (int alpha = 0; alpha < 256; alpha++)
    {
        Array.Fill(buffer, (byte)alpha);
        fixed (byte* memory = buffer)
        {
            nint self = (nint)(memory + guard);
            // Former offsets contain flags, not a count or activation state.
            *(int*)(self + 0x258) = 0x01010101;
            *(int*)(self + 0x588) = state == 1 ? 0 : 1;
            *(int*)(self + KeyHelpColors.SpriteCount) = count;
            *(int*)(self + KeyHelpColors.ActivationState) = state;
            byte[] expected = (byte[])buffer.Clone();
            void Expect(int offset)
            {
                expected[guard + offset + 1] = 0x6A;
                expected[guard + offset + 2] = 0x58;
                expected[guard + offset + 3] = 0xFF;
            }
            if ((movie || state is 1 or 2) && count is >= 0 and <= 4)
            {
                Expect(KeyHelpColors.TextColor);
                for (int i = 0; i < count; i++)
                    Expect(KeyHelpColors.SpriteColor + i * KeyHelpColors.SpriteStride);
            }
            if (movie)
            {
                Expect(KeyHelpColors.MovieMainColor);
                Expect(KeyHelpColors.MoviePulseColor);
                KeyHelpColors.ApplyMovie(self, rgb);
            }
            else KeyHelpColors.ApplyFastForward(self, rgb);
            if (!buffer.SequenceEqual(expected))
                throw new Exception($"Unexpected write: movie={movie}, state={state}, count={count}, alpha={alpha}");
            cases++;
        }
    }
    KeyHelpColors.ApplyFastForward(0, rgb);
    KeyHelpColors.ApplyMovie(0, rgb);
    Console.WriteLine($"PASS: {cases} prompt recoloring cases; all non-color bytes and native alpha preserved.");
}
