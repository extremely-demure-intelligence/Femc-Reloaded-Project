using p3rpc.commonmodutils;
using System.Runtime.InteropServices;

namespace p3rpc.femc.Components;

/// <summary>Edits verified color-constructor arguments without redirecting game execution.</summary>
public abstract class DirectColorModule : NativeColorModule
{
    protected DirectColorModule(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules)
        : base(context, modules) { }

    // Channels: 0=CL/red, 1=DL/green, 2=R8B/blue, 3=R9B/alpha.
    // RGB-only patches leave the game's animated alpha calculation intact.
    protected void PatchColorInputs(long anchor, string name, Func<ConfigColor> color,
        params (int Offset, string Expected, int Channel)[] inputs)
    {
        var patches = inputs.Select(input => (Input: input,
            Expected: Convert.FromHexString(input.Expected))).ToArray();
        // Validate the entire group before changing any instruction.
        foreach (var patch in patches)
        {
            for (int i = 0; i < patch.Expected.Length; i++)
            {
                if (Marshal.ReadByte((nint)(anchor + patch.Input.Offset + i)) != patch.Expected[i])
                {
                    _context._logger.WriteLine($"[FemC] Skipping {name}: color instruction differs from the verified layout.");
                    return;
                }
            }
            if (patch.Input.Channel is < 0 or > 3 || patch.Expected.Length < (patch.Input.Channel < 2 ? 2 : 3))
                throw new ArgumentException("Invalid color argument patch", nameof(inputs));
        }
        foreach (var patch in patches)
        {
            var input = patch.Input;
            var address = (nuint)(anchor + input.Offset);
            int immediate = input.Channel < 2 ? 1 : 2;
            byte Value()
            {
                var value = color();
                return input.Channel switch { 0 => value.R, 1 => value.G, 2 => value.B, _ => value.A };
            }
            var replacement = MapColorCode.Build(input.Channel, patch.Expected.Length, Value());
            // AddressToMemoryWrite grants write access. Keep config refreshes to
            // the immediate byte; opcodes and instruction lengths stay fixed.
            var install = new AddressToMemoryWrite(_context._memory, address, destination =>
            {
                for (int i = 0; i < replacement.Length; i++)
                    _context._memory.Write(destination + (nuint)i, replacement[i]);
            });
            _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, address + (nuint)immediate,
                destination => _context._memory.Write(destination, Value())));
        }
    }
}
