using InputHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MouseBuddy;
using ResolutionBuddy;
using WaterRippleBuddy;

namespace WaterRippleBuddyExample.DesktopGL
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		Texture2D _texture;

		RenderTarget2D sceneMap;

		MouseComponent MouseInput;

		WaterRippleComponent _water;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			Resolution.Init(graphics);
			this.IsMouseVisible = true;
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			Resolution.SetDesiredResolution(1280, 720);
			Resolution.SetScreenResolution(1280, 720, false);

			MouseInput = new MouseComponent(this, ResolutionBuddy.Resolution.ScreenToGameCoord);

			var debug = new DebugInputComponent(this, Resolution.TransformationMatrix);
			debug.DrawOrder = 100;

			_water = new WaterRippleComponent(this);
			_water.DrawOrder = 101;
			Components.Add(_water);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);
			_texture = Content.Load<Texture2D>("Braid_screenshot8");

			PresentationParameters pp = GraphicsDevice.PresentationParameters;
			SurfaceFormat format = pp.BackBufferFormat;
			DepthFormat depthFormat = pp.DepthStencilFormat;

			// create textures for reading back the backbuffer contents
			sceneMap = new RenderTarget2D(GraphicsDevice, Resolution.ScreenArea.Width, Resolution.ScreenArea.Width, false, format, depthFormat);

			_water.RenderTarget = sceneMap;
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			if (sceneMap != null)
			{
				sceneMap.Dispose();
				sceneMap = null;
			}
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			// TODO: Add your update logic here

			if (MouseInput.MouseManager.LMouseClick)
			{
				_water.AddDrop(MouseInput.MousePos);
			}

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.SetRenderTarget(sceneMap);

			// Clear to Black
			graphics.GraphicsDevice.Clear(Color.Black);

			// Calculate Proper Viewport according to Aspect Ratio
			Resolution.ResetViewport();

			spriteBatch.Begin(SpriteSortMode.Immediate,
							  BlendState.AlphaBlend,
							  null, null, null, null,
							  Resolution.TransformationMatrix());

			spriteBatch.Draw(_texture, Vector2.Zero, Color.White);

			spriteBatch.End();

			base.Draw(gameTime);

			//finally, darw the completed scenemap rendertarget to the screen
			GraphicsDevice.SetRenderTarget(null);
			DrawFullscreenQuad(sceneMap, Resolution.ScreenArea.Width, Resolution.ScreenArea.Width, null);
		}

		/// <summary>
		/// Helper for drawing a texture into the current rendertarget,
		/// using a custom shader to apply postprocessing effects.
		/// </summary>
		void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect)
		{
			spriteBatch.Begin(0, BlendState.Opaque, null, null, null, effect);
			spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
			spriteBatch.End();
		}
	}
}
