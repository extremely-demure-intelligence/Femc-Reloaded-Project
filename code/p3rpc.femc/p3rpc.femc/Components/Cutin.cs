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

namespace p3rpc.femc.Components
{   
    public class Cutin : NativeColorModule
    {
        private string ACutinDraw_DrawPortraitBustupShadowColor_SIG = "44 8B C8 41 B8 02 00 00 00";
        private string ACutinDraw_DrawOuterTriangleHiglhightColor_SIG = "0F 57 DB 89 87 ?? ?? ?? ?? 0F 57 D2 C6 87 ?? ?? ?? ?? 01";
        private string ACutinDraw_DrawEmotion_SIG = "45 85 C0 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 48 89 7C 24 ?? 55";

        private IAsmHook _drawPortraitBustupShadowColor;

        private IAsmHook _drawOuterTriangleHiglhightColor;

        private IHook<ACutinDraw_CutinDrawEmotion> _drawEmotion;

        private UICommon _uiCommon;

        public unsafe Cutin(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(ACutinDraw_DrawPortraitBustupShadowColor_SIG, "ACutinDraw::DrawPortraitBustupShadowColor", _context._utils.GetDirectAddress, addr =>
            {
                var function = ColorInRegister(() => _context._config.CutinOuterHighlight);
                _drawPortraitBustupShadowColor = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(ACutinDraw_DrawOuterTriangleHiglhightColor_SIG, "ACutinDraw::DrawOuterTriangleHiglightColor", _context._utils.GetDirectAddress, addr =>
            {
                var function = ColorInRegister(() => _context._config.BustupShadowColor);
                _drawOuterTriangleHiglhightColor = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(ACutinDraw_DrawEmotion_SIG, "ACutinDraw::DrawEmotion", _context._utils.GetDirectAddress, addr => _drawEmotion = _context._utils.MakeHooker<ACutinDraw_CutinDrawEmotion>(ACutinDraw_CutinDrawEmotionImpl, addr));
        }

        public override void Register()
        {
            _uiCommon = GetModule<UICommon>();
        }

        public unsafe void ACutinDraw_CutinDrawEmotionImpl(ACutInDraw* self, nint masker, int emotion)
        {
            if (emotion == 1 || emotion == 3 || emotion == 6) // All emotion colors -> FF E1 41 B1 ?? 41 B0 F9
            {
                ConfigColor.SetColorIgnoreAlpha(ref self->EmotionGradientMain.ColorCornerTopLeft, _context._config.CutinEmotionGradient);
                ConfigColor.SetColorIgnoreAlpha(ref self->EmotionGradientMain.ColorCornerTopRight, _context._config.CutinEmotionGradient);
                ConfigColor.SetColorIgnoreAlpha(ref self->EmotionPlgTint.Color, _context._config.CutinEmotionTint);
                ConfigColor.SetColorIgnoreAlpha(ref self->EmotionHighlight.color, _context._config.CutinEmotionGradient);
            }
            _drawEmotion.OriginalFunction(self, masker, emotion);
        }

        public unsafe delegate void ACutinDraw_CutinDrawEmotion(ACutInDraw* self, nint masker, int emotion);
    }
}
