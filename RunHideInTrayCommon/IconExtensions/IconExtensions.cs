using System.Drawing;

namespace IconExtensions;

public static class IconExtensions
{
    extension(Icon)
    {
        public static Size LargestSize => new(256, 256);
    }
}
