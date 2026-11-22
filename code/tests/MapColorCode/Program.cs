using System.Text.Json;
using p3rpc.femc.Components;

var cases = from channel in Enumerable.Range(0, 4)
            from length in Enumerable.Range(channel < 2 ? 2 : 3, 5)
            select new { channel, length, bytes = Convert.ToHexString(MapColorCode.Build(channel, length,
                new byte[] { 0x12, 0x34, 0x56, 0x78 }[channel])) };
Console.WriteLine(JsonSerializer.Serialize(cases));
foreach (var (channel, length) in new[] { (-1, 3), (4, 3), (0, 1), (2, 2) })
{
    try { MapColorCode.Build(channel, length, 0); }
    catch (ArgumentException) { continue; }
    throw new Exception("Invalid color instruction accepted");
}
