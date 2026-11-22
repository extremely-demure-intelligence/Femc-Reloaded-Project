using p3rpc.commonmodutils;

namespace p3rpc.femc.Components
{
    // The old marker callback wrote IconColor and MarkerOutline through object offsets. Keep those accents native until their constructor sites are verified.
    public class TownMap : DirectColorModule
    {
        private const string AUITownMapActor_TownMapTextColor_SIG = "48 8D 54 24 30 89 44 24 58 48 8D 8F 90 03 00 00 E8 ?? ?? ?? ?? F3 0F 10 05 ?? ?? ?? ?? 48 8D 4C 24 40 4C 8B 87 F8 02 00 00 49 8B D6 F3 0F 11 44 24 20 F3 0F 10 58 04 F3 0F 59 1D ?? ?? ?? ?? F3 0F 58 1D ?? ?? ?? ?? E8 ?? ?? ?? ?? 45 33 C0 49 8B D6 48 8B CF E8 ?? ?? ?? ?? 4C 8B 87 80 04 00";
        private const string AUITownMapActor_TownMapBorderColor_SIG = "48 8D 54 24 30 89 44 24 7C 48 8D 8F 90 03 00 00 89 45 FC C7 45 98 33 33 93 41 C7 45 18 33 33 93 41 E8 ?? ?? ?? ?? F3 0F 10 1D ?? ?? ?? ?? 48 8D 4D C0 49 8B D6 F3 0F 10 10 F3 0F 59 15 ?? ?? ?? ?? F3 0F 58 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 54 24 30 48 8D 8F 90 03 00 00 E8 ?? ?? ?? ?? F3";
        private const string AUITownMapActor_LocationDetailsTintColor_SIG = "41 B1 FF 89 85 CC 03 00 00 45 0F B6 C1 41 0F B6 D1 41 0F B6 C9 E8 ?? ?? ?? ?? 45 33 C9 89 85 2C 01 00 00 45 33 C0 C6 85 5C 01 00 00 01 33 D2 33 C9 E8 ?? ?? ?? ?? F3 0F 10 46 24 48 8D 54 24 50 F3 0F 10 4E 20 48 8D 8E 38 01 00 00 0F 14 C8 89 85 60 01 00 00 41 8B C4 F2 0F 11 8D 34 01 00 00";
        private const string AUITownMapActor_LocationDetailsTopLeft_SIG = "41 B1 FF 89 85 9C 02 00 00 ?? ?? ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? F3 0F 10 1D ?? ?? ?? ?? 48 8D 8D 60 02 00 00 F3 0F 10 15 ?? ?? ?? ?? 49 8B D6 89 85 4C 03 00 00 E8 ?? ?? ?? ?? F3 0F 10 1D ?? ?? ?? ?? 48 8D 8D 10 03 00 00 F3 0F 10 15 ?? ?? ?? ?? 49 8B D6 E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ??";
        private const string AUITownMapActor_LocationDetailsLowerBand_SIG = "F3 0F 10 1D ?? ?? ?? ?? 48 8D 8D 60 02 00 00 F3 0F 10 15 ?? ?? ?? ?? 49 8B D6 89 85 4C 03 00 00 E8 ?? ?? ?? ?? F3 0F 10 1D ?? ?? ?? ?? 48 8D 8D 10 03 00 00 F3 0F 10 15 ?? ?? ?? ?? 49 8B D6 E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 0F 57 C9 41 B1 FF 0F 29 85 90 01 00 00 ?? ?? ?? 66 0F 7F 8D C0";
        private const string AUITownMapActor_LocationDetailsText_SIG = "48 8D 54 24 50 89 85 88 01 00 00 48 8D 8E 98 01 00 00 E8 ?? ?? ?? ?? 48 8B 8E 00 3D 00 00 F3 0F 10 35 ?? ?? ?? ?? F3 0F 10 00 F3 0F 59 05 ?? ?? ?? ?? F3 0F 11 85 70 01 00 00 48 85 C9 74 ?? 33 D2 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 44 0F 10 40 08 F3 0F 10 70 0C 4C 8B 46 70 48 8D 8D 70 01 00";
        private const string AUITownMap_LocationSubtleShadow_SIG = "E8 ?? ?? ?? ?? 41 B1 FF 89 83 C0 01 00 00 ?? ?? ?? C7 83 D4 00 00 00 1C 00 00 00 ?? ?? C7 83 74 02 00 00 1D 00 00 00 ?? ?? C7 83 14 02 00 00 00 00 CE C2 E8 ?? ?? ?? ?? ?? ?? 89 83 28 02 00 00 ?? ?? ?? C7 83 7C 02 00 00 00 00 CE C2 41 B1 FF ?? ?? ?? E8 ?? ?? ?? ?? 89 83 90 02 00 00 83 FF";
        private const string AUITownMap_LocationRoundedOutline_SIG = "E8 ?? ?? ?? ?? ?? ?? 89 83 28 02 00 00 ?? ?? ?? C7 83 7C 02 00 00 00 00 CE C2 41 B1 FF ?? ?? ?? E8 ?? ?? ?? ?? 89 83 90 02 00 00 83 FF 05 0F 87 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ?? 8B 8C BA EC AD 44 01 48 03 CA FF E1 C7 83 A4 01 00 00 0A 00 00 00 C7 83 DC 02 00 00 21 00 00 00 EB ?? C7 83 A4";
        private const string AUITownMap_LocationPreviewTaint_SIG = "E8 ?? ?? ?? ?? 89 83 90 02 00 00 83 FF 05 0F 87 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ?? 8B 8C BA EC AD 44 01 48 03 CA FF E1 C7 83 A4 01 00 00 0A 00 00 00 C7 83 DC 02 00 00 21 00 00 00 EB ?? C7 83 A4 01 00 00 07 00 00 00 C7 83 DC 02 00 00 1E 00 00 00 EB ?? C7 83 A4 01 00 00 0B 00 00 00 C7 83 DC";
        private const string AUITownMap_LocationMiniIndicator_SIG = "E8 ?? ?? ?? ?? 45 33 C9 89 87 B0 38 00 00 48 8D 8F 18 39 00 00 C6 44 24 20 00 45 33 C0 41 8D 51 03 E8 ?? ?? ?? ?? 41 8B F5 48 8D 9F 00 3B 00 00 66 66 0F 1F 84 00 00 00 00 00 45 33 C9 C6 44 24 20 00 48 8D 4B 30 41 8D 51 06 45 8D 41 03 E8 ?? ?? ?? ?? 45 33 C9 C6 44 24 20 00 48 8B CB 41 8D";
        private const string AUITownMap_LocationMiniIndicatorSubtleShadow_SIG = "E8 ?? ?? ?? ?? 41 B1 4C 89 87 48 38 00 00 ?? ?? ?? C7 87 FC 38 00 00 10 00 00 00 ?? ?? ?? ?? E8 ?? ?? ?? ?? 45 33 C9 89 87 B0 38 00 00 48 8D 8F 18 39 00 00 C6 44 24 20 00 45 33 C0 41 8D 51 03 E8 ?? ?? ?? ?? 41 8B F5 48 8D 9F 00 3B 00 00 66 66 0F 1F 84 00 00 00 00 00 45 33 C9 C6 44 24 20";
        private const string AUITownMap_LocationDetailsArrows_SIG = "E8 ?? ?? ?? ?? 48 8D 4E 70 89 44 24 68 E8 ?? ?? ?? ?? 8B 0E 0F 28 CF F3 0F 10 96 D8 00 00 00 F3 0F 5C C8 F3 0F 59 0D ?? ?? ?? ?? 85 C9 74 ?? 83 E9 01 74 ?? 83 E9 01 74 ?? 83 F9 01 75 ?? F2 0F 10 44 24 50 F3 0F 5C D1 0F C6 C0 E1 F3 0F 10 C2 0F C6 C0 E1 EB ?? F2 0F 10 44 24 50 F3 0F 5C CA";
        private const string AUITownMap_SocialLinksTitle_SIG = "E8 ?? ?? ?? ?? 48 8B 8E 88 3F 00 00 0F 57 F6 89 85 A8 02 00 00 45 0F 57 C0 48 85 C9 74 ?? 41 8D 54 24 01 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 0F 10 70 08 F3 44 0F 10 40 0C F3 44 0F 58 05 ?? ?? ?? ?? 4C 8B 86 F8 02 00 00 F3 0F 58 F7 48 8B D7 48 8D 8D 90 02 00 00 F3 44 0F 11 44 24 20 0F 28 DE";
        private const string AUITownMap_SocialLinkName_SIG = "E8 ?? ?? ?? ?? F2 0F 10 44 24 64 41 B0 FF 89 44 24 7C 45 0F B6 CE 8B 44 24 6C 41 0F B6 D0 F2 0F 11 85 60 01 00 00 41 0F B6 C8 89 85 68 01 00 00 0F 57 C0 33 C0 66 0F 7F 85 90 01 00 00 48 89 85 74 01 00 00 89 85 88 01 00 00 48 89 85 A0 01 00 00 48 C7 85 6C 01 00 00 00 00 80 3F 48 C7 85 40";
        private const string AUITownMap_SocialLinkCard_SIG = "E8 ?? ?? ?? ?? F2 0F 10 44 24 58 33 C9 0F 28 0D ?? ?? ?? ?? F2 0F 11 45 9C 0F 28 05 ?? ?? ?? ?? 0F 29 45 D0 8D 51 05 0F 28 05 ?? ?? ?? ?? 0F 29 4D E0 0F 28 0D ?? ?? ?? ?? 89 85 E8 00 00 00 8B 44 24 60 48 89 4D 88 48 89 4D 90 89 4D C0 48 89 4C 24 70 89 4C 24 78 48 89 4D B0 89 4D B8 48 8D";
        private const string AUITownMap_SocialLinkRankUpCard1_SIG = "E8 ?? ?? ?? ?? C7 85 DC 00 00 00 0A D7 63 3F C7 85 E0 00 00 00 0A D7 63 3F EB ?? 48 8B 86 C8 05 00 00 41 83 7C 04 04 0A 7C ?? 41 B0 FF 41 0F 28 C2 F3 41 0F 59 C6 41 0F B6 D0 41 0F B6 C8 F3 44 0F 2C C8 E8 ?? ?? ?? ?? 89 85 E8 00 00 00 4C 8B 86 F8 02 00 00 48 8D 8D D0 00 00 00 0F 28 DF F3";
        private const string AUITownMap_SocialLinkRankUpCard2_SIG = "E8 ?? ?? ?? ?? BA 02 00 00 00 89 85 38 02 00 00 48 8B CF C7 85 2C 02 00 00 0A D7 83 3F C7 85 30 02 00 00 0A D7 83 3F C7 85 34 02 00 00 00 00 D8 41 E8 ?? ?? ?? ?? 4C 8B 86 F8 02 00 00 48 8D 8D 20 02 00 00 0F 28 DF F3 0F 11 74 24 20 48 8B D7 E8 ?? ?? ?? ?? 33 D2 48 8B CF E8 ?? ?? ?? ?? 41";
        private const string AUITownMap_SocialLinkRankUpGlow_SIG = "E8 ?? ?? ?? ?? 48 8B 8E 88 3F 00 00 45 0F 28 CB F3 44 0F 10 05 ?? ?? ?? ?? 89 45 78 48 85 C9 74 ?? BA 04 00 00 00 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 44 0F 10 40 08 F3 44 0F 10 48 0C 4C 8B 86 F8 02 00 00 F3 44 0F 58 C7 F3 44 0F 58 CE 48 8B D7 48 8D 4D 60 41 0F 28 D8 F3 44 0F 11 4C 24 20 E8";
        private const string AUITownMap_SocialLinkEpisodeLogo_SIG = "E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 48 8D 4D 88 0F 28 0D ?? ?? ?? ?? BA 05 00 00 00 0F 29 45 D0 0F 28 05 ?? ?? ?? ?? 0F 29 4D E0 0F 28 0D ?? ?? ?? ?? 89 45 78 8B 44 24 60 0F 29 45 F0 0F 29 4D 00 4C 89 65 88 48 C7 45 90 00 00 00 00 F2 44 0F 11 5D 9C 89 45 A4 48 C7 45 A8 00 00 80 3F 44 89 65 C0 48 C7 44 24 70 00 00 00 00 C7 44 24 78 00 00 00 00 C7 44 24 7C FF FF FF FF C7 45 80 00 00 80 3F 48 C7 45 B0 00 00 00 00 C7 45 B8 00 00 00 00 E8 ?? ?? ?? ?? 8B 55 90 8D 42 05 89 45 90 3B 45 94 7E ?? 48 8D 4D 88 E8 ?? ?? ?? ?? 48 8B 4D 88 F2 0F 10 05 ?? ?? ?? ?? F2 0F 11 01 0F B7 05 ?? ?? ?? ?? 66 89 41 08 48 8B 8E D8 05 00 00 44 89 65 98 C6 45 10 01 8B 44 0B 10 85 C0 44 8D 70";
        private const string AUITownMap_SocialLinkEpisodeName_SIG = "E8 ?? ?? ?? ?? 4C 8B 86 F8 02 00 00 48 8D 4D 60 0F 28 DF F3 0F 11 74 24 20 49 8B D7 89 44 24 7C E8 ?? ?? ?? ?? 48 8B 8E 80 3F 00 00 45 0F 57 C0 45 0F 57 C9 48 85 C9 74 ?? BA 04 00 00 00 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 44 0F 10 40 08 F3 44 0F 10 48 0C C7 44 24 38 FF FF FF FF 48 8D 4C 24 70 41 0F 28 CC C6 44 24 30 01 0F 28 C7 F3 0F 58 CE F3 41 0F 58 C5 41 0F 28 D9 41 0F 28 D0 49 8B D7 F3 0F 11 4C 24 28 F3 0F 11 44 24 20 E8 ?? ?? ?? ?? 41 8D 45 01 83 F8 04 7C ?? 41 0F 28 F6 F3 41 0F 58 FF EB ?? 33 C9 8B D1 89 4D 90 8B 4D 94 41 3B CC 74 ?? 41 8B D4 48 8D 4D 88 E8 ?? ?? ?? ?? 8B 4D 94 8B 55 90 41 8D 46 01 03 C2 89 45 90 3B C1 7E ?? 48 8D 4D 88 E8 ?? ?? ?? ?? 48 8B 7D 88 48 8B 55 28 48 8B CF 49 63 C6 48 8D 1C 00 4C 8B C3 E8 ?? ?? ?? ?? 45 33 E4 66 44 89 24 1F E9 ?? ?? ?? ?? F3 0F 58 35 ?? ?? ?? ?? 48 8B 4D 88 45 8B EC 44 0F 4C E8 48 85 C9 74 ?? E8 ?? ?? ?? ?? FF C7 48 83 C3 18 89 7C 24 44 48 89 5C 24 48 3B BE E0 05 00 00 0F 8C ?? ?? ?? ?? 83 BE F0 05 00 00 00 41 8B FC 44 89 64 24 40 0F 8E ?? ?? ?? ?? 49 8B DC C7 44 24 64 00 00 70 44 48 C7 44 24";
        private const string AUITownMap_SocialLinkHangoutLogo_SIG = "E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 48 8D 4D 88 0F 28 0D ?? ?? ?? ?? BA 05 00 00 00 0F 29 45 D0 0F 28 05 ?? ?? ?? ?? 0F 29 4D E0 0F 28 0D ?? ?? ?? ?? 89 45 78 8B 44 24 60 0F 29 45 F0 0F 29 4D 00 4C 89 65 88 48 C7 45 90 00 00 00 00 F2 44 0F 11 5D 9C 89 45 A4 48 C7 45 A8 00 00 80 3F 44 89 65 C0 48 C7 44 24 70 00 00 00 00 C7 44 24 78 00 00 00 00 C7 44 24 7C FF FF FF FF C7 45 80 00 00 80 3F 48 C7 45 B0 00 00 00 00 C7 45 B8 00 00 00 00 E8 ?? ?? ?? ?? 8B 55 90 8D 42 05 89 45 90 3B 45 94 7E ?? 48 8D 4D 88 E8 ?? ?? ?? ?? 48 8B 4D 88 F2 0F 10 05 ?? ?? ?? ?? F2 0F 11 01 0F B7 05 ?? ?? ?? ?? 66 89 41 08 48 8B 8E E8 05 00 00 44 89 65 98 C6 45 10 01 8B 44 0B 10 85 C0 44 8D 70";
        private const string AUITownMap_SocialLinkHangoutName_SIG = "E8 ?? ?? ?? ?? 4C 8B 86 F8 02 00 00 48 8D 4D 60 0F 28 DF F3 0F 11 74 24 20 49 8B D7 89 44 24 7C E8 ?? ?? ?? ?? 48 8B 8E 80 3F 00 00 45 0F 57 C0 45 0F 57 C9 48 85 C9 74 ?? BA 04 00 00 00 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 44 0F 10 40 08 F3 44 0F 10 48 0C C7 44 24 38 FF FF FF FF 48 8D 4C 24 70 41 0F 28 CC C6 44 24 30 01 0F 28 C7 F3 0F 58 CE F3 41 0F 58 C5 41 0F 28 D9 41 0F 28 D0 49 8B D7 F3 0F 11 4C 24 28 F3 0F 11 44 24 20 E8 ?? ?? ?? ?? 41 8D 45 01 83 F8 04 7C ?? 41 0F 28 F6 F3 41 0F 58 FF EB ?? 33 C9 8B D1 89 4D 90 8B 4D 94 41 3B CC 74 ?? 41 8B D4 48 8D 4D 88 E8 ?? ?? ?? ?? 8B 4D 94 8B 55 90 41 8D 46 01 03 C2 89 45 90 3B C1 7E ?? 48 8D 4D 88 E8 ?? ?? ?? ?? 48 8B 7D 88 48 8B 55 28 48 8B CF 49 63 C6 48 8D 1C 00 4C 8B C3 E8 ?? ?? ?? ?? 45 33 E4 66 44 89 24 1F E9 ?? ?? ?? ?? F3 0F 58 35 ?? ?? ?? ?? 48 8B 4D 88 45 8B EC 44 0F 4C E8 48 85 C9 74 ?? E8 ?? ?? ?? ?? FF C7 48 83 C3 18 89 7C 24 40 48 89 5C 24 48 3B BE F0 05 00 00 0F 8C ?? ?? ?? ?? 83 BE 00 06 00 00 00 41 8B FC 44 89 64 24 40 0F 8E ?? ?? ?? ?? 49 8B DC C7 44 24 64 00 00 70 44 48 C7 44 24";
        private const string AUITownMap_SocialLinkKoroLogo_SIG = "E8 ?? ?? ?? ?? 0F 28 05 ?? ?? ?? ?? 48 8D 4D 88 0F 28 0D ?? ?? ?? ?? BA 05 00 00 00 0F 29 45 D0 0F 28 05 ?? ?? ?? ?? 0F 29 4D E0 0F 28 0D ?? ?? ?? ?? 89 45 78 8B 44 24 60 0F 29 45 F0 0F 29 4D 00 4C 89 65 88 48 C7 45 90 00 00 00 00 F2 44 0F 11 5D 9C 89 45 A4 48 C7 45 A8 00 00 80 3F 44 89 65 C0 48 C7 44 24 70 00 00 00 00 C7 44 24 78 00 00 00 00 C7 44 24 7C FF FF FF FF C7 45 80 00 00 80 3F 48 C7 45 B0 00 00 00 00 C7 45 B8 00 00 00 00 E8 ?? ?? ?? ?? 8B 55 90 8D 42 05 89 45 90 3B 45 94 7E ?? 48 8D 4D 88 E8 ?? ?? ?? ?? 48 8B 4D 88 F2 0F 10 05 ?? ?? ?? ?? F2 0F 11 01 0F B7 05 ?? ?? ?? ?? 66 89 41 08 48 8B 8E F8 05 00 00 44 89 65 98 C6 45 10 01 8B 44 0B 10 85 C0 44 8D 70";
        private const string AUITownMap_SocialLinkKoroName_SIG = "E8 ?? ?? ?? ?? 4C 8B 86 F8 02 00 00 48 8D 4D 60 0F 28 DF F3 0F 11 74 24 20 49 8B D7 89 44 24 7C E8 ?? ?? ?? ?? 48 8B 8E 80 3F 00 00 45 0F 57 C0 45 0F 57 C9 48 85 C9 74 ?? BA 04 00 00 00 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 44 0F 10 40 08 F3 44 0F 10 48 0C C7 44 24 38 FF FF FF FF 48 8D 4C 24 70 41 0F 28 CC C6 44 24 30 01 0F 28 C7 F3 0F 58 CE F3 41 0F 58 C5 41 0F 28 D9 41 0F 28 D0 49 8B D7 F3 0F 11 4C 24 28 F3 0F 11 44 24 20 E8 ?? ?? ?? ?? 41 8D 45 01 83 F8 04 7C ?? 41 0F 28 F6 F3 41 0F 58 FF EB ?? 33 C9 8B D1 89 4D 90 8B 4D 94 41 3B CC 74 ?? 41 8B D4 48 8D 4D 88 E8 ?? ?? ?? ?? 8B 4D 94 8B 55 90 8D 42 01 41 03 C6 89 45 90 3B C1 7E ?? 48 8D 4D 88 E8 ?? ?? ?? ?? 48 8B 7D";
        // Anchor on CALL: leading wildcards can stall Reloaded's AVX2 scanner.
        private const string AUITownMap_LocationDetailsGenericIconColor_SIG = "E8 ?? ?? ?? ?? 89 85 E8 00 00 00 48 63 44 24 48 49 63 8F 98 39 00 00 48 03 C9 48 6B D0 78 49 03 94 CF 68 05 00 00 49 8B CD E8 ?? ?? ?? ?? 41 B0 FF 41 0F B6 D0 41 0F B6 C8 84 C0 74 ?? 44 0F B6 CE EB ?? 49 8B 45 58 45 33 F6 48 6B CE 38 41 8B DE 48 89 5C 24 50 48 63 7C 01 20 48 8B 74";

