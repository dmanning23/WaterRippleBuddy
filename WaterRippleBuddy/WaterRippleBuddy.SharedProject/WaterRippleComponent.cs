using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ResolutionBuddy;

namespace WaterRippleBuddy
{
	public class WaterRippleComponent : DrawableGameComponent, IWaterRipple
	{
		#region Properties

		SpriteBatch spriteBatch;

		Effect distortEffect;

		public RenderTarget2D RenderTarget { private get; set; }

		Texture2D gradTexture;

		Droplet droplet;
		public float waveSpeed = 1.25f;
		public float reflectionStrength = 1.7f;
		public Color reflectionColor = Color.Gray;
		public float refractionStrength = 2.5f;
		public float dropInterval = 1.5f;

		List<Droplet> Drops { get; set; }

		#endregion //Properties

		#region Methods

		public WaterRippleComponent(Game game) : base(game)
		{
			Drops = new List<Droplet>();
		}

		public override void Initialize()
		{
			spriteBatch = new SpriteBatch(Game.GraphicsDevice);

			base.Initialize();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			distortEffect = Game.Content.Load<Effect>("Distorter_Ripple");

			// Build Displacement texture
			Curve waveform = new Curve();

			waveform.Keys.Add(new CurveKey(0.00f, 0.50f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.05f, 1.00f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.15f, 0.10f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.25f, 0.80f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.35f, 0.30f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.45f, 0.60f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.55f, 0.40f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.65f, 0.55f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.75f, 0.46f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.85f, 0.52f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.99f, 0.50f, 0, 0));

			gradTexture = new Texture2D(GraphicsDevice, 256, 1, false, SurfaceFormat.Color);

			Color[] datas = new Color[256];

			for (var i = 0; i < gradTexture.Width; i++)
			{
				var x = 1.0f / gradTexture.Width * i;
				var a = waveform.Evaluate(x);
				datas[i] = new Color(a, a, a, a);
			}

			gradTexture.SetData<Color>(datas);
		}

		protected override void UnloadContent()
		{
			base.UnloadContent();

			spriteBatch = null;
			distortEffect = null;
			gradTexture = null;
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			//update all the drops
			for (int i = 0; i < Drops.Count; i++)
			{
				Drops[i].Update(gameTime);
			}

			//Remove any drops that have expired
			Drops.RemoveAll(x => !x.Timer.HasTimeRemaining());
		}

		public override void Draw(GameTime gameTime)
		{
			//are there any drops to render?
			if (0 < Drops.Count)
			{
				var gd1 = GraphicsDevice;

				Viewport viewport = GraphicsDevice.Viewport;

				float aspect = Resolution.ScreenArea.Width / Resolution.ScreenArea.Height;

				Matrix projection;
				Matrix.CreateOrthographicOffCenter(0, Resolution.ScreenArea.Width, Resolution.ScreenArea.Height, 0, 0, -1, out projection);
				Matrix _matrix = Matrix.Identity;//matrix use in spriteBatch.Draw

				Matrix.Multiply(ref _matrix, ref projection, out projection);

				distortEffect.Parameters["MatrixTransform"].SetValue(projection);
				distortEffect.Parameters["GradTexture"].SetValue(gradTexture);
				distortEffect.Parameters["_Reflection"].SetValue(reflectionColor.ToVector4());
				distortEffect.Parameters["_Params1"].SetValue(new Vector4(aspect, 1, 1 / waveSpeed, 0));    // [ aspect, 1, scale, 0 ]
				distortEffect.Parameters["_Params2"].SetValue(new Vector4(1, 1 / aspect, refractionStrength, reflectionStrength));    // [ 1, 1/aspect, refraction, reflection ]

				//Render the water ripple on top of the sceneMap rendertarget
				spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, DepthStencilState.None, null, distortEffect, Resolution.TransformationMatrix());

				//spriteBatch.Begin(SpriteSortMode.Immediate,
				//			  BlendState.AlphaBlend,
				//			  null, null, null, null,
				//			  Resolution.TransformationMatrix());

				for (int i = 0; i < Drops.Count; i++)
				{
					Vector2 scaleFactor = new Vector2(0.5f, 0.5f);
					Vector2 origin = new Vector2(Resolution.ScreenArea.Width / 2, Resolution.ScreenArea.Height / 2);

					distortEffect.Parameters["_Drop1"].SetValue(Drops[i].MakeShaderParameter(aspect));
					spriteBatch.Draw(RenderTarget, Drops[i].Position, null, Color.White, 0f, origin, scaleFactor, SpriteEffects.None, 0);
				}

				spriteBatch.End();
			}

			base.Draw(gameTime);
		}

		public void AddDrop(Vector2 position)
		{
			Drops.Add(new Droplet(position));
		}

		#endregion //Methods
	}
}
