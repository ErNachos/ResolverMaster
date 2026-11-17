using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Marks a graphic drawn on top of another on purpose (a fountain on its pipe stub, splash drops, highlights): the preview
    /// measurements do not report its overlaps or its leaving the container (it is still checked for being on screen and for having
    /// a sprite). With <see cref="mayLeaveScreen"/> (side cards of the Home carousel, which peek in from the screen edges) the
    /// on-screen check is skipped too. A disabled component does not count.
    /// </summary>
    public sealed class Decoration : MonoBehaviour
    {
        public bool mayLeaveScreen;
    }
}
