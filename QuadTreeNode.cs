namespace QuadTreeSample
{
	#region

	using System;
	using System.Collections;
	using System.Collections.Generic;

	#endregion

	#region

	#endregion

	/// <summary>
	/// Spatial/sparse quad tree implementation for C#.
	/// This data structure optimizes storage of big condensed filled areas.
	/// It's suboptimal for storing very sparse unique data as overhead of having multiple subnodes is quite high.
	/// It works similar to https://www.youtube.com/watch?v=NfjybO2PIq0 except it uses lazy initialization for subnodes.
	/// Made by Vladimir Kozlov, AtomicTorch Studio http://atomictorch.com
	/// </summary>
	public class QuadTreeNode : IEnumerable<Vector2Int>
	{
		private const int IndexBottomLeft = 0;

		private const int IndexBottomRight = 1;

		private const int IndexTopLeft = 2;

		private const int IndexTopRight = 3;

		public readonly Vector2Int Position;

		public readonly Vector2Int Size;

		/// <summary>
		/// Determines if the node is completely filled or not.
		/// </summary>
		private bool isFilled;

		private QuadTreeNode[] subNodes;

		/// <param name="position">QuadTreeNode start position</param>
		/// <param name="size">Please ensure that the size is a power-of-two number!</param>
		public QuadTreeNode(Vector2Int position, Vector2Int size)
		{
			this.Position = position;
			this.Size = size;
		}

		/// <summary>
		/// Gets sub-nodes array - please note that QuadTreeNode uses lazy initialization,
		/// so the returned array could be null or any of it elements could be null.
		/// </summary>
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
					if (subNode == null)
					{
						continue;
					}

					result += 1 + subNode.SubNodesCount;
				}

				return result;
			}
		}

		public IEnumerator<Vector2Int> GetEnumerator()
		{
			if (this.subNodes == null)
			{
				if (!this.isFilled)
				{
					// no position(s) stored in this node
					yield break;
				}

				if (this.Size.X == 1
				    && this.Size.Y == 1)
				{
					// single-cell quad tree node
					yield return this.Position;
					// enumeration done
					yield break;
				}

				// calculate and return all the positions stored in this node
				for (var x = 0; x < this.Size.X; x++)
				{
					for (var y = 0; y < this.Size.Y; y++)
					{
						yield return new Vector2Int(this.Position.X + x, this.Position.Y + y);
					}
				}

				// enumeration done
				yield break;
			}

			// enumerate all the subNodes
			for (byte index = 0; index < 4; index++)
			{
				var subNode = this.subNodes[index];
				if (subNode == null)
				{
					continue;
				}

				// enumerate all positions stored in the subNode
				foreach (var position in subNode)
				{
					yield return position;
				}
			}
		}

		public void SetFilledPosition(Vector2Int position)
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

			// Optimization: this flag determines if we need to check subnodes for consolidation:
			// when all the subnodes are "filled" they must be consolidated into a single node (this node).
			bool checkSubnodesForConsolidation;
			if (this.subNodes == null)
			{
				// create nodes array
				this.subNodes = new QuadTreeNode[4];
				checkSubnodesForConsolidation = false;
			}
			else
			{
				// nodes are already created
				checkSubnodesForConsolidation = true;
			}

			var subNode = this.subNodes[nodeIndex];
			if (subNode == null)
			{
				subNode = this.CreateNode(nodeIndex);
				this.subNodes[nodeIndex] = subNode;
			}

			subNode.SetFilledPosition(position);

			if (checkSubnodesForConsolidation)
			{
				this.TryConsolidate();
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

		/// <summary>
		/// Creates node for according subNodeIndex.
		/// </summary>
		private QuadTreeNode CreateNode(byte subNodeIndex)
		{
			var x = this.Position.X;
			var y = this.Position.Y;

			var subWidth = this.Size.X / 2;
			var subHeight = this.Size.Y / 2;
			var subSize = new Vector2Int(subWidth, subHeight);

			switch (subNodeIndex)
			{
				case IndexBottomLeft:
					return new QuadTreeNode(new Vector2Int(x, y), subSize);

				case IndexBottomRight:
					return new QuadTreeNode(new Vector2Int(x + subWidth, y), subSize);

				case IndexTopLeft:
					return new QuadTreeNode(new Vector2Int(x, y + subHeight), subSize);

				case IndexTopRight:
					return new QuadTreeNode(new Vector2Int(x + subWidth, y + subHeight), subSize);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void TryConsolidate()
		{
// check if all the nodes are filled now
			for (byte i = 0; i < 4; i++)
			{
				var n = this.subNodes[i];
				if (n == null
				    || !n.isFilled)
				{
					return;
				}
			}

			// all nodes are filled! we can merge them
			this.subNodes = null;
			this.isFilled = true;
		}
	}
}