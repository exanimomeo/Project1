using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Project1
{
    internal static class VectorHelpers
    {

        public static Vector2 GetNormal(Wall w, Vector2 b)
        {
            Vector2 v = w.p1 - w.p2;
            Vector2 u = new Vector2(v.X, -v.Y);
            u.Normalize();
            float dir = Vector2.Dot(b - w.p1, u);
            if (dir > 0)
            {
                return v;
            }
            else
            {

                return -v;
            }
        }
    }
}