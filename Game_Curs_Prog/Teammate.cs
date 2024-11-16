namespace Game_Curs_Prog
{
    public class Teammate : Entity
    {
        public Teammate(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol) { }

        public void UpdatePosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
