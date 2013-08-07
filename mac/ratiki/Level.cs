#region File Description
//-----------------------------------------------------------------------------
// Level.cs
//
// Ryan Sadwick
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;

namespace Platformer
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    class Level : IDisposable
    {
        // Physical structure of the level.
        private Tile[,] tiles;
        private Layer[] layers;
        //camera:
        private float cameraPositionX;
		public float cameraPositionY;
        const float fadeTime = 12.0f;  // TODO: Change this value to your liking. Bigger numbers equal more smoothing.
        const float smoothingFactor = (1.0f / fadeTime) * 60.0f;
        // The layer which entities are drawn on top of.
        private const int EntityLayer = 2;

        // Entities in the level.
        public Player Player
        {
            get { return player; }
        }
        Player player;

        private List<Gem> gems = new List<Gem>();
        private List<DrawablePhysicsObject> crates = new List<DrawablePhysicsObject>();
        private List<DrawablePhysicsObject> floors = new List<DrawablePhysicsObject>();

        public World World {
            get { return world; }
        }

        private World world;

		public List<Enemy> enemies = new List<Enemy>();
        public List<MovableTile> movableTiles = new List<MovableTile>();
		public List<WallTile> wallTiles = new List<WallTile>();

        // Key locations in the level.        
        private Vector2 start;
        private Point exit = InvalidPosition;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state.
        private Random random = new Random(354668); // Arbitrary, but constant seed

        public int Score
        {
            get { return score; }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;

        private const int PointsPerSecond = 5;

        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        private SoundEffect exitReachedSound;

		private Texture2D sprite;


        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex)
        {
			world = new World(new Vector2(0, 9.8f));  
			random = new Random();

            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            timeRemaining = TimeSpan.FromMinutes(6.5);

            LoadTiles(fileStream);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Layer[3];
            layers[0] = new Layer(Content, "Backgrounds/Layer0", 0.2f);
            layers[1] = new Layer(Content, "Backgrounds/Layer1", 0.5f);
            layers[2] = new Layer(Content, "Backgrounds/Layer2", 0.8f);

            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Sounds/StageCleared");
        }

		public void SpawnCrate()
		{
            DrawablePhysicsObject crate;
            crate = new DrawablePhysicsObject(world, "Tiles/grass.png", new Vector2(50.0f, 50.0f), 0.1f, this);
            crate.Position = new Vector2(random.Next(10, 100), 100);
            crate.Body.BodyType = BodyType.Dynamic;
            crate.Body.Friction = 20.0f;
            crate.Body.Restitution = 0.3f;
			crate.Body.SleepingAllowed = false;
			crates.Add(crate);
		}

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");

        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);

                // Gem
                case 'G':
                    return LoadGemTile(x, y, false);

                //Powerup:
                case 'P':
                    return LoadGemTile(x, y, true);

                // Floating platform
                case '-':
                    return LoadTile("Platform", TileCollision.Platform);

                // Various enemies
                case 'A':
					return LoadEnemyTile(x, y, "MonsterA", TileCollision.Passable);
                case 'B':
					return LoadEnemyTile(x, y, "MonsterB", TileCollision.Passable);
                case 'C':
					return LoadEnemyTile(x, y, "MonsterC", TileCollision.Passable);
                case 'D':
					return LoadEnemyTile(x, y, "MonsterD", TileCollision.Passable);

                // Platform block
                case '~':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

                // Passable block
                case ':':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y);

                // Impassable block
                case '#':
                    return LoadVarietyTile("BlockA", 7, TileCollision.Impassable);

                //Movable Tile:
                case 'M':
                    return LoadMovableTile(x, y, TileCollision.Platform);

				case 'W':
					return LoadWallTile(x, y, TileCollision.Impassable);

				//Floor
				case 'F':
					return LoadFloorTile (x, y, TileCollision.Impassable);

				//crates
				case 'f':
					return LoadCrateTile (x, y, TileCollision.Passable);

                case 'L':
                    return LoadTile("BlockB1", TileCollision.Ladder);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        private Tile LoadMovableTile(int x, int y, TileCollision collision)
        {
            Point position = GetBounds(x, y).Center;
            movableTiles.Add(new MovableTile(this, new Vector2(position.X, position.Y), collision));
            return new Tile(null, TileCollision.Passable);
        }

		private Tile LoadWallTile(int x, int y, TileCollision collision)
		{
			Point position = GetBounds(x, y).Center;
			wallTiles.Add(new WallTile(this, new Vector2(position.X, position.Y), collision));
			return new Tile(null, TileCollision.Impassable);
		}

		private Tile LoadFloorTile(int x, int y, TileCollision collision)
		{
            Point position = GetBounds(x, y).Center;
            DrawablePhysicsObject floor = new DrawablePhysicsObject(world, "Tiles/grass.png", new Vector2(50.0f, 50.0f), 0.1f, this);
            floor.Position = new Vector2(position.X, position.Y);
            floor.Body.BodyType = BodyType.Static;
            floor.Body.Friction = 20.0f;
            floor.Body.Restitution = 0.3f;
            floors.Add(floor);
			return new Tile(null, TileCollision.Impassable);
		}

		private Tile LoadCrateTile(int x, int y, TileCollision collision)
		{
			Point position = GetBounds (x, y).Center;
            DrawablePhysicsObject crate = new DrawablePhysicsObject(world, "Tiles/grass.png", new Vector2(50.0f, 50.0f), 0.1f, this);
            crate.Position = new Vector2(position.X, position.Y);
            crate.Body.BodyType = BodyType.Dynamic;
            crate.Body.Friction = 20.0f;
            crate.Body.Restitution = 0.3f;
            crates.Add(crate);
			return new Tile(null, TileCollision.Passable);
		}


        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }


        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start);

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet, TileCollision collision)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet, collision));
			return new Tile(null, collision);
        }


        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadGemTile(int x, int y, bool isPowerUp)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Gem(this, new Vector2(position.X, position.Y), isPowerUp));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        public TileCollision GetTileCollisionBehindPlayer(Vector2 playerPosition)
        {
            int x = (int)playerPosition.X / Tile.Width;
            int y = (int)(playerPosition.Y - 1) / Tile.Height;

			//prevent out of range:
			if (y < 0)
				y = 0;

            //prevent escapping past level ends:
            if(x == Width)
                return TileCollision.Impassable;
            //allow jumping past the level top and falling through bottom:
            if(y == Height)
                return TileCollision.Passable;

             return tiles[x, y].Collision;
        }

        public TileCollision GetTileCollisionBelowPlayer(Vector2 playerPosition)
        {
            int x = (int)playerPosition.X / Tile.Width;
            int y = (int)(playerPosition.Y) / Tile.Height;

			//prevent out of range:
			if (y < 0)
				y = 0;

            if(x == Width)
                return TileCollision.Impassable;
            //allow jumping past the level top and falling through bottom:
            if(y == Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;

        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {

            
			// Pause while the player is dead or time is expired.
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                timeRemaining -= TimeSpan.FromSeconds(seconds);
                score += seconds * PointsPerSecond;
            }
            else
            {
                timeRemaining -= gameTime.ElapsedGameTime;
                Player.Update(gameTime, keyboardState, gamePadState, touchState, accelState, orientation);
                
				UpdateGems(gameTime);

                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled(null);

                UpdateEnemies(gameTime);
                UpdateMovableTiles(gameTime);
				UpdateWallTiles (gameTime);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit))
                {
                    OnExitReached();
                }
            }

            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;

			world.Step((float) gameTime.ElapsedGameTime.TotalSeconds);
        }

        /// <sum				mary>
        /// Animates each gem and checks to allows the player to collect them.
        /// </summary>
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Gem gem = gems[i];

                gem.Update(gameTime);

                if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player);
                }
            }
        }


        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.Update(gameTime);
                
				if (enemy.PlayerIsOn && !enemy.PlayerIsAttackingTop) {
					//Make player move with tile if the player is on top of tile
					player.Position += enemy.Velocity;
				} 

				else if (!enemy.PlayerIsOn && enemy.PlayerIsAttackingTop) {
					Console.WriteLine ("Thrust HIT");
					OnEnemyKilled(enemy, Player);
					player.Velocity -= new Vector2 (0, 2000);
					player.Position -= new Vector2 (0, 50);
					enemy.PlayerIsAttackingTop = false;
				}

                // Enemy collisions: if enemy collides with player - power up or not:
                if (enemy.IsAlive && Player.BoundingRectangle.Intersects(enemy.BoundingRectangle))
                {

                    if (Player.IsPoweredUp)
                    {
                        OnEnemyKilled(enemy, Player);
                    }
					else if(enemy.PlayerIsAttackingTop)
					{


					}
                    
                    else if(Player.IsInvulnerable)
                    {
                        
                    }
					else
					{
						player.Lives -= 1;
						Player.IsInvulnerable = true;

						//player.Velocity -= new Vector2 (1000, 30);
						//player.Position -= new Vector2 (55, 10);

					}
                }

            }
        }

        private void OnEnemyKilled(Enemy enemy, Player killedBy)
        {
            enemy.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnGemCollected(Gem gem, Player collectedBy)
        {
            score += gem.PointValue;

            gem.OnCollected(collectedBy);
        }



		private void UpdateMovableTiles(GameTime gameTime)
		{
			foreach (MovableTile tile in movableTiles)
			{
				tile.Update(gameTime);
				if (tile.PlayerIsOn)
				{
					//Make player move with tile if the player is on top of tile
					player.Position += tile.Velocity;
					Console.WriteLine (player.Position);
				}
			}
		}

		private void UpdateWallTiles(GameTime gameTime)
		{
            player.IsOnWall = false;
			foreach (WallTile tile in wallTiles)
			{
				tile.Update(gameTime);
				if (tile.PlayerIsOn)
				{
					player.IsOnWall = true;
				}
			}
		}
 
        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerKilled(Enemy killedBy)
        {
            Player.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            Player.OnReachedExit();
            exitReachedSound.Play();
            reachedExit = true;
        }

        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            Player.Reset(start);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
			int bottom = (Height * Tile.Height);
			bottom = Math.Min(bottom, Height);
			bottom -= Tile.Height;

            spriteBatch.Begin();
            for (int i = 0; i <= EntityLayer; ++i)
                layers[i].Draw(spriteBatch, cameraPositionX, cameraPositionY, bottom);
            spriteBatch.End();

            ScrollCamera(spriteBatch.GraphicsDevice.Viewport);
           // Matrix cameraTransform = Matrix.CreateTranslation(-cameraPositionX, 0.0f, 0.0f);
			Matrix cameraTransform = Matrix.CreateTranslation(-cameraPositionX, -cameraPositionY, 0.0f);
            

			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default,
                              RasterizerState.CullCounterClockwise, null, cameraTransform);

            DrawTiles(spriteBatch);
            
            foreach (MovableTile tile in movableTiles)
                tile.Draw(gameTime, spriteBatch);

			foreach (WallTile tile in wallTiles)
				tile.Draw (gameTime, spriteBatch);

            foreach (Gem gem in gems)
                gem.Draw(gameTime, spriteBatch);

			foreach (DrawablePhysicsObject floor in floors)
			{
				floor.Draw(spriteBatch);
			}

            foreach(DrawablePhysicsObject crate in crates)
			{
				crate.Draw(spriteBatch);
			}

			//Clean up crates that fall beyond the level:
			for (int currentCrate = crates.Count - 1; currentCrate >= 0; currentCrate--)
			{
				Vector2 pos = CoordinateHelper.ToScreen(crates[currentCrate].Body.Position);

				if (pos.Y > spriteBatch.GraphicsDevice.Viewport.Height) 
				{
					world.RemoveBody(crates[currentCrate].Body);
					crates.RemoveAt(currentCrate);
				}
			}

            Player.Draw(gameTime, spriteBatch);


            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();
            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                layers[i].Draw(spriteBatch, cameraPositionX, cameraPositionY, bottom);
            spriteBatch.End();




        }

        private void ScrollCamera(Viewport viewport)
        {
            const float ViewMargin = 0.35f;
			const float TopMargin = 0.3f;
			const float BottomMargin = 0.1f;

            //calculate the edges of the screen:
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPositionX + marginWidth;
            float marginRight = cameraPositionX + viewport.Width - marginWidth;

			//height for Y:
			float marginTop = cameraPositionY + viewport.Height * TopMargin;
			float marginBottom = cameraPositionY + viewport.Height - viewport.Height * BottomMargin;

            //calculate how far to scroll when the player is near the edges of the screen:
            float cameraMovementX = 0.0f;

            if (Player.Position.X < marginLeft)
                cameraMovementX = Player.Position.X - marginLeft;
            else if (Player.Position.X > marginRight)
                cameraMovementX = Player.Position.X - marginRight;

            //Update the camera position but prevent scrolling off the ends of the level:
            float maxCameraPositionX = Tile.Width * Width - viewport.Width / 3;
            cameraPositionX = MathHelper.Clamp(cameraPositionX + cameraMovementX, 0.0f, maxCameraPositionX);

			//calculate how far to scroll when the player is top the edges of the screen:
			float cameraMovementY = 0.0f;

			if (Player.Position.Y < marginTop)
				//above margin top
				cameraMovementY = Player.Position.Y - marginTop;
			else if (Player.Position.Y > marginBottom)
				//below marginBottom
				cameraMovementY = Player.Position.Y - marginBottom;

			float maxCameraPositionY = Tile.Height * Height - viewport.Height;
			cameraPositionY = MathHelper.Clamp(cameraPositionY + cameraMovementY, 0.0f, maxCameraPositionY); 
      
            //cameraPositionX = Vector2.Lerp(maxCameraPositionX, cameraMovementX, smoothingFactor * 45);
            //_pos = Vector2.Lerp(_pos, _targetPosition, smoothingFactor * seconds);
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            //calulate the visible range of tiles:
            int left = (int)Math.Floor(cameraPositionX / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);

            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        #endregion
    }
}
