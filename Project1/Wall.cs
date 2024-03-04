using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Project1
{
    internal class Wall
    {
        public Vector2 p1;
        public Vector2 p2;
        public Rectangle collider;
        private Microsoft.Xna.Framework.Vector2 p11;
        private Microsoft.Xna.Framework.Vector2 p21;

        public Wall(Vector2 p1, Vector2 p2) 
        {
            this.p1 = p1;
            this.p2 = p2;
            collider = new Rectangle((int) Math.Min(p1.X,p2.X),(int) Math.Min(p1.Y,p2.Y), (int) Math.Abs(p1.X-p2.X), (int) Math.Abs(p1.Y-p2.Y));
        }

        public Boolean checkCollider(Rectangle r)
        {
            Vector2 l1 = new Vector2(collider.X, collider.Y);
            Vector2 r1 = new Vector2(collider.X + collider.Width, collider.Y + collider.Height);
            Vector2 l2 = new Vector2(r.X, r.Y);
            Vector2 r2 = new Vector2(r.X + r.Width, r.Y + r.Height);

            //area 0 = false
            if (l1.X == r1.X || l1.Y == r1.Y || r2.X == l2.X || l2.Y == r2.Y)
            {
                return false;
            }
            //one to the side of the other
            if (l1.X > r2.X || l2.X > r1.X)
            {
                return false;
            }
            //one is above another
            if (r1.Y > l2.Y || r2.Y > l1.Y)
            {
                return false;
            }
            //else, a rectangle overlaps
            return true;
        }
    }
}
