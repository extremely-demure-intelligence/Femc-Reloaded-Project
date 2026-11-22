using System.Text.Json;
using p3rpc.femc.Components;

var cases = from register in new[] { "eax", "edi", "r9d" }
            from preserveAlpha in new[] { false, true }
            from bgra in new[] { false, true }
            from onlyIfLowByteNonZero in new[] { false, true }
            select new { register, preserveAlpha, bgra, onlyIfLowByteNonZero,
                instructions = NativeColorCode.Build(0x1122334455667788, register, preserveAlpha, bgra, onlyIfLowByteNonZero) };
Console.WriteLine(JsonSerializer.Serialize(cases));
try
{
    NativeColorCode.Build(0, "rsp", true);
    throw new Exception("Stack pointer accepted as color output");
}
catch (ArgumentException) { }
