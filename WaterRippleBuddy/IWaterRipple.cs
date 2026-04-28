using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaterRippleBuddy;

/// <summary>
/// Minimal contract for the water ripple component.
/// </summary>
public interface IWaterRipple
{
    /// <summary>
    /// The render target containing the scene to distort.
    /// Assign this every frame before the component's Draw is called.
    /// The component owns drawing this target to the back buffer — do not
    /// draw it yourself.
    /// </summary>
    RenderTarget2D? RenderTarget { get; set; }

    /// <summary>
    /// Spawns a ripple at the given screen position (pixels).
    /// </summary>
    void AddDrop(Vector2 screenPosition);
}
