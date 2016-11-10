namespace QuadTreeSample
{
	#region

	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Shapes;

	#endregion

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public const ushort CanvasSize = 128;

		private const int DefaultBrushSize = 5;

		private const int DefaultCanvasScale = 8;

		public static readonly DependencyProperty BrushSizeProperty =
			DependencyProperty.Register("BrushSize", typeof(int), typeof(MainWindow), new PropertyMetadata(DefaultBrushSize));

		public static readonly DependencyProperty StatsPointsCountProperty =
			DependencyProperty.Register("StatsPointsCount", typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

		public static readonly DependencyProperty StatsQuadTreeNodesCountProperty =
			DependencyProperty.Register(
				"StatsQuadTreeNodesCount",
				typeof(int),
				typeof(MainWindow),
				new PropertyMetadata(default(int)));

		public static readonly DependencyProperty CanvasScaleProperty =
			DependencyProperty.Register("CanvasScale", typeof(int), typeof(MainWindow), new PropertyMetadata(DefaultCanvasScale));

		private readonly UIElementCollection canvasChildren;

		private readonly List<QuadTreeNode.QuadTreeNodeSnapshot> quadTreeSnapshots =
			new List<QuadTreeNode.QuadTreeNodeSnapshot>();

		private QuadTreeNode quadTreeRoot;

		public MainWindow()
		{
			this.InitializeComponent();

			this.canvasChildren = this.CanvasControl.Children;
			this.CanvasControl.Width = this.CanvasControl.Height = CanvasSize;

			this.MouseLeftButtonDown += this.MouseButtonDownHandler;
			this.MouseRightButtonDown += this.MouseButtonDownHandler;
			this.MouseMove += this.MouseMoveHandler;
			this.KeyDown += this.KeyDownHandler;

			this.CreateQuadTree();
		}

		public int BrushSize
		{
			get { return (int)this.GetValue(BrushSizeProperty); }
			set { this.SetValue(BrushSizeProperty, value); }
		}

		public int CanvasScale
		{
			get { return (int)this.GetValue(CanvasScaleProperty); }
			set { this.SetValue(CanvasScaleProperty, value); }
		}

		public int StatsPointsCount
		{
			get { return (int)this.GetValue(StatsPointsCountProperty); }
			set { this.SetValue(StatsPointsCountProperty, value); }
		}

		public int StatsQuadTreeNodesCount
		{
			get { return (int)this.GetValue(StatsQuadTreeNodesCountProperty); }
			set { this.SetValue(StatsQuadTreeNodesCountProperty, value); }
		}

		private void Clear()
		{
			this.CreateQuadTree();
			this.RebuildVisualization();
		}

		private void CreateQuadTree()
		{
			this.quadTreeRoot = new QuadTreeNode(new Vector2Int(0, 0), CanvasSize);
		}

		private void CreateVisualizationForPoint(Vector2Int position)
		{
			var rectangle = new Rectangle
			{
				Fill = Brushes.Red,
				Width = 1,
				Height = 1,
				IsHitTestVisible = false
			};

			this.canvasChildren.Add(rectangle);
			Canvas.SetLeft(rectangle, position.X);
			Canvas.SetTop(rectangle, position.Y);
		}

		private void CreateVisualizationForQuadRecursive(QuadTreeNode node)
		{
			var rectangle = new Rectangle
			{
				Stroke = Brushes.Black,
				StrokeThickness = 0.08,
				Width = node.Size,
				Height = node.Size,
				IsHitTestVisible = false
			};

			this.canvasChildren.Add(rectangle);
			Canvas.SetLeft(rectangle, node.Position.X);
			Canvas.SetTop(rectangle, node.Position.Y);

			if (node.SubNodes == null)
			{
				return;
			}

			for (var index = 0; index < 4; index++)
			{
				var subNode = node.SubNodes[index];
				if (subNode != null)
				{
					this.CreateVisualizationForQuadRecursive(subNode);
				}
			}
		}

		private void DrawAtPosition(Vector2Int position)
		{
			var isFill = Mouse.LeftButton == MouseButtonState.Pressed;
			foreach (var point in BrushHelper.GetPointsInCircle(position, this.BrushSize))
			{
				if (!this.IsInside(point))
				{
					continue;
				}

				if (isFill)
				{
					this.quadTreeRoot.SetFilledPosition(point);
				}
				else
				{
					this.quadTreeRoot.ResetFilledPosition(point);
				}
			}

			this.RebuildVisualization();
		}

		private bool IsInside(Vector2Int point)
		{
			return point.X >= 0 
				&& point.Y >= 0 
				&& point.X < CanvasSize 
				&& point.Y < CanvasSize;
		}

		private void KeyDownHandler(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					this.Clear();
					return;

				case Key.S:
					this.quadTreeSnapshots.Clear();
					this.quadTreeRoot.Save(this.quadTreeSnapshots);
					return;

				case Key.L:
					this.CreateQuadTree();
					this.quadTreeRoot.Load(this.quadTreeSnapshots);
					this.RebuildVisualization();
					return;
			}

			var brushSize = this.BrushSize;
			switch (e.Key)
			{
				case Key.OemMinus:
					brushSize--;
					break;
				case Key.OemPlus:
					brushSize++;
					break;
			}

			if (brushSize < 1)
			{
				brushSize = 1;
			}
			else if (brushSize > 50)
			{
				brushSize = 50;
			}

			this.BrushSize = brushSize;
		}

		private void MouseButtonDownHandler(object sender, MouseButtonEventArgs e)
		{
			var position = e.GetPosition(this.CanvasControl);
			this.DrawAtPosition(new Vector2Int(position));
		}

		private void MouseMoveHandler(object sender, MouseEventArgs e)
		{
			if (Mouse.LeftButton != MouseButtonState.Pressed
			    && Mouse.RightButton != MouseButtonState.Pressed)
			{
				return;
			}

			var position = e.GetPosition(this.CanvasControl);
			this.DrawAtPosition(new Vector2Int(position));
		}

		private void RebuildVisualization()
		{
			this.canvasChildren.Clear();

			var subNodesCount = 1 + this.quadTreeRoot.SubNodesCount;
			this.StatsQuadTreeNodesCount = subNodesCount;
			var tree = this.quadTreeRoot;

			var pointsCount = 0;
			foreach (var position in tree)
			{
				pointsCount++;
				this.CreateVisualizationForPoint(position);
			}

			this.StatsPointsCount = pointsCount;

			this.CreateVisualizationForQuadRecursive(tree);
		}
	}
}