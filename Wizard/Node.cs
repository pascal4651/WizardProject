using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wizard
{
    enum NodeType { START, GOAL, WALL, EMPTY, KEY };

    class Node
    {
        public int G { get; set; }
        public int H { get; set; }
        public int F { get { return G + H; } }

        public int X { get { return position.X; } }
        public int Y { get { return position.Y; } }

        public Node ParentNode { get; set; }
        public Color NodeColor { get; set; }
        public Color NativColor { get; set; }
        public bool PrintData { get; set; }

        public string KeyNumber { get; set; }
        private int offset;

        /// <summary>
        /// The grid position of the cell
        /// </summary>
        private Point position;

        /// <summary>
        /// The size of the cell
        /// </summary>
        private int cellSize;

        /// <summary>
        /// The cell's sprite
        /// </summary>
        public Image Sprite { get; set; }

        /// <summary>
        /// Sets the celltype to empty as default
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// The bounding rectangle of the cell
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
               return new Rectangle(position.X * cellSize + offset, position.Y * cellSize + offset, cellSize, cellSize);
            }
        }

        /// <summary>
        /// The cell's constructor
        /// </summary>
        /// <param name="position">The cell's grid position</param>
        /// <param name="size">The cell's size</param>
        public Node(Point position, int size, int offset)
        {
            //Sets the position
            this.position = position;

            //Sets the cell size
            this.cellSize = size;

            this.offset = offset;

            Type = NodeType.EMPTY;

            NodeColor = NativColor = Color.Chartreuse;
            PrintData = false;
        }

        /// <summary>
        /// Renders the cell
        /// </summary>
        /// <param name="dc">The graphic context</param>
        public void Render(Graphics dc)
        {
            //Draws the rectangles color
            dc.FillRectangle(new SolidBrush(NodeColor), BoundingRectangle);

            //Draws the rectangles border
            dc.DrawRectangle(new Pen(Color.Black), BoundingRectangle);

            //If the cell has a sprite, then we need to draw it
            lock(this)
            {
                if (Sprite != null)
                {
                    dc.DrawImage(Sprite, BoundingRectangle);
                }
            }

            //Write's the cells grid position
            //dc.DrawString(string.Format("{0}", position), new Font("Arial", 7, FontStyle.Regular), new SolidBrush(Color.Black), position.X * cellSize, (position.Y * cellSize) + 10);
            if (PrintData)
            {
                dc.DrawString(string.Format("G:{0}", G), new Font("Arial", 10, FontStyle.Regular), new SolidBrush(Color.Black), position.X * cellSize + offset, (position.Y * cellSize) + 5 + offset);
                dc.DrawString(string.Format("H:{0}", H), new Font("Arial", 10, FontStyle.Regular), new SolidBrush(Color.Black), position.X * cellSize + offset, (position.Y * cellSize) + 20 + offset);
                dc.DrawString(string.Format("F:{0}", F), new Font("Arial", 10, FontStyle.Regular), new SolidBrush(Color.Black), position.X * cellSize + offset, (position.Y * cellSize) + 35 + offset);
            }


            if(Type == NodeType.KEY || Type == NodeType.GOAL || Type == NodeType.WALL)
            {
                if(!Form1.GameRunning && Map.Targets.Contains(this))
                {
                    KeyNumber = (Map.Targets.IndexOf(this) + 1).ToString();
                }
                lock (this)
                {
                    if (KeyNumber != null)
                        dc.DrawString(string.Format(KeyNumber), new Font("Arial", 16, FontStyle.Regular), new SolidBrush(Color.DarkRed), position.X * cellSize + offset, (position.Y * cellSize) + 40 + offset);
                }
            }
        }

        /// <summary>
        /// Clicks the cell
        /// </summary>
        /// <param name="clickType">The click type</param>
        public void Click(ref NodeType clickType)
        {
            if (Type == NodeType.EMPTY && clickType == NodeType.GOAL) //If the click type is START
            {
                Sprite = Image.FromFile(@"Images\fire.png");
                Type = clickType;
                clickType = NodeType.KEY;
                Map.Targets.Insert(0, this);
            }
            else if (Type == NodeType.EMPTY && clickType == NodeType.KEY) //If the click type is START
            {
                Sprite = Image.FromFile(@"Images\key.png");
                Type = clickType;
                Map.Targets.Add(this);
            }
            else if (Type == NodeType.GOAL && clickType == NodeType.KEY) //If the click type is GOAL
            {
                Sprite = null;
                clickType = NodeType.GOAL;
                Type = NodeType.EMPTY;
                Map.Targets.Remove(this);
            }      
            else if (Type == NodeType.KEY && clickType == NodeType.KEY) //If the click type is START
            {
                Sprite = null;
                Type = NodeType.EMPTY;
                Map.Targets.Remove(this);
            }
        }
    }
}
