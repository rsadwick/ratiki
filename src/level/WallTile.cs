using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Platformer
{
	class WallTile
	{
		private Texture2D texture;
		private Vector2 origin;
		public Level Level
		{
			get { return level; }
		}
		Level level;
		public Vector2 Position
		{
			get { return position; }
		}

		Vector2 position;
		public Vector2 Velocity
		{
			get { return velocity; }
		}

		Vector2 velocity;
		/// <summary>
		/// Gets whether or not the player's feet are on the WallTile.
		/// </summary>
		public bool PlayerIsOn { get; set; }
		public Rectangle BoundingRectangle
		{
			get
			{
				int left = (int)Math.Round(Position.X - origin.X) + localBounds.X;
				int top = (int)Math.Round(Position.Y - origin.Y) + localBounds.Y;
				return new Rectangle(left, top, localBounds.Width, localBounds.Height);
			}
		}
		public FaceDirection Direction
		{
			get { return direction; }
			set { direction = value; }
		}
		FaceDirection direction = FaceDirection.Left;

		public TileCollision Collision
		{
			get { return collision; }
			set { collision = value; }
		}
		private TileCollision collision;
		private Rectangle localBounds;
		private float waitTime;
		private const float MaxWaitTime = 0.1f;
		private const float MoveSpeed = 120.0f;
		public WallTile(Level level, Vector2 position, TileCollision collision)
		{
			this.level = level;
			this.position = position;
			this.collision = collision;
			LoadContent();
		}
		public void LoadContent()
		{
			texture = collision == TileCollision.Platform ?
				Level.Content.Load<Texture2D>("Tiles/BlockA5") :
					Level.Content.Load<Texture2D>("Tiles/BlockA5");
			origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
			// Calculate bounds within texture size.
			localBounds = new Rectangle(0, 0, texture.Width, texture.Height);
		}
		public void Update(GameTime gameTime)
		{

		}
		public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
		{
			spriteBatch.Draw(
				texture,
				Position,
				null,
				Color.White,
				0.0f,
				origin,
				1.0f,
				SpriteEffects.None,
				0.0f);
		}
	}
}