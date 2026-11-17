namespace ResolverMaster.Core.Localization
{
    /// <summary>Languages shipped with the app. Add a value here, a code in <see cref="LanguageCodes"/> and a folder in Resources/Localization.</summary>
    public enum Language
    {
        Italian,
        English,
    }

    /// <summary>Two-letter codes used in the save file and as the Resources/Localization sub-folder names.</summary>
    public static class LanguageCodes
    {
        public static readonly Language[] All = { Language.Italian, Language.English };

        public static string ToCode(Language language) => language == Language.Italian ? "it" : "en";

        public static bool TryParse(string code, out Language language)
        {
            switch (code)
            {
                case "it": language = Language.Italian; return true;
                case "en": language = Language.English; return true;
                default: language = Language.English; return false;
            }
        }
    }
}
