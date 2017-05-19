using GameTimer;
using Microsoft.Xna.Framework;

namespace WaterRippleBuddy
{
	internal class Droplet
	{
		#region Properties

		readonly Vector2 _shaderPosition;

		public CountdownTimer Timer { get; private set; }

		public Vector2 Position { get; set; }

		#endregion //Properties

		#region Methods

		public Droplet(Vector2 pos)
		{
			Position = pos;
			_shaderPosition = new Vector2(0.5f, 0.5f);
			Timer = new CountdownTimer();
			Timer.Start(1.5f);
		}
		
		public void Update(GameTime time)
		{
			Timer.Update(time);
		}

		public Vector3 MakeShaderParameter(float aspect)
		{
			return new Vector3(_shaderPosition.X * aspect, _shaderPosition.Y, Timer.CurrentTime);
		}

		#endregion //Methods
	}
}
