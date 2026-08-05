using IForm.Web.Models;

namespace IForm.Web.Data;

public static class AccessoryCatalog
{
    private static Product P(string code, string name, string family, string material, string? spec = null)
        => new()
        {
            ProductCode = code,
            Name = name,
            Family = family,
            Material = material,
            Specification = spec,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public static IReadOnlyList<Product> Build() => new List<Product>
    {
        // ---- Ties ----
        P("DAAA", "SNAP TIE", "Tie", "Steel", "Wall thickness (mm)"),
        P("DABA", "2HOLE - REUSABLE TIE", "Tie", "Steel", "Wall thickness (mm)"),
        P("DACA", "3HOLE - REUSABLE TIE (W37)", "Tie", "Steel", "Wall thickness (mm)"),
        P("DAHA", "3HOLE - REUSABLE TIE (W33)", "Tie", "Steel", "Wall thickness (mm)"),
        P("DTGD", "RE-CONE TIE 1/2", "Tie", "Steel + PVC", "Wall thickness (mm)"),
        P("DADA", "T-TIE", "Tie", "Steel", "Wall thickness (mm)"),
        P("DAFA", "DOUBLE POUR TIE", "Tie", "Steel", "Wall thk - wall space distance"),
        P("DAGA", "AL - ROD TIE", "Tie", "Steel", "Wall thickness (mm)"),

        // ---- Tie rod & separator bolt ----
        P("DAGB", "TIE ROD (1/2)", "Tie Rod / Separator Bolt", "Steel", "Length"),
        P("DAGC", "TIE ROD (5/8)", "Tie Rod / Separator Bolt", "Steel", "Length"),
        P("DAIB", "SEPA BOLT (1/2)", "Tie Rod / Separator Bolt", "Steel", "Length"),
        P("DAIC", "SEPA BOLT (5/8)", "Tie Rod / Separator Bolt", "Steel", "Length"),

        // ---- Support ----
        P("DRVA0001", "SUPPORT (V1)", "Support", "Steel", "Min. - max. length"),
        P("DRVA0002", "SUPPORT (V2)", "Support", "Steel", "Min. - max. length"),
        P("DRWA0001", "SUPPORT (V3)", "Support", "Steel", "Min. - max. length"),
        P("DRWA0002", "SUPPORT (V4)", "Support", "Steel", "Min. - max. length"),
        P("DRTA0005", "PIPE HEAD ADAPTOR", "Support", "Steel", "Pipe dia."),

        // ---- D-Cone ----
        P("DBAA0000", "D-CONE [1/2] - 40MM", "D-Cone", "Steel + PVC", "[1/2] 40MM"),
        P("DBAA0060", "D-CONE [5/8] - 60MM", "D-Cone", "Steel + PVC", "[5/8] 60MM"),

        // ---- Pin ----
        P("DCAA0001", "PIN (KK-Type)", "Pin", "Steel", "KK"),
        P("DCAA0015", "PIN (ALFA-Type)", "Pin", "Steel", "ASIA"),
        P("DCAB0059", "PIN (AO-Type)", "Pin", "Steel", "A-ONE"),
        P("DCAC0059", "PIN (ALFU-Type)", "Pin", "Steel", "USA"),

        // ---- Long pin ----
        P("DCBA0064", "LONG PIN 64L", "Long Pin", "Steel", "HD - 100L"),
        P("DCBB0100", "LONG PIN 100L", "Long Pin", "Steel", "SM - 150L"),
        P("DCBB0150", "LONG PIN 150L", "Long Pin", "Steel", "KK - 152L"),
        P("DCBB0152", "LONG PIN 152L", "Long Pin", "Steel", "ALF - Pin"),
        P("DCBC0157", "LONG PIN 157L", "Long Pin", "Steel", "USA"),

        // ---- Wedge ----
        P("DCCA0001", "WEDGE (ALFA-Type)", "Wedge", "Steel", "ASIA"),
        P("DCCB0001", "WEDGE (AO-Type)", "Wedge", "Steel", "A-ONE"),
        P("DCCC0001", "Straight WEDGE (ALFU-Type)", "Wedge", "Steel", "USA"),
        P("DCCD0001", "5 DEGREE CURVED WEDGE (ALFU-Type)", "Wedge", "Steel", "USA"),
        P("DCCE0001", "CURVED WEDGE (ALFU-Type)", "Wedge", "Steel", "USA"),

        // ---- Waler & bracket ----
        P("DDAA0001", "Adjustable waler bracket (ALFA-Type)", "Waler / KL Bracket", "Steel", "2x4"),
        P("DDAA0003", "Adjustable waler bracket (ALFU-Type)", "Waler / KL Bracket", "Steel", "2x4"),
        P("DDBA0001", "STD. Waler (ALFU-Type)", "Waler / KL Bracket", "Steel", "Length (M)"),
        P("DRMA", "WALER BOARD 50x50x3.2t", "Waler / KL Bracket", "Steel", "50x50"),
        P("DDCA0001", "WALER BAND SET", "Waler / KL Bracket", "Steel", "Width x length"),
        P("DDCA0099", "KL BRACKET \"U\" TYPE - 99.2MM", "Waler / KL Bracket", "Steel", "U-99.2MM"),
        P("DDCB0099", "KL BRACKET \"Z\" TYPE - 99.2MM", "Waler / KL Bracket", "Steel", "Z-99.2MM"),
        P("DDCE0092", "KL BRACKET \"U\" TYPE - 92.5MM", "Waler / KL Bracket", "Steel", "U-92.5MM"),
        P("DDCF0092", "KL BRACKET \"Z\" TYPE - 92.5MM", "Waler / KL Bracket", "Steel", "Z-92.5MM"),
        P("DEAA0600", "STD. WALL BRACKET (DYVIDAG-Type)", "Waler / KL Bracket", "Steel", "1150X1000X600"),

        // ---- Wall & slab bracket ----
        P("DEAA0740", "WALL BRACKET (TIE-Type)", "Wall / Slab Bracket", "Steel", "1070X950X740"),
        P("DEBA1000", "SLAB BRACKET", "Wall / Slab Bracket", "Steel", "1150X1000"),
        P("DECA0245", "SPECIAL WALL BRACKET", "Wall / Slab Bracket", "Steel", "1150X1000X245"),

        // ---- Bracket bolt & kicker anchor ----
        P("DFAA", "BRACKET BOLT", "Bracket Bolt / Kicker Anchor", "Steel", "17Ø x Length"),
        P("DFAB1600", "KICKER ANCHOR NUT", "Bracket Bolt / Kicker Anchor", "Steel", "M16 x 2.0"),
        P("DFAB1601", "KICKER ANCHOR WASHER", "Bracket Bolt / Kicker Anchor", "Steel", "M16"),
        P("DFAB1610", "ANCHOR SLEEVE 100MM", "Bracket Bolt / Kicker Anchor", "PVC", "100MM"),
        P("DFAB1675", "KICKER ANCHOR BOLT", "Bracket Bolt / Kicker Anchor", "Steel", "M16x75L"),
        P("DFAC1610", "DYVIDAG KICKER ANCHOR BOLT", "Bracket Bolt / Kicker Anchor", "Steel", "100mm"),
        P("DFAC1611", "DYVIDAG KICKER ANCHOR AL-NUT", "Bracket Bolt / Kicker Anchor", "Aluminum", "M16x35"),
        P("DFAC1635", "PANEL JOIN - BOLT", "Bracket Bolt / Kicker Anchor", "Steel", "M16"),
        P("DFAC1636", "PANEL JOIN - NUT", "Bracket Bolt / Kicker Anchor", "Steel", "17Ø x Length"),
        P("DFAE", "DYVIDAG BOLT", "Bracket Bolt / Kicker Anchor", "Steel", "M16*35 - Length"),
        P("DFAF0150", "WALER FIXING BOLT (HEX Bolt-Type)", "Bracket Bolt / Kicker Anchor", "Steel", "Length"),
        P("DFAG0200", "WALER FIXING BOLT (Pin-Type) - 5/8", "Bracket Bolt / Kicker Anchor", "Steel", "Length"),
        P("DFAH2012", "WALER FIXING BOLT (Pin-Type) - 1/2", "Bracket Bolt / Kicker Anchor", "Steel", "Length"),
        P("DHAA0001", "WING NUT 1/2", "Bracket Bolt / Kicker Anchor", "Cast-iron", "1/2"),
        P("DHBA0001", "WING NUT 5/8", "Bracket Bolt / Kicker Anchor", "Cast-iron", "5/8"),

        // ---- Form clip ----
        P("DIAA0001", "FORM CLIP-LH (ALFA-Type)", "Form Clip", "Steel", "LH (Asia)"),
        P("DIAB0001", "FORM CLIP-RH (ALFA-Type)", "Form Clip", "Steel", "RH (Asia)"),
        P("DIBA0001", "FORM CLIP-LH (ALFU-Type)", "Form Clip", "Steel", "LH (USA)"),
        P("DIBB0001", "FORM CLIP-RH (ALFU-Type)", "Form Clip", "Steel", "RH (USA)"),

        // ---- Pin lock ----
        P("DJAC0001", "PIN LOCK PVC", "Pin Lock", "PVC", "Cylinder"),
        P("DJBA0001", "PIN LOCK LH-16.5 (WALL)", "Pin Lock", "Steel + PVC", "LH (Asia)"),
        P("DJBB0001", "PIN LOCK RH-16.5 (WALL)", "Pin Lock", "Steel + PVC", "RH (Asia)"),

        // ---- PVC pipe & sleeve ----
        P("DKAA", "PVC TIE SLEEVE", "PVC Pipe / Sleeve", "PVC", "Wall thickness (mm)"),
        P("DLAA0000", "PVC PIPE 22Ø, 2M", "PVC Pipe / Sleeve", "PVC", "22Ø / 2M"),
        P("DLAA0002", "PVC PIPE [1/2, 2M]", "PVC Pipe / Sleeve", "PVC", "[1/2 - 2M]"),
        P("DLAA0003", "PVC PIPE [5/8, 2M]", "PVC Pipe / Sleeve", "PVC", "[5/8 - 2M]"),

        // ---- Door brace ----
        P("DQAA04000900", "DOOR BRACE 400~900", "Door Brace", "Steel", "400~900"),
        P("DQAA05000700", "DOOR BRACE 500~700", "Door Brace", "Steel", "600"),
        P("DQAA06000800", "DOOR BRACE 600~800", "Door Brace", "Steel", "600~800"),
        P("DQAA07000900", "DOOR BRACE 700~900", "Door Brace", "Steel", "700~900"),
        P("DQAA07001100", "DOOR BRACE 700~1100", "Door Brace", "Steel", "700~1100"),
        P("DQAA07500950", "DOOR BRACE 750~950", "Door Brace", "Steel", "750~950"),
        P("DQAA09001100", "DOOR BRACE 900~1100", "Door Brace", "Steel", "900~1100"),
        P("DQAA09001600", "DOOR BRACE 900~1600", "Door Brace", "Steel", "900~1600"),
        P("DQAA09501100", "DOOR BRACE 950~1100", "Door Brace", "Steel", "950~1100"),
        P("DQAA10501200", "DOOR BRACE 1050~1200", "Door Brace", "Steel", "1050~1200"),
        P("DQAA11001300", "DOOR BRACE 1100~1300", "Door Brace", "Steel", "1100~1300"),
        P("DQAA11501300", "DOOR BRACE 1150~1300", "Door Brace", "Steel", "1150~1300"),
        P("DQAA12001400", "DOOR BRACE 1200~1400", "Door Brace", "Steel", "1200~1400"),
        P("DQAA14001600", "DOOR BRACE 1400~1600", "Door Brace", "Steel", "1400~1600"),
        P("DQAA16001800", "DOOR BRACE 1600~1800", "Door Brace", "Steel", "1600~1800"),
        P("DQAA18002000", "DOOR BRACE 1800~2000", "Door Brace", "Steel", "1800~2000"),

        // ---- Control / plumbing brace ----
        P("DEDA0001", "LOW CONTROL BRACE", "Control / Plumbing Brace", "Steel", "600L"),
        P("DQAE2000", "PLUMBING WALL BRACE", "Control / Plumbing Brace", "Steel", "2000 [2400H]"),
        P("DQAE2200", "PLUMBING WALL BRACE", "Control / Plumbing Brace", "Steel", "2200 [3000H]"),
        P("DQAE2700", "PLUMBING WALL BRACE", "Control / Plumbing Brace", "Steel", "2700 [3500H]"),
        P("DQAE2800", "PLUMBING WALL BRACE", "Control / Plumbing Brace", "Steel", "2800 [3500H]"),
        P("DQAG3000", "PLUMBING WALL BRACE", "Control / Plumbing Brace", "Steel", "3000"),

        // ---- Cap brace & push-pull ----
        P("DZAA", "PUSH-PULL BRACING SET", "Cap Brace / Push-Pull", "Steel", "LONG 1800L & SHORT 800L"),
        P("DQAB0001", "CAP BRACES (ALFU-Type)", "Cap Brace / Push-Pull", "Steel", "STD (USA)"),
        P("DQAB0700", "CAP BRACES (Special)", "Cap Brace / Push-Pull", "Steel", "Special (700)"),
        P("DQAF0600", "CAP BRACES (ALFA-Type)", "Cap Brace / Push-Pull", "Steel", "STD (Asia)"),
        P("DPAA0001", "TIE KEEPER (Omniwedge)", "Cap Brace / Push-Pull", "Steel"),

        // ---- Tools & etc ----
        P("DRAA1710", "BRACKET FLANGE NUT", "Tools & Etc", "Cast-iron", "17-100Ø"),
        P("DRBA0001", "TIE PULLER", "Tools & Etc", "Steel"),
        P("DRAA0001", "PIN LOCK Stripping Tool", "Tools & Etc", "Cast-iron"),
        P("DRCA0002", "PANEL PULLER", "Tools & Etc", "Steel", "Y style"),
        P("DRDA0001", "HOLE ALIGNER", "Tools & Etc", "Steel"),
        P("DRFA0001", "TIE BREAKE BAR", "Tools & Etc", "Steel"),
        P("DRGA0001", "SLEEVE EJECT BAR", "Tools & Etc", "Steel"),
        P("DRNA0002", "WORK BENCH (1000H)", "Tools & Etc", "Steel", "1200x500x1000(H)"),
        P("DRNA0004", "WORK BENCH (750H)", "Tools & Etc", "Steel", "1200X500X750(H)"),
        P("DROB0001", "WIRE TURNBUCKLE", "Tools & Etc", "Steel", "5/8 x 6M"),
        P("DTGA0001", "PVC CONE", "Tools & Etc", "PVC"),
        P("DUAA0001", "SQUARE WASHER", "Tools & Etc", "Steel"),
        P("DZAA0004", "DOUBLE WALER NUT CLAMP", "Tools & Etc", "Steel"),
        P("DZAA0005", "DOUBLE WALER CLAMP WASHER", "Tools & Etc", "Steel", "130X50"),
        P("DZAA0006", "PLASTIC CAP", "Tools & Etc", "PVC", "16Ø"),
        P("DZAA0008", "PLASTIC CAP", "Tools & Etc", "PVC", "18Ø"),
        P("DKAA0001", "SPONGE TIE SLEEVE", "Tools & Etc", "Sponge", "400MM"),
        P("DRAA0002", "SCRAPER", "Tools & Etc", "Steel + PVC"),
        P("DRPA0001", "PLYWOOD ADAPTOR", "Tools & Etc", "Aluminum")
    };
}
