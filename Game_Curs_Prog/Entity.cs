namespace Game_Curs_Prog
{
    public class Entity
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public char Symbol { get; set; }

        public Entity(int x, int y, int width, int height, char symbol)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Symbol = symbol;
        }

        public virtual bool IsCollidingBottom(Entity other)
        {
            return Y + Height == other.Y && X < other.X + other.Width && X + Width > other.X;
        }

        public virtual bool IsCollidingTop(Entity other)
        {
            return Y == other.Y + other.Height && X < other.X + other.Width && X + Width > other.X;
        }

        public virtual bool IsCollidingLeft(Entity other)
        {
            return X == other.X + other.Width && Y < other.Y + other.Height && Y + Height > other.Y;
        }

        public virtual bool IsCollidingRight(Entity other)
        {
            return X + Width == other.X && Y < other.Y + other.Height && Y + Height > other.Y;
        }

        public virtual bool IsCollidingInDirection(Entity other, int deltaX, int deltaY)
        {
            int newX = X + deltaX;
            int newY = Y + deltaY;
            return newX < other.X + other.Width && newX + Width > other.X &&
                   newY < other.Y + other.Height && newY + Height > other.Y;
        }
    }
}












