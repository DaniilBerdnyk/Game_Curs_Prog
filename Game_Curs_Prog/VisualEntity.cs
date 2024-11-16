namespace Game_Curs_Prog
{
    public class VisualEntity : Entity
    {
        public VisualEntity(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol) { }

        public override bool IsCollidingRight(Entity other)
        {
            // Проницаемый объект не сталкивается с другими объектами
            return false;
        }

        public override bool IsCollidingLeft(Entity other)
        {
            // Проницаемый объект не сталкивается с другими объектами
            return false;
        }

        public override bool IsCollidingBottom(Entity other)
        {
            // Проницаемый объект не сталкивается с другими объектами
            return false;
        }

        public override bool IsColliding(Entity other)
        {
            // Проницаемый объект не сталкивается с другими объектами
            return false;
        }

        public override bool IsCollidingInDirection(Entity other, int directionX, int directionY)
        {
            // Проницаемый объект не сталкивается с другими объектами
            return false;
        }
    }
}


