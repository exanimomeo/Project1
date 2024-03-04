﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Reflection;
using Color = Microsoft.Xna.Framework.Color;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Security.Policy;
using System.Windows.Forms.VisualStyles;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct3D11;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using System.Security.Cryptography.Xml;
using System.Runtime.ConstrainedExecution;
using SharpDX.X3DAudio;
using SharpDX.WIC;


/**
 * Features:
 * Procedural generation
 * File reading for levels
 */
namespace Project1
{
    public class GolfGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public static Gamestate state;
        private Zone zone;
        Random rand = new Random();

        //Wall width
        private int wallWidth = 10;
        //Ball size
        private int ballSize = 64;

        //Texture data (move to loadcontent eventually)
        private Texture2D background;
        private Texture2D golfball;
        private Texture2D wall;
        private Texture2D pointer;
        private Texture2D arrow;
        private Texture2D debugpixel;
        private Texture2D hole;

        private float friction = .98f;
        private float sv = .2f; //Stopping value. The minimum speed before friction stops an object.
        private float forcemult = 1000f;

        public float deltTime;
        public float prevTime;

        //Storage for map features
        List<Ball> balls;
        List<Wall> walls;
        List<Zone> zones;

        //debug markers. One produces a vector and one places points
        List<Vector2> debug;
        List<Vector2> debuglines;
        List<Vector2> debuglinesRed;

        //The origin of the currently selected map. The ball will spawn here.
        private Vector2 origin;

        //makes it so only one update runs per frame drawn.
        public static Boolean updateSinceLastFrame = false;

        public String levelname;

        //Player control variables:
        // power = power the ball is hit at
        // aim = direction the player hits the ball in
        // bf = backwards-forwards for toggling which way the power moves in
        // maxPower = the power that the bf changes at to start decreasing
        // minPower = the power that the bf changes at to start increasing
        // mult = the multiplier for the power
        // sensitivity = how quickly the aim changes
        public float power = 1f;
        public float aim = 0f;
        public int bf = 1;
        public float maxPower = 5f;
        public float minPower = .7f;
        public float mult = 1.1f;
        public float sensitivity = .025f;
        public Boolean spacedownlast = false;

        public GolfGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add main menu and level select.





            origin = new Vector2(400, 400);

            state = Gamestate.Init;
            //Initialize first ball. Will move later if multiplayer is enabled.
            balls = new List<Ball>();

