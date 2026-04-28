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
		/// <summary>
		/// xna junk
		/// </summary>
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		/// <summary>
		/// A texture that will be used to draw a simple scnene
		/// </summary>
		Texture2D _background;

		/// <summary>
		/// The main render target we are going to use instead of drawing straight to the screen
		/// </summary>
		RenderTarget2D sceneMap;

		/// <summary>
		/// Component used to listen for mouse input
		/// </summary>
		MouseComponent MouseInput;

		/// <summary>
		/// The componenent used to draw ater ripples
		/// </summary>
		WaterRippleComponent _water;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			Resolution.Init(graphics);
			IsMouseVisible = true;
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			//Setup for reolution independent rendering
			Resolution.SetDesiredResolution(1280, 720);
			Resolution.SetScreenResolution(1024,768, false);

			//Create the input things for listening for mouse clicks
			MouseInput = new MouseComponent(this, Resolution.ScreenToGameCoord);
			var debug = new DebugInputComponent(this, Resolution.TransformationMatrix);
			debug.DrawOrder = 100;

			//Create the water ripple component
			_water = new WaterRippleComponent(this);
			_water.DrawOrder = 101;

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
			_background = Content.Load<Texture2D>("Braid_screenshot8");

			Resolution.ResetViewport();

			//Create the render target to cover the whole screen
			PresentationParameters pp = GraphicsDevice.PresentationParameters;
			int width = pp.BackBufferWidth;
			int height = pp.BackBufferHeight;
			SurfaceFormat format = pp.BackBufferFormat;
			DepthFormat depthFormat = pp.DepthStencilFormat;
			sceneMap = new RenderTarget2D(GraphicsDevice, width, height, false, format, depthFormat);

			//Make sure the water ripples use the correct render target
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

			//Add a water ripple drop whenever the user clicks the window
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

			// Calculate Proper Viewport according to Aspect Ratio
			Resolution.ResetViewport();

			// Clear to Black
			graphics.GraphicsDevice.Clear(Color.Black);

			//draw a simple scene
			spriteBatch.Begin(SpriteSortMode.Immediate,
							  BlendState.AlphaBlend,
							  null, null, null, null,
							  Resolution.TransformationMatrix());
			spriteBatch.Draw(_background, Vector2.Zero, Color.White);
			spriteBatch.End();

			//Draw the rest of the game components
			base.Draw(gameTime);

			//finally, darw the completed scenemap rendertarget to the screen
			GraphicsDevice.SetRenderTarget(null);
			spriteBatch.Begin();
			spriteBatch.Draw(sceneMap, Vector2.Zero, Color.White);
			spriteBatch.End();
		}
	}
}
