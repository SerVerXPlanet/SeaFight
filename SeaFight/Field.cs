using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;


namespace SeaFight
{
    public enum Mode { View, Battle, Build, Check};

    public enum Status { Empty, Miss, Hit, Ship, Ghost, Buffer, ShipOff, BufferOff }; // пустая, промах, попадание, корабль, временный корабль, буферная зона, запретная зона по кораблю, запретная зона по буферу

    public enum Step { Prepare, Run, Wait, Stop };


    public partial class Field: UserControl
    {
        #region Constants
        const int MAX_SIZE = 30;
        const int INDENT = 3; // отступ от нижнего и правого краёв
        const float INNER_LINE_RATIO = 0.02f; // отношение ширины контрола к толщине внутренней линии
        const float BORDER_LINE_RATIO = 0.1f; // отношение ширины контрола к толщине внешней линии
        const string ALPHABET = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧШЩЪЫЭЮЯ";
        readonly Color W_COLOR = Color.Red; // красный цвет светофора
        readonly Color P_COLOR = Color.Yellow; // желтый цвет светофора
        readonly Color R_COLOR = Color.Green; // зеленый цвет светофора
        readonly Color S_COLOR = Color.Black; // черный цвет светофора

        #endregion // Constants


        #region Variables

        float cellWidth; // ширина ячейки
        float cellHeight; // высота ячейки

        Mode mode; // режим поля

        float[] linesX; // координаты вертикальных линий
        float[] linesY; // координаты горизонтальных линий

        Status[,] cells; // статусы ячеек поля
        Status[,] enemies; // статусы ячеек вражеского поля

        int tempWidth; // буфер для хранения ширины поля
        int tempHeight; // буфер для хранения высоты поля

        int nWidth; // размер поля по горизонтали (в кол-ве ячеек)
        int nHeight; // размер поля по вертикали (в кол-ве ячеек)
        
        Color borderColor; // цвет внешних линий
        Color innerColor; // цвет внутренних линий
        Color fontColor; // цвет надписей
        Color shipColor; // цвет корабля
        Color bufferColor; // цвет буферной зоны
        Color missColor; // цвет промаха
        Color hitColor; // цвет попадания
        Color cursorColor; // цвет обводки последней ячейки
        Color lights; // цвет светофора

        string[] namesX; // наименования по оси абцисс
        string[] namesY; // наименования по оси ординат

        List<int> ships; // список возможных кораблей
        int currentShipIndex; // индекс текущего корабля
        bool isWheel; // выполнен скролл мышью
        bool orientation; // горизонтальная (true) или вертикальная (false) ориентация корабля
        Point tempPos; // предыдущая ячейка при установке корабля
        Point[] shipCoords; // ячейки временного корабля

        List<Ship> myShips; // мои корабли
        List<Ship> itsShips; // корабли противника

        Point activeCell; // ячейка, по которой кликнули мышью

        bool shipsInstalled;
        bool shipsDestroyed;
        bool shipsChecked;

        #endregion // Variables


        #region Constructors

        public Field()
        {
            InitializeComponent();

            nWidth = 10;
            nHeight = 10;
            BackColor = Color.White;
            borderColor = Color.DarkBlue;
            innerColor = Color.Blue;
            fontColor = Color.OrangeRed;
            shipColor = Color.FromArgb(40, 40, 40);
            bufferColor = Color.Gray;
            missColor = Color.SaddleBrown;
            hitColor = Color.Red;
            cursorColor = Color.Cyan;

            namesX = Enumerable.Range(0, MAX_SIZE)
                .Select(c => ALPHABET.Substring(c, 1)).ToArray(); // А..Я (кроме Ё, Й, Ь)

            namesY = Enumerable.Range(1, MAX_SIZE)
                .Select(x => x.ToString().PadLeft(2, ' ')).ToArray(); // 1..30

            //FillFieldRandom();

            UpdateSpace();

            myShips = new List<Ship>();
            itsShips = new List<Ship>();

            orientation = false;
            tempPos = new Point(0, 0);

            this.MouseWheel += Field_MouseWheel;
        }

        #endregion // Constructors


        #region Properties

