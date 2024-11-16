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

        public virtual bool IsColliding(Entity other)
        {
            return X < other.X + other.Width &&
                   X + Width > other.X &&
                   Y < other.Y + other.Height &&
                   Y + Height > other.Y;
        }

        public virtual bool IsCollidingBottom(Entity other)
        {
            return X < other.X + other.Width &&
                   X + Width > other.X &&
                   Y + Height == other.Y;
        }

        public virtual bool IsCollidingLeft(Entity other)
        {
            return X == other.X + other.Width &&
                   Y < other.Y + other.Height &&
                   Y + Height > other.Y;
        }

        public virtual bool IsCollidingRight(Entity other)
        {
            return X + Width == other.X &&
                   Y < other.Y + other.Height &&
                   Y + Height > other.Y;
        }

        public bool IsCollidingInDirection(Entity other, int directionX, int directionY)
        {
            int projectedX = X + directionX;
            int projectedY = Y + directionY;

            return projectedX < other.X + other.Width &&
                   projectedX + Width > other.X &&
                   projectedY < other.Y + other.Height &&
                   projectedY + Height > other.Y;
        }
    }
}

















