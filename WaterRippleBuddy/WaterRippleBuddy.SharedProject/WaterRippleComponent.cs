using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ResolutionBuddy;
using System.Collections.Generic;

namespace WaterRippleBuddy
{
	public class WaterRippleComponent : DrawableGameComponent, IWaterRipple
	{
		#region Properties

		SpriteBatch spriteBatch;

		Effect distortEffect;

		private Vector2 _renderTargetOrigin;
		private RenderTarget2D _renderTarget;
		public RenderTarget2D RenderTarget
		{
			private get
			{
				return _renderTarget;
			}
			set
			{
				_renderTarget = value;
				_renderTargetOrigin = new Vector2(RenderTarget.Width / 2, RenderTarget.Height / 2);
			}
		}

		Texture2D gradTexture;

		public float waveSpeed = 1.25f;
		public float reflectionStrength = 1.7f;
		public Color reflectionColor = new Color(0, 0, 100);
		public float refractionStrength = 2.3f;
		public float dropInterval = 1.5f;

		List<Droplet> Drops { get; set; }

		#endregion //Properties

		#region Methods

		public WaterRippleComponent(Game game) : base(game)
		{
			Drops = new List<Droplet>();

			Game.Components.Add(this);
			Game.Services.AddService<IWaterRipple>(this);
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

				float aspect = gd1.Viewport.AspectRatio;

				Matrix projection;
				Matrix.CreateOrthographicOffCenter(0, gd1.Viewport.Width, gd1.Viewport.Height, 0, 0, -1, out projection);
				Matrix matrix = Resolution.TransformationMatrix();//matrix use in spriteBatch.Draw
				Matrix.Multiply(ref matrix, ref projection, out projection);

				distortEffect.Parameters["MatrixTransform"].SetValue(projection);
				distortEffect.Parameters["GradTexture"].SetValue(gradTexture);
				distortEffect.Parameters["_Reflection"].SetValue(reflectionColor.ToVector4());
				distortEffect.Parameters["Aspect"].SetValue(aspect);
				distortEffect.Parameters["Scale"].SetValue(1f / waveSpeed);
				distortEffect.Parameters["RefractionStrength"].SetValue(refractionStrength);
				distortEffect.Parameters["ReflectionStrength"].SetValue(reflectionStrength);
				
				distortEffect.Parameters["_Params2"].SetValue(new Vector4(1, 1 / aspect, refractionStrength, reflectionStrength));    // [ 1, 1/aspect, refraction, reflection ]

				//Render the water ripple on top of the sceneMap rendertarget
				spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, DepthStencilState.None, null, distortEffect, Resolution.TransformationMatrix());

				for (int i = 0; i < Drops.Count; i++)
				{
					Vector2 scaleFactor = new Vector2(0.5f, 0.5f);

					distortEffect.Parameters["_Drop1"].SetValue(Drops[i].MakeShaderParameter(aspect));
					spriteBatch.Draw(RenderTarget, Drops[i].Position, null, Color.White, 0f, _renderTargetOrigin, scaleFactor, SpriteEffects.None, 0);
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
