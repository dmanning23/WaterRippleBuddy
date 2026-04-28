using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WaterRippleBuddy;

namespace WaterRippleBuddy.Example;

/// <summary>
/// Demo game: click anywhere to spawn a water ripple at the cursor position.
///
/// Render pipeline
/// ───────────────
///   1.  Game.Draw renders the background into _sceneTarget (a RenderTarget2D).
///   2.  WaterRippleComponent.Draw (called automatically via Components) reads
///       _sceneTarget, applies the distortion shader, and outputs directly to
///       the back buffer.
///   3.  Game.Draw does NOT draw _sceneTarget itself — the component owns that.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private Texture2D _background = null!;
    private RenderTarget2D _sceneTarget = null!;
    private WaterRippleComponent _water = null!;

    private MouseState _prevMouse;

    // ── Window size ──────────────────────────────────────────────────────────
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _water = new WaterRippleComponent(this)
        {
            // Draw after the game's own DrawOrder (default 0) so the scene
            // render target is populated before the component reads it.
            DrawOrder = 100,

            // Tweak these to taste:
            WaveSpeed = 1.5f,
            WaveFrequency = 40f,
            RefractionStrength = 0.03f,
            ReflectionStrength = 0.25f,
            ReflectionColor = new Color(200, 220, 255),  // soft blue highlight
            DropletLifetime = 2.5f
        };

        Components.Add(_water);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _background = Content.Load<Texture2D>("Braid_screenshot8");

        _sceneTarget = new RenderTarget2D(
            GraphicsDevice,
            ScreenWidth,
            ScreenHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None
        );

        // Tell the component which render target to distort.
        // This reference is valid for the lifetime of the game.
        _water.RenderTarget = _sceneTarget;
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        if (kb.IsKeyDown(Keys.Escape))
            Exit();

        // Spawn a droplet on the leading edge of a left-click (not on hold)
        if (mouse.LeftButton == ButtonState.Pressed &&
            _prevMouse.LeftButton == ButtonState.Released)
        {
            _water.AddDrop(new Vector2(mouse.X, mouse.Y));
        }

        _prevMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_sceneTarget);

        // Clear to Black
        _graphics.GraphicsDevice.Clear(Color.Black);

        //draw a simple scene
        _spriteBatch.Begin();
        _spriteBatch.Draw(_background, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
        _spriteBatch.End();

        //Draw the rest of the game components, including the WaterRippleComponent
        base.Draw(gameTime);

        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin();
        _spriteBatch.Draw(_sceneTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();
    }

    protected override void UnloadContent()
    {
        _sceneTarget?.Dispose();
        base.UnloadContent();
    }
}
