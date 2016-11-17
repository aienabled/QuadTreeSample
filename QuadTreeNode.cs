namespace QuadTreeSample
{
	#region

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;

	#endregion

	/// <summary>
	/// Spatial/sparse quad tree implementation for C#.
	/// This data structure optimizes storage of big condensed filled areas.
	/// It's suboptimal for storing very sparse non-condensed data as overhead of having multiple subnodes is quite high.
	/// It works similar to https://www.youtube.com/watch?v=NfjybO2PIq0 except it uses lazy initialization for subnodes.
	/// Coding: Vladimir Kozlov, AtomicTorch Studio http://atomictorch.com
	/// </summary>
	public sealed class QuadTreeNode : IEnumerable<Vector2Int>
	{
		public readonly Vector2Int Position;

		private readonly byte sizePowerOfTwo;

		private QuadTreeNode subNodeBottomLeft;

		private QuadTreeNode subNodeBottomRight;

		private QuadTreeNode subNodeTopLeft;

		private QuadTreeNode subNodeTopRight;

		/// <param name="position">QuadTreeNode start position</param>
		/// <param name="size">Size (will be rounded up to power of two; for example: 50->64, 200->256, 500->512, etc)</param>
		public QuadTreeNode(Vector2Int position, ushort size)
		{
			if (size == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(size), "Size must be > 0");
			}

			this.Position = position;
			var canvasSizeRoundedUp = RoundUpToPowerOfTwo(size);
			// calculate what the power of two corresponds to this size (take logarithm with base==2)
			this.sizePowerOfTwo = (byte)Math.Log(canvasSizeRoundedUp, 2);
		}

		/// <param name="position">QuadTreeNode start position</param>
		/// <param name="sizePowerOfTwo">Size as power of two</param>
		private QuadTreeNode(Vector2Int position, byte sizePowerOfTwo)
		{
			this.Position = position;
			this.sizePowerOfTwo = sizePowerOfTwo;
		}

		public enum SubNodeIndex : byte
		{
			BottomLeft = 0,

			BottomRight = 1,

			TopLeft = 2,

			TopRight = 3
		}

		public bool HasSubNodes
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return !this.IsNodeFilled
				       && (this.subNodeBottomLeft != null
				           || this.subNodeBottomRight != null
				           || this.subNodeTopLeft != null
				           || this.subNodeTopRight != null);
			}
		}

		/// <summary>
		/// Returns true if the node is completely filled. If the node is filled, it cannot contain nodes.
		/// </summary>
		public bool IsNodeFilled { get; private set; }

		public ushort Size => (ushort)(1 << this.sizePowerOfTwo);

		public int SubNodesCount
		{
			get
			{
				var result = 0;
				for (byte subNodeIndex = 0; subNodeIndex < 4; subNodeIndex++)
				{
					var subNode = this.GetSubNode((SubNodeIndex)subNodeIndex);
					if (subNode != null)
					{
						result += 1 + subNode.SubNodesCount;
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Adds all stored positions in this quad tree node (and its subnodes) to the list.
		/// </summary>
		public void AddStoredPositions(IList<Vector2Int> list)
		{
			if (this.sizePowerOfTwo == 0)
			{
				// single-cell quad tree node - add self position
				list.Add(this.Position);
				return;
			}

			if (this.IsNodeFilled)
			{
				// filled node cannot have subnodes
				// calculate and return all the positions stored in this node
				var size = this.Size;
				for (var x = 0; x < size; x++)
				{
					for (var y = 0; y < size; y++)
					{
						list.Add(new Vector2Int(this.Position.X + x, this.Position.Y + y));
					}
				}

				return;
			}

			for (byte subNodeIndex = 0; subNodeIndex < 4; subNodeIndex++)
			{
				// add all positions stored in the subNode
				var subNode = this.GetSubNode((SubNodeIndex)subNodeIndex);
				subNode?.AddStoredPositions(list);
			}
		}

		public IEnumerator<Vector2Int> GetEnumerator()
		{
			// We will not actually enumerate as it's very memory consuming (high overhead due to creation of enumerators).
			// Instead we will create a new list and fill all the stored positions there recursively.

			// TODO: it's better to use higher initial list capacity to avoid resizing of the inner array
			var list = new List<Vector2Int>(capacity: 100);
			this.AddStoredPositions(list);
			return list.GetEnumerator();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QuadTreeNode GetSubNode(SubNodeIndex subNodeIndex)
		{
			switch (subNodeIndex)
			{
				case SubNodeIndex.BottomLeft:
					return this.subNodeBottomLeft;

				case SubNodeIndex.BottomRight:
					return this.subNodeBottomRight;

				case SubNodeIndex.TopLeft:
					return this.subNodeTopLeft;

				case SubNodeIndex.TopRight:
					return this.subNodeTopRight;

				default:
					throw new ArgumentOutOfRangeException(nameof(subNodeIndex), subNodeIndex, null);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsPositionFilled(Vector2Int position)
		{
			Debug.Assert(position.X >= this.Position.X);
			Debug.Assert(position.Y >= this.Position.Y);
			Debug.Assert(position.X < this.Position.X + this.Size);
			Debug.Assert(position.Y < this.Position.Y + this.Size);

			if (!this.HasSubNodes)
			{
				return this.IsNodeFilled;
			}

			var subNodeIndex = this.CalculateNodeIndex(position);
			var subNode = this.GetSubNode(subNodeIndex);
			return subNode != null && subNode.IsPositionFilled(position);
		}

		/// <summary>
		/// Load (additive).
		/// </summary>
		public void Load(IReadOnlyList<QuadTreeNodeSnapshot> snapshots)
		{
			foreach (var snapshot in snapshots)
			{
				this.SetFilledPosition(snapshot.Position, snapshot.SizePowerOfTwo);
			}
		}

		public void ResetFilledPosition(Vector2Int position)
		{
			if (this.sizePowerOfTwo == 0)
			{
				Debug.Assert(this.Position == position);
				this.IsNodeFilled = false;
				return;
			}

			if (!this.HasSubNodes)
			{
				if (!this.IsNodeFilled)
				{
					// no subnodes exists and this node is not filled, so nothing to reset
					return;
				}

				// need to split this filled node on the filled subnodes
				this.IsNodeFilled = false;
				for (byte index = 0; index < 4; index++)
				{
					var node = this.CreateAndSetNode((SubNodeIndex)index);
					node.IsNodeFilled = true;
				}
			}

			// find subnode
			var subNodeIndex = this.CalculateNodeIndex(position);
			var subNode = this.GetSubNode(subNodeIndex);
			if (subNode == null)
			{
				// not subnode exists - nothing to reset
				return;
			}

			subNode.ResetFilledPosition(position);
			this.TryConsolidateOnReset();
		}

		public IList<QuadTreeNodeSnapshot> Save()
		{
			var list = new List<QuadTreeNodeSnapshot>();
			this.Save(list);
			return list;
		}

		public void Save(IList<QuadTreeNodeSnapshot> snapshots)
		{
			if (this.IsNodeFilled)
			{
				snapshots.Add(new QuadTreeNodeSnapshot(this.Position, this.sizePowerOfTwo));
			}
			else
			{
				this.subNodeBottomLeft?.Save(snapshots);
				this.subNodeBottomRight?.Save(snapshots);
				this.subNodeTopLeft?.Save(snapshots);
				this.subNodeTopRight?.Save(snapshots);
			}
		}

		public void SetFilledPosition(Vector2Int position)
		{
			if (this.IsNodeFilled)
			{
				return;
			}

			if (this.sizePowerOfTwo == 0)
			{
				Debug.Assert(this.Position == position);
				this.IsNodeFilled = true;
				return;
			}

			var subNode = this.GetOrCreateSubNode(position);
			subNode.SetFilledPosition(position);
			this.TryConsolidateOnSet();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// Round up to the next highest power of 2
		/// http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
		private static int RoundUpToPowerOfTwo(int v)
		{
			v--;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			v |= v >> 16;
			v++;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private SubNodeIndex CalculateNodeIndex(Vector2Int position)
		{
			var isLeftHalf = position.X < this.Position.X + this.Size / 2;
			var isBottomHalf = position.Y < this.Position.Y + this.Size / 2;

			if (isBottomHalf)
			{
				return isLeftHalf ? SubNodeIndex.BottomLeft : SubNodeIndex.BottomRight;
			}

			// top half
			return isLeftHalf ? SubNodeIndex.TopLeft : SubNodeIndex.TopRight;
		}

		/// <summary>
		/// Creates node for according subNodeIndex.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private QuadTreeNode CreateAndSetNode(SubNodeIndex subNodeIndex)
		{
			var x = this.Position.X;
			var y = this.Position.Y;

			var subSize = (ushort)(this.Size / 2);
			var subSizePowerOfTwo = (byte)(this.sizePowerOfTwo - 1);

			switch (subNodeIndex)
			{
				case SubNodeIndex.BottomLeft:
					return this.subNodeBottomLeft = new QuadTreeNode(new Vector2Int(x, y), subSizePowerOfTwo);

				case SubNodeIndex.BottomRight:
					return this.subNodeBottomRight = new QuadTreeNode(new Vector2Int(x + subSize, y), subSizePowerOfTwo);

				case SubNodeIndex.TopLeft:
					return this.subNodeTopLeft = new QuadTreeNode(new Vector2Int(x, y + subSize), subSizePowerOfTwo);

				case SubNodeIndex.TopRight:
					return this.subNodeTopRight = new QuadTreeNode(new Vector2Int(x + subSize, y + subSize), subSizePowerOfTwo);

				default:
					throw new ArgumentOutOfRangeException(nameof(subNodeIndex), subNodeIndex, null);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DestroySubNode(SubNodeIndex subNodeIndex)
		{
			switch (subNodeIndex)
			{
				case SubNodeIndex.BottomLeft:
					this.subNodeBottomLeft = null;
					break;
				case SubNodeIndex.BottomRight:
					this.subNodeBottomRight = null;
					break;
				case SubNodeIndex.TopLeft:
					this.subNodeTopLeft = null;
					break;
				case SubNodeIndex.TopRight:
					this.subNodeTopRight = null;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(subNodeIndex), subNodeIndex, null);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DestroySubNodes()
		{
			this.subNodeBottomLeft = null;
			this.subNodeBottomRight = null;
			this.subNodeTopLeft = null;
			this.subNodeTopRight = null;
		}

		/// <param name="checkSubnodesForConsolidation">
		/// Optimization: this flag determines if we need to check subnodes for consolidation:
		/// </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private QuadTreeNode GetOrCreateSubNode(Vector2Int position)
		{
			// find subnode
			var subNodeIndex = this.CalculateNodeIndex(position);

			var subNode = this.GetSubNode(subNodeIndex)
			              ?? this.CreateAndSetNode(subNodeIndex);

			return subNode;
		}

		private void ResetFilledPosition(Vector2Int position, byte sizePowerOfTwo)
		{
			if (sizePowerOfTwo > this.sizePowerOfTwo)
			{
				throw new Exception(
					"Size exceeded - this quadtree node is lower size than required: size (power of two) is "
					+ this.sizePowerOfTwo + " and set filled position size is " + sizePowerOfTwo);
			}

			if (this.sizePowerOfTwo == sizePowerOfTwo)
			{
				Debug.Assert(this.Position == position);
				this.IsNodeFilled = false;
				return;
			}

			if (!this.HasSubNodes)
			{
				if (!this.IsNodeFilled)
				{
					// no subnodes exists and this node is not filled, so nothing to reset
					return;
				}

				// need to split this filled node on the filled subnodes
				this.IsNodeFilled = false;
				for (byte index = 0; index < 4; index++)
				{
					var node = this.CreateAndSetNode((SubNodeIndex)index);
					node.IsNodeFilled = true;
				}
			}

			// find subnode
			var subNodeIndex = this.CalculateNodeIndex(position);
			var subNode = this.GetSubNode(subNodeIndex);
			if (subNode == null)
			{
				// not subnode exists - nothing to reset
				return;
			}

			subNode.ResetFilledPosition(position, sizePowerOfTwo);
			this.TryConsolidateOnReset();
		}

		private void SetFilledPosition(Vector2Int position, byte sizePowerOfTwo)
		{
			if (this.IsNodeFilled)
			{
				return;
			}

			if (this.sizePowerOfTwo == sizePowerOfTwo)
			{
				Debug.Assert(this.Position == position);
				if (this.IsNodeFilled)
				{
					return;
				}

				// consolidate and make filled
				this.DestroySubNodes();
				this.IsNodeFilled = true;
				return;
			}

			if (this.sizePowerOfTwo == 0)
			{
				throw new Exception("Size mismatch!");
			}

			var subNode = this.GetOrCreateSubNode(position);
			subNode.SetFilledPosition(position, sizePowerOfTwo);
			this.TryConsolidateOnSet();
		}

		private void TryConsolidateOnReset()
		{
			// it doesn't make sense calling this method for filled node as it's already "consolidated"
			Debug.Assert(!this.IsNodeFilled);

			// check if all the nodes are not filled now
			var isCanConsolidate = true;
			for (byte subNodeIndex = 0; subNodeIndex < 4; subNodeIndex++)
			{
				var subNode = this.GetSubNode((SubNodeIndex)subNodeIndex);
				if (subNode == null)
				{
					continue;
				}

				if (!subNode.IsNodeFilled
				    && !subNode.HasSubNodes)
				{
					// destroy subnode because it's not used anymore
					this.DestroySubNode((SubNodeIndex)subNodeIndex);
				}
				else
				{
					// used node found
					isCanConsolidate = false;
				}
			}

			if (isCanConsolidate)
			{
				// all nodes are not filled! we can merge them
				this.DestroySubNodes();
			}
		}

		/// <summary>
		/// When all the subnodes are "filled" they must be consolidated.
		/// </summary>
		private void TryConsolidateOnSet()
		{
			// check if all the nodes are filled now
			for (byte subNodeIndex = 0; subNodeIndex < 4; subNodeIndex++)
			{
				var n = this.GetSubNode((SubNodeIndex)subNodeIndex);
				if (n == null
				    || !n.IsNodeFilled)
				{
					return;
				}
			}

			// all nodes are filled! we can merge them
			this.DestroySubNodes();
			this.IsNodeFilled = true;
		}

		public struct QuadTreeNodeSnapshot
		{
			public readonly Vector2Int Position;

			public readonly byte SizePowerOfTwo;

			public QuadTreeNodeSnapshot(Vector2Int position, byte sizePowerOfTwo)
			{
				this.Position = position;
				this.SizePowerOfTwo = sizePowerOfTwo;
			}

			public bool Equals(QuadTreeNodeSnapshot other)
			{
				return this.Position.Equals(other.Position) && this.SizePowerOfTwo.Equals(other.SizePowerOfTwo);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				return obj is QuadTreeNodeSnapshot && this.Equals((QuadTreeNodeSnapshot)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (this.Position.GetHashCode() * 397) ^ this.SizePowerOfTwo.GetHashCode();
				}
			}
		}
	}
}