namespace Game_Curs_Prog
{
    public class Teammate : Entity
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Teammate(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol)
        {
            Width = width;
            Height = height;
        }

        public void UpdatePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void UpdateSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
