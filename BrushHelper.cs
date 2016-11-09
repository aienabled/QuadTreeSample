namespace QuadTreeSample
{
	#region

	using System.Collections.Generic;

	#endregion

	public static class BrushHelper
	{
		public static IEnumerable<Vector2Int> GetPointsInCircle(Vector2Int position, int brushSize)
		{
			foreach (var offset in GenerateOffsetsCircle(brushSize))
			{
				yield return position + offset;
			}
		}

		private static IEnumerable<Vector2Int> GenerateOffsetsCircle(int brushSize)
		{
			if (brushSize <= 2)
			{
				foreach (var offset in GenerateOffsetsSquare(brushSize))
				{
					yield return offset;
				}

				yield break;
			}

			var startX = -(brushSize - 1) / 2;
			var startY = startX;

			// ReSharper disable once PossibleLossOfFraction
			double center = brushSize / 2;

			double radiusSquared;
			switch (brushSize)
			{
				case 3:
					radiusSquared = 2;
					break;

				case 4:
					center = 1.5;
					radiusSquared = 4.25;
					break;

				case 6:
					center = 2.5;
					radiusSquared = 8;
					break;

				default:
					radiusSquared = brushSize / 2d;
					radiusSquared *= radiusSquared;
					break;
			}

			for (var x = 0; x < brushSize; x++)
			{
				for (var y = 0; y < brushSize; y++)
				{
					var dx = center - x;
					var dy = center - y;
					var distanceSquared = dx * dx + dy * dy;

					if (distanceSquared < radiusSquared)
					{
						yield return new Vector2Int(x + startX, y + startY);
					}
				}
			}
		}

		private static IEnumerable<Vector2Int> GenerateOffsetsSquare(int brushSize)
		{
			var startX = -(brushSize - 1) / 2;
			var startY = startX;

			for (var x = 0; x < brushSize; x++)
			{
				for (var y = 0; y < brushSize; y++)
				{
					yield return new Vector2Int(x + startX, y + startY);
				}
			}
		}
	}
}