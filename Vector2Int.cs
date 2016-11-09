namespace QuadTreeSample
{
	#region

	using System.Windows;

	#endregion

	public struct Vector2Int
	{
		public readonly int X;

		public readonly int Y;

		public Vector2Int(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public Vector2Int(Point position) : this((int)position.X, (int)position.Y)
		{
		}

		public override string ToString()
		{
			return $"{this.X};{this.Y}";
		}
	}
}