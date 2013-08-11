#region File Description
//-----------------------------------------------------------------------------
// ParticleEngine.cs
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
	public class ParticleEngine
	{
		private Random random;
		public Vector2 EmitterLocation { get; set; }
		private List<Particle> particles;
		private List<Texture2D> textures;

		public ParticleEngine(List<Texture2D> textures, Vector2 location)
		{
			EmitterLocation = location;
			this.textures = textures;
			this.particles = new List<Particle>();
			random = new Random();
		}

		public void Update()
		{
			int total = 10;

			for (int i = 0; i < total; i++)
			{
				particles.Add(GenerateNewParticle());
			}

			RemoveParticle();
		}

		private Particle GenerateNewParticle()
		{
			Texture2D texture = textures[random.Next(textures.Count)];
			Vector2 position = EmitterLocation;
			Vector2 velocity = new Vector2(
				1f * (float)(random.NextDouble() * 2 - 1),
				1f * (float)(random.NextDouble() * 2 - 1));
			float angle = 0;
			float angularVelocity = 0.1f * (float)(random.NextDouble() * 2 - 1);
			Color color = new Color(
				(float)random.NextDouble(),
				(float)random.NextDouble(),
				(float)random.NextDouble(),
				0.3f);
			float size = (float)random.NextDouble();
			int life = 12 + random.Next(24);

			return new Particle(texture, position, velocity, angle, angularVelocity, color, size, life);
		}

		public void RemoveParticle()
		{
			for (int particle = 0; particle < particles.Count; particle++)
			{
				particles[particle].Update();
				if (particles[particle].Life <= 0)
				{
					particles.RemoveAt(particle);
					particle--;
				}
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.GraphicsDevice.BlendState = BlendState.Additive;
			for (int index = 0; index < particles.Count; index++)
			{
				particles[index].Draw(spriteBatch);
			}
			spriteBatch.GraphicsDevice.BlendState = BlendState.AlphaBlend;
		}
	}
}
