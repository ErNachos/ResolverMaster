namespace ResolverMaster.Core.Localization
{
    /// <summary>
    /// Pure rule for the active language (no Unity types, tested in EditMode):
    /// the language chosen in Settings wins; on the first launch (nothing saved) Italian if the phone is in Italian, otherwise English.
    /// </summary>
    public static class LanguagePicker
    {
        /// <param name="savedCode">Language code from the save file ("it", "en"); empty or unknown means "never chosen".</param>
        /// <param name="deviceLanguage">The phone language name, as Unity's SystemLanguage.ToString() (e.g. "Italian", "French").</param>
        public static Language Resolve(string savedCode, string deviceLanguage)
        {
            if (!string.IsNullOrEmpty(savedCode) && LanguageCodes.TryParse(savedCode, out var saved))
                return saved;
            return deviceLanguage == "Italian" ? Language.Italian : Language.English;
        }
    }
}
