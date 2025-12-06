using System.Drawing;
using System.Runtime.CompilerServices;

namespace IconExtensions;

internal static class IconExtensions
{
    extension(Icon)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_iconData")]
        public extern static ref byte[]? GetIconData(Icon icon);
    }
}
