using System.Drawing;

public static class MathExtensions
{
    public static int CeilDiv(int a, int b)
    {
        return (a + b - 1) / b;
    }

    public static Size CeilDiv(Size a, int b)
    {
        return new()
        {
            Width = CeilDiv(a.Width, b),
            Height = CeilDiv(a.Height, b),
        };
    }
}
