using CurrencyTracker.Manager;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace CurrencyTracker.Infos;

public class PresetFont
{
    public static IFontAtlas? FontAtlas { get; private set; }
    public static IFontHandle? Axis96 { get; private set; }
    public static IFontHandle? Axis12 { get; private set; }
    public static IFontHandle? Axis14 { get; private set; }
    public static IFontHandle? Axis18 { get; private set; }


    public static void Init()
    {
        FontAtlas ??= P.PI.UiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.OnNewFrame);
        Axis96 ??= ConstructFontHandle(GameFontFamilyAndSize.Axis96);
        Axis12 ??= ConstructFontHandle(GameFontFamilyAndSize.Axis12);
        Axis14 ??= ConstructFontHandle(GameFontFamilyAndSize.Axis14);
        Axis18 ??= ConstructFontHandle(GameFontFamilyAndSize.Axis18);
    }

    private static IFontHandle ConstructFontHandle(GameFontFamilyAndSize fontInfo)
        => FontAtlas.NewGameFontHandle(new GameFontStyle(fontInfo));
}
