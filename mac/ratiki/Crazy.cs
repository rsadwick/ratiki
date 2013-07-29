using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;

namespace Platformer
{
	class Crazy
	{
		// Because Farseer uses 1 unit = 1 meter we need to convert
		// between pixel coordinates and physics coordinates.
		// I've chosen to use the rule that 100 pixels is one meter.
		// We have to take care to convert between these two 
		// coordinate-sets wherever we mix them!

		public const float unitToPixel = 100.0f;
		public const float pixelToUnit = 1 / unitToPixel;

		public Body body;
		public Vector2 Position
		{
			get { 
				if (!isUnit) {
					return position;
				} 
				else {
					return body.Position * unitToPixel;
				}
					
			}
			set { body.Position = value * pixelToUnit; }
		}
		Vector2 position;

		private bool isUnit;

		/*public Vector2 Position
		{
			get { return body.Position * unitToPixel; }
			set { body.Position = value * pixelToUnit; }
		}*/

		public Level Level
		{
			get { return level; }
		}
		Level level;

		public Texture2D texture;

		private Vector2 size;
		public Vector2 Size
		{
			get { return size * unitToPixel; }
			set { size = value * pixelToUnit; }
		}

		private Vector2 origin;

		/// <param name="world">The farseer simulation this object should be part of</param>
		/// <param name="texture">The image that will be drawn at the place of the body</param>
		/// <param name="size">The size in pixels</param>
		/// <param name="mass">The mass in kilograms</param>
		/// <param name="level">current level</param>
		public Crazy(World world, string texture, Vector2 size, float mass, Level level, Vector2 position, bool isUnit)
		{
			body = BodyFactory.CreateRectangle(world, size.X * pixelToUnit, size.Y * pixelToUnit, 1);
			body.BodyType = BodyType.Dynamic;
			body.Friction = 30.7f;
			body.Restitution = 1.1f;
			this.isUnit = isUnit;
			this.position = position;

			this.Size = size;
			if (texture != null) {
				this.texture = level.Content.Load<Texture2D>(texture);
				origin = new Vector2(this.texture.Width / 2.0f, this.texture.Height / 2.0f);
			}
				
			this.level = level;

		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Vector2 scale = new Vector2(Size.X / (float)texture.Width, Size.Y / (float)texture.Height);
			spriteBatch.Draw(texture, 
			                 Position, 
			                 null, 
			                 Color.White, 
			                 body.Rotation, 
			                 origin, 
			                 scale, 
			                 SpriteEffects.None, 
			                 0);

		}

		public void Update(Vector2 position)
		{
			//this.position = position;

		}
	}
}
