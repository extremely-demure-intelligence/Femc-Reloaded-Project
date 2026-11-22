using p3rpc.commonmodutils;

namespace p3rpc.femc.Components;

/// <summary>Native color substitutions without a managed transition during drawing.</summary>
public abstract class NativeColorModule : ModuleAsmInlineColorEdit<FemcContext>
{
    protected NativeColorModule(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules)
        : base(context, modules) { }

    protected string[] ColorInRegister(Func<ConfigColor> color, string register = "eax", bool preserveAlpha = true,
        bool bgra = false, bool onlyIfLowByteNonZero = false)
    {
        if (register is not ("eax" or "edi" or "r9d"))
            throw new ArgumentException("Unsupported packed color register", nameof(register));

        // These allocations must live as long as the installed assembly hooks.
        // Like the hooks themselves, they are retained for the game process lifetime.
        // Separate native storage also lets config updates change a single aligned
        // word rather than modifying executable instructions while they are running.
        var storage = _context._memory.Allocate(16).Address;
        _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, storage,
            address => _context._memory.Write(address,
                bgra ? (preserveAlpha ? color().ToU32ARGB() & 0x00FFFFFFu : color().ToU32ARGB())
                     : (preserveAlpha ? color().ToU32IgnoreAlpha() : color().ToU32()))));

        return NativeColorCode.Build((ulong)storage, register, preserveAlpha, bgra, onlyIfLowByteNonZero);
    }
}
