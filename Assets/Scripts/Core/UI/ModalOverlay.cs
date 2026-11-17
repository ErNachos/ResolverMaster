using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Marks a full-screen dimming layer that hosts a popup (win panel...). While it is visible the previews measure only the popup,
    /// since everything under the dim is intentionally covered.
    /// </summary>
    public sealed class ModalOverlay : MonoBehaviour { }
}
