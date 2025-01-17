using System.Threading.Tasks;

namespace Game_Curs_Prog
{
    public class Enemy : Entity
    {
        private DateTime? heroInVicinityStartTime = null;

        public Enemy(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol) { }

        public async Task CheckForHeroAsync(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity is Hero hero)
                {
                    if (IsHeroInVicinity(hero))
                    {
                        if (heroInVicinityStartTime == null)
                        {
                            heroInVicinityStartTime = DateTime.Now;
                        }
                        else if ((DateTime.Now - heroInVicinityStartTime.Value).TotalMilliseconds >= 50)
                        {
#if DEBUG
                            Console.WriteLine("Enemy detected Hero nearby for 0.5 seconds. Respawning player.");
#endif
                            Program.RespawnPlayer();
                        }
                    }
                    else
                    {
                        heroInVicinityStartTime = null;
                    }
                }
            }
        }

        private bool IsHeroInVicinity(Hero hero)
        {
            int checkRadiusX = (this.Width / 2) + 3;
            int checkRadiusY = (this.Height / 2) + 3;

            for (int y = -checkRadiusY; y <= checkRadiusY; y++)
            {
                for (int x = -checkRadiusX; x <= checkRadiusX; x++)
                {
                    if (hero.X == this.X + x && hero.Y == this.Y + y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}



