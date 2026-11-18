using ResolverMaster.Core.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Stack of screens in the single Main scene. <see cref="Push"/> opens a screen above the current one, <see cref="Back"/> closes the top one;
    /// the Android Back button (Keyboard.escapeKey in the Input System) calls <see cref="Back"/>; Back on the root screen quits the app.
    /// </summary>
    public sealed class ScreenManager : MonoBehaviour
    {
        readonly Dictionary<string, UiScreen> _screens = new Dictionary<string, UiScreen>();
        readonly List<UiScreen> _stack = new List<UiScreen>();
        float _fade;
        bool _busy;

        public UiScreen Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public void Init(float fadeSeconds) => _fade = fadeSeconds;

        public void Register(UiScreen screen)
        {
            _screens[screen.Id] = screen;
            screen.gameObject.SetActive(false);
        }

        public T Get<T>(string id) where T : UiScreen => _screens.TryGetValue(id, out var s) ? s as T : null;

        /// <summary>Clears the stack and shows <paramref name="id"/> at once (start-up, previews).</summary>
        public void ShowRoot(string id)
        {
            foreach (var s in _stack) s.gameObject.SetActive(false);
            _stack.Clear();
            var screen = _screens[id];
            _stack.Add(screen);
            Activate(screen, 1f);
        }

        /// <summary>Shows <paramref name="id"/> on top at once, without animation (previews).</summary>
        public void PushImmediate(string id)
        {
            var screen = _screens[id];
            if (Top != null) Top.gameObject.SetActive(false);
            _stack.Add(screen);
            Activate(screen, 1f);
        }

        public void Push(string id)
        {
            if (_busy || !_screens.TryGetValue(id, out var screen) || screen == Top) return;
            var previous = Top;
            _stack.Add(screen);
            Activate(screen, 0f);
            StartCoroutine(Fade(screen, 0f, 1f, () => { if (previous != null) previous.gameObject.SetActive(false); }));
        }

        public void Back()
        {
            if (_busy || Top == null) return;
            if (Top.HandleBack()) return;
            if (_stack.Count <= 1)
            {
                Application.Quit();
                return;
            }
            var closing = Top;
            _stack.RemoveAt(_stack.Count - 1);
            var below = Top;
            below.gameObject.SetActive(true);
            below.transform.SetAsLastSibling();
            closing.transform.SetAsLastSibling();
            below.OnShow();
            PlayMusicOf(below);
            StartCoroutine(Fade(closing, 1f, 0f, () => closing.gameObject.SetActive(false)));
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
        }

        void Activate(UiScreen screen, float alpha)
        {
            screen.gameObject.SetActive(true);
            screen.transform.SetAsLastSibling();
            screen.Group.alpha = alpha;
            screen.Group.blocksRaycasts = true;
            screen.Group.interactable = true;
            screen.OnShow();
            PlayMusicOf(screen);
        }

        /// <summary>ErNa-010: the music of the screen on top (Home, or the game of the screen).</summary>
        static void PlayMusicOf(UiScreen screen)
        {
            var game = screen != null ? screen.MusicGame : null;
            if (game != null) GameAudio.PlayMusic(AudioCatalog.MusicFor(game));
        }

        IEnumerator Fade(UiScreen screen, float from, float to, System.Action done)
        {
            _busy = true;
            screen.Group.interactable = false;
            for (float t = 0; t < _fade; t += Time.unscaledDeltaTime)
            {
                screen.Group.alpha = Mathf.Lerp(from, to, t / _fade);
                yield return null;
            }
            screen.Group.alpha = to;
            screen.Group.interactable = true;
            _busy = false;
            done?.Invoke();
        }
    }
}
