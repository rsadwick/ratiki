using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;

namespace Platformer
{

    class Layer
    {
        public Texture2D[] Textures { get; private set; }
        public float ScrollRate { get; private set; }

        public Layer(ContentManager content, string basePath, float scrollRate)
        {
            //assumes each layer only has 3 segments:
            Textures = new Texture2D[3];
            for (int i = 0; i < 3; ++i)
                Textures[i] = content.Load<Texture2D>(basePath + "_" + i);
            ScrollRate = scrollRate;
        }

        public void Draw(SpriteBatch spriteBatch, float cameraPositionX, float cameraPositionY, int Height)
        {
			float x = cameraPositionX * ScrollRate;
			float y = Height * ScrollRate;
            //assume each segment is the same width:
            int segementWidth = Textures[0].Width;

            //calculate which segments to draw and how much to offet them:

            int leftSegment = (int)Math.Floor(x / segementWidth);
            int rightSegment = leftSegment + 1;
            x = (x / segementWidth - leftSegment) * -segementWidth;
            //spriteBatch.Draw(Textures[leftSegment % Textures.Length], new Vector2(x, 0.0f), Color.White);
            //spriteBatch.Draw(Textures[rightSegment % Textures.Length], new Vector2(x + segementWidth, 0.0f), Color.White);
			spriteBatch.Draw(Textures[leftSegment % Textures.Length], new Vector2(x, -y), Color.White);  
			spriteBatch.Draw(Textures[rightSegment % Textures.Length], new Vector2(x + segementWidth, -y), Color.White);
        }
    }
}
