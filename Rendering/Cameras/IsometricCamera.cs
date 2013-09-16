﻿using DeltaEngine.Core;
using DeltaEngine.Datatypes;
using DeltaEngine.Graphics;

namespace DeltaEngine.Rendering.Cameras
{
	/// <summary>
	/// Like LookAtCamera but uses Orthographic projection and the look direction can never
	/// be changed from how it was set in the constructor. Moving forwards and backwards has no
	/// meaning for Orthographic, instead Zooming rescales the world
	/// </summary>
	public sealed class IsometricCamera : Camera
	{
		public IsometricCamera(Device device, Window window, Vector lookDirection)
			: base(device, window)
		{
			base.Position = -lookDirection;
			ResetZoom();
			leftDirection = Vector.Normalize(Vector.Cross(lookDirection, -UpDirection));
		}

		private readonly Vector leftDirection;

		public void ResetZoom()
		{
			scale = 1.0f;
			ForceProjectionMatrixUpdate(window.ViewportPixelSize);
		}

		private float scale;

		protected override Matrix GetCurrentProjectionMatrix()
		{
			return Matrix.CreateOrthoProjection(window.ViewportPixelSize * scale, NearPlane, FarPlane);
		}

		public void MoveUp(float distance)
		{
			Position += UpDirection * distance;
		}

		public override Vector Position
		{
			get { return base.Position; }
			set
			{
				base.Target += value - base.Position;
				base.Position = value;
			}
		}

		public override Vector Target
		{
			get { return base.Target; }
			set
			{
				base.Position += value - base.Target;
				base.Target = value;
			}
		}

		public void MoveDown(float distance)
		{
			Position -= UpDirection * distance;
		}

		public void MoveLeft(float distance)
		{
			Position += leftDirection * distance;
		}

		public void MoveRight(float distance)
		{
			Position -= leftDirection * distance;
		}

		public void Zoom(float amount)
		{
			scale /= amount;
			ForceProjectionMatrixUpdate(window.ViewportPixelSize);
		}
	}
}