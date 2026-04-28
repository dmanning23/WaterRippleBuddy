using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaterRippleBuddy;

/// <summary>
/// A <see cref="DrawableGameComponent"/> that renders an animated water-ripple
/// distortion effect over a scene captured in a <see cref="RenderTarget2D"/>.
///
/// <para><b>Integration pattern:</b></para>
/// <list type="number">
///   <item>Add to game components: <c>Components.Add(waterRipple)</c></item>
///   <item>
///     In <c>Draw</c>, render your scene to a <see cref="RenderTarget2D"/> and
///     assign it: <c>waterRipple.RenderTarget = myTarget</c>
///   </item>
///   <item>
///     The component draws the distorted result to the back buffer automatically.
///     Do <b>not</b> draw the render target yourself.
///   </item>
///   <item>Call <see cref="AddDrop"/> to spawn ripples.</item>
/// </list>
///
/// <para>
/// The shader must be compiled by your game's content pipeline.  Copy
/// <c>WaterRippleBuddy/Content/Distorter_Ripple.fx</c> into your game's
/// <c>Content/</c> folder and add it to <c>Content.mgcb</c>.
/// </para>
/// </summary>
public sealed class WaterRippleComponent : DrawableGameComponent, IWaterRipple
{
    // Maximum simultaneous droplets the shader supports.
    // Changing this requires updating MAX_DROPLETS in Distorter_Ripple.fx too.
    private const int MaxDroplets = 16;

    // ── Effect / rendering ───────────────────────────────────────────────────
    private Effect? _rippleEffect;
    private SpriteBatch? _spriteBatch;
    private readonly string _shaderContentPath;

    // Cached parameter handles (avoids dictionary lookup every frame)
    private EffectParameter? _paramSceneTexture;
    private EffectParameter? _paramDropletData;
    private EffectParameter? _paramDropletCount;
    private EffectParameter? _paramRefractionStrength;
    private EffectParameter? _paramReflectionStrength;
    private EffectParameter? _paramReflectionColor;
    private EffectParameter? _paramWaveSpeed;
    private EffectParameter? _paramWaveFrequency;
    private EffectParameter? _paramAspectRatio;

    // Reused buffer — avoids per-frame allocation
    private readonly Vector4[] _dropletDataBuffer = new Vector4[MaxDroplets];
    private readonly List<Droplet> _droplets = new(MaxDroplets);

    // ── Public API ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public RenderTarget2D? RenderTarget { get; set; }

    /// <summary>
    /// How fast the wave pattern travels outward across the screen.
    /// Higher values = faster, more energetic ripples. Default: <c>1.5</c>.
    /// </summary>
    public float WaveSpeed { get; set; } = 1.5f;

    /// <summary>
    /// Controls ring spacing — higher values produce tighter, more numerous
    /// rings. Default: <c>40</c>.
    /// </summary>
    public float WaveFrequency { get; set; } = 40f;

    /// <summary>
    /// Intensity of the UV distortion (how far the image bends under a wave).
    /// Default: <c>0.03</c>.
    /// </summary>
    public float RefractionStrength { get; set; } = 0.03f;

    /// <summary>
    /// How strongly the reflection tint blends in at wave crests. Default: <c>0.25</c>.
    /// </summary>
    public float ReflectionStrength { get; set; } = 0.25f;

    /// <summary>
    /// The color blended in at wave crests. Default: <c>Color.White</c>
    /// (produces a bright highlight; set to a blue tint for a water feel).
    /// </summary>
    public Color ReflectionColor { get; set; } = Color.White;

    /// <summary>
    /// How long (in seconds) each droplet's ripple lasts before fading out.
    /// Default: <c>2.0</c>.
    /// </summary>
    public float DropletLifetime { get; set; } = 2.0f;

