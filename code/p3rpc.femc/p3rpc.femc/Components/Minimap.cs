using p3rpc.commonmodutils;

namespace p3rpc.femc.Components
{
    // Edit native color setup instead of writing through a version-dependent object layout.
    public class Minimap : DirectColorModule
    {
        private const string UUIMinimapDraw_DrawMinimapFieldInnerCircle_SIG = "E8 ?? ?? ?? ?? 0F 28 DE 89 86 EC 03 00 00 0F 28 D7 48 8D 8E D0 03 00 00 48 8B D7 E8 ?? ?? ?? ?? BA 06 00 00 00 48 8B CF E8 ?? ?? ?? ?? 0F 28 DE 48 8D 8E D0 03 00 00 0F 28 D7 48 8B D7 E8 ?? ?? ?? ?? F3 44 0F 59 05 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? F3 45 0F 2C C8 E8 ?? ?? ?? ?? 44 0F 2F 2D";
        private const string UUIMinimapDraw_DrawMinimapFieldOutterCircle_SIG = "E8 ?? ?? ?? ?? 44 0F 2F 2D ?? ?? ?? ?? 48 8B 9C 24 D0 00 00 00 89 86 B8 03 00 00 44 0F 28 6C 24 40 73 ?? 89 6C 24 30 41 0F 28 DA C7 44 24 28 01 00 00 00 41 0F 28 D3 41 0F 28 CC F3 44 0F 11 4C 24 20 48 8B CF E8 ?? ?? ?? ?? BA 04 00 00 00 48 8B CF E8 ?? ?? ?? ?? 0F 57 DB 48 8D 8E 40 02 00";
        private const string UUIMinimapDraw_DrawMinimapLocationsHighStrip_SIG = "E8 ?? ?? ?? ?? F3 0F 10 15 ?? ?? ?? ?? ?? ?? ?? F3 0F 59 35 ?? ?? ?? ?? ?? ?? 89 83 EC 06 00 00 F2 0F 10 8B 48 0D 00 00 F3 0F 58 8B D0 02 00 00 F2 0F 10 83 98 0D 00 00 8B 83 50 0D 00 00 89 45 8F 8B 83 A0 0D 00 00 F3 0F 58 C8 89 45 8F F3 44 0F 2C CE F3 0F 11 8B B0 06 00 00 8B 83 D4 02 00";
        private const string UUIMinimapDraw_DrawMinimapLocationsLowerStrip_SIG = "E8 ?? ?? ?? ?? 44 0F 28 64 24 60 0F 28 BC 24 B0 00 00 00 0F 28 B4 24 C0 00 00 00 89 83 60 07 00 00 F2 0F 10 8B 48 0D 00 00 F3 0F 58 8B D0 02 00 00 F2 0F 10 83 E0 0D 00 00 F3 0F 58 C8 F3 0F 11 8B 24 07 00 00 8B 83 D4 02 00 00 89 83 28 07 00 00 C7 83 64 07 00 00 00 00 40 40 48 81 C4 F0 00";
        private const string UUIMinimapDraw_DrawMinimapLocationsHighlightedElement_SIG = "E8 ?? ?? ?? ?? 89 83 C8 02 00 00 0F 28 74 24 30 48 83 C4 40 5B C3 CC CC CC CC CC CC CC CC C7 81 04 03 00 00 00 00 00 00 33 D2 C6 81 14 01 00 00 01 48 81 C1 E0 00 00 00 48 8B 01 48 FF 60 08 CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 44 88 44 24 18 53 55 56 41 54 48 81 EC 28 01 00 00 41";
        private const string UUIMinimapDraw_DrawTownMapLocationsHighlightedElement_SIG = "E8 ?? ?? ?? ?? 89 83 C8 02 00 00 41 0F 28 C1 F3 0F 58 87 B8 3E 00 00 0F 2F 05 ?? ?? ?? ?? F3 0F 11 87 B8 3E 00 00 72 ?? FF 87 B4 3E 00 00 8B 87 B4 3E 00 00 83 F8 04 89 B7 B8 3E 00 00 0F 4D C6 89 87 B4 3E 00 00 48 8D 8F 90 03 00 00 41 0F 28 C9 E8 ?? ?? ?? ?? 48 8D 8F C0 03 00 00 41 0F 28";

