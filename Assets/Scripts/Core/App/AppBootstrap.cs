using ResolverMaster.Core.Games;
using ResolverMaster.Core.Localization;
using ResolverMaster.Core.Save;
using ResolverMaster.Core.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ResolverMaster.Core.App
{
    /// <summary>
    /// The only component of the Main scene that starts things: loads save, language, minigame registry and style, then builds the UI.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        public const int TargetFrameRate = 60;

        public AppServices Context { get; private set; }

        void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;

            var style = Resources.Load<UiKitStyle>(UiKitStyle.ResourcesPath);
            var icons = Resources.Load<IconLibrary>(IconLibrary.ResourcesPath);
            if (style == null || icons == null)
            {
                Debug.LogError("AppBootstrap: UiKitStyle or IconLibrary missing in Resources/ResolverMaster. Run Resolver Master > Setup > Apply All.");
                return;
            }

            var store = SaveStore.Default();
            var data = store.Load();
            var language = LanguagePicker.Resolve(data.settings.language, Application.systemLanguage.ToString());

            Context = new AppServices
            {
                Loc = LocalizationLoader.Load(language),
                Data = data,
                Store = store,
                Games = GameRegistry.Discover(),
                Style = style,
                Icons = icons,
            };

            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = AppUi.CreateCanvas(style);
            canvas.transform.SetParent(transform, false);
            AppUi.Build(Context, (RectTransform)canvas.transform);
        }
    }
}
