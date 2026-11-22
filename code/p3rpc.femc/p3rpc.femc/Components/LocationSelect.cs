using p3rpc.commonmodutils;

namespace p3rpc.femc.Components
{
    // The old DrawMinimap callback wrote mapBg.Color through a stale object offset. The label accent stays native; the label background is patched below.
    public class LocationSelect : DirectColorModule
    {
        private const string UUILocationSelect_DrawLocationSelectBaseColor_SIG = "0F 57 DB 89 85 AC 00 00 00 0F 57 D2 48 8D 4D 70 49 8B D6 E8 ?? ?? ?? ?? BA 01 00 00 00 49 8B CE E8 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F 57 DB 89 85 AC 00 00 00 0F 57 D2 48 8D 4D 70 49 8B D6 E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 0F 57 C9 0F 29 85 80 02 00 00 41 0F 28";
        private const string UUILocationSelect_DrawLocationSelectTintColor_SIG = "0F 57 DB 89 85 AC 00 00 00 0F 57 D2 48 8D 4D 70 49 8B D6 E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 0F 57 C9 0F 29 85 80 02 00 00 41 0F 28 F4 F3 0F 5C 35 ?? ?? ?? ?? 41 0F 28 C3 48 89 B5 90 02 00 00 F3 0F 58 05 ?? ?? ?? ?? 33 D2 66 0F 7F 8D B0 02 00 00 49 8B CE 89 B5 68 02 00 00 F3 0F 11 B5 64";
        private const string UUILocationSelect_DrawLocationSelectMarkerSprite_SIG = "4C 8B 47 48 48 8D 8D 60 02 00 00 0F 57 DB F3 44 0F 11 54 24 20 49 8B D6 89 85 78 02 00 00 E8 ?? ?? ?? ?? 33 D2 49 8B CE E8 ?? ?? ?? ?? 0F 57 C9 0F 28 05 ?? ?? ?? ?? 0F 57 FF 48 8B 8F 20 03 00 00 0F 29 85 10 01 00 00 41 0F 28 C3 F3 0F 58 05 ?? ?? ?? ?? F3 0F 11 B5 F4 00 00 00 0F 57 F6 48";
        private const string FShortcutItem_SelectedShortcutColor_SIG = "48 8D 53 08 89 7B 3C 48 8D 4B 48 E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 48 8D 4B 30 F3 0F 10 5D 77 0F 57 F6 F3 0F 10 55 6F 49 8B D6 0F 11 83 90 00 00 00 C7 44 24 38 FF FF FF FF 0F 28 0D ?? ?? ?? ?? 0F 11 8B A0 00 00 00 C6 44 24 30 01 0F 28 05 ?? ?? ?? ?? 0F 11 83 B0 00 00 00 F3 0F 10 45 7F";
        private const string AUIAccessInfoDraw_DrawMinimapLabel_SIG = "BA 04 00 00 00 89 44 24 64 48 8B CF E8 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ?? 48 8D 4D C0 0F 28 0D ?? ?? ?? ?? 0F 57 DB F3 0F 11 45 CC 0F 57 D2 0F 28 05 ?? ?? ?? ?? 48 8B D7 0F 29 45 E0 F3 0F 10 05 ?? ?? ?? ?? 0F 29 4D D0 F3 0F 10 0D ?? ?? ?? ?? F3 0F 11 45 F4 F3 0F 11 4D F0 C7 45 04 00 00";

        public LocationSelect(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UUILocationSelect_DrawLocationSelectBaseColor_SIG, "UUILocationSelect_DrawLocationSelectBaseColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUILocationSelect_DrawLocationSelectBaseColor_SIG", () => new ConfigColor(255, 255, 255, 0x66),
                    (-7, "B10A", 0),
                    (-9, "B229", 1),
                    (-12, "41B079", 2),
                    (-15, "41B166", 3)));
            _context._utils.SigScan(UUILocationSelect_DrawLocationSelectTintColor_SIG, "UUILocationSelect_DrawLocationSelectTintColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUILocationSelect_DrawLocationSelectTintColor_SIG", () => _context._config.LocationSelectBgColor,
                    (-7, "B107", 0),
                    (-9, "B239", 1),
                    (-12, "41B0D0", 2),
                    (-15, "41B1FF", 3)));
            _context._utils.SigScan(UUILocationSelect_DrawLocationSelectMarkerSprite_SIG, "UUILocationSelect_DrawLocationSelectMarkerSprite_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "UUILocationSelect_DrawLocationSelectMarkerSprite_SIG", () => _context._config.LocationSelectMarkerColor,
                    (-7, "B118", 0),
                    (-13, "33D2", 1),
                    (-11, "450FB6C1", 2),
                    (-16, "41B1FF", 3)));
            _context._utils.SigScan(FShortcutItem_SelectedShortcutColor_SIG, "FShortcutItem_SelectedShortcutColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "FShortcutItem_SelectedShortcutColor_SIG", () => _context._config.LocationSelectSelColor,
                    (-9, "B118", 0),
                    (-15, "33D2", 1),
                    (-13, "450FB6C1", 2)));
            _context._utils.SigScan(AUIAccessInfoDraw_DrawMinimapLabel_SIG, "AUIAccessInfoDraw_DrawMinimapLabel_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUIAccessInfoDraw_DrawMinimapLabel_SIG", () => _context._config.LocationSelMapBg,
                    (-12, "B12E", 0),
                    (-14, "B231", 1),
                    (-17, "41B050", 2)));
        }

        public override void Register() { }
    }
}
