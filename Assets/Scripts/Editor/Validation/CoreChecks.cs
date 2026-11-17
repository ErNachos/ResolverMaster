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

    /// <summary>Registered minigames: unique non-empty id, name key present in every language, icon present in the IconLibrary.</summary>
    public sealed class GameRegistryCheck : IValidationCheck
    {
        public string Name => "Minigames";

        public void Run(ValidationReport report)
        {
            var registry = GameRegistry.Discover();
            if (registry.Games.Count == 0) report.Error("no minigame registered");
            var loc = LocalizationLoader.Load(Language.English);
            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons == null) report.Error("IconLibrary missing: run Resolver Master > UI Kit > Rebuild Icon Library");
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
            }
        }
    }

    /// <summary>UI kit: style asset with fonts and generated sprites, every Core icon present.</summary>
    public sealed class UiKitCheck : IValidationCheck
    {
        public string Name => "UiKit";

        public void Run(ValidationReport report)
        {
            var style = AssetDatabase.LoadAssetAtPath<UiKitStyle>(UiKitAssets.StylePath);
            if (style == null)
            {
                report.Error("UiKitStyle missing: run Resolver Master > Setup > Apply All");
                return;
            }
            if (style.fontRegular == null || style.fontBold == null) report.Error("UiKitStyle: font missing");
            if (style.buttonSprite == null || style.cardSprite == null || style.panelSprite == null || style.outlineSprite == null || style.circleSprite == null)
                report.Error("UiKitStyle: generated sprites missing: run Resolver Master > UI Kit > Regenerate Sprites");
            var icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(UiKitAssets.IconLibraryPath);
            if (icons == null) return;
            foreach (var name in IconLibrary.CoreIcons)
                if (!icons.Has(name)) report.Error($"icon '{name}' missing in Assets/Art/Icons");
        }
    }
}
