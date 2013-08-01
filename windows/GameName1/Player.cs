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
        private Crazy floor;
        private DrawablePhysicsObject farseerRect;

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

        //invulerable state:
        public bool IsInvulnerable;
        protected float invulnerableTimer = 0.0f;

        protected bool IsCharged;
        protected bool IsPowerJump;
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


        public bool IsOnWall {
            get { return isOnWall; }
            set { isOnWall = value; }
        }
        bool isOnWall;

        /// <summary>
        /// Current user movement input.
        /// </summary>
        private float movement;

        // Jumping state

        private bool isJumping;
        private bool wasJumping;

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
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Idle2"), 0.1f, true);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Run2"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Jump2"), 0.1f, false);
            duckAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Duck"), 0.1f, true);
            lookAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Up"), 0.1f, true);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int) (idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int) (idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerDeath");
            jumpSound = Level.Content.Load<SoundEffect>("Sounds/jumper");
            fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerDeath");
            powerUpSound = Level.Content.Load<SoundEffect>("Sounds/Secret");

            //farseer
            //floor = new Crazy(this.level.World, "Tiles/grass", new Vector2(25.0f, 35.0f), 0.1f, this.level, new Vector2(position.X, position.Y - 15), false, 134.0f, 0.4f); 
            farseerRect = new DrawablePhysicsObject(this.level.World, "Tiles/grass", new Vector2(25.0f, 35.0f), 0.1f, this.level);
            farseerRect.Body.BodyType = BodyType.Static; 
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

             // floor.Position = new Vector2(position.X, position.Y - 5);

           //  this.level.World.Step((float) gameTime.ElapsedGameTime.TotalSeconds);

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

            // Clear input.
            movement = 0.0f;
            isJumping = false;
            isDucking = false;
            isLooking = false;
            isOnWall = false;

           // floor.Position = new Vector2(position.X, position.Y);
            farseerRect.Position = new Vector2(position.X, position.Y - 15);
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
            movement = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if(Math.Abs(movement) < 0.5f)
                movement = 0.0f;

            // Move the player with accelerometer
            if(Math.Abs(accelState.Acceleration.Y) > 0.10f) {
                // set our movement speed
                movement = MathHelper.Clamp(-accelState.Acceleration.Y * AccelerometerScale, -1f, 1f);

                // if we're in the LandscapeLeft orientation, we must reverse our movement
                if(orientation == DisplayOrientation.LandscapeRight)
                    movement = -movement;
            }

            // If any digital horizontal movement input is found, override the analog movement.
            if(gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A)) {
                movement = -1.0f;
            }
            else if(gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D)) {
                movement = 1.0f;
            }

            // Check if the player wants to jump.
            isJumping =
                gamePadState.IsButtonDown(JumpButton) ||
                keyboardState.IsKeyDown(Keys.Space) ||
                keyboardState.IsKeyDown(Keys.W) ||
                touchState.AnyTouch();

            isWallJumping =
                gamePadState.IsButtonDown(JumpButton) && isOnWall ||
                keyboardState.IsKeyDown(Keys.Space) && keyboardState.IsKeyDown(Keys.Left) && isOnWall ||
                keyboardState.IsKeyDown(Keys.Space) && keyboardState.IsKeyDown(Keys.Right) && isOnWall;


            //check is player wants to duck:
            isDucking =
                gamePadState.IsButtonDown(DuckButton) ||
                keyboardState.IsKeyDown(Keys.Down);

            /*gamePadState.IsButtonDown(DuckButton) &&
            gamePadState.IsButtonUp(Buttons.DPadRight) ||
            keyboardState.IsKeyDown(Keys.Down) &&
            keyboardState.IsKeyUp(Keys.Right);*/

            isLooking =
                gamePadState.IsButtonDown(LookButton) ||
                keyboardState.IsKeyDown(Keys.Up);

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
                velocity.X += movement * MoveAcceleration * elapsed * 2;

            }
            else if(IsPoweredUp) {
                velocity.X += movement * MoveAcceleration * elapsed * 2;
            }
            else {
                velocity.X += movement * MoveAcceleration * elapsed;

            }
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

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
            else if(isDucking) {
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
                    sprite.PlayAnimation(jumpAnimation);

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
                    velocityY = JumpLaunchVelocity * (1.0f - (float) Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));

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
                    BoundingRectangle.Right <= movableTile.BoundingRectangle.Right + (BoundingRectangle.Width / 2))) {
                    movableTile.PlayerIsOn = true;
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

            //movable enemies:
            foreach(var movableEnemy in level.enemies) {
                //reset flag to search for movableTile collisions:
                movableEnemy.PlayerIsOn = false;

                //check to see if player is on enemy:
                if((BoundingRectangle.Bottom == movableEnemy.BoundingRectangle.Top + 1) &&
                    (BoundingRectangle.Left >= movableEnemy.BoundingRectangle.Left - (BoundingRectangle.Width / 2) &&
                    BoundingRectangle.Right <= movableEnemy.BoundingRectangle.Right + (BoundingRectangle.Width / 2))) {
                    movableEnemy.PlayerIsOn = true;
                }
                else if(BoundingRectangle.Right == movableEnemy.BoundingRectangle.Left) {
                    //position.X = movableEnemy.Position.X - movableEnemy.BoundingRectangle.Width * 2;


                    // velocity.X *= -1 / 2;
                    // position.X += velocity.X;

                    /* velocity.X *= -1;
                     velocity.Y *= 0;
                     position += velocity;*/

                    // Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, movableEnemy.BoundingRectangle);
                    // Position = new Vector2( movableEnemy.Position.X - movableEnemy.BoundingRectangle.Width * 2 + depth.X, Position.Y);

                    //IsInvulnerable = true;

                    //IsInvulerable = true;


                    //   Position = new Vector2(movableEnemy.Velocity);
                }

                bounds = HandleCollision(bounds, movableEnemy.Collision, movableEnemy.BoundingRectangle);
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
                            if(absDepthY < absDepthX || collision == TileCollision.Platform) {
                                // If we crossed the top of a tile, we are on the ground.
                                if(previousBottom <= tileBounds.Top)
                                    isOnGround = true;

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

            //phyrics


            // floor.Draw(spriteBatch);
            //farseerRect.Draw(spriteBatch);
            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, flip, color);

        }

        public void PowerUp() {
            powerUpTime = MaxPowerUpTime;
            powerUpSound.Play();
        }
    }
}
