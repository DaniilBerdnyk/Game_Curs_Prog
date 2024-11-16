namespace Game_Curs_Prog
{
    public class VisualEntity : Entity
    {
        public VisualEntity(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol) { }

        public override bool IsCollidingRight(Entity other)
        {
            return base.IsCollidingRight(other);
        }

        public override bool IsCollidingLeft(Entity other)
        {
            return base.IsCollidingLeft(other);
        }

        public override bool IsCollidingBottom(Entity other)
        {
            return base.IsCollidingBottom(other);
        }

        public override bool IsColliding(Entity other)
        {
            return base.IsColliding(other);
        }
    }
}

