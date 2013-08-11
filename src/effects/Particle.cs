#region File Description
//-----------------------------------------------------------------------------
// Particle.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Platformer
{
	public class Particle
	{
		public Texture2D Texture { get; set; }
		public Vector2 Position { get; set; }
		public Vector2 Velocity { get; set; }
		public float Angle { get; set; }
		public float AngularVelocity { get; set;}
		public Color Color { get; set; }
		public float Size { get; set;}
		public int Life { get; set; }

		public Particle(Texture2D texture, Vector2 position, Vector2 velocity, float angle, float angularVelocity, Color color, float size, int life)
		{
			Texture = texture;
			Position = position;
			Velocity = velocity;
			Angle = angle;
			AngularVelocity = angularVelocity;
			Color = color;
			Size = size;
			Life = life;
		}

		public void Update()
		{
			Life--;
			Position += Velocity;
			Angle += AngularVelocity;
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Rectangle sourceRectangle = new Rectangle (0, 0, Texture.Width, Texture.Height);
			Vector2 origin = new Vector2 (Texture.Width / 2, Texture.Height / 2);

			spriteBatch.Draw (Texture, Position, sourceRectangle, Color, Angle, origin, Size, SpriteEffects.None, 0f);
		}

	}

}
