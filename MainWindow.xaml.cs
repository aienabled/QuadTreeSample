namespace QuadTreeSample
{
	#region

	using System.Diagnostics;
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
		private readonly UIElementCollection canvasChildren;

		private bool isMousePressed;

		private QuadTreeNode quadTreeRoot;

		public MainWindow()
		{
			this.CreateQuadTree();
			this.InitializeComponent();
			this.canvasChildren = this.CanvasControl.Children;
			this.CanvasControl.MouseLeftButtonDown += this.CanvasControlMouseLeftButtonDown;
			this.CanvasControl.MouseLeftButtonUp += this.CanvasControlMouseLeftButtonUp;
			this.CanvasControl.MouseRightButtonDown += this.CanvasControl_MouseRightButtonDown;
			this.CanvasControl.MouseMove += this.CanvasControl_MouseMove;
		}

		private void CanvasControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (!this.isMousePressed)
			{
				return;
			}

			var position = e.GetPosition(this.CanvasControl);
			this.SelectPoint(new Vector2Int(position));
		}

		private void CanvasControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.CreateQuadTree();
			this.RebuildVisualization();
		}

		private void CanvasControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.isMousePressed = true;
			var position = e.GetPosition(this.CanvasControl);
			this.SelectPoint(new Vector2Int(position));
		}

		private void CanvasControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.isMousePressed = false;
		}

		private void CreateQuadTree()
		{
			this.quadTreeRoot = new QuadTreeNode(new Vector2Int(0, 0), new Vector2Int(1024, 1024));
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

		private void RebuildVisualization()
		{
			this.canvasChildren.Clear();

			//Debug.WriteLine("Tree: " + string.Join(", ", tree.Select(e => e.ToString())));
			Debug.WriteLine("Tree size: " + this.quadTreeRoot.SubNodesCount + " nodes");
			var tree = this.quadTreeRoot;

			foreach (var position in tree)
			{
				this.CreateVisualizationForPoint(position);
			}

			CreateVisualizationForQuadRecursive(tree);
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

			for (int index = 0; index < 4; index++)
			{
				var subNode = node.SubNodes[index];
				this.CreateVisualizationForQuadRecursive(subNode);
			}
		}

		private void SelectPoint(Vector2Int position)
		{
			this.quadTreeRoot.AddElement(position);
			this.RebuildVisualization();
		}
	}
}