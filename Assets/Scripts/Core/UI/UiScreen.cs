using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>A full-screen panel managed by <see cref="ScreenManager"/>. Subclasses build their content once and refresh in <see cref="OnShow"/>.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UiScreen : MonoBehaviour
    {
        public abstract string Id { get; }

        public CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());
        CanvasGroup _group;

        /// <summary>Called every time the screen becomes the top one.</summary>
        public virtual void OnShow() { }

        /// <summary>Return true to consume the Back key (e.g. close an inner popup) instead of leaving the screen.</summary>
        public virtual bool HandleBack() => false;
    }
}