            Ball b = new Ball(new Vector2 (0,0), 16);
            b.SetVector(new Vector2(0, 0));
            b.Update();
            balls.Add(b);
            //Initialize test wall.
            walls = new List<Wall>();
            zones = new List<Zone>();
            debug = new List<Vector2>();
            debuglines = new List<Vector2>();
            debuglinesRed = new List<Vector2>();
            base.Initialize();
        }

        /**
         * Reads a file of the name filename that is a stored map and loads it onto memory.
         */
        private void ReadMap(String filename)
        {
            //gets the path to the base project1 folder.
            string winDir = Directory.GetCurrentDirectory();
            winDir = winDir.Substring(0, winDir.Length - 24);
            //uses levelname inside of content to search for a text file made with split in mind.
            StreamReader reader = new StreamReader(winDir +"/Content/" + filename);
            try
            {
                //Get the starting point for the ball
                String[] line = reader.ReadLine().Split(',');
                origin = new Vector2(int.Parse(line[0]), int.Parse(line[1]));
                
                //until the file is empty, add each line to the list and parse it as a wall with two points.
                do
                {
                    AddListItem(reader.ReadLine());
                }
                while (reader.Peek() != -1);
            }
            catch
            {
                throw new FileNotFoundException();
            }
            finally
            {
                reader.Close();
            }
        }

        //Adds the wall of the given format to the list %dd, %dd, %dd, %dd
        private void AddListItem(String text)
        {
            String[] first = text.Split('|');
            if (first[0].ToCharArray()[0] == 'a')
            {
                String[] points = first[1].Split(',');
                Vector2 p1 = new Vector2(int.Parse(points[0]), int.Parse(points[1]));
                Vector2 p2 = new Vector2(int.Parse(points[2]), int.Parse(points[3]));
                Wall w = new Wall(p1,p2);
                walls.Add(w);
            }
            else if (first[0].ToCharArray()[0] == 'b')
            {
                //form: direction 1-8/9, force, rectangle
                String[] points = first[1].Split(',');
                Zone z = new Zone();
                z.direction = int.Parse(points[0]);
                z.force = float.Parse(points[1]);
                z.zoneRect = new Rectangle(int.Parse(points[2]), int.Parse(points[3]), int.Parse(points[4]), int.Parse(points[5]));
                zones.Add(z);
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            //Load the Golf Ball sprite
            golfball = Content.Load<Texture2D>("golf ball");
            wall = Content.Load<Texture2D>("wall");
            background = Content.Load<Texture2D>("background");
            pointer = Content.Load<Texture2D>("pointer");
            arrow = Content.Load<Texture2D>("arrow");
            debugpixel = Content.Load<Texture2D>("debugpixel");
            hole = Content.Load<Texture2D>("hole");

            //Loads the level of name levelname in the content folder.
            levelname = "0.txt";
            ReadMap(levelname);
            balls[0].SetX(origin.X);
            balls[0].SetY(origin.Y);
        }

        protected override void Update(GameTime gameTime)
        {
            if (!updateSinceLastFrame)
            {
                debug.Clear();
                debuglines.Clear();
                debuglinesRed.Clear();
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    state = Gamestate.Exit;
                    Exit();
                }
                deltTime = forcemult * (float)(gameTime.ElapsedGameTime.TotalSeconds - prevTime);
                prevTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

                //Input handling
                if (state == Gamestate.Input)
                {
                    //Input works as intended
                    //This section will make the arrow denoting hitting power grow and shrink.
                    if (Keyboard.GetState().IsKeyDown(Keys.Space))
                    {
                        if (bf > 0)
                        {
                            power *= mult;
                            if (power > maxPower) bf = -1;
                        }
                        else
                        {
                            power /= mult;
                            if (power < minPower) bf = 1;
                        }
                        spacedownlast = true;
                    }
                    else
                    {
                        //If the player lets go of space, it enters the Running state and begins simulating the game again.
                        if (spacedownlast)
                        {
                            state = Gamestate.Running;
                            Vector2 forceVector = new Vector2((float)(Math.Cos(aim-Math.PI/2) * power * 2), (float)(Math.Sin(aim-Math.PI/2) * power * 2));
                            balls[0].SetVector(forceVector.X, forceVector.Y);
                            spacedownlast = false;
                        }
                        else
                        {
                            //This portion handles the direcitonal movement of the arrow.
                            int direction = 0;
                            if (Keyboard.GetState().IsKeyDown(Keys.Left))
                            {
                                direction -= 1;
                            }
                            if (Keyboard.GetState().IsKeyDown(Keys.Right))
                            {
                                direction += 1;
                            }
                            aim += (float)direction * sensitivity;

                        }
                    }

                    }

                //Physics handling
                for (int i = 0; i < balls.Count; i++)
                {
                    if (balls[i].vel_mag != 0)
                    {
                        balls[i].PreCalc();

                        //move to collision
                        balls[i].SetVector(balls[i].vector * friction);
                        balls[i].rot += balls[i].vel_mag;
                        zone = checkZone();
                        if (zone.direction != 0)
                        {
                            float distance, angle;
                            switch (zone.direction)
                            {
                                case 9:
                                    //on the hole.
                                    state = Gamestate.Menu;
                                    //TODO
                                    // add the angle to velocity with an inverse proportion to distance. At a minimum distance, change state to finished.
                                    break;
                                default:
                                    //the values 1-8 denote direction of hill movement, starting from a north facing hill and moving clockwise.
                                    //calculate the combined factors and revert it to magnitude and angle
                                    float x = (float)(balls[i].vel_mag * Math.Sin(balls[i].vel_rot) + zone.force * Math.Sin(Math.PI * zone.direction / 4));
                                    float y = (float)(balls[i].vel_mag * Math.Cos(balls[i].vel_rot) + zone.force * Math.Cos(Math.PI * zone.direction / 4));
                                    angle = (float)Math.Atan2(y, x);
                                    distance = (float)Math.Sqrt(x * x + y * y);
                                    balls[i].vel_rot = angle;
                                    balls[i].vel_mag = distance;
                                    break;
                            }
                        }
                    //checkCollision for all walls for all balls'
                    //todo (add a collision handler that finds the shortest distance collision to react to first and repeat.
                    collisionStep:
                        
                        CollisionResult cr = new CollisionResult();
                        cr.isCollision = false;
                        cr.distance = float.MaxValue;
                        //error here, no collisions being confirmed.
                        for (int j = 0; j < walls.Count; j++)
                        {
                            //cr = Collision(walls[j], balls[i]);
                            Vector2? result = closestPointOnLine(walls[j], balls[i].center);
                            //if (result != null) debug.Add((Vector2) result);
                            if (true) //Distance(result, balls[i].center + balls[i].vector) < balls[i].radius + wallWidth
                            {
                                //checks the collision of the wall and the ball to see if they intersect.
                                
                                CollisionResult cr1 = checkCollision(balls[i], walls[j]);
                                if (cr1.isCollision && cr1.distance > 0 && cr1.distance < cr.distance)
                                {
                                    //replace the collision result with the closest collision target.
                                    cr = cr1;
                                }
                            }


                        }
                        //after checking all walls, if there is a collision, move the ball the collision's distance. Else, move it the full length.

                        if (cr.isCollision && cr.distance <= balls[i].dist_remaining)
                        {
                            //move the ball the closest to the nearest source of collision and repeat the check with remaining movement.

                            balls[i].SetVector(cr.reflection);
                            balls[i].Move(new Vector2((float)(cr.distance * Math.Cos(balls[i].vel_rot)), (float)(cr.distance * Math.Sin(balls[i].vel_rot))));
                            balls[i].dist_remaining -= cr.distance;
                            //balls[i].vel_rot = cr.reflection;
                            //balls[i].vector = new Vector2( (float)Math.Sin(balls[i].vel_rot) * balls[i].vel_mag, (float)Math.Cos(balls[i].vel_rot) * balls[i].vel_mag);
                            
                            goto collisionStep;
                        }
                        else
                        {
                            balls[i].Move(new Vector2((float)(balls[i].dist_remaining * Math.Cos(balls[i].vel_rot)), (float)(balls[i].dist_remaining * Math.Sin(balls[i].vel_rot))));
                        }
                        if (balls[i].vel_mag < sv) { balls[i].vel_mag = 0; state = Gamestate.Input; }

                     //checkOverlapping
                    } else
                    {
                        state = Gamestate.Input;
                    }
                }
                updateSinceLastFrame = true;
            }
                base.Update(gameTime);
        }

        /**
         * gets the distance that a ball can travel before colliding with a wall w.
         * Operates on the assumption the ball does collide, so only run if the ball collides at its current trajectory.
         */
        private float distanceToCollision(Wall w, Ball ball,Vector2 intersect, Vector2 closestpoint)
        {
            debuglines.Add(closestpoint);
            debuglines.Add(ball.center);
            debuglines.Add(ball.center);
            debuglines.Add(intersect);
            debug.Add(intersect);
            debug.Add(closestpoint);
            Vector2 aCv = intersect - ball.center;
            Vector2 p1Cv = ball.center - closestpoint;
            Vector2 p2 = intersect - ball.radius * ((aCv.Length() / p1Cv.Length())) * ball.vector / ball.vector.Length();
            debuglines.Add(ball.center);

            debuglines.Add(ball.center + ball.vector * 4);
            //debugVector.Add(p2);
            //debug.Add(p2);
            return (p2 - ball.center).Length();
        }

        
        //TODO checks if on a hill or a hole.
        //Doesn't do multiple overlapping zones. keep them separate. To fix this, make it a list of zones that apply.
        private Zone checkZone()
        {
            for (int i = 0; i < zones.Count; i++)
            {
                //checks if the centerpoint of the ball is within the zoneRect.
                if (balls[0].GetX() > zones[i].zoneRect.Left && balls[0].GetX() < zones[i].zoneRect.Right && balls[0].GetY() < zones[i].zoneRect.Top && balls[0].GetY() > zones[i].zoneRect.Bottom)
                {
                    return zones[i];
                }
            }
            return new Zone();
        }

        
        /**
         * This equation gets the closest perpendicular point on the line between the line and the circle. (distance)
         * Works for horizontal lines, not vertical lines.
         * This algorithm is called "Line-Line-Intersect by Weisstein, Eric W. 2008"
         */
        Vector2? closestPointOnLine(Wall w, Vector2 ball)
        {
            float lx1 = w.p1.X;
            float ly1 = w.p1.Y;
            float lx2 = w.p2.X;
            float ly2 = w.p2.Y;
            float x0 = ball.X;
            float y0 = ball.Y;
            //long equation 
            float k = ((ly2 - ly1) * (x0 - lx1) - (lx2 - lx1) * (y0 - ly1)) / ((ly2 - ly1) * (ly2 - ly1) + (lx2 - lx1) * (lx2 - lx1));
            float x = x0 - k * (ly2 - ly1);
            float y = y0 + k * (lx2 - lx1);
            if (x >= Math.Min(lx1, lx2) && x <= Math.Max(lx1, lx2)
                && y >= Math.Min(ly1, ly2) && y <= Math.Max(ly1, ly2)) return new Vector2(x, y);
            
            /*
             * old equation (doesn't work
            float A1 = ly2 - ly1;
            float B1 = lx1 - lx2;
            double C1 = (ly2 - ly1) * lx1 + (lx1 - lx2) * ly1;
            double C2 = -B1 * x0 + A1 * y0;
            double det = A1 * A1 - B1 * B1;
            float cx = 0;
            float cy = 0;
            if (det != 0)
            {
                cx = (float)((B1 * C2 - A1 * C1) / det);
                cy = (float)((A1 * C2 - B1 * C1) / det);
            } else
            {
                cx = x0;
                cy = y0;
            }
            
            if (cx >= Math.Min(lx1,lx2) && cx <= Math.Max(lx1,lx2)
                && cy >= Math.Min(ly1,ly2) && cy <= Math.Max(ly1,ly2))
                return new Vector2(cx, cy);
            return null;
            */
            return null;
        }

        /**
         * Checks collision between a wall and a ball for next tick.
         * Fails (doesn't check ahead of the ball.
         */
        CollisionResult checkCollision(Ball b, Wall w)
        {
            //Collision check should test two things: a) the ball's distance perpendicular to the line is not less than radius + wallwidth b) the circle between the two points parallel to the wall.
            //Modify this to test whether the circle collides before it makes contact.
            //To do that, make a linear collision that is (radius + wallwidth) units closer to the circle. Add a change vector to v1,v2
            float wall_angle = (float) Math.Atan2(w.p1.Y-w.p2.Y, w.p1.X - w.p2.X);
            float wall_perpendicular = wall_angle;
            Vector2 perpendicular_vector = new Vector2((float)Math.Sin(wall_perpendicular), (float)Math.Cos(wall_perpendicular));
            perpendicular_vector *= (b.radius);

            float mod = 5;

            Vector2? intersect = checkLinesCollide(w.p1,
                                                  w.p2,
                                                  b.center,
                                                  b.center + b.vector * mod) ;
            

            if (intersect != null)
            {
                CollisionResult cr = new CollisionResult();
                Vector2 vector = new Vector2((float)Math.Sin(b.vel_rot), (float)Math.Cos(b.vel_mag));
                
                
                Vector2? closestpoint = closestPointOnLine(w, new Vector2 (b.collider.Center.X, b.collider.Center.Y));
                if (closestpoint != null)
                {
                    Vector2 normal =GetNormal(w, b.center);
                    debuglinesRed.Add((Vector2)intersect);
                    debuglinesRed.Add((Vector2) intersect + normal * 20);
                    float d = distanceToCollision(w, b, (Vector2)intersect, (Vector2) closestpoint);
                    if (d < 0) return new CollisionResult();
                    //float reflectionAngle = 2 * (float) Math.Atan2(w.p1.X - w.p2.X, w.p1.Y - w.p2.Y) - b.vel_rot;
                    //cr.reflection = reflectionAngle;
                    cr.reflection = Vector2.Reflect(b.vector, normal);

                    cr.distance = d;
                    cr.isCollision = true;
                    cr.intersect = (Vector2)intersect;

                    return cr;
                }
            }
            return new CollisionResult();
        }

        static Vector2 GetNormal(Wall w, Vector2 b)
        {
            Vector2 v = w.p1 - w.p2;
            Vector2 u = new Vector2(v.Y, -v.X);
            u.Normalize();
            float dir = Vector2.Dot(b - w.p1, u);
            if (dir > 0)
            {
                return u;
            } else
            {
                
                return -u;
            }
        }

        /**
         * returns the point where two lines collide. If they are parallel, returns null.
         */
        Vector2? checkLinesCollide(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4)
        {
            float x1 = v1.X;
            float x2 = v2.X;
            float x3 = v3.X;
            float x4 = v4.X;
            
            float y1 = v1.Y;
            float y2 = v2.Y;
            float y3 = v3.Y;
            float y4 = v4.Y;float a  = x1;

            float Px = ((x1 * y2 - y1 * x2)*(x3 - x4) - (x1 - x2)*(x3 * y4 - y3*x4))/ ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 -x4));
            float Py = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            if (Px >= Math.Min(x1, x2) && Px <= Math.Max(x1, x2)
                    && Py >= Math.Min(y1, y2) && Py <= Math.Max(y1, y2))
                return new Vector2(Px, Py);
            /*
            float A1 = y2 - y1;
            float B1 = x1 - x2;
            float C1 = A1 * x1 + B1 * y1;
            float A2 = y4 - y3;
            float B2 = x3 - x4;
            float C2 = A2 * x3 + B2 * y3;
            float det = A1 * B2 - A2 * B1;
            
            if (det != 0)
            {
                float x = (B2 * C1 - B1 * C2) / det;
                float y = (A1 * C2 - A2 * C1) / det;
                //return new Vector2(x, y);
                
                if (x >= Math.Min(x1, x2) && x <= Math.Max(x1, x2)
                    && y >= Math.Min(y1, y2) && y <= Math.Max(y1, y2))
                    return new Vector2(x, y);
                
            }
            */
            return null;

        }

        /**
         * Working as expected
         */
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            //Draw zones
            for (int i = 0; i < zones.Count; i++)
            {
                _spriteBatch.Draw(background, zones[i].zoneRect, Color.White);
            }
            //draw the player controls
            if (state == Gamestate.Input)
            {

                Color c = new Color();
                c.R = (byte)(255 - power * 2);
                c.B = (byte)(255 - power * 32);
                c.G = (byte)(255 - power * 32);
                c.A = (byte)255;
                Rectangle sourceRectangle = new Rectangle(0, 0, 24, 48 + (int) (power));
                float rumblefactor_x = (float) rand.NextDouble();
                float rumblefactor_y = (float) rand.NextDouble();
                _spriteBatch.Draw(arrow, new Vector2(balls[0].GetX() + (int) (rumblefactor_x * (power -.7)), balls[0].GetY() + (int) (rumblefactor_y * (power - .7))), sourceRectangle, c, aim, new Vector2(12,48), power/6 + 1, SpriteEffects.None, 1);
            }

            //draw golf balls
            for (int i = 0; i < balls.Count;i++)
            {
                _spriteBatch.Draw(golfball, new Vector2(balls[i].GetX(), balls[i].GetY()), new Rectangle(0,0,ballSize,ballSize), Color.White, balls[i].rot, new Vector2(ballSize/2,ballSize/2), .5f, SpriteEffects.None, 1);
            }

            //Draw walls after the balls
            for(int i = 0; i < walls.Count; i++)
            {

                DrawWall(gameTime, walls[i].p1, walls[i].p2);
            }

            //debug features
            for(int i = 0; i < debug.Count; i++)
            {
                _spriteBatch.Draw(pointer, new Rectangle((int) debug[i].X-8, (int) debug[i].Y-8, 16,16), Color.White);
            }
            for(int i = 0; i < debuglines.Count; i += 2)
            {
                Vector2 p1 = debuglines[i];
                Vector2 p2 = debuglines[i+1];

                DrawLine(_spriteBatch, p1, p2, Color.White);
            }
            for (int i = 0; i < debuglinesRed.Count; i += 2)
            {
                Vector2 p1 = debuglinesRed[i];
                Vector2 p2 = debuglinesRed[i + 1];

                DrawLine(_spriteBatch, p1, p2, Color.Red);
            }

            //draw ui

            _spriteBatch.End();
            updateSinceLastFrame = false;
            base.Draw(gameTime);
        }

        //This method is meant to be ran only inside of the Draw method as a helper function. It relies on _spriteBatch being open.
        protected void DrawRectangle(GameTime gameTime, int x, int y, int width, int height)
        {
            //Background
            _spriteBatch.Draw(background, new Rectangle(x,y,width,height), Color.White);
            //Top wall 
            _spriteBatch.Draw(wall, new Rectangle(x, y-wallWidth/2, width, wallWidth), Color.White);
            //Left wall
            _spriteBatch.Draw(wall, new Rectangle(x-wallWidth/2, y, wallWidth, height), Color.White);
            //Bottom wall
            _spriteBatch.Draw(wall, new Rectangle(x, y+height-wallWidth/2, width, wallWidth), Color.White);
            //Right wall
            _spriteBatch.Draw(wall, new Rectangle(x+width-wallWidth/2, y, wallWidth, height), Color.White);

        }

        /**
         * Helper function for debug.
         */
        public void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color)
        {
            var distance = Vector2.Distance(point1, point2);
            var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            DrawLine(spriteBatch, point1, distance, angle, color);
        }
        public void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color)
        {
            float thickness = 1f;
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(length, thickness);
            spriteBatch.Draw(debugpixel, point, null, color, angle, origin, scale, SpriteEffects.None, 0);
        }

        //This method draws a wall in between the points p1 = (x1,y1) and p2 = (x2,y2). All collision addition is done based on these.
        //Run this in the middle of the Draw() method.
        protected void DrawWall(GameTime gameTime, Vector2 p1, Vector2 p2)
        {
            //Basic right angle triangle math.
            int width = (int) (p1.X - p2.X);
            int height = (int) (p1.Y - p2.Y);
            float length = (float) Math.Sqrt((float)(width*width + height*height));
            //Create the wall of the width wallWidth and the height of the hypotenuse, and place it at point 1.
            Rectangle sourceRectangle = new Rectangle((int)p1.X-wallWidth/2, (int)p1.Y, wallWidth, (int) length);
            float angle = (float)Math.Atan2(width, height);
            //Draw the wall between point 1 and point 2. 
            _spriteBatch.Draw(wall, p1, sourceRectangle, Color.White, (float)Math.PI - angle, new Vector2(5,5), 1.0f, SpriteEffects.None, 1);
        }
    }

    
    
    struct CollisionResult
    {
        public Boolean isCollision;     //If there was a collision, this should be 1, else, it is initialized as 0.
        public float distance;
        public Vector2 reflection;
        public Vector2 intersect;
    }

    //Zones that the ball can enter. Zones effect the movement of the ball.
    struct Zone
    {
        public int direction; //1-8 north to northwest. 9 means center
        public float force; //
        public Rectangle zoneRect;
        public Texture2D texture;
    }

    struct Menu
    {
        public Rectangle rectangle;
        public Texture2D texture;
    }
}
