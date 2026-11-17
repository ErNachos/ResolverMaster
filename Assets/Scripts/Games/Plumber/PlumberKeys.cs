namespace ResolverMaster.Games.Plumber
{
    /// <summary>
    /// Every Plumber text key used by code (Resources/Localization/&lt;code&gt;/plumber.txt). The Plumber validator checks that each
    /// constant here exists in every language.
    /// </summary>
    public static class PlumberKeys
    {
        public const string Chapter1 = "plumber.chapter1.name";
        public const string Chapter2 = "plumber.chapter2.name";
        public const string Chapter3 = "plumber.chapter3.name";
        public const string Chapter4 = "plumber.chapter4.name";
        public const string Chapter5 = "plumber.chapter5.name";
        public const string Chapter6 = "plumber.chapter6.name";
        public const string Chapter7 = "plumber.chapter7.name";
        public const string Chapter8 = "plumber.chapter8.name";
        public const string Chapter9 = "plumber.chapter9.name";
        /// <summary>Main action button: "Open the water".</summary>
        public const string OpenWater = "plumber.action.open";
        public const string FailLeak = "plumber.fail.leak";
        public const string FailMix = "plumber.fail.mix";
        public const string FailWrongColor = "plumber.fail.wrong_color";
        public const string FailDry = "plumber.fail.dry";

        public static string ForProblem(WaterProblemKind kind)
        {
            switch (kind)
            {
                case WaterProblemKind.Leak: return FailLeak;
                case WaterProblemKind.ColorMix: return FailMix;
                case WaterProblemKind.WrongColor: return FailWrongColor;
                default: return FailDry;
            }
        }
    }
}
