namespace QuadTreeSample
{
	#region

	using System.Collections;
	using System.Collections.Generic;

	#endregion

	#region

	#endregion

	public class QuadTreeNode : IEnumerable<Vector2Int>
	{
		public const int IndexBottomLeft = 0;

		public const int IndexTopLeft = 2;

		public const int IndexTopRight = 3;

		private const int IndexBottomRight = 1;

		public readonly Vector2Int Position;

		public readonly Vector2Int Size;

		private bool isFilled;

		private QuadTreeNode[] subNodes;

		/// <param name="position">QuadTreeNode start position</param>
		/// <param name="size">Please ensure that the size is a power-of-two number!</param>
		public QuadTreeNode(Vector2Int position, Vector2Int size)
		{
			this.Position = position;
			this.Size = size;
		}

		public QuadTreeNode[] SubNodes => this.subNodes;

		public int SubNodesCount
		{
			get
			{
				if (this.subNodes == null)
				{
					return 0;
				}

				var result = 0;
				for (var index = 0; index < this.subNodes.Length; index++)
				{
					var subNode = this.subNodes[index];
					if (subNode.isFilled)
					{
						result++;
					}

					result += subNode.SubNodesCount;
				}

				return result;
			}
		}

		public void AddElement(Vector2Int position)
		{
			if (this.isFilled)
			{
				return;
			}

			if (this.Size.X == 1
			    && this.Size.Y == 1)
			{
				this.isFilled = true;
				return;
			}

			// find and create node
			var nodeIndex = this.CalculateNodeIndex(position);
			if (this.subNodes == null)
			{
				// split and add new node
				this.Split();
				this.subNodes[nodeIndex].AddElement(position);
				return;
			}

			this.subNodes[nodeIndex].AddElement(position);

			// check if all the nodes are filled now
			for (byte i = 0; i < 4; i++)
			{
				if (!this.subNodes[i].isFilled)
				{
					return;
				}
			}

			// all nodes are filled! we can merge them
			this.subNodes = null;
			this.isFilled = true;
		}

		public IEnumerator<Vector2Int> GetEnumerator()
		{
			if (this.subNodes == null)
			{
				if (!this.isFilled)
				{
					yield break;
				}

				if (this.Size.X == 1
				    && this.Size.Y == 1)
				{
					yield return this.Position;

					yield break;
				}

				for (var x = 0; x < this.Size.X; x++)
				{
					for (var y = 0; y < this.Size.Y; y++)
					{
						yield return new Vector2Int(this.Position.X + x, this.Position.Y + y);
					}
				}

				yield break;
			}

			for (byte index = 0; index < 4; index++)
			{
				var node = this.subNodes[index];

				foreach (var position in node)
				{
					yield return position;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		private byte CalculateNodeIndex(Vector2Int position)
		{
			var isLeftHalf = position.X < this.Position.X + this.Size.X / 2;
			var isBottomHalf = position.Y < this.Position.Y + this.Size.Y / 2;

			if (isBottomHalf)
			{
				if (isLeftHalf)
				{
					return IndexBottomLeft;
				}

				return IndexBottomRight;
			}

			// top half
			if (isLeftHalf)
			{
				return IndexTopLeft;
			}

			return IndexTopRight;
		}

		private void Split()
		{
			// split quad tree on four nodes
			this.subNodes = new QuadTreeNode[4];
			var x = this.Position.X;
			var y = this.Position.Y;

			var subWidth = this.Size.X / 2;
			var subHeight = this.Size.Y / 2;
			var subSize = new Vector2Int(subWidth, subHeight);
			this.subNodes[IndexBottomLeft] = new QuadTreeNode(new Vector2Int(x, y), subSize);
			this.subNodes[IndexBottomRight] = new QuadTreeNode(new Vector2Int(x + subWidth, y), subSize);
			this.subNodes[IndexTopLeft] = new QuadTreeNode(new Vector2Int(x, y + subHeight), subSize);
			this.subNodes[IndexTopRight] = new QuadTreeNode(new Vector2Int(x + subWidth, y + subHeight), subSize);
		}
	}
}