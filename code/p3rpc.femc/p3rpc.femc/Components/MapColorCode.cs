namespace p3rpc.femc.Components;

internal static class MapColorCode
{
    internal static byte[] Build(int channel, int instructionLength, byte value)
    {
        int minimumLength = channel < 2 ? 2 : 3;
        if (channel is < 0 or > 3 || instructionLength < minimumLength)
            throw new ArgumentException("Invalid color argument instruction");
        byte[] result = Enumerable.Repeat((byte)0x90, instructionLength).ToArray();
        if (channel < 2) result[0] = channel == 0 ? (byte)0xB1 : (byte)0xB2;
        else
        {
            result[0] = 0x41;
            result[1] = channel == 2 ? (byte)0xB0 : (byte)0xB1;
        }
        result[minimumLength - 1] = value;
        return result;
    }
}