    // ── Constructor ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="WaterRippleComponent"/>.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> this component belongs to.</param>
    /// <param name="shaderContentPath">
    ///   Content-pipeline path to the compiled ripple effect.
    ///   Defaults to <c>"Distorter_Ripple"</c> (i.e. the root of the
    ///   Content directory).
    /// </param>
    public WaterRippleComponent(Game game, string shaderContentPath = "Distorter_Ripple")
        : base(game)
    {
        _shaderContentPath = shaderContentPath;
    }

    // ── IWaterRipple ─────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a ripple at <paramref name="screenPosition"/> (pixel coordinates).
    /// Silently ignored when the maximum of <c>16</c> concurrent droplets is reached.
    /// </summary>
    public void AddDrop(Vector2 screenPosition)
    {
        if (_droplets.Count < MaxDroplets)
            _droplets.Add(new Droplet(screenPosition, DropletLifetime));
    }

    // ── DrawableGameComponent overrides ──────────────────────────────────────

    protected override void LoadContent()
    {
        _rippleEffect = Game.Content.Load<Effect>(_shaderContentPath);
        _spriteBatch  = new SpriteBatch(GraphicsDevice);

        // Cache parameter handles once at load time
        _paramSceneTexture       = _rippleEffect.Parameters["SceneTexture"];
        _paramDropletData        = _rippleEffect.Parameters["DropletData"];
        _paramDropletCount       = _rippleEffect.Parameters["DropletCount"];
        _paramRefractionStrength = _rippleEffect.Parameters["RefractionStrength"];
        _paramReflectionStrength = _rippleEffect.Parameters["ReflectionStrength"];
        _paramReflectionColor    = _rippleEffect.Parameters["ReflectionColor"];
        _paramWaveSpeed          = _rippleEffect.Parameters["WaveSpeed"];
        _paramWaveFrequency      = _rippleEffect.Parameters["WaveFrequency"];
        _paramAspectRatio        = _rippleEffect.Parameters["AspectRatio"];

        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Iterate backwards so RemoveAt doesn't skip items
        for (int i = _droplets.Count - 1; i >= 0; i--)
        {
            _droplets[i].Update(delta);
            if (_droplets[i].IsExpired)
                _droplets.RemoveAt(i);
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        if (RenderTarget == null || _rippleEffect == null || _spriteBatch == null)
            return;

        var viewport    = GraphicsDevice.Viewport;
        float aspect    = (float)viewport.Width / viewport.Height;
        int   count     = Math.Min(_droplets.Count, MaxDroplets);

        // Pack droplet state into the float4 buffer
        // xy = normalized screen position [0,1], z = normalized age [0,1], w = unused
        for (int i = 0; i < count; i++)
        {
            var d = _droplets[i];
            _dropletDataBuffer[i] = new Vector4(
                d.Position.X / viewport.Width,
                d.Position.Y / viewport.Height,
                d.NormalizedAge,
                0f
            );
        }

        // Explicit bind required: MonoGame's effect param system clears the implicit SpriteBatch Textures[0] when a named Texture2D param exists.
        _paramSceneTexture?.SetValue(RenderTarget);
        _paramDropletData?.SetValue(_dropletDataBuffer);
        _paramDropletCount?.SetValue((float)count);   // float to avoid int uniform quirks
        _paramRefractionStrength?.SetValue(RefractionStrength);
        _paramReflectionStrength?.SetValue(ReflectionStrength);
        _paramReflectionColor?.SetValue(ReflectionColor.ToVector3());
        _paramWaveSpeed?.SetValue(WaveSpeed);
        _paramWaveFrequency?.SetValue(WaveFrequency);
        _paramAspectRatio?.SetValue(aspect);

        // SpriteBatch with Immediate mode so the effect fires on every draw call.
        // The effect handles the DropletCount == 0 case gracefully (pass-through).
        _spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _rippleEffect
        );
        _spriteBatch.Draw(RenderTarget, viewport.Bounds, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        base.UnloadContent();
    }
}
