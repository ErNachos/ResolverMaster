using System.Linq;
using System.Reflection;
using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.UI;
using UnityEditor;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// Text tables: no malformed lines or duplicates; every key present in one language is present in all the others;
    /// every key used by Core code (<see cref="LocKeys"/>) exists in every language.
    /// </summary>
    public sealed class LocalizationCheck : IValidationCheck
    {
        public string Name => "Localization";

        public void Run(ValidationReport report)
        {
            var loc = LocalizationLoader.Load(Language.English);
            foreach (var lang in LanguageCodes.All)
            {
                var table = loc.Table(lang);
                if (table.Entries.Count == 0) report.Error($"no texts for '{LanguageCodes.ToCode(lang)}' in Resources/{LocalizationLoader.ResourcesRoot}/{LanguageCodes.ToCode(lang)}");
                foreach (var e in table.Errors) report.Error(e);
                foreach (var kv in table.Entries)
                {
                    if (string.IsNullOrWhiteSpace(kv.Value)) report.Error($"empty value for '{kv.Key}' in '{LanguageCodes.ToCode(lang)}'");
                    foreach (var other in LanguageCodes.All.Where(o => o != lang))
                        if (!loc.Has(other, kv.Key)) report.Error($"key '{kv.Key}' exists in '{LanguageCodes.ToCode(lang)}' but not in '{LanguageCodes.ToCode(other)}'");
                }
            }
            var codeKeys = typeof(LocKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Concat(LanguageCodes.All.Select(LocKeys.LanguageName));
            foreach (var key in codeKeys)
            foreach (var lang in LanguageCodes.All)
                if (!loc.Has(lang, key)) report.Error($"key '{key}' used by code is missing in '{LanguageCodes.ToCode(lang)}'");
        }
    }

    /// <summary>
    /// Registered minigames: unique non-empty id, name key present in every language, icon present in the IconLibrary, Home card color set;
    /// Home card image present in the card library (warning only: Home draws a placeholder).
    /// </summary>
    public sealed class GameRegistryCheck : IValidationCheck
    {
        public string Name => "Minigames";

        public void Run(ValidationReport report)
        {
            var registry = GameRegistry.Discover();
            if (registry.Games.Count == 0) report.Error("no minigame registered");
            var loc = LocalizationLoader.Load(Language.English);
            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons == null) report.Error("IconLibrary missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries");
            var cards = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.CardLibraryPath);
            if (cards == null) report.Error("CardLibrary missing: run Resolver Master > UI Kit > Rebuild Sprite Libraries");
            foreach (var dup in registry.Games.GroupBy(g => g.Id).Where(g => g.Count() > 1))
                report.Error($"duplicate minigame id '{dup.Key}'");
            foreach (var g in registry.Games)
            {
                if (string.IsNullOrWhiteSpace(g.Id) || g.Id != g.Id.ToLowerInvariant() || g.Id.Contains(" "))
                    report.Error($"invalid id '{g.Id}' (lowercase, no spaces)");
                if (string.IsNullOrEmpty(g.NameKey)) report.Error($"'{g.Id}': empty name key");
                else
                    foreach (var lang in LanguageCodes.All)
                        if (!loc.Has(lang, g.NameKey)) report.Error($"'{g.Id}': name key '{g.NameKey}' missing in '{LanguageCodes.ToCode(lang)}'");
                if (icons != null && !icons.Has(g.IconName)) report.Error($"'{g.Id}': icon '{g.IconName}' not in the IconLibrary");
                if (g.CardColor.a < 0.01f) report.Error($"'{g.Id}': Home card color not set (GameInfo.CardColor)");
                if (string.IsNullOrEmpty(g.CardImage)) report.Warning($"'{g.Id}': no Home card image name (GameInfo.CardImage): placeholder used");
                else if (cards == null || !cards.Has(g.CardImage))
                    report.Warning($"'{g.Id}': Home card image '{g.CardImage}' missing in {UiKitAssets.CardsDir}: placeholder used");
            }
        }
    }

    /// <summary>
    /// UI kit (ErNa-005): every Gemini element expected in Assets/Art/UI, TextMesh Pro resources, font assets and the outlined material,
    /// the material of disabled buttons, the style filled with all its sprites, every Core icon present, touch size of the buttons.
    /// </summary>
    public sealed class UiKitCheck : IValidationCheck
    {
        public string Name => "UiKit";

        public void Run(ValidationReport report)
        {
            foreach (var name in UiArtSetup.Elements)
                if (!System.IO.File.Exists($"{UiArtSetup.UiArtDir}/{name}.png"))
                    report.Error($"UI element '{name}' missing in {UiArtSetup.UiArtDir}: run python Tools/Art/make_ui_art.py");
            if (!System.IO.File.Exists(UiArtSetup.ManifestPath)) report.Error($"{UiArtSetup.ManifestPath} missing: run python Tools/Art/make_ui_art.py");
            if (!System.IO.File.Exists(UiArtSetup.TmpSettingsPath)) report.Error("TextMesh Pro resources missing: import TMP Essential Resources (Setup > Import TMP Essentials, or -importPackage)");

            var style = AssetDatabase.LoadAssetAtPath<UiKitStyle>(UiKitAssets.StylePath);
            if (style == null)
            {
                report.Error("UiKitStyle missing: run Resolver Master > Setup > Apply All");
                return;
            }
            if (style.fontRegular == null || style.fontBold == null) report.Error("UiKitStyle: TextMesh Pro font asset missing (Setup > Apply All)");
            if (style.fontBoldOutlined == null) report.Error("UiKitStyle: outlined font material missing (Setup > Apply All)");
            if (style.disabledMaterial == null) report.Error("UiKitStyle: material of disabled buttons missing (Setup > Apply All)");
            var sprites = new (string name, UnityEngine.Sprite sprite)[]
            {
                ("buttonGreen", style.buttonGreen), ("buttonCream", style.buttonCream), ("roundCream", style.roundCream), ("roundYellow", style.roundYellow),
                ("panel", style.panel), ("ribbon", style.ribbon), ("chapterBanner", style.chapterBanner), ("tileLevel", style.tileLevel),
                ("tileDone", style.tileDone), ("tileLocked", style.tileLocked), ("starFull", style.starFull), ("starEmpty", style.starEmpty),
                ("cardSprite", style.cardSprite), ("circleSprite", style.circleSprite),
            };
            foreach (var (name, sprite) in sprites)
                if (sprite == null) report.Error($"UiKitStyle.{name} is empty (Setup > Apply All)");
            if (style.buttonGreen != null && style.buttonGreen.border == UnityEngine.Vector4.zero) report.Error("btn_green has no 9-slice border (Setup > Apply All)");
            if (style.ribbon != null && style.ribbon.border != UnityEngine.Vector4.zero) report.Error("the ribbon must not be 9-sliced (its folds would bend)");
            if (style.iconButtonSize < style.minTouchSize || style.gameButtonSize < style.minTouchSize || style.buttonHeight < style.minTouchSize)
                report.Error($"UiKitStyle: buttons smaller than minTouchSize {style.minTouchSize} (iconButtonSize, gameButtonSize, buttonHeight)");
            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons == null) return;
            foreach (var name in IconLibrary.CoreIcons)
                if (!icons.Has(name)) report.Error($"icon '{name}' missing in the IconLibrary");
        }
    }

    /// <summary>
    /// Chapter themes (ErNa-005): ThemeConfig present, ids unique, default theme valid, names in every language, and no strong blue or
    /// orange in the terrains (the pipes are grey and the water blue and orange: they must stand out).
    /// </summary>
    public sealed class ThemeCheck : IValidationCheck
    {
        public string Name => "Themes";

        public void Run(ValidationReport report)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<ResolverMaster.Core.UI.Themes.ThemeConfig>(ConfigAssets.ThemeConfigPath);
            if (cfg == null)
            {
                report.Error($"{ConfigAssets.ThemeConfigPath} missing: run Resolver Master > Setup > Ensure Config Assets");
                return;
            }
            if (cfg.themes.Count == 0) report.Error("ThemeConfig has no theme");
            foreach (var dup in cfg.themes.GroupBy(t => t.id).Where(g => g.Count() > 1)) report.Error($"theme id '{dup.Key}' used twice");
            if (cfg.themes.All(t => t.id != cfg.defaultTheme)) report.Error($"default theme '{cfg.defaultTheme}' does not exist");
            var loc = LocalizationLoader.Load(Language.English);
            foreach (var t in cfg.themes)
            {
                if (string.IsNullOrWhiteSpace(t.id)) report.Error("a theme has no id");
                foreach (var lang in LanguageCodes.All)
                    if (!loc.Has(lang, t.nameKey)) report.Error($"theme '{t.id}': name key '{t.nameKey}' missing in '{LanguageCodes.ToCode(lang)}'");
                foreach (var (what, c) in new[] { ("ground", t.ground), ("groundDetail", t.groundDetail), ("slab", t.slab), ("slabDetail", t.slabDetail), ("slabEdge", t.slabEdge) })
                    if (IsStrongWaterColor(c)) report.Error($"theme '{t.id}': {what} is a strong blue or orange (the water colors)");
            }
        }

        /// <summary>Saturated and bright, with the hue of the water: blue (190-250) or orange (15-40 degrees).</summary>
        public static bool IsStrongWaterColor(UnityEngine.Color c)
        {
            UnityEngine.Color.RGBToHSV(c, out float h, out float sat, out float v);
            float deg = h * 360f;
            bool waterHue = (deg >= 190f && deg <= 250f) || (deg >= 15f && deg <= 40f);
            return waterHue && sat > 0.5f && v > 0.75f;
        }
    }
}

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// ErNa-008, daily challenge of every game with a Challenge: exactly <see cref="ResolverMaster.Core.Challenge.DailyChallenge.Levels"/>
    /// steps, every tier id in the Challenge table, steps never going down (difficulty rising), and the 5 levels of a sample day generated.
    /// </summary>
    public sealed class DailyChallengeCheck : IValidationCheck
    {
        public string Name => "DailyChallenge";

        public void Run(ValidationReport report)
        {
            foreach (var module in GameRegistry.Discover().Modules)
            {
                var source = module.Challenge;
                if (source == null) continue;
                string id = module.Info.Id;
                var ids = source.Settings?.dailyTiers;
                int levels = ResolverMaster.Core.Challenge.DailyChallenge.Levels;
                if (ids == null || ids.Count != levels)
                {
                    report.Error($"'{id}': the daily challenge needs {levels} steps (ChallengeSettings.dailyTiers), found {ids?.Count ?? 0}");
                    continue;
                }
                foreach (var t in ids)
                    if (source.Table.tiers.FindIndex(x => x.id == t) < 0) report.Error($"'{id}': daily step '{t}' is not in the Challenge table");
                var steps = ResolverMaster.Core.Challenge.DailyChallenge.Steps(source);
                for (int i = 1; i < steps.Count; i++)
                    if (steps[i] < steps[i - 1]) report.Error($"'{id}': daily step {i + 1} ('{ids[i]}') is easier than step {i} ('{ids[i - 1]}')");
                if (steps.Count != levels) continue;
                const int day = 20261117;
                try
                {
                    for (int n = 1; n <= levels; n++)
                        ResolverMaster.Core.Challenge.ChallengeRules.Generate(source, ResolverMaster.Core.Challenge.DailyChallenge.RunSeed(day), n, 0, steps[n - 1]);
                }
                catch (System.Exception e)
                {
                    report.Error($"'{id}': the daily challenge of {day} cannot be generated: {e.Message}");
                }
            }
        }
    }
}
