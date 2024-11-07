namespace Game_Curs_Prog
{
    [Serializable]
    public class StaticEntity : Entity
    {
        public StaticEntity(int x, int y, int width, int height, char symbol)
            : base(x, y, width, height, symbol)
        {
        }

        // Здесь можно добавить методы и свойства, специфичные для статических объектов
    }
}

