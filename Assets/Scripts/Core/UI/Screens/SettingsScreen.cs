using System;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI.Screens
{
    /// <summary>
    /// Settings, in one cream Gemini window: language (globe, Italiano / English pills: the selected one green), sound (round button
    /// with the "sound on" or "sound off" icon) and vibration (round button with its icon, faded and crossed by a small X drawn by
    /// code when off). Every change is saved at once. ErNa-013: under the window "Privacy options" (opens the consent choices),
    /// visible only when the consent SDK says it is required.
    /// </summary>
    public sealed class SettingsScreen : UiScreen
    {
        public override string Id => ScreenIds.Settings;

        AppServices _ctx;
        readonly (Language lang, Button button, TextMeshProUGUI label)[] _languages = new (Language, Button, TextMeshProUGUI)[LanguageCodes.All.Length];
        Action _refreshToggles;
        Button _privacy;

        public static SettingsScreen Create(AppServices ctx, Transform parent)
        {
            var layout = ScreenLayout.Create<SettingsScreen>(ctx, parent, "SettingsScreen");
            var screen = layout.Root.gameObject.AddComponent<SettingsScreen>();
            screen._ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;

            ScreenLayout.TopBarButton(ctx, layout.TopBar, "icon_back", left: true).onClick.AddListener(() => ctx.Screens.Back());
            ScreenLayout.TopBarTitle(ctx, layout.TopBar, LocKeys.SettingsTitle, s.headerSize);

            const int padX = 56, padTop = 48, padBottom = 56;
            var card = ui.Panel("SettingsCard", layout.Body);
            ui.Column(card.rectTransform, 24, new RectOffset(padX, padX, padTop, padBottom));
            // header row, pills, gap, music row, effects row, vibration row: 6 spacings of 24
            UiFactory.Layout(card, preferredHeight: padTop + s.rowHeight * 4 + s.buttonHeight + 24 + 6 * 24 + padBottom);

            // ---- language: globe + label, then the two pills ----
            CreateRowHeader(ctx, card.transform, "icon_language", LocKeys.SettingsLanguage);
            var seg = ui.Rect("Languages", card.transform);
            UiFactory.Layout(seg, preferredHeight: s.buttonHeight);
            ui.Row(seg, 28);
            for (int i = 0; i < LanguageCodes.All.Length; i++)
            {
                var lang = LanguageCodes.All[i];
                var b = ui.SecondaryButton("Lang_" + LanguageCodes.ToCode(lang), seg, LocKeys.LanguageName(lang), out var label);
                b.onClick.AddListener(() => screen.SelectLanguage(lang));
                screen._languages[i] = (lang, b, label);
            }
            var gap = ui.Rect("Gap", card.transform);
            UiFactory.Layout(gap, preferredHeight: 24);

            // ---- music, effects (ErNa-010: two switches) and vibration: label on the left, round toggle on the right ----
            var music = CreateToggleRow(ctx, card.transform, "Music", LocKeys.SettingsMusic,
                () => ctx.Data.settings.music, v => ctx.Data.settings.music = v, "icon_sound_on", "icon_sound_off", false);
            var sound = CreateToggleRow(ctx, card.transform, "Sound", LocKeys.SettingsSound,
                () => ctx.Data.settings.sound, v => ctx.Data.settings.sound = v, "icon_sound_on", "icon_sound_off", false);
            var vibration = CreateToggleRow(ctx, card.transform, "Vibration", LocKeys.SettingsVibration,
                () => ctx.Data.settings.vibration, v => ctx.Data.settings.vibration = v, "icon_vibration", "icon_vibration", true);
            screen._refreshToggles = () => { music(); sound(); vibration(); };

            // ---- ErNa-013: privacy options (UMP), below the window ----
            screen._privacy = ui.SecondaryButton("PrivacyOptions", layout.Body, LocKeys.SettingsPrivacy, out _);
            UiFactory.Layout((RectTransform)screen._privacy.transform, preferredHeight: s.buttonHeight);
            screen._privacy.onClick.AddListener(screen.OpenPrivacyOptions);
            if (ctx.Consent != null) ctx.Consent.Changed += () => { if (screen != null) screen.RefreshPrivacy(); };
            screen.RefreshPrivacy();
            return screen;
        }

        static RectTransform CreateRowHeader(AppServices ctx, Transform parent, string iconName, string key)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var row = ui.Rect("Row_" + key, parent);
            UiFactory.Layout(row, preferredHeight: s.rowHeight);
            if (iconName != null)
            {
                var icon = ui.Icon("Icon", row, iconName, s.rowIconSize);
                UiFactory.Place(icon.rectTransform, 0, 0.5f, new Vector2(s.rowIconSize, s.rowIconSize), Vector2.zero);
            }
            var label = ui.Label("Label", row, key, s.headerSize, s.text, TextAnchor.MiddleLeft, bold: true);
            UiFactory.Stretch(label.rectTransform, iconName != null ? s.rowIconSize + 32 : 0, 0, 0, 0);
            return row;
        }

        /// <returns>The refresh action of the row (icon from the save data).</returns>
        static Action CreateToggleRow(AppServices ctx, Transform parent, string name, string key, Func<bool> get, Action<bool> set,
            string onIcon, string offIcon, bool crossWhenOff)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var row = CreateRowHeader(ctx, parent, null, key);
            ((RectTransform)row.Find("Label")).offsetMax = new Vector2(-(s.rowHeight + 24), 0);   // leave room for the round button
            var toggle = ui.IconButton("Toggle", row, onIcon, s.rowHeight, s.rowHeight * 0.58f);
            toggle.name = name + "Toggle";
            UiFactory.Place((RectTransform)toggle.transform, 1, 0.5f, new Vector2(s.rowHeight, s.rowHeight), Vector2.zero);
            var icon = toggle.transform.Find("Icon").GetComponent<Image>();

            // the small X of a switched-off toggle: two short red bars drawn by code, on the lower right of the icon
            RectTransform cross = null;
            if (crossWhenOff)
            {
                cross = ui.Rect("OffMark", toggle.transform);
                float mark = s.rowHeight * 0.34f;
                UiFactory.Place(cross, 0.5f, 0.5f, new Vector2(mark, mark), new Vector2(s.rowHeight * 0.24f, -s.rowHeight * 0.2f));
                for (int k = 0; k < 2; k++)
                {
                    var bar = ui.Image("Bar" + (k + 1), cross, s.circleSprite, s.offMark);
                    UiFactory.Place(bar.rectTransform, 0.5f, 0.5f, new Vector2(mark * 1.2f, mark * 0.34f), Vector2.zero);
                    bar.rectTransform.localRotation = Quaternion.Euler(0, 0, k == 0 ? 45 : -45);
                    bar.gameObject.AddComponent<Decoration>();   // the two bars cross on purpose
                }
                cross.gameObject.AddComponent<Decoration>();
            }

            void Refresh()
            {
                bool on = get();
                icon.sprite = ctx.Icons.Get(on ? onIcon : offIcon);
                icon.color = on || !crossWhenOff ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                if (cross != null) cross.gameObject.SetActive(!on);
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
            foreach (var (lang, button, label) in _languages) _ctx.Ui.SetGreen(button, label, lang == _ctx.Loc.Current);
        }

        void OpenPrivacyOptions()
        {
            if (_ctx.Consent == null || !_ctx.Consent.PrivacyOptionsRequired) return;
            UiFactory.SetInteractable(_privacy, false);
            _ctx.Consent.ShowPrivacyOptions(() =>
            {
                if (this == null) return;
                UiFactory.SetInteractable(_privacy, true);
                RefreshPrivacy();
            });
        }

        /// <summary>The button exists only when the SDK requires it (never in the Editor: no consent service).</summary>
        void RefreshPrivacy() => _privacy.gameObject.SetActive(ForcePrivacyButton || (_ctx.Consent != null && _ctx.Consent.PrivacyOptionsRequired));

        /// <summary>Previews: the button visible as on a phone in the EEA.</summary>
        public static bool ForcePrivacyButton;

        public override void OnShow()
        {
            RefreshLanguages();
            _refreshToggles?.Invoke();
            RefreshPrivacy();
        }
    }
}
