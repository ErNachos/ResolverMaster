using System;
using ResolverMaster.Core.Ads;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// The test rewarded ad (ErNa-007): a dim layer over everything with a cream window "Test ad", a short wait (seconds counting
    /// down), then two buttons: "Watched" (the reward) and "Close" (no reward). Built once, above every screen.
    /// </summary>
    public sealed class TestAdPanel : MonoBehaviour, ITestAdPresenter
    {
        AppServices _ctx;
        float _wait, _time = -1f;
        Button _watched, _closed;
        TextMeshProUGUI _countdown;
        Action _onWatched, _onClosed;

        public bool Showing => gameObject.activeSelf;

        public static TestAdPanel Create(AppServices ctx, RectTransform root, float waitSeconds)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var dim = ui.Image("TestAd", root, null, s.dim);
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<ModalOverlay>();
            var panel = dim.gameObject.AddComponent<TestAdPanel>();
            panel._ctx = ctx;
            panel._wait = Mathf.Max(0f, waitSeconds);

            const float pad = 64, height = 640;
            var window = ui.Panel("Window", dim.transform);
            UiFactory.Place(window.rectTransform, 0.5f, 0.5f, new Vector2(s.winPanelWidth, height), Vector2.zero);
            var title = ui.Label("Title", window.transform, LocKeys.AdTestTitle, s.headerSize, s.text, TextAnchor.MiddleCenter, bold: true);
            Top(title.rectTransform, pad, 100, pad);
            UiFactory.AutoSize(title, s.bodySize);
            panel._countdown = ui.Label("Countdown", window.transform, null, s.bodySize, s.textMuted, TextAnchor.MiddleCenter);
            Top(panel._countdown.rectTransform, pad + 130, 150, pad);

            var buttons = ui.Rect("Buttons", window.transform);
            buttons.anchorMin = new Vector2(0, 0);
            buttons.anchorMax = new Vector2(1, 0);
            buttons.pivot = new Vector2(0.5f, 0);
            buttons.offsetMin = new Vector2(pad, 72);
            buttons.offsetMax = new Vector2(-pad, 72 + s.buttonHeight);
            ui.Row(buttons, 32);
            panel._closed = ui.SecondaryButton("Closed", buttons, LocKeys.AdTestClosed, out _);
            panel._closed.onClick.AddListener(() => panel.Finish(false));
            panel._watched = ui.PrimaryButton("Watched", buttons, LocKeys.AdTestWatched, out _);
            panel._watched.onClick.AddListener(() => panel.Finish(true));
            dim.gameObject.SetActive(false);
            return panel;
        }

        static void Top(RectTransform rt, float top, float height, float sidePad)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(sidePad, -top - height);
            rt.offsetMax = new Vector2(-sidePad, -top);
        }

        public void Present(Action onWatched, Action onClosed)
        {
            _onWatched = onWatched;
            _onClosed = onClosed;
            _time = 0f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Tick(0f);
        }

        /// <summary>Previews: the panel with the wait over (buttons enabled).</summary>
        public void ShowForPreview()
        {
            Present(null, null);
            Tick(_wait + 0.01f);
        }

        void Finish(bool watched)
        {
            if (_time < _wait) return;
            gameObject.SetActive(false);
            _time = -1f;
            var cb = watched ? _onWatched : _onClosed;
            _onWatched = _onClosed = null;
            cb?.Invoke();
        }

        void Tick(float dt)
        {
            if (_time < 0f) return;
            _time += dt;
            float left = Mathf.Max(0f, _wait - _time);
            bool ready = left <= 0f;
            _countdown.text = ready ? _ctx.Loc.Get(LocKeys.AdTestReady) : string.Format(_ctx.Loc.Get(LocKeys.AdTestWait), Mathf.CeilToInt(left));
            UiFactory.SetInteractable(_watched, ready);
            UiFactory.SetInteractable(_closed, ready);
        }

        void Update() => Tick(Time.unscaledDeltaTime);
    }
}
