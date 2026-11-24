using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.Pointers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace p3rpc.femc.Components
{
    public class KeyHelp : DirectColorModule
    {
        private UICommon _uiCommon;

        private string FKeyHelpButtonAuto_DrawTextFillColor_SIG = "83 F8 01 77 ?? B2 24";
        private string FKeyHelpButtonAuto_DrawTextFillColorTransOut_SIG = "E8 ?? ?? ?? ?? 48 8B 4C 24 ?? 45 33 C0 33 D2 E8 ?? ?? ?? ?? B9 01 00 00 00";
        private string FKeyHelpButtonAuto_DrawTextTriangleColor_SIG = "48 8B 54 24 ?? 48 8D 4C 24 ?? 0F 57 DB 89 44 24 ??";

        // this signature sucks, usually sigs for in function asm hooks are way shorter
        private string FKeyHelpButtonFastForward_TriangleColor1_SIG = "41 0F 28 D9 89 45 ?? 41 0F 28 D1 48 8D 4C 24 ?? 48 8B D6 E8 ?? ?? ?? ?? F3 0F 10 87 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? F3 0F 10 8F ?? ?? ?? ?? F3 0F 11 44 24 ?? 0F 28 05 ?? ?? ?? ?? 0F 29 44 24 ?? F3 0F 11 4C 24 ?? C7 44 24 ?? 00 00 00 00 C7 45 ?? 00 00 00 00 E8 ?? ?? ?? ?? F3 0F 59 05 ?? ?? ?? ?? F3 0F 59 05 ?? ?? ?? ??";
        private string FKeyHelpButtonFastForward_TriangleColor2_SIG = "41 0F 28 D9 89 45 ?? 41 0F 28 D1 48 8D 4C 24 ?? 48 8B D6 E8 ?? ?? ?? ?? 33 D2";
        private string FKeyHelpButtonFastForward_TriangleColor3_SIG = "41 0F 28 D9 89 45 ?? 41 0F 28 D1 48 8D 4C 24 ?? 48 8B D6 E8 ?? ?? ?? ?? F3 0F 10 87 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? F3 0F 10 8F ?? ?? ?? ?? F3 0F 11 44 24 ?? 0F 28 05 ?? ?? ?? ?? 0F 29 44 24 ?? F3 0F 11 4C 24 ?? C7 44 24 ?? 00 00 00 00 C7 45 ?? 00 00 00 00 E8 ?? ?? ?? ?? F3 0F 59 05 ?? ?? ?? ?? F3 41 0F 59 C0";

        private string FKeyHelpButtonMovie_UpdateState_SIG = "40 55 56 41 55 48 8D 6C 24 ?? 48 81 EC 20 01 00 00";

        private IAsmHook _autoDrawTextFillColor;
        private IAsmHook _autoDrawTextTriangleColor;
        private IAsmHook _autoDrawTextFillTransOutColor;

        // Recolor the native non-white palette branch, before native code builds
        // sprite/text colors. No managed callback touches a live prompt object.
        private const string PromptPalette_SIG = "44 0F B6 B3 55 02 00 00 0F B6 F0 44 0F B6 BB 56 02 00 00";

        public unsafe KeyHelp(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(FKeyHelpButtonAuto_DrawTextFillColor_SIG, "FKeyHelpButtonAuto::DrawTextFillColor", _context._utils.GetDirectAddress, addr =>
            {
                // Keep the native cmp/ja/mov sequence intact. Both branches converge on
                // this call; hook exactly its five bytes so a short branch can never
                // land inside a larger replacement and the original call is relocated
                // by Reloaded rather than conditionally recreated by us.
                var callAddress = addr + 7;
                if (!new ReadOnlySpan<byte>((void*)addr, 8).SequenceEqual(
                    new byte[] { 0x83, 0xF8, 0x01, 0x77, 0x02, 0xB2, 0x24, 0xE8 }))
                    return;
                string[] function =
                {
                    "use64",
                    "cmp eax, 0x1",
                    "ja hookEnd",
                    $"mov r9b, ${_context._config.ButtonPromptHighlightColor.B:X}",
                    $"mov r8b, ${_context._config.ButtonPromptHighlightColor.G:X}",
                    $"mov dl, ${_context._config.ButtonPromptHighlightColor.R:X}",
                    "label hookEnd"
                };
                _autoDrawTextFillColor = _context._hooks.CreateAsmHook(function, callAddress,
                    new AsmHookOptions
                    {
                        Behaviour = AsmHookBehaviour.ExecuteFirst,
                        PreferRelativeJump = true,
                        MaxOpcodeSize = 5,
                        hookLength = 5
                    }).Activate();
            });
            _context._utils.SigScan(FKeyHelpButtonAuto_DrawTextTriangleColor_SIG, "FKeyHelpButtonAuto::DrawTextTriangleColor", _context._utils.GetDirectAddress, addr =>
            {
                var function = ColorInRegister(() => _context._config.ButtonPromptTriangleColor);
                _autoDrawTextTriangleColor = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(FKeyHelpButtonAuto_DrawTextFillColorTransOut_SIG, "FKeyHelpButtonAuto::DrawTextFillColorTransOut", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"mov r9b, ${_context._config.ButtonPromptHighlightColor.B:X}",
                    $"mov r8b, ${_context._config.ButtonPromptHighlightColor.G:X}",
                    $"mov dl, ${_context._config.ButtonPromptHighlightColor.R:X}",
                };
                _autoDrawTextFillTransOutColor = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });

            _context._utils.SigScan(FKeyHelpButtonFastForward_TriangleColor1_SIG, "FastForward::Triangle1", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "FastForward::Triangle1", () => _context._config.ButtonPromptTriangleColor,
                    (-36, "33C9", 0), (-38, "33D2", 1), (-41, "41B0DE", 2)));
            _context._utils.SigScan(FKeyHelpButtonFastForward_TriangleColor2_SIG, "FastForward::Triangle2", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "FastForward::Triangle2", () => _context._config.ButtonPromptTriangleColor,
                    (-20, "33C9", 0), (-22, "33D2", 1), (-25, "41B0DE", 2)));
            _context._utils.SigScan(FKeyHelpButtonFastForward_TriangleColor3_SIG, "FastForward::Triangle3", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "FastForward::Triangle3", () => _context._config.ButtonPromptTriangleColor,
                    (-33, "33C9", 0), (-35, "33D2", 1), (-38, "41B0DE", 2)));
            _context._utils.SigScan(PromptPalette_SIG, "KeyHelp::NativeHighlightPalette", _context._utils.GetDirectAddress, PatchPromptPalette);
            _context._utils.SigScan(FKeyHelpButtonMovie_UpdateState_SIG, "Movie::Colors", _context._utils.GetDirectAddress, addr =>
            {
                PatchColorInputs(addr, "Movie::MainColor", () => _context._config.ButtonPromptHighlightColor,
                    (0x89D, "B11C", 0), (0x89B, "B24A", 1), (0x898, "41B0F9", 2));
                PatchColorInputs(addr, "Movie::PulseColor", () => _context._config.ButtonPromptHighlightColor,
                    (0xADA, "B11C", 0), (0xABD, "B24A", 1), (0xABA, "41B0F9", 2));
            });
        }

        private unsafe void PatchPromptPalette(long address)
        {
            var expected = Convert.FromHexString("440FB6B3550200000FB6F0440FB6BB56020000");
            if (!new ReadOnlySpan<byte>((void*)address, expected.Length).SequenceEqual(expected)) return;
            // Only the low bytes are consumed by the native color constructors.
            // Keep the preceding white/disabled-color branches and all alpha math.
            PatchPaletteByte(address, 8, 0x41, 0xB6, () => _context._config.ButtonPromptHighlightColor.G); // r14b
            PatchPaletteByte(address + 8, 3, 0x40, 0xB6, () => _context._config.ButtonPromptHighlightColor.R); // sil
            PatchPaletteByte(address + 11, 8, 0x41, 0xB7, () => _context._config.ButtonPromptHighlightColor.B); // r15b
        }

        private void PatchPaletteByte(long address, int length, byte rex, byte opcode, Func<byte> value)
        {
            var replacement = Enumerable.Repeat((byte)0x90, length).ToArray();
            replacement[0] = rex;
            replacement[1] = opcode;
            replacement[2] = value();
            _ = new AddressToMemoryWrite(_context._memory, (nuint)address, destination =>
            {
                for (int i = 0; i < replacement.Length; i++)
                    _context._memory.Write(destination + (nuint)i, replacement[i]);
            });
            _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, (nuint)(address + 2),
                destination => _context._memory.Write(destination, value())));
        }

        public override void Register()
        {
            _uiCommon = GetModule<UICommon>();
        }

    }
}