        [Category("Settings"), Description("Horizontal dimension.")]
        public int HorizontalSize
        {
            get => nWidth;
            set
            {
                nWidth = value;

                if (nWidth < 1)
                    nWidth = 1;

                if (nWidth > MAX_SIZE)
                    nWidth = MAX_SIZE;

                Width = (int)((float)Height * (nWidth + 1) / (nHeight + 1)); // ширина контрола
                tempWidth = Width;

                UpdateSpace();

                //FillFieldRandom();

                // The Invalidate method calls the OnPaint method, which redraws the control.  
                Invalidate();
            }
        }


        [Category("Settings"), Description("Vertical dimension.")]
        public int VerticalSize
        {
            get => nHeight;
            set
            {
                nHeight = value;

                if (nHeight < 1)
                    nHeight = 1;

                if (nHeight > MAX_SIZE)
                    nHeight = MAX_SIZE;

                Height = (int)((float)Width * (nHeight + 1) / (nWidth + 1)); // высота контрола
                tempHeight = Height;

                UpdateSpace();

                //FillFieldRandom();

                // The Invalidate method calls the OnPaint method, which redraws the control.  
                Invalidate();
            }
        }


        [Category("Settings"), Description("Border lines color.")]
        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Inner lines color.")]
        public Color InnerColor
        {
            get => innerColor;
            set
            {
                innerColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Font color.")]
        public Color FontColor
        {
            get => fontColor;
            set
            {
                fontColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Ship color.")]
        public Color ShipColor
        {
            get => shipColor;
            set
            {
                shipColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Buffer color.")]
        public Color BufferColor
        {
            get => bufferColor;
            set
            {
                bufferColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Miss color.")]
        public Color MissColor
        {
            get => missColor;
            set
            {
                missColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Hit color.")]
        public Color HitColor
        {
            get => hitColor;
            set
            {
                hitColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Cursor color.")]
        public Color CursorColor
        {
            get => cursorColor;
            set
            {
                cursorColor = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("X-axis titles.")]
        public string[] NamesX
        {
            get => namesX;
            set
            {
                namesX = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("Y-axis titles.")]
        public string[] NamesY
        {
            get => namesY;
            set
            {
                namesY = value;

                Invalidate();
            }
        }


        [Category("Settings"), Description("List of ships.")]
        public int[] Ships
        {
            get => ships?.ToArray();
            set
            {
                ships = value.ToList<int>().FindAll(x => x > 0);
            }
        }


        public Mode Mode
        {
            get => mode;
            set
            {
                mode = value;

                //ClearField(cells);
                //ClearField(enemies);
            }
        }


        public Status[,] Cells
        {
            get => cells;
            set => cells = value;
        }


        public Status[,] Enemies
        {
            get => enemies;
        }


        public Point ActiveCell
        {
            get => activeCell;
        }


        public List<Ship> MyShips
        {
            get => myShips;
        }

        public List<Ship> ItsShips
        {
            get => itsShips;
        }


        #endregion // Properties


        #region Events

        public event EventHandler FieldClick;

        public delegate void Installed();
        public event Installed ShipsInstalled;

        public delegate void Destroyed();
        public event Destroyed ShipsDestroyed;

        public delegate void Checked();
        public event Checked ShipsChecked;

        #endregion // Events


        private void Field_Load(object sender, EventArgs e)
        {
            tempWidth = Width;
            tempHeight = Height;

            ClearField(cells);
            ClearField(enemies);

            mode = Mode.View;

            //mode = Mode.Build;

            //mode = Mode.Battle;
            ships = new List<int> { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            //bool result = GenerateShips();

            currentShipIndex = 0;

            lights = BackColor;
        }


        private void Field_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            float fWidth = Width - INDENT;
            float fHeight = Height - INDENT;

            cellWidth = fWidth / (nWidth + 1);
            cellHeight = fHeight / (nHeight + 1);

            // cellWidth == cellHeight
            float halfCellSize = 0.5f * cellWidth;
            float quarterCellSize = 0.25f * cellWidth;
            float restFromCross = BORDER_LINE_RATIO * cellWidth;
            float innerLineWidth = INNER_LINE_RATIO * cellWidth;
            float borderLineWidth = BORDER_LINE_RATIO * cellWidth;
            float crossLineWidth = innerLineWidth * 4;
            float fontSizeX = 0.65f * cellWidth;
            float fontSizeY = 0.55f * cellWidth;

            float w;
            float h;

            // отрисовка ячеек поля
            for (int i = 1; i <= nWidth; i++)
            {
                for (int j = 1; j <= nHeight; j++)
                {
                    switch (cells[i, j])
                    {
                        case Status.Empty:
                            g.FillRectangle(new SolidBrush(BackColor), linesX[i], linesY[j], cellWidth, cellHeight);
                            break;

                        case Status.Buffer:
                            g.FillEllipse(new SolidBrush(bufferColor), linesX[i] + quarterCellSize, linesY[j] + quarterCellSize, halfCellSize, halfCellSize);
                            break;

                        case Status.Miss:
                            g.FillEllipse(new SolidBrush(missColor), linesX[i] + quarterCellSize, linesY[j] + quarterCellSize, halfCellSize, halfCellSize);
                            break;

                        case Status.Hit:
                            g.FillRectangle(new SolidBrush(shipColor), linesX[i], linesY[j], cellWidth, cellHeight);
                            g.DrawLine(new Pen(hitColor, crossLineWidth), linesX[i] + restFromCross, linesY[j] + restFromCross, linesX[i + 1] - restFromCross, linesY[j + 1] - restFromCross);
                            g.DrawLine(new Pen(hitColor, crossLineWidth), linesX[i] + restFromCross, linesY[j + 1] - restFromCross, linesX[i + 1] - restFromCross, linesY[j] + restFromCross);
                            break;

                        case Status.Ship:
                            g.FillRectangle(new SolidBrush(shipColor), linesX[i], linesY[j], cellWidth, cellHeight);
                            break;

                        case Status.Ghost:
                            g.FillRectangle(new SolidBrush(bufferColor), linesX[i], linesY[j], cellWidth, cellHeight);
                            break;

                        case Status.ShipOff:
                        case Status.BufferOff:
                            g.FillRectangle(new SolidBrush(hitColor), linesX[i], linesY[j], cellWidth, cellHeight);
                            break;
                    }
                }
            }

            // отрисовка нулевой ячейки-светофора
            g.FillRectangle(new SolidBrush(lights), linesX[0], linesY[0], cellWidth, cellHeight);

            // отрисовка вертикальных линий
            for (int i = 0; i <= nWidth + 1; i++)
            {
                w = cellWidth * i;
                linesX[i] = w;
                g.DrawLine(new Pen(innerColor, innerLineWidth), new PointF(w, 0), new PointF(w, fHeight));

                if(i < nWidth)
                    g.DrawString(namesX[i], new Font("Courier New", fontSizeX, FontStyle.Bold), new SolidBrush(fontColor), new PointF(cellWidth + w + 2.0f, 0));
            }

            // отрисовка горизонтальных линий
            for (int j = 0; j <= nHeight + 1; j++)
            {
                h = cellHeight * j;
                linesY[j] = h;
                g.DrawLine(new Pen(innerColor, innerLineWidth), new PointF(0, h), new PointF(fWidth, h));

                if (j < nHeight)
                    g.DrawString(namesY[j], new Font("Arial", fontSizeY, FontStyle.Bold), new SolidBrush(fontColor), new PointF(0, cellHeight + h));
            }

            // отрисовка границ поля
            g.DrawRectangle(new Pen(borderColor, borderLineWidth), cellWidth, cellHeight, fWidth - cellWidth, fHeight - cellHeight);

            // отрисовка выделения последней ячейки
            g.DrawRectangle(new Pen(cursorColor, crossLineWidth), linesX[activeCell.X], linesY[activeCell.Y], cellWidth, cellHeight);
        }


        private void Field_Resize(object sender, EventArgs e)
        {
            if (Width != tempWidth)
            {
                Height = (int)((float)Width * (nHeight + 1) / (nWidth + 1));
            }

            if (Height != tempHeight)
            {
                Width = (int)((float)Height * (nWidth + 1) / (nHeight + 1));
            }

            tempWidth = Width;
            tempHeight = Height;

            this.Update();
        }


        #region MouseEvents

        private void Field_MouseClick(object sender, MouseEventArgs e)
        {
            if (mode == Mode.View)
                return;

            Point currentCell = GetCurrentCell(e.X, e.Y);

            bool isLegal = ActivateCell(currentCell, e.Button);

            if(isLegal)
                FieldClick?.Invoke(sender, e);
        }


        private void Field_MouseMove(object sender, MouseEventArgs e)
        {
            if (mode == Mode.Build && ships.Count > 0)
            {
                Point currentCell = GetCurrentCell(e.X, e.Y);

                if (currentCell.X == 0 || currentCell.Y == 0)
                {
                    this.OnMouseLeave(e);
                    return;
                }

                if (isWheel || (tempPos.X != currentCell.X || tempPos.Y != currentCell.Y))
                {
                    //Debug.WriteLine(String.Format("temp {0}:{1}    curr {2}:{3}", tempPos.X, tempPos.Y, currentCell.X, currentCell.Y));

                    int currentShip = ships[currentShipIndex];
                    Point shipPositionVector = GetShipPositionVector(currentShip, currentCell, orientation);
                    shipCoords = GetShipCoords(currentShip, shipPositionVector, currentCell, orientation);
                    CreateTempShip(cells, shipPositionVector, currentCell, orientation);

                    tempPos = new Point(currentCell.X, currentCell.Y);
                    isWheel = false;
                }

                Invalidate();
            }
        }


        private void Field_MouseLeave(object sender, EventArgs e)
        {
            if (mode == Mode.Build)
            {
                for (int i = 1; i <= nWidth; i++)
                {
                    for (int j = 1; j <= nHeight; j++)
                    {
                        switch (cells[i, j])
                        {
                            case Status.Ghost:
                                cells[i, j] = Status.Empty;
                                break;
                            case Status.BufferOff:
                                cells[i, j] = Status.Buffer;
                                break;
                            case Status.ShipOff:
                                cells[i, j] = Status.Ship;
                                break;
                        }
                    }
                }

                tempPos = new Point(0, 0);

                Invalidate();
            }
        }


        private void Field_MouseWheel(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
            if (mode == Mode.Build)
            {
                currentShipIndex = (ships.Count + currentShipIndex + (e.Delta > 0 ? -1 : 1)) % ships.Count;
                isWheel = true;
                Field_MouseMove(sender, e);
            }

            //Debug.Print(currentShipIndex.ToString());
        }

        #endregion // MouseEvents


        Point GetCurrentCell(int x, int y)
        {
            Point currentCell = new Point(0, 0);

            for (int i = 0; i < linesX.Length; i++)
            {
                if (x >= linesX[i])
                    currentCell.X = Math.Min(i, nWidth); // MIN на случай выбора ячейки за границей последней линии из-за округления
            }

            for (int j = 0; j < linesY.Length; j++)
            {
                if (y >= linesY[j])
                    currentCell.Y = Math.Min(j, nHeight);  // MIN на случай выбора ячейки за границей последней линии из-за округления
            }

            return currentCell;
        }


        public void ClearField(Status[,] cells, Status newStatus = Status.Empty)
        {
            for (int i = 1; i <= nWidth; i++)
            {
                for (int j = 1; j <= nHeight; j++)
                {
                    cells[i, j] = newStatus;
                }
            }
        }


        public void ClearStatus(Status[,] cells, Status status, Status newStatus = Status.Empty)
        {
            for (int i = 1; i <= nWidth; i++)
            {
                for (int j = 1; j <= nHeight; j++)
                {
                    if (cells[i, j] == status)
                        cells[i, j] = newStatus;
                }
            }
        }


        void FillFieldRandom()
        {
            Random rnd = new Random();

            int countStatus = Enum.GetValues(typeof(Status)).Length;

            linesX = new float[nWidth + 1 + 1];
            linesY = new float[nHeight + 1 + 1];

            cells = new Status[nWidth + 1, nHeight + 1];
            enemies = new Status[nWidth + 1, nHeight + 1];

            for (int i = 0; i <= nWidth; i++)
            {
                for (int j = 0; j <= nHeight; j++)
                {
                    if (i == 0 || j == 0)
                        cells[i, j] = Status.Empty;
                    else
                        cells[i, j] = (Status)(rnd.Next(0, countStatus));
                }
            }
        }


        void UpdateSpace()
        {
            cells = new Status[nWidth + 1, nHeight + 1];
            enemies = new Status[nWidth + 1, nHeight + 1];

            linesX = new float[nWidth + 1 + 1];
            linesY = new float[nHeight + 1 + 1];
        }


        public bool ActivateCell(Point currentCell, MouseButtons mouseButtons = MouseButtons.None)
        {
            if (mode == Mode.View)
                return false;

            if (mode == Mode.Battle || mode == Mode.Check)
                activeCell = currentCell;

            if (currentCell.X == 0 || currentCell.Y == 0)
            {
                return false;
            }

            if (mode == Mode.Battle)
            {
                if (!shipsDestroyed)
                {
                    if (cells[currentCell.X, currentCell.Y] == Status.Empty)
                    {
                        if (enemies[currentCell.X, currentCell.Y] == Status.Ship)
                        {
                            cells[currentCell.X, currentCell.Y] = Status.Hit;

                            if (IsShipDestroyed(currentCell))
                            {
                                //Debug.WriteLine("FULL");
                                SetBufferZone(cells, GetHitCoords(currentCell));
                            }
                        }
                        else
                        {
                            cells[currentCell.X, currentCell.Y] = Status.Miss;
                        }

                        shipsDestroyed = DestructionCheck();

                        if (shipsDestroyed)
                        {
                            Invalidate();
                            ShipsDestroyed?.Invoke();
                        }
                    }
                    else
                        return false;
                }
            }
            else if (mode == Mode.Check)
            {
                if (!shipsChecked)
                {
                    if (cells[currentCell.X, currentCell.Y] == Status.Ship)
                    {
                        cells[currentCell.X, currentCell.Y] = Status.Hit;

                        if (IsShipDestroyed(currentCell, true))
                        {
                            //Debug.WriteLine("FULL");
                            SetBufferZone(cells, GetHitCoords(currentCell));
                        }

                        shipsChecked = NavyCheck();

                        if (shipsChecked)
                        {
                            Invalidate();
                            ShipsChecked?.Invoke();
                        }
                    }
                    else if (cells[currentCell.X, currentCell.Y] == Status.Empty)
                    {
                        cells[currentCell.X, currentCell.Y] = Status.Miss;
                    }
                    else
                        return false;
                }
            }
            else if (mode == Mode.Build)
            {
                if (!shipsInstalled)
                {
                    if (mouseButtons == MouseButtons.Right)
                    {
                        orientation = !orientation;

                        tempPos = new Point(0, 0);
                    }
                    else if (mouseButtons == MouseButtons.Left)
                    {
                        _ = SetShip(cells, shipCoords, ships, ref currentShipIndex, currentCell);
                    }

                    shipsInstalled = ships.Count == 0;

                    if (shipsInstalled)
                    {
                        ClearStatus(cells, Status.Buffer);
                        Invalidate();
                        ShipsInstalled?.Invoke();
                    }
                }
            }

            Invalidate();

            return true;
        }


        public bool IsShipDestroyed(Point currentCell, bool shipsOnly = false)
        {
            bool fullShip = true;

            bool left = CheckShip(currentCell, true, -1, shipsOnly);
            bool right = CheckShip(currentCell, true, 1, shipsOnly);
            bool top = CheckShip(currentCell, false, -1, shipsOnly);
            bool bottom = CheckShip(currentCell, false, 1, shipsOnly);

            if (!(left && right && top && bottom))
                fullShip = false;

            return fullShip;
        }


        Point[] GetHitCoords(Point currentCell)
        {
            Point[] hitShip;

            bool orientation = false;
            int left = currentCell.X - 1;
            int rigft = currentCell.X + 1;

            if (left > 0 && cells[left, currentCell.Y] == Status.Hit || rigft <= nWidth && cells[rigft, currentCell.Y] == Status.Hit)
                orientation = true;

            int n;
            int start = -1;
            int end = -1;

            if (orientation)
            {
                n = currentCell.X;

                while (n > 0 && cells[n, currentCell.Y] == Status.Hit)
                {
                    start = n;
                    n--;
                }

                n = currentCell.X;

                while (n <= nWidth && cells[n, currentCell.Y] == Status.Hit)
                {
                    end = n;
                    n++;
                }

                hitShip = new Point[end - start + 1];

                for (int i = start; i <= end; i++)
                {
                    hitShip[i - start] = new Point(i, currentCell.Y);
                }
            }
            else
            {
                n = currentCell.Y;

                while (n > 0 && cells[currentCell.X, n] == Status.Hit)
                {
                    start = n;
                    n--;
                }

                n = currentCell.Y;

                while (n <= nHeight && cells[currentCell.X, n] == Status.Hit)
                {
                    end = n;
                    n++;
                }

                hitShip = new Point[end - start + 1];

                for (int j = start; j <= end; j++)
                {
                    hitShip[j - start] = new Point(currentCell.X, j);
                }
            }

            return hitShip;
        }


        bool CheckShip(Point currentCell, bool direct, int delta, bool shipsOnly = false)
        {
            bool result = true;

            int n = (direct ? currentCell.X : currentCell.Y) + delta;
            int min = 0;
            int max = direct ? nWidth : nHeight;
            int x = direct ? n : currentCell.X;
            int y = direct ? currentCell.Y : n;

            if (delta == -1)
            {
                if (n > min)
                    if (shipsOnly || enemies[x, y] == Status.Ship)
                        if(cells[x, y] == Status.Hit)
                            result = CheckShip(new Point(x, y), direct, delta, shipsOnly);
                        else if(shipsOnly && cells[x, y] != Status.Ship)
                            return true;
                        else
                            return false;
            }
            else
            {
                if (n <= max)
                    if (shipsOnly || enemies[x, y] == Status.Ship)
                        if (cells[x, y] == Status.Hit)
                            result = CheckShip(new Point(x, y), direct, delta, shipsOnly);
                        else if(shipsOnly && cells[x, y] != Status.Ship)
                            return true;
                        else
                            return false;
            }

            return result;
        }


        void SetBufferZone(Status[,] cells, Point[] shipCoords)
        {
            // установим буферную зону вокруг корабля
            for (int i = shipCoords[0].X - 1; i <= shipCoords[shipCoords.Length - 1].X + 1; i++)
            {
                for (int j = shipCoords[0].Y - 1; j <= shipCoords[shipCoords.Length - 1].Y + 1; j++)
                {
                    if (i > 0 && i <= nWidth && j > 0 && j <= nHeight)
                    {
                        if (cells[i, j] != Status.Ship && cells[i, j] != Status.Hit && cells[i, j] != Status.Miss)
                            cells[i, j] = Status.Buffer;
                    }
                }
            }
        }


        Point GetShipPositionVector(int currentShip, Point currentCell, bool orientation)
        {
            int median = currentShip / 2;
            int head = 0 - median + (currentShip % 2 == 0 ? 1 : 0);
            int tail = 0 + median;

            int coord = 0;
            int len = 0;

            switch (orientation)
            {
                case true: // фигура по горизонтали
                    coord = currentCell.X;
                    len = nWidth;
                    break;
                case false: // фигура по вертикали
                    coord = currentCell.Y;
                    len = nHeight;
                    break;
            }

            int start = coord + head;
            int end = coord + tail;

            if (start < 1)
            {
                end += 1 - start;
                start = 1;
            }

            if (end > len)
            {
                start -= end - len;
                end = len;
            }

            return new Point(start, end);
        }


        Point[] GetShipCoords(int currentShip, Point shipPositionVector, Point currentCell, bool orientation)
        {
            Point[] shipCoords = new Point[currentShip];

            int start = shipPositionVector.X;
            int end = shipPositionVector.Y;

            switch (orientation)
            {
                case true: // фигура по горизонтали
                    for (int i = start; i <= end; i++)
                    {
                        shipCoords[i - start] = new Point(i, currentCell.Y);
                    }
                    break;
                case false: // фигура по вертикали
                    for (int j = start; j <= end; j++)
                    {
                        shipCoords[j - start] = new Point(currentCell.X, j);
                    }
                    break;
            }

            return shipCoords;
        }


        void CreateTempShip(Status[,] cells, Point shipPositionVector, Point currentCell, bool orientation)
        {
            // очистим предыдущее положение временного корабля

            for (int i = 1; i <= nWidth; i++)
            {
                switch (cells[i, tempPos.Y])
                {
                    case Status.Ghost:
                        cells[i, tempPos.Y] = Status.Empty;
                        break;
                    case Status.BufferOff:
                        cells[i, tempPos.Y] = Status.Buffer;
                        break;
                    case Status.ShipOff:
                        cells[i, tempPos.Y] = Status.Ship;
                        break;
                }
                switch (cells[i, currentCell.Y])
                {
                    case Status.Ghost:
                        cells[i, currentCell.Y] = Status.Empty;
                        break;
                    case Status.BufferOff:
                        cells[i, currentCell.Y] = Status.Buffer;
                        break;
                    case Status.ShipOff:
                        cells[i, currentCell.Y] = Status.Ship;
                        break;
                }
            }

            for (int j = 1; j <= nHeight; j++)
            {
                switch (cells[tempPos.X, j])
                {
                    case Status.Ghost:
                        cells[tempPos.X, j] = Status.Empty;
                        break;
                    case Status.BufferOff:
                        cells[tempPos.X, j] = Status.Buffer;
                        break;
                    case Status.ShipOff:
                        cells[tempPos.X, j] = Status.Ship;
                        break;
                }
                switch (cells[currentCell.X, j])
                {
                    case Status.Ghost:
                        cells[currentCell.X, j] = Status.Empty;
                        break;
                    case Status.BufferOff:
                        cells[currentCell.X, j] = Status.Buffer;
                        break;
                    case Status.ShipOff:
                        cells[currentCell.X, j] = Status.Ship;
                        break;
                }
            }

            int start = shipPositionVector.X;
            int end = shipPositionVector.Y;

            switch (orientation)
            {
                case true: // фигура по горизонтали
                    for (int i = start; i <= end; i++)
                    {
                        switch(cells[i, currentCell.Y])
                        {
                            case Status.Buffer:
                                cells[i, currentCell.Y] = Status.BufferOff;
                                break;
                            case Status.Ship:
                                cells[i, currentCell.Y] = Status.ShipOff;
                                break;
                            default:
                                cells[i, currentCell.Y] = Status.Ghost;
                                break;
                        }
                    }
                    break;
                case false: // фигура по вертикали
                    for (int j = start; j <= end; j++)
                    {
                        switch (cells[currentCell.X, j])
                        {
                            case Status.Buffer:
                                cells[currentCell.X, j] = Status.BufferOff;
                                break;
                            case Status.Ship:
                                cells[currentCell.X, j] = Status.ShipOff;
                                break;
                            default:
                                cells[currentCell.X, j] = Status.Ghost;
                                break;
                        }
                    }
                    break;
            }
        }


        bool SetShip(Status[,] cells, Point[] shipCoords, List<int> ships, ref int currentShipIndex, Point currentCell)
        {
            bool isPossible = true;

            // проверка на возможность установки корабля
            foreach (Point p in shipCoords)
            {
                if (cells[p.X, p.Y] == Status.Buffer || cells[p.X, p.Y] == Status.Ship || cells[p.X, p.Y] == Status.BufferOff || cells[p.X, p.Y] == Status.ShipOff)
                {
                    isPossible = false;
                    break;
                }
            }

            if (isPossible)
            {
                //for (int i = 1; i <= nWidth; i++)
                //{
                //    if (cells[i, currentCell.Y] == Status.Ghost)
                //        cells[i, currentCell.Y] = Status.Ship;
                //}

                //for (int j = 1; j <= nHeight; j++)
                //{
                //    if (cells[currentCell.X, j] == Status.Ghost)
                //        cells[currentCell.X, j] = Status.Ship;
                //}
                foreach (Point p in shipCoords)
                {
                    cells[p.X, p.Y] = Status.Ship;
                }

                myShips.Add(new Ship(ships[currentShipIndex], orientation, shipCoords));

                ships.RemoveAt(currentShipIndex);

                if(ships.Count > 0 && currentShipIndex == ships.Count)
                    currentShipIndex--;

                SetBufferZone(cells, shipCoords);
            }

            return isPossible;
        }


        public bool GenerateShips()
        {
            //if (mode != Mode.Battle)
            //    return false;

            //int sq = 0;

            //foreach (var shipSize in ships.ToArray())
            //{
            //    sq += shipSize;
            //}

            //if (sq > nWidth * nHeight * 0.33)
            //    return false;

            enemies = new Status[nWidth + 1, nHeight + 1];

            ClearField(enemies);

            tempPos = new Point(0, 0);

            bool setSuccess;

            Random rnd = new Random();

            List<int> enemyShips = new List<int>(ships);
            enemyShips.OrderByDescending(n => n);
            //enemyShips = (new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }).ToList<int>();
            //enemyShips = Enumerable.Repeat(1, 20).ToList<int>();

            int enemyIndex = 0;

            int iterationsCount = 10 * nWidth * nHeight; // на случай большого кол-ва кораблей

            while(enemyShips.Count > 0 && iterationsCount != 0)
            {
                Point currentCell = new Point(rnd.Next(1, nWidth + 1), rnd.Next(1, nHeight + 1));
                orientation = Convert.ToBoolean(rnd.Next(0, 2));

                int enemyShipLen = enemyShips[enemyIndex];
                Point shipPositionVector = GetShipPositionVector(enemyShipLen, currentCell, orientation);
                Point[] tempShip = GetShipCoords(enemyShipLen, shipPositionVector, currentCell, orientation);

                setSuccess = true;

                foreach (var p in tempShip)
                {
                    if (enemies[p.X, p.Y] == Status.Buffer || enemies[p.X, p.Y] == Status.Ship || enemies[p.X, p.Y] == Status.BufferOff || enemies[p.X, p.Y] == Status.ShipOff)
                    {
                        setSuccess = false;
                        break;
                    }
                }

                if (setSuccess)
                {
                    CreateTempShip(enemies, shipPositionVector, currentCell, orientation);
                    _ = SetShip(enemies, tempShip, enemyShips, ref enemyIndex, currentCell);
                    itsShips.Add(new Ship(enemyShipLen, orientation, tempShip));
                }

                iterationsCount--;
            }

            if(enemyShips.Count > 0)
            {
                ClearField(enemies);
                return false;
            }

            ClearStatus(enemies, Status.Buffer);

            return true;
        }


        public bool DestructionCheck()
        {
            for (int i = 1; i <= nWidth; i++)
            {
                for (int j = 1; j <= nHeight; j++)
                {
                    if (enemies[i, j] == Status.Ship && cells[i, j] != Status.Hit)
                        return false;
                }
            }

            return true;
        }


        public bool NavyCheck()
        {
            for (int i = 1; i <= nWidth; i++)
            {
                for (int j = 1; j <= nHeight; j++)
                {
                    if (cells[i, j] == Status.Ship)
                        return false;
                }
            }

            return true;
        }


        public void SetLights(Step step)
        {
            switch (step)
            {
                case Step.Prepare:
                    lights = P_COLOR;
                    break;
                case Step.Run:
                    lights = R_COLOR;
                    break;
                case Step.Wait:
                    lights = W_COLOR;
                    break;
                case Step.Stop:
                    lights = S_COLOR;
                    break;
                default:
                    lights = BackColor;
                    break;
            }

            Invalidate();
        }


        public void ClearState()
        {
            ClearField(cells);
            ClearField(enemies);

            ships = new List<int> { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            currentShipIndex = 0;

            shipsInstalled = false;
            shipsDestroyed = false;
            shipsChecked = false;

            activeCell = new Point();

            lights = BackColor;

            myShips.Clear();
            itsShips.Clear();
        }


        public override string ToString()
        {
            string str = string.Empty;

            str += ShipsToStr(myShips);

            str += ShipsToStr(itsShips);

            return str;
        }


        string ShipsToStr(List<Ship> ships)
        {
            string str = string.Empty;

            for (int i = 1; i <= nWidth; i++)
            {
                for (int j = 1; j <= nHeight; j++)
                {
                    if (ships.Find(x => x.Coords.FirstOrDefault(c => c.X == i && c.Y == j) != new Point()) != null)
                        str += "#";
                    else
                        str += "*";
                }

                str += "\n";
            }

            str += "\n";

            return str;
        }
    }
}
