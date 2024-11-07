namespace Game_Curs_Prog
{
    public class VisualEntity : Entity
    {
        public VisualEntity(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol)
        {
        }

        // Переопределяем методы столкновений, чтобы они не учитывались для визуальных объектов
        public override bool IsCollidingBottom(Entity other)
        {
            return false;
        }

        public override bool IsCollidingTop(Entity other)
        {
            return false;
        }

        public override bool IsCollidingLeft(Entity other)
        {
            return false;
        }

        public override bool IsCollidingRight(Entity other)
        {
            return false;
        }
    }
}
