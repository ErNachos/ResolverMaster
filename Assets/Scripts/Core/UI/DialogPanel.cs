using System;
using System.Collections.Generic;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// A small modal window (ErNa-007): dim layer, cream Gemini window, a title, a few text lines and one or two buttons in a row
    /// (secondary left, primary right). Used by the Challenge pause, the end of a run and the "new run?" confirmation.
    /// Built once under a screen; <see cref="Show"/> / <see cref="Hide"/>.
    /// </summary>
    public sealed class DialogPanel
    {
        const float Pad = 64, LineHeight = 76, TitleHeight = 96, BottomPad = 72, Gap = 24;

        readonly AppServices _ctx;
        readonly GameObject _root;
        readonly RectTransform _window;
        readonly List<TextMeshProUGUI> _lines = new List<TextMeshProUGUI>();
        Action _primary, _secondary;

        public bool Visible => _root.activeSelf;
        public TextMeshProUGUI Title { get; }
        public Button PrimaryButton { get; }
        public Button SecondaryButton { get; }
        public IReadOnlyList<TextMeshProUGUI> Lines => _lines;
        public RectTransform Window => _window;

        /// <param name="lines">Text lines between the title and the buttons (their text is set before showing).</param>
        /// <param name="extraHeight">Room left empty above the buttons (e.g. for a button that arrives in a later Update).</param>
        public DialogPanel(AppServices ctx, Transform parent, string name, string titleKey, int lines, string secondaryKey, string primaryKey, float extraHeight = 0)
        {
            _ctx = ctx;
            var ui = ctx.Ui;
            var s = ctx.Style;
            var dim = ui.Image(name, parent, null, s.dim);
            dim.raycastTarget = true;   // blocks what is under it
            UiFactory.Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<ModalOverlay>();
            _root = dim.gameObject;

            float height = Pad + TitleHeight + Gap + lines * LineHeight + extraHeight + Gap + s.buttonHeight + BottomPad;
            var window = ui.Panel("Window", dim.transform);
            _window = window.rectTransform;
            UiFactory.Place(_window, 0.5f, 0.5f, new Vector2(s.winPanelWidth, height), Vector2.zero);

            Title = ui.Label("Title", window.transform, titleKey, s.headerSize, s.text, TextAnchor.MiddleCenter, bold: true);
            Top(Title.rectTransform, Pad, TitleHeight);
            UiFactory.AutoSize(Title, s.bodySize);
            for (int i = 0; i < lines; i++)
            {
                var line = ui.Label("Line" + (i + 1), window.transform, null, s.bodySize, s.text, TextAnchor.MiddleCenter, bold: i == 0);
                Top(line.rectTransform, Pad + TitleHeight + Gap + i * LineHeight, LineHeight);
                UiFactory.AutoSize(line, s.smallSize);
                _lines.Add(line);
            }

            var buttons = ui.Rect("Buttons", window.transform);
            buttons.anchorMin = new Vector2(0, 0);
            buttons.anchorMax = new Vector2(1, 0);
            buttons.pivot = new Vector2(0.5f, 0);
            buttons.offsetMin = new Vector2(Pad, BottomPad);
            buttons.offsetMax = new Vector2(-Pad, BottomPad + s.buttonHeight);
            ui.Row(buttons, 32);
            if (secondaryKey != null)
            {
                SecondaryButton = ui.SecondaryButton("Secondary", buttons, secondaryKey, out _);
                SecondaryButton.onClick.AddListener(() => _secondary?.Invoke());
            }
            PrimaryButton = ui.PrimaryButton("Primary", buttons, primaryKey, out _);
            PrimaryButton.onClick.AddListener(() => _primary?.Invoke());
            _root.SetActive(false);
        }

        void Top(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(Pad, -top - height);
            rt.offsetMax = new Vector2(-Pad, -top);
        }

        public void Show(Action primary, Action secondary)
        {
            _primary = primary;
            _secondary = secondary;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Hide() => _root.SetActive(false);

        public void SetLine(int index, string text, Color? color = null)
        {
            _lines[index].text = text;
            _lines[index].color = color ?? _ctx.Style.text;
        }
    }
}
