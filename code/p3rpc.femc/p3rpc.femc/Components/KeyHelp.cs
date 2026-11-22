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
    public class KeyHelp : NativeColorModule
    {
        private UICommon _uiCommon;

        private string FKeyHelpButtonAuto_DrawTextFillColor_SIG = "83 F8 01 77 ?? B2 24";
        private string FKeyHelpButtonAuto_DrawTextFillColorTransOut_SIG = "E8 ?? ?? ?? ?? 48 8B 4C 24 ?? 45 33 C0 33 D2 E8 ?? ?? ?? ?? B9 01 00 00 00";
        private string FKeyHelpButtonAuto_DrawTextTriangleColor_SIG = "48 8B 54 24 ?? 48 8D 4C 24 ?? 0F 57 DB 89 44 24 ??";

        private string FKeyHelpButtonFastForward_UpdateState_SIG = "48 8D 05 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? 48 89 07 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ??";
        // this signature sucks, usually sigs for in function asm hooks are way shorter
        private string FKeyHelpButtonFastForward_TriangleColor1_SIG = "41 0F 28 D9 89 45 ?? 41 0F 28 D1 48 8D 4C 24 ?? 48 8B D6 E8 ?? ?? ?? ?? F3 0F 10 87 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? F3 0F 10 8F ?? ?? ?? ?? F3 0F 11 44 24 ?? 0F 28 05 ?? ?? ?? ?? 0F 29 44 24 ?? F3 0F 11 4C 24 ?? C7 44 24 ?? 00 00 00 00 C7 45 ?? 00 00 00 00 E8 ?? ?? ?? ?? F3 0F 59 05 ?? ?? ?? ?? F3 0F 59 05 ?? ?? ?? ??";
        private string FKeyHelpButtonFastForward_TriangleColor2_SIG = "41 0F 28 D9 89 45 ?? 41 0F 28 D1 48 8D 4C 24 ?? 48 8B D6 E8 ?? ?? ?? ?? 33 D2";
        private string FKeyHelpButtonFastForward_TriangleColor3_SIG = "41 0F 28 D9 89 45 ?? 41 0F 28 D1 48 8D 4C 24 ?? 48 8B D6 E8 ?? ?? ?? ?? F3 0F 10 87 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? F3 0F 10 8F ?? ?? ?? ?? F3 0F 11 44 24 ?? 0F 28 05 ?? ?? ?? ?? 0F 29 44 24 ?? F3 0F 11 4C 24 ?? C7 44 24 ?? 00 00 00 00 C7 45 ?? 00 00 00 00 E8 ?? ?? ?? ?? F3 0F 59 05 ?? ?? ?? ?? F3 41 0F 59 C0";

        private string FKeyHelpButtonMovie_UpdateState_SIG = "40 55 56 41 55 48 8D 6C 24 ?? 48 81 EC 20 01 00 00";

        private IAsmHook _autoDrawTextFillColor;
        private IAsmHook _autoDrawTextTriangleColor;
        private IAsmHook _autoDrawTextFillTransOutColor;

        private IAsmHook _ffwdDrawTriangle1;
        private IAsmHook _ffwdDrawTriangle2;
        private IAsmHook _ffwdDrawTriangle3;

        private IHook<UpdatePromptState> _ffwdUpdateState;
        private IHook<UpdatePromptState> _movieUpdateState;
        private readonly object _promptHookLock = new();
        private bool _promptLayoutVerified;
        private long _fastForwardAddress;
        private long _movieAddress;

        // The current base campaign uses the expanded layout too. Do not select
        // the old native-types layout merely because bIsAigis is false.
        private const string PromptColorLayout_SIG = "48 8D 7B 50 0F 1F 00 F3 0F 10 83 4C 02 00 00 45 0F B6 C7 F3 0F 59 C6 41 0F B6 D6 40 0F B6 CE F3 44 0F 2C C8 E8 ?? ?? ?? ?? 89 07 48 8D 7F 68 FF C5 3B AB 60 02 00 00 7C CE";

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

            _context._utils.SigScan(FKeyHelpButtonFastForward_TriangleColor1_SIG, "FKeyHelpButtonFastForward::DrawTriangleColor1", _context._utils.GetDirectAddress, addr =>
            {
                var function = ColorInRegister(() => _context._config.ButtonPromptTriangleColor);
                _ffwdDrawTriangle1 = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(FKeyHelpButtonFastForward_TriangleColor2_SIG, "FKeyHelpButtonFastForward::DrawTriangleColor2", _context._utils.GetDirectAddress, addr =>
            {
                var function = ColorInRegister(() => _context._config.ButtonPromptTriangleColor);
                _ffwdDrawTriangle2 = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(FKeyHelpButtonFastForward_TriangleColor3_SIG, "FKeyHelpButtonFastForward::DrawTriangleColor3", _context._utils.GetDirectAddress, addr =>
            {
                var function = ColorInRegister(() => _context._config.ButtonPromptTriangleColor);
                _ffwdDrawTriangle3 = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(PromptColorLayout_SIG, "KeyHelp::VerifiedColorLayout", _context._utils.GetDirectAddress, addr =>
            {
                // Verify the text-color store as well as sprite start/count/stride.
                if (!new ReadOnlySpan<byte>((void*)(addr + 0x75), 6).SequenceEqual(
                    new byte[] { 0x89, 0x83, 0xF0, 0x01, 0x00, 0x00 }))
                    return;
                lock (_promptHookLock)
                {
                    _promptLayoutVerified = true;
                    InstallPromptHooks();
                }
            });
            _context._utils.SigScan(FKeyHelpButtonFastForward_UpdateState_SIG, "FKeyHelpButtonFastForward::UpdateState",
                offset => *((nuint*)_context._utils.GetIndirectAddressLong(offset) + 1), address =>
                {
                    lock (_promptHookLock)
                    {
                        _fastForwardAddress = address;
                        InstallPromptHooks();
                    }
                });
            _context._utils.SigScan(FKeyHelpButtonMovie_UpdateState_SIG, "FKeyHelpButtonMovie::UpdateState", _context._utils.GetDirectAddress, address =>
            {
                lock (_promptHookLock)
                {
                    _movieAddress = address;
                    InstallPromptHooks();
                }
            });
        }

        // Scans can finish in any order; install only after the native layout and
        // each function-specific field access have both been verified.
        private unsafe void InstallPromptHooks()
        {
            if (!_promptLayoutVerified) return;
            if (_fastForwardAddress != 0 && _ffwdUpdateState == null &&
                new ReadOnlySpan<byte>((void*)(_fastForwardAddress + 0x123), 6).SequenceEqual(
                    new byte[] { 0x8B, 0x8F, 0x90, 0x05, 0x00, 0x00 }))
                _ffwdUpdateState = _context._utils.MakeHooker<UpdatePromptState>(FastForwardUpdateState, _fastForwardAddress);
            if (_movieAddress != 0 && _movieUpdateState == null &&
                new ReadOnlySpan<byte>((void*)(_movieAddress + 0x8CD), 6).SequenceEqual(
                    new byte[] { 0x89, 0x86, 0x1C, 0x04, 0x00, 0x00 }) &&
                new ReadOnlySpan<byte>((void*)(_movieAddress + 0xAE4), 6).SequenceEqual(
                    new byte[] { 0x89, 0x86, 0xDC, 0x04, 0x00, 0x00 }))
                _movieUpdateState = _context._utils.MakeHooker<UpdatePromptState>(MovieUpdateState, _movieAddress);
        }

        public override void Register()
        {
            _uiCommon = GetModule<UICommon>();
        }

        private void FastForwardUpdateState(nint self, float deltaTime, nint position, float opacity)
        {
            _ffwdUpdateState.OriginalFunction(self, deltaTime, position, opacity);
            KeyHelpColors.ApplyFastForward(self, _context._config.ButtonPromptHighlightColor.ToU32IgnoreAlpha());
        }

        private void MovieUpdateState(nint self, float deltaTime, nint position, float opacity)
        {
            _movieUpdateState.OriginalFunction(self, deltaTime, position, opacity);
            KeyHelpColors.ApplyMovie(self, _context._config.ButtonPromptHighlightColor.ToU32IgnoreAlpha());
        }

        private delegate void UpdatePromptState(nint self, float deltaTime, nint position, float opacity);
    }
}
