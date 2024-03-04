using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1
{
    public class Textbox:Component
    {
        public static GameWindow gw;
        public static MouseState mouseState;
        bool myBoxHasFocus = true;
        StringBuilder myTextBoxDisplayCharacters = new StringBuilder();
        Texture2D textboxtexture;
        Rectangle rect;
        SpriteFont _font;

        public Textbox(int x, int y, int width, int height, Texture2D tex, SpriteFont font, GameWindow w)
        {
            gw = w;
            rect = new Rectangle(x, y, width, height);
            textboxtexture = tex;
            _font = font;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(textboxtexture, rect, Color.White);
            spriteBatch.DrawString(_font,myTextBoxDisplayCharacters.ToString(),new Vector2(rect.Location.X, rect.Location.Y),Color.Black);
        }
        public static void RegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            gw.TextInput += method;
        }
        public static void UnRegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            gw.TextInput -= method;
        }
        // these two are textbox specific.
        public void CheckClickOnMyBox(Point mouseClick, bool isClicked, Rectangle r)
        {
            if (r.Contains(mouseClick) && isClicked)
            {
                myBoxHasFocus = !myBoxHasFocus;
                if (myBoxHasFocus)
                    RegisterFocusedButtonForTextInput(OnInput);
                else
                    UnRegisterFocusedButtonForTextInput(OnInput);
            }
        }
        public void OnInput(object sender, TextInputEventArgs e)
        {
            var k = e.Key;
            var c = e.Character;
            myTextBoxDisplayCharacters.Append(c);
            Console.WriteLine(myTextBoxDisplayCharacters);
        }


        public override void Update(GameTime gameTime)
        {
            mouseState = Mouse.GetState();
            var isClicked = mouseState.LeftButton == ButtonState.Pressed;
            CheckClickOnMyBox(mouseState.Position, isClicked, new Rectangle(0, 0, 200, 200));

        }
        public override string ToString() { return myTextBoxDisplayCharacters.ToString(); }
    }
}
