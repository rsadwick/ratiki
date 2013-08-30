#region File Description
//-----------------------------------------------------------------------------
// DrawablePhysicsObject.cs
// Encapsulates farseer physics
// 
//-----------------------------------------------------------------------------
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FarseerPhysics.Factories;

namespace Platformer {
    class DrawablePhysicsObject {

        public Body Body { get; set; }

        public Vector2 Position {
            get {
                return CoordinateHelper.ToScreen(Body.Position);
            }
            set { Body.Position = CoordinateHelper.ToWorld(value); }
        }
        Vector2 position;
        private bool isUnit;

        public Texture2D texture;

        private Vector2 size;
        public Vector2 Size {
            get { return CoordinateHelper.ToScreen(size); }
            set { size = CoordinateHelper.ToWorld(value); }
        }

        public Level Level {
            get { return level; }
        }
        Level level;

        ///<summary>
        /// Creates a rectangular drawable physics object
        ///</summary>
        /// <param name="world">The farseer simulation this object should be part of</param>
        /// <param name="texture">The image that will be drawn at the place of the body</param>
        /// <param name="size">The size in pixels</param>
        /// <param name="mass">The mass in kilograms</param>
        public DrawablePhysicsObject(World world, string texture, Vector2 size, float mass, Level level) {
            Body = BodyFactory.CreateRectangle(world, size.X * CoordinateHelper.pixelToUnit, size.Y * CoordinateHelper.pixelToUnit, 1);
            Body.BodyType = BodyType.Dynamic;
            this.Size = size;
			Body.Mass = mass;
            this.texture = level.Content.Load<Texture2D>(texture);
        }

        /// <summary>
        /// Creates a circular drawable physics object
        /// </summary>
        /// <param name="world">The farseer simulation this object should be part of</param>
        /// <param name="texture">The image that will be drawn at the place of the body</param>
        /// <param name="diameter"> The diameter in pixels</param>
        /// <param name="mass">The mass in kilograms</param>
        public DrawablePhysicsObject(World world, string texture, float diameter, float mass, Level level) {
            size = new Vector2(diameter, diameter);
            Body = BodyFactory.CreateCircle(world, (diameter / 2.0f) * CoordinateHelper.pixelToUnit, 1);
            this.Size = size;
            this.texture = level.Content.Load<Texture2D>(texture);
        }

        public void Draw(SpriteBatch spriteBatch) {
            Rectangle destination = new Rectangle
            (
                (int) Position.X,
                (int) Position.Y,
                (int) Size.X,
                (int) Size.Y
            );

            spriteBatch.Draw(texture, destination, null, Color.White, Body.Rotation, new Vector2(texture.Width / 2.0f, texture.Height / 2.0f), SpriteEffects.None, 0);
        }
    }
}
