namespace QuadTreeSample
{
	#region

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
		public const int CanvasSize = 128;

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

		private bool isMousePressed;

		private QuadTreeNode quadTreeRoot;

		public MainWindow()
		{
			this.InitializeComponent();

			this.canvasChildren = this.CanvasControl.Children;
			this.CanvasControl.Width = this.CanvasControl.Height = CanvasSize;

			this.MouseLeftButtonDown += this.MouseLeftButtonDownHandler;
			this.MouseLeftButtonUp += this.MouseLeftButtonUpHandler;
			this.MouseRightButtonDown += this.MouseRightButtonDownHandler;
			this.MouseMove += this.MouseMoveHandler;
			this.KeyDown += this.MainWindow_KeyDown;

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

		private void CreateQuadTree()
		{
			this.quadTreeRoot = new QuadTreeNode(new Vector2Int(0, 0), new Vector2Int(CanvasSize, CanvasSize));
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
				Width = node.Size.X,
				Height = node.Size.Y,
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
			foreach (var point in CircleBrushHelper.GetPoints(position, this.BrushSize))
			{
				this.quadTreeRoot.SetFilledPosition(point);
			}

			this.RebuildVisualization();
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
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

		private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
		{
			this.isMousePressed = true;
			var position = e.GetPosition(this.CanvasControl);
			this.DrawAtPosition(new Vector2Int(position));
		}

		private void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
		{
			this.isMousePressed = false;
		}

		private void MouseMoveHandler(object sender, MouseEventArgs e)
		{
			if (!this.isMousePressed)
			{
				return;
			}

			var position = e.GetPosition(this.CanvasControl);
			this.DrawAtPosition(new Vector2Int(position));
		}

		private void MouseRightButtonDownHandler(object sender, MouseButtonEventArgs e)
		{
			this.CreateQuadTree();
			this.RebuildVisualization();
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