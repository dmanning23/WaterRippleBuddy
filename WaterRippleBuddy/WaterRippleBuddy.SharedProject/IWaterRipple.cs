using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaterRippleBuddy
{
	public interface IWaterRipple : IDrawable
	{
		RenderTarget2D RenderTarget { set; }

		/// <summary>
		/// Add a drop to the water ripple engine
		/// </summary>
		/// <param name="position">the screen position to add the drop</param>
		void AddDrop(Vector2 position);
	}
}
