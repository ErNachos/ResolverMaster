using UnityEngine;

namespace ResolverMaster.Core.UI
{
    /// <summary>
    /// Marks a graphic drawn on top of another on purpose (a symbol on a piece, splash drops, highlights): the preview measurements do
    /// not report its overlaps or its leaving the container (it is still checked for being on screen and for having a sprite).
    /// </summary>
    public sealed class Decoration : MonoBehaviour { }
}
