using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wizard
{
    class Map
    {
        private Node startNode;
        private Node goalNode;
        private Node[,] grid;
        private List<Node> open = new List<Node>();
        private List<Node> closed = new List<Node>();
        private List<Node> pathList = new List<Node>();
        private List<Node> ghostList = new List<Node>();
        private Queue<Node> nodesQueue = new Queue<Node>();
        private Stack<Node> nodesStack = new Stack<Node>();

        //Handeling of graphics
        private BufferedGraphics backBuffer;
        private Graphics dc;
        private Rectangle displayRectangle;

        /// <summary>
        /// Amount of rows in the grid
        /// </summary>
        private int cellRowCount;

        /// <summary>
        /// The current click type
        /// </summary>
        private NodeType clickType;

        private int margin;

        public static List<Node> Targets { get; set; }

        public Map(Graphics dc, Rectangle displayRectangle)
        {
            //Create's (Allocates) a buffer in memory with the size of the display
            this.backBuffer = BufferedGraphicsManager.Current.Allocate(dc, displayRectangle);

            //Sets the graphics context to the graphics in the buffer
            this.dc = backBuffer.Graphics;

            //Sets the displayRectangle
            this.displayRectangle = displayRectangle;

            //Sets the row count to then, this will create a 10 by 10 grid.
            cellRowCount = 10;
            margin = 5;

            grid = CreateGrid();
            Targets = new List<Node>();
            clickType = NodeType.GOAL;
        }

        public Node[,] CreateGrid()
        {
            //Sets the cell size
            int cellSize = (displayRectangle.Height - margin * 2)/ cellRowCount;

            grid = new Node[cellRowCount, cellRowCount];

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    grid[x, y] = new Node(new Point(x, y), cellSize, margin);
                }
            }
            for (int x = 4; x < 7; x++)
            {
                for (int y = 1; y < 7; y++)
                {
                    grid[x, y].Sprite = Image.FromFile(@"Images\stone.png");
                    grid[x, y].Type = NodeType.WALL;
                }
            }
            for (int x = 2; x < 7; x++)
            {
                grid[x, 7].Sprite = Image.FromFile(@"Images\tree.png");
                grid[x, 7].Type = NodeType.WALL;
                grid[x, 9].Sprite = Image.FromFile(@"Images\tree.png");
                grid[x, 9].Type = NodeType.WALL;
            }
            // Create road
            for (int x = 3; x < 8; x++)
            {
                grid[x, 0].NativColor = grid[x, 0].NodeColor = Color.BurlyWood;
            }
            for (int x = 1; x < 9; x++)
            {
                grid[x, 8].NativColor = grid[x, 8].NodeColor = Color.BurlyWood;
            }
            for (int x = 1; x < 4; x++)
            {
                grid[x, 6].NativColor = grid[x, 6].NodeColor = Color.BurlyWood;
            }
            grid[1, 7].NativColor = grid[1, 7].NodeColor = Color.BurlyWood;
            for (int y = 1; y < 6; y++)
            {
                grid[3, y].NativColor = grid[3, y].NodeColor = Color.BurlyWood;
            }
            for (int y = 1; y < 6; y++)
            {
                grid[7, y].NativColor = grid[7, y].NodeColor = Color.BurlyWood;
            }
            for (int y = 5; y < 8; y++)
            {
                grid[8, y].NativColor = grid[8, y].NodeColor = Color.BurlyWood;
            }

            grid[1, 8].Sprite = Image.FromFile(@"Images\wizard.png");
            grid[1, 8].Type = NodeType.START;

            grid[2, 4].Sprite = Image.FromFile(@"Images\tower.png");
            grid[2, 4].Type = NodeType.WALL;

            grid[0, 8].Sprite = Image.FromFile(@"Images\portal.png");
            grid[0, 8].Type = NodeType.WALL;

            if (ghostList.Count > 0) ghostList.Clear();

            for (int x = 2; x < 7; x++)
            {
                ghostList.Add(grid[x, 8]);
            }
            return grid;
        }

        /// <summary>
        /// Renders all the cells
        /// </summary>
        public void Render()
        {
            dc.Clear(SystemColors.Control);

            foreach (Node node in grid)
            {
                node.Render(dc);
            }

            //Draws the rectangles border
            dc.DrawRectangle(new Pen(Color.Black), new Rectangle(2, 2, displayRectangle.Height - 5, displayRectangle.Height - 5));

            //Renders the content of the buffered graphics context to the real context(Swap buffers)
            backBuffer.Render();
        }

        /// <summary>
        /// If the mouse clicks on a cell
        /// </summary>
        /// <param name="mousePos"></param>
        public void ClickNode(Point mousePos)
        {
            foreach (Node node in grid) //Finds the cell that we just clicked
            {
                if (node.BoundingRectangle.IntersectsWith(new Rectangle(mousePos, new Size(1, 1))))
                {
                    //node.Click(ref clickType);
                    node.Click(ref clickType);
                }
            }
        }

        private int GetHeuristic(Node startNode, Node goalNode)
        {
            return (Math.Abs(startNode.X - goalNode.X) + Math.Abs(startNode.Y - goalNode.Y)) * 15;
        }

        private void StartSetupNodes()
        {
            foreach (Node n in grid)
            {
                if (n.Type == NodeType.START) startNode = n;
            }
            Node tower = grid[2, 4];
            Targets.Add(tower);
            tower.KeyNumber = (Targets.IndexOf(tower) + 1).ToString();

            Node portal = grid[0, 8];
            Targets.Add(portal);
            portal.KeyNumber = (Targets.IndexOf(portal) + 1).ToString();
        }

        public void ASearchPath()
        {
            StartSetupNodes();

            while (Targets.Count > 0)
            {
                goalNode = Targets[0];
                Targets.RemoveAt(0);
                goalNode.Type = NodeType.GOAL;
                goalNode.H = 0;

                startNode.G = 0;
                startNode.H = GetHeuristic(startNode, goalNode);
                startNode.PrintData = true;
                open.Add(startNode);

                while (true)
                {
                    Node nextNode = GetNextNode();

                    if (nextNode == null) break;

                    if (nextNode.Type == NodeType.GOAL)
                    {
                        closed.Add(nextNode);
                        pathList.Add(nextNode);

                        while (true)
                        {
                            Node node = nextNode.ParentNode;
                            if (node.Type == NodeType.START) break;
                            pathList.Add(node);
                            nextNode = node;
                        }
                        break;
                    }
                    else CheckNode(nextNode);
                }
                if (pathList.Count > 0)
                {
                    foreach (Node node in pathList)
                    {
                        Thread.Sleep(100 + Form1.Speed * 4);
                        while(Form1.IsPaused) { }

                        node.NodeColor = Color.Blue;
                    }

                    if (open.Count > 0)
                    {
                        foreach (Node node in open)
                        {
                            node.PrintData = false;
                            if (node.NodeColor == Color.Yellow)
                                node.NodeColor = node.NativColor;
                        }
                    }
                    open.Clear();

                    if (closed.Count > 0)
                    {
                        foreach (Node node in closed)
                        {
                            node.PrintData = false;
                            if (node.NodeColor == Color.Orange)
                                node.NodeColor = node.NativColor;
                        }
                    }
                    closed.Clear();

                    pathList.Reverse();
                    while (pathList.Count > 0)
                    {
                        Thread.Sleep(100 + Form1.Speed * 4);
                        while (Form1.IsPaused) { }

                        Node newNode = pathList[0];
                        pathList.RemoveAt(0);
                        if (Targets.Contains(newNode)) Targets.Remove(newNode);
                        newNode.NodeColor = newNode.NativColor;

                        if (Targets.Count == 1 && pathList.Count == 0)
                        {
                            lock(startNode)
                            {
                                startNode.Sprite = null;
                            }
                            Thread.Sleep(200 + Form1.Speed * 4); // in the tower
                            lock (startNode)
                            {
                                startNode.Sprite = Image.FromFile(@"Images\wizard.png");
                            }
                            newNode.Type = NodeType.WALL;
                            lock (newNode)
                            {
                                newNode.Sprite = Image.FromFile(@"Images\towerwithfire.png");
                                newNode.KeyNumber = null;
                            }
                            startNode.NodeColor = startNode.NativColor;
                            break;
                        }
                        else if (Targets.Count == 0 && pathList.Count == 0)
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            startNode.Type = NodeType.EMPTY;
                            startNode.NodeColor = startNode.NativColor;
                            lock (newNode)
                            {
                                newNode.KeyNumber = null;
                            }
                            break;
                        }

                        newNode.Type = NodeType.START;
                        lock (newNode)
                        {
                            newNode.Sprite = startNode.Sprite;
                        }

                        if (ghostList.Contains(startNode))
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = Image.FromFile(@"Images\ghost.png");
                            }
                            startNode.Type = NodeType.WALL;
                        }
                        else
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            startNode.Type = NodeType.EMPTY;
                        }

                        startNode.NodeColor = startNode.NativColor;
                        startNode = newNode;
                    }
                }

            }
        }

        private Node GetNextNode()
        {
            if (open.Count > 1)
            {
                open.Sort((node1, node2) => node1.F.CompareTo(node2.F));            
            }
            Node node = open.First();
            open.Remove(node);
            return node;
        }


        public void CheckNode(Node nextNode)
        {
            closed.Add(nextNode);
            nextNode.NodeColor = Color.Orange;

            for (int x = nextNode.X - 1; x <= nextNode.X + 1; x++)
            {
                for (int y = nextNode.Y - 1; y <= nextNode.Y + 1; y++)
                {
                    Thread.Sleep(1 + Form1.Speed);

                    while (Form1.IsPaused) { }

                    if (x < 0 || x > 9 || y < 0 || y > 9 || grid[x, y].Type == NodeType.WALL || closed.Contains(grid[x, y]))
                    {
                        continue;
                    }
                    else
                    {
                        int valueG;

                        if (x == nextNode.X || y == nextNode.Y)
                        {
                            valueG = nextNode.G + 10;
                        }
                        else if (((x < nextNode.X && y < nextNode.Y) && (x < 9 && grid[x + 1, y].Type == NodeType.WALL)) || ((x < nextNode.X && y < nextNode.Y) && ((y < 9 && grid[x, y + 1].Type == NodeType.WALL))))
                        {
                            continue;
                        }

                        else if (((x < nextNode.X && y > nextNode.Y) && (x < 9 && grid[x + 1, y].Type == NodeType.WALL)) || ((x < nextNode.X && y > nextNode.Y) && ((y > 0 && grid[x, y - 1].Type == NodeType.WALL))))
                        {
                            continue;
                        }

                        else if (((x > nextNode.X && y < nextNode.Y) && ((y < 9 && grid[x, y + 1].Type == NodeType.WALL)) || ((x > nextNode.X && y < nextNode.Y) && (x > 0 && grid[x - 1, y].Type == NodeType.WALL))))
                        {
                            continue;
                        }

                        else if (((x > nextNode.X && y > nextNode.Y) && (x > 0 && grid[x - 1, y].Type == NodeType.WALL)) || ((x > nextNode.X && y > nextNode.Y) && (y > 0 && grid[x, y - 1].Type == NodeType.WALL)))
                        {
                            continue;
                        }

                        else valueG = nextNode.G + 14;

                        if (!open.Contains(grid[x, y]))
                        {
                            grid[x, y].G = valueG;
                            open.Add(grid[x, y]);
                            grid[x, y].ParentNode = nextNode;
                        }
                        else if (grid[x, y].G > valueG)
                        {
                            grid[x, y].G = valueG;
                            grid[x, y].ParentNode = nextNode;
                        }
                        else continue;

                        grid[x, y].H = GetHeuristic(grid[x, y], goalNode);
                    }
                    grid[x, y].NodeColor = Color.Yellow;
                    grid[x, y].PrintData = true;
                }
            }
        }

        public void RestartGame()
        {
            grid = CreateGrid();
            Targets.Clear();
            open.Clear();
            closed.Clear();
            nodesQueue.Clear();
            nodesQueue.Clear();
            clickType = NodeType.GOAL;
        }

        public void BSearchPass()
        {
            StartSetupNodes();

            startNode.ParentNode = startNode;

            while (Targets.Count > 0)
            {
                goalNode = Targets[0];
                Targets.RemoveAt(0);
                goalNode.Type = NodeType.GOAL;
                closed.Add(startNode);
                nodesQueue.Enqueue(startNode);

                bool runQueue = true;
                while (nodesQueue.Count > 0 && runQueue)
                {
                    Node nextNode = nodesQueue.Dequeue();
                    nextNode.NodeColor = Color.Orange;
                    closed.Add(nextNode);

                    for (int x = nextNode.X - 1; x <= nextNode.X + 1; x++)
                    {
                        for (int y = nextNode.Y - 1; y <= nextNode.Y + 1; y++)
                        {
                            while (Form1.IsPaused) { }

                            Thread.Sleep(10 + Form1.Speed);
                            if (x < 0 || x > 9 || y < 0 || y > 9 || grid[x, y].Type == NodeType.WALL || closed.Contains(grid[x, y]) || nodesQueue.Contains(grid[x, y]))
                            {
                                continue;
                            }
                            else if (x == nextNode.X || y == nextNode.Y)
                            {
                                if(grid[x,y] == goalNode)
                                {
                                    grid[x, y].ParentNode = nextNode;
                                    grid[x, y].NodeColor = Color.Yellow;
                                    pathList.Add(grid[x, y]);
                                    if(nextNode != startNode) pathList.Add(nextNode);

                                    while (true)
                                    {
                                        Node node = nextNode.ParentNode;
                                        if (node.Type == NodeType.START) break;
                                        pathList.Add(node);
                                        nextNode = node;
                                    }
                                    runQueue = false;
                                    break;
                                }
                                grid[x, y].ParentNode = nextNode;
                                grid[x, y].NodeColor = Color.Yellow;
                                if(grid[x, y].Type == NodeType.EMPTY)
                                {
                                    if (x < nextNode.X)
                                    {
                                        lock(grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\right.png");
                                        }
                                    }
                                    else if (x > nextNode.X)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\left.png");
                                        }
                                    }    
                                    else if (y < nextNode.Y)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\down.png");
                                        }
                                    }
                                    else if (y > nextNode.Y)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\up.png");
                                        }
                                    }
                                }
                                nodesQueue.Enqueue(grid[x, y]);
                            }

                        }
                        if (!runQueue) break;
                    }
                }
                nodesQueue.Clear();
                closed.Clear();

                if (pathList.Count > 0)
                {
                    foreach (Node node in pathList)
                    {
                        Thread.Sleep(100 + Form1.Speed * 4);
                        while (Form1.IsPaused) { }

                        node.NodeColor = Color.Blue;
                    }

                    foreach (Node node in grid)
                    {
                        if (node.NodeColor == Color.Yellow || node.NodeColor == Color.Orange || node.NodeColor == Color.Blue)
                        {
                            if (node.Type == NodeType.EMPTY)
                            {
                                lock (node)
                                {
                                    node.Sprite = null;
                                }
                            }
                            if (node.NodeColor == Color.Yellow || node.NodeColor == Color.Orange)
                                node.NodeColor = node.NativColor;
                        }
                    }

                    pathList.Reverse();
                    while (pathList.Count > 0)
                    {
                        Thread.Sleep(100 + Form1.Speed * 4);
                        while (Form1.IsPaused) { }

                        Node newNode = pathList[0];
                        pathList.RemoveAt(0);

                        if (Targets.Contains(newNode)) Targets.Remove(newNode);
                        newNode.NodeColor = newNode.NativColor;

                        if (Targets.Count == 1 && pathList.Count == 0) // if tower
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            Thread.Sleep(200 + Form1.Speed * 4); // in the tower
                            lock (startNode)
                            {
                                startNode.Sprite = Image.FromFile(@"Images\wizard.png");
                            }
                            newNode.Type = NodeType.WALL;
                            lock (newNode)
                            {
                                newNode.Sprite = Image.FromFile(@"Images\towerwithfire.png");
                                newNode.KeyNumber = null;
                            }
                            startNode.NodeColor = startNode.NativColor;
                            break;
                        }
                        else if (Targets.Count == 0 && pathList.Count == 0) // if portal
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            startNode.Type = NodeType.EMPTY;
                            startNode.NodeColor = startNode.NativColor;
                            lock (newNode)
                            {
                                newNode.KeyNumber = null;
                            }
                            break;
                        }

                        newNode.Type = NodeType.START;
                        lock (newNode)
                        {
                            newNode.Sprite = startNode.Sprite;
                        }

                        if (ghostList.Contains(startNode))
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = Image.FromFile(@"Images\ghost.png");
                            }
                            startNode.Type = NodeType.WALL;
                        }
                        else
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            startNode.Type = NodeType.EMPTY;
                        }

                        startNode.NodeColor = startNode.NativColor;
                        startNode = newNode;
                    }
                }
            }
        }

        public void DSearchPass()
        {
            StartSetupNodes();

            startNode.ParentNode = startNode;

            while (Targets.Count > 0)
            {
                goalNode = Targets[0];
                Targets.RemoveAt(0);
                goalNode.Type = NodeType.GOAL;
                closed.Add(startNode);
                nodesStack.Push(startNode);

                bool runQueue = true;
                while (nodesStack.Count > 0 && runQueue)
                {
                    Node nextNode = nodesStack.Pop();
                    nextNode.NodeColor = Color.Orange;
                    closed.Add(nextNode);

                    for (int x = nextNode.X - 1; x <= nextNode.X + 1; x++)
                    {
                        for (int y = nextNode.Y - 1; y <= nextNode.Y + 1; y++)
                        {
                            while (Form1.IsPaused) { }

                            Thread.Sleep(10 + Form1.Speed);
                            if (x < 0 || x > 9 || y < 0 || y > 9 || grid[x, y].Type == NodeType.WALL || closed.Contains(grid[x, y]) || nodesStack.Contains(grid[x, y]))
                            {
                                continue;
                            }
                            else if (x == nextNode.X || y == nextNode.Y)
                            {
                                if (grid[x, y] == goalNode)
                                {
                                    grid[x, y].ParentNode = nextNode;
                                    grid[x, y].NodeColor = Color.Yellow;
                                    pathList.Add(grid[x, y]);
                                    if (nextNode != startNode) pathList.Add(nextNode);

                                    while (true)
                                    {
                                        Node node = nextNode.ParentNode;
                                        if (node.Type == NodeType.START) break;
                                        pathList.Add(node);
                                        nextNode = node;
                                    }
                                    runQueue = false;
                                    break;
                                }
                                grid[x, y].ParentNode = nextNode;
                                grid[x, y].NodeColor = Color.Yellow;
                                if (grid[x, y].Type == NodeType.EMPTY)
                                {
                                    if (x < nextNode.X)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\right.png");
                                        }
                                    }
                                    else if (x > nextNode.X)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\left.png");
                                        }
                                    }
                                    else if (y < nextNode.Y)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\down.png");
                                        }
                                    }
                                    else if (y > nextNode.Y)
                                    {
                                        lock (grid[x, y])
                                        {
                                            grid[x, y].Sprite = Image.FromFile(@"Images\up.png");
                                        }
                                    }
                                }
                                nodesStack.Push(grid[x, y]);
                            }

                        }
                        if (!runQueue) break;
                    }
                }
                nodesStack.Clear();
                closed.Clear();

                if (pathList.Count > 0)
                {
                    foreach (Node node in pathList)
                    {
                        Thread.Sleep(100 + Form1.Speed * 4);
                        while (Form1.IsPaused) { }

                        node.NodeColor = Color.Blue;
                    }

                    foreach (Node node in grid)
                    {
                        if (node.NodeColor == Color.Yellow || node.NodeColor == Color.Orange || node.NodeColor == Color.Blue)
                        {
                            if (node.Type == NodeType.EMPTY)
                            {
                                lock (node)
                                {
                                    node.Sprite = null;
                                }
                            }
                            if (node.NodeColor == Color.Yellow || node.NodeColor == Color.Orange)
                                node.NodeColor = node.NativColor;
                        }
                    }

                    pathList.Reverse();
                    while (pathList.Count > 0)
                    {
                        Thread.Sleep(100 + Form1.Speed * 4);
                        while (Form1.IsPaused) { }

                        Node newNode = pathList[0];
                        pathList.RemoveAt(0);

                        if (Targets.Contains(newNode)) Targets.Remove(newNode);
                        newNode.NodeColor = newNode.NativColor;

                        if (Targets.Count == 1 && pathList.Count == 0) // if tower
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            Thread.Sleep(200 + Form1.Speed * 4); // in the tower
                            lock (startNode)
                            {
                                startNode.Sprite = Image.FromFile(@"Images\wizard.png");
                            }
                            newNode.Type = NodeType.WALL;
                            lock (newNode)
                            {
                                newNode.Sprite = Image.FromFile(@"Images\towerwithfire.png");
                                newNode.KeyNumber = null;
                            }
                            startNode.NodeColor = startNode.NativColor;
                            break;
                        }
                        else if (Targets.Count == 0 && pathList.Count == 0) // if portal
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            startNode.Type = NodeType.EMPTY;
                            startNode.NodeColor = startNode.NativColor;
                            lock (newNode)
                            {
                                newNode.KeyNumber = null;
                            }
                            break;
                        }

                        newNode.Type = NodeType.START;
                        lock (newNode)
                        {
                            newNode.Sprite = startNode.Sprite;
                        }

                        if (ghostList.Contains(startNode))
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = Image.FromFile(@"Images\ghost.png");
                            }
                            startNode.Type = NodeType.WALL;
                        }
                        else
                        {
                            lock (startNode)
                            {
                                startNode.Sprite = null;
                            }
                            startNode.Type = NodeType.EMPTY;
                        }

                        startNode.NodeColor = startNode.NativColor;
                        startNode = newNode;
                    }
                }
            }
        }
    }
}