        private const string AUIAccessInfoDraw_PlaceNameBackgroundColor_SIG = "E8 ?? ?? ?? ?? F2 0F 10 83 18 0B 00 00 48 8D 8B 00 0C 00 00 F2 0F 10 8B 78 0A 00 00 0F 28 D0 F3 0F 10 9B B8 02 00 00 F3 0F 5C 1D ?? ?? ?? ?? 89 83 8C 0F 00 00 8B 83 80 0A 00 00 89 44 24 28 8B 83 20 0B 00 00 F3 0F 58 D8 89 45 8F 0F 28 C1 0F C6 D2 55 F3 0F 58 93 C4 02 00 00 0F C6 C0 55 F3";

        public Minimap(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UUIMinimapDraw_DrawMinimapFieldInnerCircle_SIG, "UUIMinimapDraw_DrawMinimapFieldInnerCircle_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUIMinimapDraw_DrawMinimapFieldInnerCircle_SIG", () => _context._config.MinimapFieldInnerCircle,
                    (-7, "33C9", 0),
                    (-9, "B232", 1),
                    (-20, "41B0CD", 2)));
            _context._utils.SigScan(UUIMinimapDraw_DrawMinimapFieldOutterCircle_SIG, "UUIMinimapDraw_DrawMinimapFieldOutterCircle_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUIMinimapDraw_DrawMinimapFieldOutterCircle_SIG", () => _context._config.MinimapFieldOutterCircle,
                    (-7, "B115", 0),
                    (-9, "B217", 1),
                    (-12, "41B02C", 2)));
            _context._utils.SigScan(UUIMinimapDraw_DrawMinimapLocationsHighStrip_SIG, "UUIMinimapDraw_DrawMinimapLocationsHighStrip_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUIMinimapDraw_DrawMinimapLocationsHighStrip_SIG", () => _context._config.MinimapLocationsHighStrip,
                    (-100, "B115", 0),
                    (-116, "B217", 1),
                    (-127, "41B02C", 2)));
            _context._utils.SigScan(UUIMinimapDraw_DrawMinimapLocationsLowerStrip_SIG, "UUIMinimapDraw_DrawMinimapLocationsLowerStrip_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUIMinimapDraw_DrawMinimapLocationsLowerStrip_SIG", () => _context._config.MinimapLocationsLowerStrip,
                    (-2, "33C9", 0),
                    (-307, "B232", 1),
                    (-318, "41B0CD", 2)));
            _context._utils.SigScan(UUIMinimapDraw_DrawMinimapLocationsHighlightedElement_SIG, "UUIMinimapDraw_DrawMinimapLocationsHighlightedElement_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUIMinimapDraw_DrawMinimapLocationsHighlightedElement_SIG", () => _context._config.MinimapLocationsSelectionColor,
                    (-2, "B114", 0),
                    (-109, "B240", 1),
                    (-152, "41B0E1", 2)));
            _context._utils.SigScan(UUIMinimapDraw_DrawTownMapLocationsHighlightedElement_SIG, "UUIMinimapDraw_DrawTownMapLocationsHighlightedElement_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUIMinimapDraw_DrawTownMapLocationsHighlightedElement_SIG", () => _context._config.MinimapLocationsSelectionColor,
                    (-56, "B114", 0),
                    (-70, "B240", 1),
                    (-81, "41B0E1", 2)));
            // The location box renderer reads [object + 0xF68 + 0x24].
            // This constructor supplies that color; R9 keeps the animated alpha.
            _context._utils.SigScan(AUIAccessInfoDraw_PlaceNameBackgroundColor_SIG, "Current location background", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIAccessInfoDraw_PlaceNameBackgroundColor_SIG", () => _context._config.MinimapPlaceNameBgColor,
                    (-2, "B118", 0),
                    (-4, "B220", 1),
                    (-7, "41B069", 2)));
        }

        public override void Register() { }
    }
}
