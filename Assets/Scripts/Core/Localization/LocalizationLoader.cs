using UnityEngine;

namespace ResolverMaster.Core.Localization
{
    /// <summary>
    /// Unity side of localization: fills a <see cref="Localizer"/> with every text file in Resources/Localization/&lt;code&gt;/
    /// (core.txt plus one file per minigame), for every language.
    /// </summary>
    public static class LocalizationLoader
    {
        public const string ResourcesRoot = "Localization";

        public static Localizer Load(Language initial)
        {
            var localizer = new Localizer(initial);
            foreach (var language in LanguageCodes.All)
            {
                string code = LanguageCodes.ToCode(language);
                var table = localizer.Table(language);
                foreach (var file in Resources.LoadAll<TextAsset>(ResourcesRoot + "/" + code))
                    table.Parse(file.text, code + "/" + file.name);
            }
            return localizer;
        }
    }
}
