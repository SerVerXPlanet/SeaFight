using System.Drawing;


namespace SeaFight
{
    //struct Position
    //{
    //    public Point start;
    //    public Point end;

    //    //public void Print()
    //    //{
    //    //    Console.WriteLine($"X: {start}  Y: {end}");
    //    //}
    //}


    public class Ship
    {
        int length;
        bool orientation;
        //Position position;
        Point[] coords;
        bool isSunk;


        public int Length
        {
            get => length; 
        }

        public bool Orientation
        {
            get => orientation;
        }

        //public Position Position
        //{
        //    get => position;
        //}


        public Point[] Coords
        {
            get => coords;
        }


        public bool IsSunk
        {
            get => isSunk;
        }


        public Ship(int length, bool orientation, Point[] coords)
        {
            this.length = length;
            this.orientation = orientation;
            this.coords = coords;
            this.isSunk = false;
        }


        //public Ship(int length, bool orientation, int startX, int startY, int endX, int endY)
        //{
        //    Position position;
        //    position.start = new Point(startX, startY);
        //    position.end = new Point(endX, endY);

        //    this.length = length;
        //    this.orientation = orientation;
        //    this.position = position;
        //    this.isSunk = false;
        //}


    }
}
