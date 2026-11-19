using ResolverMaster.Core.Ads;
using ResolverMaster.Core.App;
using ResolverMaster.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// ErNa-013: what the app does around a real ad (<see cref="IAdHost"/>), above every screen: an invisible layer that takes every
    /// tap while the ad is on (the game's input is blocked), music and effects paused (<see cref="AudioListener.pause"/>) and given
    /// back at the end, and the short message "Ad not available. Try again shortly." (same red pill as the game messages).
    /// </summary>
    public sealed class AdOverlay : MonoBehaviour, IAdHost
    {
        AppServices _ctx;
        GameObject _shield;
        CanvasGroup _notice;
        TextMeshProUGUI _noticeText;
        float _noticeTime = -1f, _hold;
        bool _pausedAudio;

        public bool Blocking => _shield.activeSelf;

        public RectTransform NoticeRect => _notice.gameObject.activeSelf ? (RectTransform)_notice.transform : null;

        public static AdOverlay Create(AppServices ctx, RectTransform root, float noticeSeconds)
        {
            var ui = ctx.Ui;
            var s = ctx.Style;
            var rt = ui.Rect("AdOverlay", root);
            UiFactory.Stretch(rt);
            var overlay = rt.gameObject.AddComponent<AdOverlay>();
            overlay._ctx = ctx;
            overlay._hold = Mathf.Max(0.5f, noticeSeconds);

            var shield = ui.Image("InputShield", rt, null, new Color(0, 0, 0, 0));
            shield.raycastTarget = true;
            UiFactory.Stretch(shield.rectTransform);
            shield.gameObject.AddComponent<Decoration>();   // invisible: covers everything on purpose
            overlay._shield = shield.gameObject;
            overlay._shield.SetActive(false);

            // the message lies over the game on purpose (Decoration: its overlaps are not layout problems); as wide as the screen
            // padding allows, so the longest language fits on one line
            var slot = new Vector2(s.referenceResolution.x - 2 * s.screenPadding, s.mainActionHeight);
            // in the safe area, exactly on the slot of the game's main action and messages (it hides the button while shown)
            var safe = ui.Rect("SafeArea", rt);
            UiFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            var notice = ui.Image("AdNotice", safe, s.buttonCream, s.toastColor, pill: true);
            UiFactory.Place(notice.rectTransform, 0.5f, 0, slot, new Vector2(0, s.bottomBarHeight + s.mainActionGap));
            notice.gameObject.AddComponent<Decoration>();
            overlay._noticeText = ui.Title("Text", notice.transform, null, s.headerSize);
            overlay._noticeText.color = s.toastText;
            UiFactory.Stretch(overlay._noticeText.rectTransform, 40, 14, 40, 4);
            UiFactory.AutoSize(overlay._noticeText, s.bodySize * 0.8f);
            overlay._notice = notice.gameObject.AddComponent<CanvasGroup>();
            overlay._notice.blocksRaycasts = false;
            overlay._notice.interactable = false;
            notice.gameObject.SetActive(false);
            return overlay;
        }

        public void AdStarted()
        {
            transform.SetAsLastSibling();
            _shield.SetActive(true);
            if (!AudioListener.pause)
            {
                AudioListener.pause = true;
                _pausedAudio = true;
            }
        }

        public void AdEnded()
        {
            _shield.SetActive(false);
            if (_pausedAudio) AudioListener.pause = false;
            _pausedAudio = false;
        }

        public void ShowNotAvailable()
        {
            transform.SetAsLastSibling();
            _noticeText.text = _ctx.Loc.Get(LocKeys.AdNotAvailable);
            _notice.gameObject.SetActive(true);
            _notice.alpha = 1f;
            _noticeTime = 0f;
        }

        void Update()
        {
            if (_noticeTime < 0f) return;
            _noticeTime += Time.unscaledDeltaTime;
            float fade = Mathf.Max(0.01f, _ctx.Style.toastFade);
            float t = _noticeTime - _hold;
            _notice.alpha = t <= 0 ? 1f : Mathf.Clamp01(1f - t / fade);
            if (t >= fade)
            {
                _notice.gameObject.SetActive(false);
                _noticeTime = -1f;
            }
        }
    }
}
