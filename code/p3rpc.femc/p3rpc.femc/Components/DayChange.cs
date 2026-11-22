using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace p3rpc.femc.Components
{
    public class DayChange : DirectColorModule
    {
        private UICommon _uiCommon;
        private string AUIDayChange_UpdateParams_SIG = "48 8B C4 55 53 56 57 41 54 41 55 48 8D 6C 24 ??";
        private const string AUIDayChange_CurrentDayMoonShadowColor_SIG = "F3 0F 10 03 F3 41 0F 58 FE";
        // AUIDayChange::DrawDaysUntilFullMoon
        private const string AUIDayChange_DrawDaysUntilFullMoon1_SIG = "F3 0F 10 05 ?? ?? ?? ?? 45 33 ED F3 0F 10 0D ?? ?? ?? ?? 48 8B CB";
        private const string AUIDayChange_DrawDaysUntilFullMoon2_SIG = "F3 0F 10 0D ?? ?? ?? ?? F3 44 0F 10 1D ?? ?? ?? ?? F3 0F 11 4D ??";
        private const string AUIDayChange_DrawDaysUntilFullMoon3_SIG = "F3 0F 10 05 ?? ?? ?? ?? 48 8B CB F3 0F 10 1D ?? ?? ?? ?? F3 0F 10 15 ?? ?? ?? ?? F3 0F 10 0D ?? ?? ?? ??";
        // In AUIDayChange::DrawRippleEffect
        private const string AUIDayChange_DrawNextDayRipple1_SIG = "0F 28 05 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B CF 89 44 24 ??";
        private const string AUIDayChange_DrawNextDayRipple2_SIG = "0F 28 05 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B CF 89 45 ?? 0F 11 45 ?? C7 45 ?? 00 00 00 00 C7 45 ?? 00 00 00 00 C7 45 ?? 00 00 00 40 E8 ?? ?? ?? ?? F3 0F 10 40 ??";

        private const string AUIDayChange_DrawNextDayBackground_SIG = "E8 ?? ?? ?? ?? 33 FF 89 86 ?? ?? ?? ?? 48 8D 8E ?? ?? ?? ??";

        private IHook<AUIDayChange_UpdateParams> _updateParams;


        public unsafe DayChange(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(AUIDayChange_UpdateParams_SIG, "AUIDayChange::UpdateParams", _context._utils.GetDirectAddress, addr => _updateParams = _context._utils.MakeHooker<AUIDayChange_UpdateParams>(AUIDayChange_UpdateParamsImpl, addr));
            _context._utils.SigScan(AUIDayChange_CurrentDayMoonShadowColor_SIG, "AUIDayChange_CurrentDayMoonShadowColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_CurrentDayMoonShadowColor_SIG", () => _context._config.NextDayMoonShadowColor,
                    (-117, "33C9", 0),
                    (-133, "B23E", 1),
                    (-136, "41B07C", 2)));
            _context._utils.SigScan(AUIDayChange_DrawDaysUntilFullMoon1_SIG, "AUIDayChange_DrawDaysUntilFullMoon1_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_DrawDaysUntilFullMoon1_SIG", () => _context._config.NextDayTextColor,
                    (-81, "B136", 0),
                    (-96, "B264", 1),
                    (-99, "41B0AD", 2)));
            _context._utils.SigScan(AUIDayChange_DrawDaysUntilFullMoon2_SIG, "AUIDayChange_DrawDaysUntilFullMoon2_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_DrawDaysUntilFullMoon2_SIG", () => _context._config.NextDayTextColor,
                    (-10, "B136", 0),
                    (-74, "B264", 1),
                    (-91, "41B0AD", 2)));
            _context._utils.SigScan(AUIDayChange_DrawDaysUntilFullMoon3_SIG, "AUIDayChange_DrawDaysUntilFullMoon3_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_DrawDaysUntilFullMoon3_SIG", () => _context._config.NextDayTextColor,
                    (-54, "B136", 0),
                    (-59, "B264", 1),
                    (-70, "41B0AD", 2)));
            // Preserve the native instruction stream at the reported ripple crash site.
            _context._utils.SigScan(AUIDayChange_DrawNextDayRipple1_SIG, "AUIDayChange::DrawNextDayRipple1", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_DrawNextDayRipple1_SIG", () => _context._config.NextDayRipple,
                    (-33, "B148", 0),
                    (-35, "B28E", 1),
                    (-38, "41B0F0", 2)));
            // Preserve the native instruction stream at the reported ripple crash site.
            _context._utils.SigScan(AUIDayChange_DrawNextDayRipple2_SIG, "AUIDayChange::DrawNextDayRipple2", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_DrawNextDayRipple2_SIG", () => _context._config.NextDayRipple,
                    (-25, "B148", 0),
                    (-27, "B28E", 1),
                    (-30, "41B0F0", 2)));

            _context._utils.SigScan(AUIDayChange_DrawNextDayBackground_SIG, "AUIDayChange_DrawNextDayBackground_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIDayChange_DrawNextDayBackground_SIG", () => _context._config.DayChangeBGColor,
                    (-13, "0FB6CA", 0),
                    (-35, "B21A", 1),
                    (-16, "41B028", 2)));
        }
        public override void Register()
        {
            _uiCommon = GetModule<UICommon>();
        }

        public unsafe void AUIDayChange_UpdateParamsImpl(AUIDayChange* self, float deltaTime)
        {
            _updateParams.OriginalFunction(self, deltaTime);
            ConfigColor.SetColor(ref self->BandColorPrevDay, _context._config.NextDayBandColor);
            ConfigColor.SetColor(ref self->BandColorNextDay, _context._config.NextDayBandColor);
            ConfigColor.SetColor(ref self->LimitTextColor, _context._config.NextDayTextColor);
        }

        public unsafe delegate void AUIDayChange_UpdateParams(AUIDayChange* self, float deltaTime);
    }

    public class TimeChange : DirectColorModule
    {
        private UICommon _uiCommon;
        private string AUITimeChange_UpdateParams_SIG = "48 8B C4 48 89 58 ?? 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57 48 8D 68 ?? 48 81 EC E0 00 00 00 0F 29 70 ??";
        private const string AUITimeChange_TimeOfDayOverlapColor_SIG = "89 86 ?? ?? ?? ?? 48 8D 8E ?? ?? ?? ?? 4C 8B 46 ??";

        private IHook<AUITimeChange_UpdateParams> _updateParams;

        public unsafe TimeChange(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(AUITimeChange_UpdateParams_SIG, "AUITimeChange::UpdateParams", _context._utils.GetDirectAddress, addr => _updateParams = _context._utils.MakeHooker<AUITimeChange_UpdateParams>(AUITimeChange_UpdateParamsImpl, addr));
            _context._utils.SigScan(AUITimeChange_TimeOfDayOverlapColor_SIG, "AUITimeChange_TimeOfDayOverlapColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITimeChange_TimeOfDayOverlapColor_SIG", () => _context._config.TimeSkipColor,
                    (-7, "B11D", 0),
                    (-9, "B248", 1),
                    (-12, "41B0F7", 2),
                    (-15, "41B1FF", 3)));
        }
        public override void Register()
        {
            _uiCommon = GetModule<UICommon>();
        }

        public unsafe void AUITimeChange_UpdateParamsImpl(AUITimeChange* self, float deltaTime)
        {
            _updateParams.OriginalFunction(self, deltaTime);
            ConfigColor.SetColor(ref self->MainBarColor, _context._config.TimeSkipColor);
            ConfigColor.SetColor(ref self->TopBarColor, _context._config.TimeSkipColor);
        }

        public unsafe delegate void AUITimeChange_UpdateParams(AUITimeChange* self, float deltaTime);
    }
}
