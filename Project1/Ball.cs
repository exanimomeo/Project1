using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Project1
{
    //Ball class, contains all the information of a ball. Does not denote which player owns it, as that is denoted as where it is stored.
    //Rot does not affect movement, only visual rotation.
    internal class Ball
    {
        public Vector2 center;
        public float radius;
        public float vel_mag;   //Magnitude of the velocity vector (in pixels per update)
        public float vel_rot;   //Direction of the velocity vector (in radians)
        public Vector2 vector;
        public float rot;
        public float dist_remaining; //The amount of distance remaining in the physics calculation. Used for sub-frame calculations.
        public Vector2 vector_remaining;
        public Rectangle collider;

        public Ball()
        {
            center = new Vector2();

        }

        public Ball(Vector2 center, float radius)
        {
            this.radius = radius;
            this.center = center;
            this.vector = new Vector2();
            this.rot = 0;
            Update();
        }

        /**
         * Updates some common values with the intrinsic qualities
         */
        public void Update()
        {
            collider = new Rectangle((int) (center.X - radius), (int) (center.Y - radius), (int) (2 * radius), (int) (2 * radius));
            vel_mag = vector.Length();
            vel_rot = getAngle();
        }

        /**
         * Moves the ball by an amount equal to its vector.
         */
        public void Move(Vector2 vector)
        {
            center.X += vector.X;
            center.Y += vector.Y;
            Update();
        }

        public void SetVector(Vector2 up)
        {
            vector = up;
            Update();
        }

        public void PreCalc()
        {
            Update();
            dist_remaining = vel_mag;
            vector_remaining = vector;
        }
        
        public float GetX()
        {
            return center.X;
        }

        public float GetY()
        {
            return center.Y;
        }

        public void SetX(float x)
        {
            center.X = x;
            Update();
        }
        public void SetY(float y)
        {
            center.Y = y;
            Update();
        }

        public void SetVector(float x, float y)
        {
            vector.X = x;
            vector.Y = y;
            vel_mag = vector.Length();
            vel_rot = getAngle();
            Update();
        }

        public void SetRemVector(float x, float y)
        {
            vector_remaining.X = x;
            vector_remaining.Y = y;
            dist_remaining = vector_remaining.Length();

        }

        private float getAngle()
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }
    }
}