        public TownMap(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(AUITownMapActor_TownMapTextColor_SIG, "AUITownMapActor_TownMapTextColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMapActor_TownMapTextColor_SIG", () => _context._config.TownMapTextColor,
                    (-80, "B116", 0),
                    (-91, "B220", 1),
                    (-103, "41B066", 2),
                    (-111, "41B1FF", 3)));
            _context._utils.SigScan(AUITownMapActor_TownMapBorderColor_SIG, "AUITownMapActor_TownMapBorderColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMapActor_TownMapBorderColor_SIG", () => _context._config.TownMapBorderColor,
                    (-287, "33C9", 0),
                    (-297, "B206", 1),
                    (-305, "41B0EC", 2),
                    (-314, "41B1FF", 3)));
            _context._utils.SigScan(AUITownMapActor_LocationDetailsTintColor_SIG, "AUITownMapActor_LocationDetailsTintColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMapActor_LocationDetailsTintColor_SIG", () => _context._config.TownMapLocationDetailsBgTint,
                    (-134, "33C9", 0),
                    (-139, "B204", 1),
                    (-150, "41B09A", 2)));
            _context._utils.SigScan(AUITownMapActor_LocationDetailsTopLeft_SIG, "AUITownMapActor_LocationDetailsTopLeft_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMapActor_LocationDetailsTopLeft_SIG", () => _context._config.TownMapLocationDetailsTopLeftText,
                    (-7, "33C9", 0),
                    (-9, "B223", 1),
                    (-12, "41B0A5", 2)));
            _context._utils.SigScan(AUITownMapActor_LocationDetailsLowerBand_SIG, "AUITownMapActor_LocationDetailsLowerBand_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMapActor_LocationDetailsLowerBand_SIG", () => _context._config.TownMapLocationDetailsTopLeftBg,
                    (-7, "33C9", 0),
                    (-9, "B232", 1),
                    (-12, "41B0EC", 2)));
            _context._utils.SigScan(AUITownMapActor_LocationDetailsText_SIG, "AUITownMapActor_LocationDetailsText_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMapActor_LocationDetailsText_SIG", () => _context._config.TownMapLocationDetailsTopLeftText,
                    (-103, "33C9", 0),
                    (-112, "B223", 1),
                    (-123, "41B0A5", 2)));
            _context._utils.SigScan(AUITownMap_LocationSubtleShadow_SIG, "AUITownMap_LocationSubtleShadow_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationSubtleShadow_SIG", () => _context._config.LocationSubtleShadowColor,
                    (-32, "B106", 0),
                    (-44, "33D2", 1),
                    (-54, "41B053", 2)));
            _context._utils.SigScan(AUITownMap_LocationRoundedOutline_SIG, "AUITownMap_LocationRoundedOutline_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationRoundedOutline_SIG", () => _context._config.PreviewRoundedOutline,
                    (-12, "33C9", 0),
                    (-24, "B2FB", 1),
                    (-37, "41B0FE", 2)));
            _context._utils.SigScan(AUITownMap_LocationPreviewTaint_SIG, "AUITownMap_LocationPreviewTaint_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationPreviewTaint_SIG", () => _context._config.PreviewTaintColor,
                    (-19, "0FB6CA", 0),
                    (-27, "B296", 1),
                    (-3, "41B0BB", 2)));
            _context._utils.SigScan(AUITownMap_LocationMiniIndicator_SIG, "AUITownMap_LocationMiniIndicator_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationMiniIndicator_SIG", () => _context._config.MiniLocationCircleColor,
                    (-2, "33C9", 0),
                    (-4, "B232", 1),
                    (-17, "41B0EC", 2)));
            _context._utils.SigScan(AUITownMap_LocationMiniIndicatorSubtleShadow_SIG, "AUITownMap_LocationMiniIndicatorSubtleShadow_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationMiniIndicatorSubtleShadow_SIG", () => _context._config.LocationSubtleShadowColor,
                    (-22, "B106", 0),
                    (-35, "33D2", 1),
                    (-49, "41B053", 2)));
            _context._utils.SigScan(AUITownMap_LocationDetailsArrows_SIG, "AUITownMap_LocationDetailsArrows_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationDetailsArrows_SIG", () => _context._config.TownMapHighlightedArrows,
                    (-126, "B1FF", 0),
                    (-135, "33D2", 1),
                    (-146, "4533C0", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinksTitle_SIG, "AUITownMap_SocialLinksTitle_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinksTitle_SIG", () => _context._config.TownMapNamesColor,
                    (-159, "B111", 0),
                    (-176, "410FB6D0", 1),
                    (-183, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkName_SIG, "AUITownMap_SocialLinkName_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkName_SIG", () => _context._config.TownMapNamesColor,
                    (-15, "B111", 0),
                    (-8, "410FB6D0", 1),
                    (-18, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkCard_SIG, "AUITownMap_SocialLinkCard_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkCard_SIG", () => _context._config.TownMapLogosColor,
                    (-134, "B111", 0),
                    (-157, "410FB6D0", 1),
                    (-175, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkRankUpCard1_SIG, "AUITownMap_SocialLinkRankUpCard1_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkRankUpCard1_SIG", () => _context._config.TownMapSLRankUpCard1,
                    (-2, "B111", 0),
                    (-4, "B2BF", 1),
                    (-7, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkRankUpCard2_SIG, "AUITownMap_SocialLinkRankUpCard2_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkRankUpCard2_SIG", () => _context._config.TownMapSLRankUpCard2,
                    (-94, "B111", 0),
                    (-115, "B265", 1),
                    (-126, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkRankUpGlow_SIG, "AUITownMap_SocialLinkRankUpGlow_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkRankUpGlow_SIG", () => _context._config.TownMapSLRankUpGlow,
                    (-113, "33C9", 0),
                    (-134, "B206", 1),
                    (-145, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkEpisodeLogo_SIG, "AUITownMap_SocialLinkEpisodeLogo_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkEpisodeLogo_SIG", () => _context._config.TownMapLogosColor,
                    (-116, "B111", 0),
                    (-141, "410FB6D0", 1),
                    (-156, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkEpisodeName_SIG, "AUITownMap_SocialLinkEpisodeName_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkEpisodeName_SIG", () => _context._config.TownMapNamesColor,
                    (-2, "B111", 0),
                    (-10, "410FB6D0", 1),
                    (-13, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkHangoutLogo_SIG, "AUITownMap_SocialLinkHangoutLogo_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkHangoutLogo_SIG", () => _context._config.TownMapLogosColor,
                    (-116, "B111", 0),
                    (-141, "410FB6D0", 1),
                    (-156, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkHangoutName_SIG, "AUITownMap_SocialLinkHangoutName_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkHangoutName_SIG", () => _context._config.TownMapNamesColor,
                    (-2, "B111", 0),
                    (-10, "410FB6D0", 1),
                    (-13, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkKoroLogo_SIG, "AUITownMap_SocialLinkKoroLogo_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkKoroLogo_SIG", () => _context._config.TownMapLogosColor,
                    (-116, "B111", 0),
                    (-141, "410FB6D0", 1),
                    (-156, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_SocialLinkKoroName_SIG, "AUITownMap_SocialLinkKoroName_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_SocialLinkKoroName_SIG", () => _context._config.TownMapNamesColor,
                    (-2, "B111", 0),
                    (-10, "410FB6D0", 1),
                    (-13, "41B0FF", 2)));
            _context._utils.SigScan(AUITownMap_LocationDetailsGenericIconColor_SIG, "AUITownMap_LocationDetailsGenericIconColor_SIG", _context._utils.GetDirectAddress, addr =>
                PatchColorInputs(addr, "AUITownMap_LocationDetailsGenericIconColor_SIG", () => _context.ColorWhite, (-2, "B160", 0)));
        }

        public override void Register() { }
    }
}
