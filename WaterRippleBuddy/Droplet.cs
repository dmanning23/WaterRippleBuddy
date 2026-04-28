using Microsoft.Xna.Framework;

namespace WaterRippleBuddy;

/// <summary>
/// Represents a single water droplet and tracks its position and age.
/// Owned and updated internally by <see cref="WaterRippleComponent"/>.
/// </summary>
internal sealed class Droplet
{
    /// <summary>Screen-space position in pixels.</summary>
    public Vector2 Position { get; }

    /// <summary>Total duration in seconds before this droplet is removed.</summary>
    public float Lifetime { get; }

    /// <summary>Seconds since this droplet was created.</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>
    /// Normalized age in [0, 1]. 0 = just created, 1 = fully expired.
    /// Passed directly to the shader.
    /// </summary>
    public float NormalizedAge => ElapsedTime / Lifetime;

    /// <summary>True once the droplet has lived its full lifetime.</summary>
    public bool IsExpired => ElapsedTime >= Lifetime;

    public Droplet(Vector2 position, float lifetime)
    {
        Position = position;
        Lifetime = lifetime;
    }

    public void Update(float deltaSeconds)
    {
        ElapsedTime = MathF.Min(ElapsedTime + deltaSeconds, Lifetime);
    }
}
