using Reloaded.Memory.Sigscan;
using System.Diagnostics;
using System.Text.Json;

using var scanner = new Scanner(File.ReadAllBytes(args[0]));
var cases = JsonSerializer.Deserialize<ScanCase[]>(File.ReadAllText(args[1]))!;
var timer = Stopwatch.StartNew();
foreach (var test in cases)
{
    Console.WriteLine($"Scanning {test.Name}");
    var result = scanner.FindPattern(test.Signature);
    if (!result.Found || result.Offset != test.FileOffset)
        throw new Exception($"{test.Name}: expected {test.FileOffset:X}, got {result.Found}/{result.Offset:X}");
}
Console.WriteLine($"PASS: installed Reloaded scanner resolved {cases.Length} map signatures in {timer.ElapsedMilliseconds}ms");
record ScanCase(string Name, string Signature, int FileOffset);
