#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using FarseerPhysics.Dynamics;
using System.Collections.Generic;

namespace Platformer {
    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player {
        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation duckAnimation;
        private Animation lookAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;
        private Animation ladderUpAnimation;
        private Animation ladderDownAnimation;
		private Animation upWardThrustAnimation;
		private Animation downWardThrustAnimation;
        private SpriteEffects flip = SpriteEffects.None;
        private AnimationPlayer sprite;
        private const float HOLD_TIMESPAN = 0.50f;
        private const float CHARGE_JUMP_TIMESPAN = 1.90f;
        private const float DAMAGE_INVULNERABLE_TIMESPAN = 1.0f;
        private float holdTimer;
        // Sounds
        private SoundEffect killedSound;
        private SoundEffect jumpSound;
        private SoundEffect fallSound;
        private DrawablePhysicsObject farseerRect;

		//Particle engine
		private ParticleEngine particleEngine;

        public Level Level {
            get { return level; }
        }
        Level level;

        public bool IsAlive {
            get { return isAlive; }
        }
        bool isAlive;

        //player lives
        public int Lives {
            get { return lives; }
            set { lives = value; }
        }

        int lives = 3;

        //Powerup states:
        private const float MaxPowerUpTime = 9.0f;
        private float powerUpTime;
        public bool IsPoweredUp {
            get { return powerUpTime > 0.0f; }
        }

		public bool IsDownwardThrusting {
			get { return isDownwardThrust ; }
		}

        //invulerable state:
        public bool IsInvulnerable;
        protected float invulnerableTimer = 0.0f;

        protected bool IsCharged;
        //Array of colors to cycle through for power up animation:
        private readonly Color[] poweredUpColors = {
                                                       Color.Peru,
                                                       Color.Yellow,
                                                       Color.Green,
                                                       Color.PaleVioletRed,
                                                       Color.Tomato,
                                                   };
        private readonly Color[] chargedUpColors = {
                                                       Color.YellowGreen,
                                                       Color.Yellow,
                                                       Color.WhiteSmoke,
                                                       Color.Navy,
                                                       Color.Gold,
                                                   };

        private readonly Color[] invulerableColors = {
                                                       Color.Gray,
                                                       Color.Black,
                                                       Color.White,
                                                   };

        private SoundEffect powerUpSound;

        // Physics state
        public Vector2 Position {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        public Vector2 Velocity {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        // Constants for controling horizontal movement
        private float MoveAcceleration = 13000.0f;
        private float MaxMoveSpeed = 1750.0f;
        private const float GroundDragFactor = 0.48f;
        private const float AirDragFactor = 0.58f;

        // Constants for controlling vertical movement
        private float MaxJumpTime = 0.40f;
        private const float JumpLaunchVelocity = -2600.0f;
        private const float GravityAcceleration = 2600.0f;
        private const float MaxFallSpeed = 450.0f;
        private float JumpControlPower = 0.09f;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;
        private const Buttons DuckButton = Buttons.LeftThumbstickDown;
        private const Buttons LookButton = Buttons.LeftThumbstickUp;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround {
            get { return isOnGround; }
        }
        bool isOnGround;

        //Wall jumping
        public bool IsOnWall {
            get { return isOnWall; }
            set { isOnWall = value; }
        }
        bool isOnWall;
        
        //Ladders & climbing
        private const int LadderAlignment = 12;
        private bool isClimbing;
        public bool IsClimbing
        {
            get { return isClimbing;}
        }

        private bool wasClimbing;
        private Vector2 movement;

		public SpriteEffects Direction 
		{
			get { return flip; }
		}

        /// <summary>
        /// Current user movement input.
        /// </summary>
       // private float movement;

        // Jumping state

        private bool isJumping;
        private bool wasJumping;

		private bool isUpwardThust;
		private bool isDownwardThrust;

        private bool isWallJumping;

        private float jumpTime;

        //Ducking
        private bool isDucking;

        //Looking Up
        private bool isLooking;

        private Rectangle localBounds;

        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public Rectangle BoundingRectangle {
            get {
                int left = (int) Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int) Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        /// <summary>
        /// Constructors a new player.
        /// </summary>
        public Player(Level level, Vector2 position) {
            this.level = level;
            LoadContent();
            Reset(position);
           
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent() {
            // Load animated textures.
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/idle_new"), 0.1f, true);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/run_new"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/jump_new"), 0.1f, false);
            duckAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/duck_new"), 0.1f, false);
            lookAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/up_new"), 0.1f, true);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/win_new"), 0.1f, false);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false);
            ladderUpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/climb_new"), 0.1f, true);
			ladderDownAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/climb_new"), 0.1f, true);
			upWardThrustAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/up_thrust"), 0.1f, false);
			downWardThrustAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/down_thrust"), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int) (idleAnimation.FrameWidth * 1.0);

            int left = (idleAnimation.FrameWidth - width) / 2;

            int height = (int) (idleAnimation.FrameWidth * 1.0);
			Console.WriteLine (height);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerDeath");
            jumpSound = Level.Content.Load<SoundEffect>("Sounds/jumper");
            fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerDeath");
            powerUpSound = Level.Content.Load<SoundEffect>("Sounds/Secret");

            //farseer body on player:
			farseerRect = new DrawablePhysicsObject(this.level.World, "Tiles/grass", new Vector2(width, height), 4.1f, this.level);
            farseerRect.Body.BodyType = BodyType.Static;


			//Particles:
			List<Texture2D> textures = new List<Texture2D>();
			textures.Add(Level.Content.Load<Texture2D>("Particles/circle"));
			textures.Add(Level.Content.Load<Texture2D>("Particles/star"));
			textures.Add(Level.Content.Load<Texture2D>("Particles/diamond"));
			particleEngine = new ParticleEngine(textures, new Vector2(position.X, position.Y));
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position) {
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            powerUpTime = 0.0f;
            sprite.PlayAnimation(idleAnimation);
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        /// <remarks>
        /// We pass in all of the input states so that our game is only polling the hardware
        /// once per frame. We also pass the game's orientation because when using the accelerometer,
        /// we need to reverse our motion when the orientation is in the LandscapeRight orientation.
        /// </remarks>
        public void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            GamePadState gamePadState,
            TouchCollection touchState,
            AccelerometerState accelState,
            DisplayOrientation orientation) {
            
            GetInput(keyboardState, gamePadState, touchState, accelState, orientation);

            ApplyPhysics(gameTime);

           //  this.level.World.Step((float) gameTime.ElapsedGameTime.TotalSeconds);

            //Alive and on ground:
            if(IsAlive && IsOnGround) {
                if(Math.Abs(Velocity.X) - 0.02f > 0) {
                    sprite.PlayAnimation(runAnimation);
                }
                else if(IsAlive && IsOnGround && isDucking) {
                    sprite.PlayAnimation(duckAnimation);
                }
                else if(isAlive && isOnGround && isLooking) {
                    sprite.PlayAnimation(lookAnimation);
                }
                else {
                    sprite.PlayAnimation(idleAnimation);
                }
            }
            //Alive and climbing:
            else if(IsAlive && !IsOnGround && IsClimbing)
            {
                if(Velocity.Y - 0.02f > 0)
                    sprite.PlayAnimation(ladderDownAnimation);
                else
                    sprite.PlayAnimation(ladderUpAnimation);
            }
                
            // Clear input.
            movement = Vector2.Zero;
            isJumping = false;
            isDucking = false;
            isLooking = false;
            isOnWall = false;
            isClimbing = false;
			isUpwardThust = false;
			//isDownwardThrust = false;

            //update physics rectangle:
            farseerRect.Position = new Vector2(position.X, position.Y - 20 );

			//Particles
			if (IsCharged) {
				particleEngine.EmitterLocation = new Vector2 (position.X, position.Y);
				particleEngine.Update ();
			} else {
				particleEngine.RemoveParticle();
			}
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput(
            KeyboardState keyboardState,
            GamePadState gamePadState,
            TouchCollection touchState,
            AccelerometerState accelState,
            DisplayOrientation orientation) {
            // Get analog horizontal movement.
            movement.X = gamePadState.ThumbSticks.Left.X * MoveStickScale;
            movement.Y = gamePadState.ThumbSticks.Left.Y * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if(Math.Abs(movement.X) < 0.5f)
                movement.X = 0.0f;
            if(Math.Abs(movement.Y) < 0.5f)
                movement.Y = 0.0f;

            // If any digital horizontal movement input is found, override the analog movement.
            if(gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A)) {
                  movement.X = -1.0f;
            }
            else if(gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D)) {
                  movement.X = 1.0f;
            }
            
            //spawn crates for testing:
            if(keyboardState.IsKeyDown(Keys.P))
            {
                level.SpawnCrate();
            }

            //spawn crates for testing:
            if(keyboardState.IsKeyDown(Keys.O)) {
                level.SpawnTrampoline();
            }

            // Check if the player wants to jump.
            isJumping =
				gamePadState.IsButtonDown(JumpButton) ||
					keyboardState.IsKeyDown(Keys.Space)   ||
					keyboardState.IsKeyDown(Keys.W) ||
                touchState.AnyTouch();

			isUpwardThust = 
				gamePadState.IsButtonDown (JumpButton) && keyboardState.IsKeyDown (Keys.Up) ||
				keyboardState.IsKeyDown (Keys.Space) && keyboardState.IsKeyDown (Keys.Up) ||
				keyboardState.IsKeyDown (Keys.W) && keyboardState.IsKeyDown (Keys.Up);

			isDownwardThrust = 
				gamePadState.IsButtonDown (JumpButton) && keyboardState.IsKeyDown (Keys.Down) && !IsCharged ||
					keyboardState.IsKeyDown (Keys.Space) && keyboardState.IsKeyDown (Keys.Down) && !IsCharged ||
					keyboardState.IsKeyDown (Keys.W) && keyboardState.IsKeyDown (Keys.Down) && !IsCharged; 

            isWallJumping =
                gamePadState.IsButtonDown(JumpButton) && isOnWall ||
                keyboardState.IsKeyDown(Keys.Space) && keyboardState.IsKeyDown(Keys.Left) && isOnWall ||
                keyboardState.IsKeyDown(Keys.Space) && keyboardState.IsKeyDown(Keys.Right) && isOnWall;

            isLooking =
                gamePadState.IsButtonDown(LookButton) ||
                keyboardState.IsKeyDown(Keys.Up);

            //Ladders:
            if(gamePadState.IsButtonDown(Buttons.DPadUp) ||
               keyboardState.IsKeyDown(Keys.Up) ||
               keyboardState.IsKeyDown(Keys.W))
               {
                   isClimbing = false;

                   if(IsAlignedToLadder())
                   {
                        //We need to check the tile behind the player:
                        if(level.GetTileCollisionBehindPlayer(position) == TileCollision.Ladder)
                        {
                            isClimbing = true;
                            isJumping = false;
                            isDucking = false;
                            isWallJumping = false;
                            isOnGround = false;
							isUpwardThust = false;
							isDownwardThrust = false;
                            movement.Y = -0.2f;
                        }
                   }
                }
                
                else if(gamePadState.IsButtonDown(DuckButton) ||
                    keyboardState.IsKeyDown(Keys.Down) ||
                    keyboardState.IsKeyDown(Keys.S))
                {
                    isClimbing = false;
                    //check for ladder:
                    if(IsAlignedToLadder())
                    {
                        if(level.GetTileCollisionBelowPlayer(level.Player.Position) == TileCollision.Ladder)
                        {
                            isClimbing = true;
                            isJumping = false;
                            isDucking = false;
                            isWallJumping = false;
                            isOnGround = false;
							isUpwardThust = false;
							isDownwardThrust = false;
                            movement.Y = 0.2f;
                        }
                    }
                    //if not on a ladder, duck:
                    else 
                    {
                        isDucking = true;
                    }
                }      
        }

        /// <summary>
        /// Ladder logic.
        /// Figure out if the player is aligned on the ladder tile
        /// </summary>
        /// 
        
        private bool IsAlignedToLadder()
        {
            int playerOffset = ((int)position.X % Tile.Width) - Tile.Center;

            if(Math.Abs(playerOffset) <= LadderAlignment && 
                                         level.GetTileCollisionBelowPlayer( new Vector2(level.Player.position.X, 
                                                                                         level.Player.position.Y + 1 )) == TileCollision.Ladder ||
                                         level.GetTileCollisionBehindPlayer(new Vector2(level.Player.position.X,
                                                                                         level.Player.position.Y -1)) == TileCollision.Ladder)
            {
                //align the player with the middle of tile:
                position.X -= playerOffset;
                return true;
            }
            else
            {
                return false;
            }    
        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime) {
            float elapsed = (float) gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            if(IsCharged) {
                velocity.X += movement.X * MoveAcceleration * elapsed * 2;

            }
            else if(IsPoweredUp) {
                velocity.X += movement.X * MoveAcceleration * elapsed * 2;
            }

            if(!isClimbing) {
                if(wasClimbing)
                    velocity.Y = 0;
                else
                    velocity.Y = MathHelper.Clamp(
                        velocity.Y + GravityAcceleration * elapsed,
                        -MaxFallSpeed,
                        MaxFallSpeed);
            }
            else {
                velocity.Y = movement.Y * MoveAcceleration * elapsed;
            }


            velocity.X += movement.X * MoveAcceleration * elapsed;
            velocity.Y = DoJump(velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if(IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity.
            Position += velocity * elapsed;
            Position = new Vector2((float) Math.Round(Position.X), (float) Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions(gameTime);

            // If the collision stopped us from moving, reset the velocity to zero.
            if(Position.X == previousPosition.X)
                velocity.X = 0;

            if(Position.Y == previousPosition.Y)
                velocity.Y = 0;

            //Check Power up:
            if(IsPoweredUp) {
                powerUpTime = Math.Max(0.0f, powerUpTime - (float) gameTime.ElapsedGameTime.TotalSeconds);
                MaxJumpTime = 0.99f;
            }
            else if(IsInvulnerable) {

                invulnerableTimer += (float) gameTime.ElapsedGameTime.TotalSeconds;
                if(invulnerableTimer > DAMAGE_INVULNERABLE_TIMESPAN) {
                    IsInvulnerable = false;
                }

            }
            else if(isDucking && !isDownwardThrust) {
                //Jump Booster:
                holdTimer += (float) gameTime.ElapsedGameTime.TotalSeconds;
                // Console.WriteLine("time : " + holdTimer);
                if(holdTimer > HOLD_TIMESPAN && holdTimer < CHARGE_JUMP_TIMESPAN) {
                    IsCharged = true;

                }
                else {
                    IsCharged = false;

                }
            }

            else {
                //reset all:
                IsCharged = false;
                holdTimer = 0.0f;
                MaxJumpTime = 0.40f;
                JumpControlPower = 0.09f;
                invulnerableTimer = 0.0f;


            }

            
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime) {

            // If the player wants to jump
            if(isJumping) {

                // Begin or continue a jump
                if((!wasJumping && IsOnGround) || jumpTime > 0.0f) {
                    if(jumpTime == 0.0f)
                        jumpSound.Play();

                    jumpTime += (float) gameTime.ElapsedGameTime.TotalSeconds;
                    
					if (isUpwardThust)
						sprite.PlayAnimation (upWardThrustAnimation);
					else if (isDownwardThrust) {
						sprite.PlayAnimation (downWardThrustAnimation);
					}
					else
					{
						sprite.PlayAnimation(jumpAnimation);
					}

                }


                // If we are in the ascent of the jump
                if(0.0f < jumpTime && jumpTime <= MaxJumpTime) {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    if(IsCharged) {
                        velocityY = JumpLaunchVelocity * (1.0f - (float) Math.Pow(jumpTime / MaxJumpTime, JumpControlPower * 2));
                    }

                    else {
                        velocityY = JumpLaunchVelocity * (1.0f - (float) Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));

                    }
                }

                else if((!wasJumping && isWallJumping)) {
                    isWallJumping = false;
                    jumpSound.Play();
                    Console.WriteLine(jumpTime);
                    //velocity = -position * 5;
                    velocityY = JumpLaunchVelocity * (1.0f + (float) Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));

                }

				else if(!wasJumping && isDownwardThrust)
				{
					isDownwardThrust = false;
				}

                else {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;

                }
            }

            else {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;

            }
            wasJumping = isJumping;


            return velocityY;
        }

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        private void HandleCollisions(GameTime gameTime) {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int) Math.Floor((float) bounds.Left / Tile.Width);
            int rightTile = (int) Math.Ceiling(((float) bounds.Right / Tile.Width)) - 1;
            int topTile = (int) Math.Floor((float) bounds.Top / Tile.Height);
            int bottomTile = (int) Math.Ceiling(((float) bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            isOnGround = false;

            //movable tiles:
            foreach(var movableTile in level.movableTiles) {
                //reset flag to search for movableTile collisions:
                movableTile.PlayerIsOn = false;

                //check to see if player is on tile:
                if((BoundingRectangle.Bottom == movableTile.BoundingRectangle.Top + 1) &&
                    (BoundingRectangle.Left >= movableTile.BoundingRectangle.Left - (BoundingRectangle.Width / 2) &&
                    BoundingRectangle.Right <= movableTile.BoundingRectangle.Right + (BoundingRectangle.Width / 2))) 
				{
                    movableTile.PlayerIsOn = true;
					//player is charged, speed up plaform and change direction according to player direction:
					if (IsCharged) {
						movableTile.MoveSpeed = 120.0f * 3;
						if(flip == SpriteEffects.FlipHorizontally)
							movableTile.Direction = FaceDirection.Right;
						else
							movableTile.Direction = FaceDirection.Left;

					} else {
						movableTile.MoveSpeed = 120.0f;
					}
                }

                bounds = HandleCollision(bounds, movableTile.Collision, movableTile.BoundingRectangle);
            }


            //Wall Jumping:
            //movable tiles:
            foreach(var wallTile in level.wallTiles) {
                //reset flag to search for movableTile collisions:
                wallTile.PlayerIsOn = false;
                if(BoundingRectangle.Right == wallTile.BoundingRectangle.Left || BoundingRectangle.Left == wallTile.BoundingRectangle.Right) {
                    wallTile.PlayerIsOn = true;

                }

                bounds = HandleCollision(bounds, wallTile.Collision, wallTile.BoundingRectangle);
            }

			if(!isDownwardThrust){
            	//movable enemies:
	            foreach(var movableEnemy in level.enemies) {
	                //reset flag to search for movableTile collisions:
	                movableEnemy.PlayerIsOn = false;
					//movableEnemy.PlayerIsAttackingTop = false;

	                //check to see if player is on enemy:
				if ( movableEnemy.IsAlive && (BoundingRectangle.Bottom == movableEnemy.BoundingRectangle.Top + 1) &&
						(BoundingRectangle.Left >= movableEnemy.BoundingRectangle.Left - (BoundingRectangle.Width / 2) &&
					 BoundingRectangle.Right <= movableEnemy.BoundingRectangle.Right + (BoundingRectangle.Width / 2))) 
					{
						movableEnemy.PlayerIsOn = true;
			
					}

					bounds = HandleCollision(bounds, movableEnemy.Collision, movableEnemy.BoundingRectangle);
				}	   
            }
			else if(isDownwardThrust)
			{
				foreach(var movableEnemy in level.enemies) {
					//movableEnemy.PlayerIsAttackingTop = false;
					movableEnemy.PlayerIsOn = false;
					if (movableEnemy.IsAlive && (BoundingRectangle.Bottom == movableEnemy.BoundingRectangle.Top + 1) &&
				        (BoundingRectangle.Left >= movableEnemy.BoundingRectangle.Left - (BoundingRectangle.Width) &&
				 		BoundingRectangle.Right <= movableEnemy.BoundingRectangle.Right + (BoundingRectangle.Width))) 
					{
						movableEnemy.PlayerIsAttackingTop = true;
					}
					bounds = HandleCollision(bounds, movableEnemy.Collision, movableEnemy.BoundingRectangle);
				}
			}

            // For each potentially colliding tile,
            for(int y = topTile; y <= bottomTile; ++y) {
                for(int x = leftTile; x <= rightTile; ++x) {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);
                    if(collision != TileCollision.Passable) {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if(depth != Vector2.Zero) {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if(absDepthY < absDepthX || collision == TileCollision.Platform ) {
                                // If we crossed the top of a tile, we are on the ground.
                                //LADDER
                                // If we crossed the top of a tile, we are on the ground.
                                if(previousBottom <= tileBounds.Top) {
									if (collision == TileCollision.Ladder) {
										if (!isClimbing && !isJumping) {
											// When walking over a ladder
											isOnGround = true;
										}
									} 
                                    else {
                                        isOnGround = true;
                                        isClimbing = false;
                                        isJumping = false;
                                        isWallJumping = false;
                                    }
                                }

                                // Ignore platforms, unless we are on the ground.
                                if(collision == TileCollision.Impassable || IsOnGround) {
                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if(collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                            else if(collision == TileCollision.Ladder && !isClimbing)
                            {
                                //when walking in front of a ladder, falling off a ladder but not climbing:
                                //Resolve the collision along the Y axis:
                                Position = new Vector2(Position.X, Position.Y);
                                bounds = BoundingRectangle;
                            }


                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }


        private Rectangle HandleCollision(Rectangle bounds, TileCollision collision, Rectangle tileBounds) {
            Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
            if(depth != Vector2.Zero) {
                float absDepthX = Math.Abs(depth.X);
                float absDepthY = Math.Abs(depth.Y);
                // Resolve the collision along the shallow axis.
				if(absDepthY < absDepthX || collision == TileCollision.Platform) {
                    // If we crossed the top of a tile, we are on the ground.
                    if(previousBottom <= tileBounds.Top)
                        isOnGround = true;
                    // Ignore platforms, unless we are on the ground.
					if(collision == TileCollision.Impassable || IsOnGround ) {
                        // Resolve the collision along the Y axis.
                        Position = new Vector2(Position.X, Position.Y + depth.Y);
                        // Perform further collisions with the new bounds.
                        bounds = BoundingRectangle;
                    }
                }
                else if(collision == TileCollision.Impassable) // Ignore platforms.
        {
                    // Resolve the collision along the X axis.
                    Position = new Vector2(Position.X + depth.X, Position.Y);
                    // Perform further collisions with the new bounds.
                    bounds = BoundingRectangle;
                }
            }
            return bounds;
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This parameter is null if the player was
        /// not killed by an enemy (fell into a hole).
        /// </param>
        public void OnKilled(Enemy killedBy) {
            isAlive = false;

            if(killedBy != null)
                killedSound.Play();
            else
                fallSound.Play();

            sprite.PlayAnimation(dieAnimation);
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit() {
            sprite.PlayAnimation(celebrateAnimation);
        }


        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            // Flip the sprite to face the way we are moving.
            if(Velocity.X > 0)
                flip = SpriteEffects.FlipHorizontally;
            else if(Velocity.X < 0)
                flip = SpriteEffects.None;

            //Calculate a tiny color based on power up state:
            Color color;
            if(IsPoweredUp) {
                float t = ((float) gameTime.TotalGameTime.TotalSeconds + powerUpTime / MaxPowerUpTime) * 20.0f;
                int colorIndex = (int) t % poweredUpColors.Length;
                color = poweredUpColors[colorIndex];
            }
            else if(IsCharged) {
                float t = ((float) gameTime.TotalGameTime.TotalSeconds + holdTimer / HOLD_TIMESPAN) * 20.0f;
                int colorIndex = (int) t % chargedUpColors.Length;
                color = chargedUpColors[colorIndex];


            }
            else if(IsInvulnerable) {
                float t = ((float) gameTime.TotalGameTime.TotalSeconds + powerUpTime / DAMAGE_INVULNERABLE_TIMESPAN) * 20.0f;
                int colorIndex = (int) t % invulerableColors.Length;
                color = new Color(invulerableColors[colorIndex], 0);
            }
            else {
                color = Color.White;
            }
            
            //show farseer rect:
            //farseerRect.Draw(spriteBatch);
			//particles:
			particleEngine.Draw (spriteBatch);
            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, flip, color);

        }

        public void PowerUp() {
            powerUpTime = MaxPowerUpTime;
            powerUpSound.Play();
        }
    }
}
