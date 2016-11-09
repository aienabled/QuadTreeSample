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

		public static Vector2Int operator +(Vector2Int a, Vector2Int b)
		{
			return new Vector2Int(a.X + b.X, a.Y + b.Y);
		}

		public static bool operator ==(Vector2Int a, Vector2Int b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Vector2Int a, Vector2Int b)
		{
			return !(a == b);
		}

		public bool Equals(Vector2Int other)
		{
			return this.X == other.X && this.Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is Vector2Int && this.Equals((Vector2Int)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (this.X * 397) ^ this.Y;
			}
		}

		public override string ToString()
		{
			return $"{this.X};{this.Y}";
		}
	}
}