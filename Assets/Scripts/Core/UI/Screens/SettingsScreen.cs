using System;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>Settings: language (Italiano / English), sound on/off, vibration on/off. Every change is saved at once.</summary>
    public sealed class SettingsScreen : UiScreen
    {
        public override string Id => ScreenIds.Settings;

        AppServices _ctx;
        readonly (Language lang, Image bg, Text label)[] _languages = new (Language, Image, Text)[LanguageCodes.All.Length];
        Action _refreshToggles;

        public static SettingsScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<SettingsScreen>(ctx, parent, "SettingsScreen");
            var screen = layout.Root.gameObject.AddComponent<SettingsScreen>();
            screen._ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            ScreenLayout.TopBarTitle(ctx, layout.TopBar, LocKeys.SettingsTitle, s.headerSize);

            // ---- language card ----
            var langCard = ui.Image("LanguageCard", layout.Body, s.cardSprite, s.surface);
            ui.Column(langCard.rectTransform, 16, new RectOffset(40, 40, 24, 40));
            UiFactory.Layout(langCard, preferredHeight: 24 + s.rowHeight + 16 + s.buttonHeight + 40);
            CreateRowHeader(ctx, langCard.transform, "language", LocKeys.SettingsLanguage);
            var seg = ui.Rect("Languages", langCard.transform);
            UiFactory.Layout(seg, preferredHeight: s.buttonHeight);
            ui.Row(seg, 24);
            for (int i = 0; i < LanguageCodes.All.Length; i++)
            {
                var lang = LanguageCodes.All[i];
                var b = ui.TextButton("Lang_" + LanguageCodes.ToCode(lang), seg, LocKeys.LanguageName(lang), s.badge, s.text, out var label);
                b.onClick.AddListener(() => screen.SelectLanguage(lang));
                screen._languages[i] = (lang, (Image)b.targetGraphic, label);
            }

            // ---- sound and vibration card ----
            var togglesCard = ui.Image("TogglesCard", layout.Body, s.cardSprite, s.surface);
            ui.Column(togglesCard.rectTransform, 8, new RectOffset(40, 40, 16, 16));
            UiFactory.Layout(togglesCard, preferredHeight: 16 + s.rowHeight * 2 + 8 + 16);
            var sound = CreateToggleRow(ctx, togglesCard.transform, "sound", LocKeys.SettingsSound,
                () => ctx.Data.settings.sound, v => ctx.Data.settings.sound = v);
            var vibration = CreateToggleRow(ctx, togglesCard.transform, "vibration", LocKeys.SettingsVibration,
                () => ctx.Data.settings.vibration, v => ctx.Data.settings.vibration = v);
            screen._refreshToggles = () => { sound(); vibration(); };
            return screen;
        }

        static RectTransform CreateRowHeader(AppServices ctx, Transform parent, string iconName, string key)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var row = ui.Rect("Row_" + iconName, parent);
            UiFactory.Layout(row, preferredHeight: s.rowHeight);
            var icon = ui.Icon("Icon", row, iconName, s.rowIconSize, s.primary);
            UiFactory.Place(icon.rectTransform, 0, 0.5f, new Vector2(s.rowIconSize, s.rowIconSize), Vector2.zero);
            var label = ui.Label("Label", row, key, s.bodySize, s.text, TextAnchor.MiddleLeft, bold: true);
            UiFactory.Stretch(label.rectTransform, s.rowIconSize + 28, 0, 0, 0);
            return row;
        }

        /// <returns>The refresh action of the row (updates color and On/Off text from the save data).</returns>
        static Action CreateToggleRow(AppServices ctx, Transform parent, string iconName, string key, Func<bool> get, Action<bool> set)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var row = CreateRowHeader(ctx, parent, iconName, key);
            ((RectTransform)row.Find("Label")).offsetMax = new Vector2(-(220 + 24), 0);   // leave room for the switch
            var toggle = ui.TextButton("Toggle", row, LocKeys.CommonOn, s.primary, s.onPrimary, out var text);
            UiFactory.Place((RectTransform)toggle.transform, 1, 0.5f, new Vector2(220, s.buttonHeight * 0.8f), Vector2.zero);
            var bg = (Image)toggle.targetGraphic;
            var loc = text.GetComponent<LocalizedText>();
            void Refresh()
            {
                bool on = get();
                bg.color = on ? s.primary : s.toggleOff;
                text.color = on ? s.onPrimary : s.text;
                loc.Bind(ctx.Loc, on ? LocKeys.CommonOn : LocKeys.CommonOff);
            }
            toggle.onClick.AddListener(() =>
            {
                set(!get());
                ctx.Save();
                Refresh();
            });
            Refresh();
            return Refresh;
        }

        void SelectLanguage(Language language)
        {
            _ctx.Data.settings.language = LanguageCodes.ToCode(language);
            _ctx.Loc.SetLanguage(language);
            _ctx.Save();
            RefreshLanguages();
        }

        void RefreshLanguages()
        {
            var s = _ctx.Style;
            foreach (var (lang, bg, label) in _languages)
            {
                bool selected = lang == _ctx.Loc.Current;
                bg.color = selected ? s.primary : s.badge;
                label.color = selected ? s.onPrimary : s.text;
            }
        }

        public override void OnShow()
        {
            RefreshLanguages();
            _refreshToggles?.Invoke();
        }
    }
}
