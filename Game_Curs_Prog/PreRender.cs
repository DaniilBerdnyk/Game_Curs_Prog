using System;
using System.Collections.Generic;

namespace Game_Curs_Prog
{
    public static class PreRenderer
    {
        public static void Update(List<Entity> entities, int consoleWidth, int consoleHeight)
        {
            foreach (var entity in entities)
            {
                HandleInput(entity, entities);
                if (entity is Hero hero)
                {
                    hero.Update(entities);
                }
            }
        }

        private static void HandleInput(Entity entity, List<Entity> entities)
        {
            if (entity is Hero hero)
            {
                // Проверка всех клавиш и вызов соответствующих методов
                if (Controls.IsKeyPressed(ConsoleKey.W) || Controls.IsKeyPressed(ConsoleKey.Spacebar))
                {
                    hero.Jump(entities);
                    Controls.ResetKey(ConsoleKey.W);
                    Controls.ResetKey(ConsoleKey.Spacebar);
                }
                if (Controls.IsKeyPressed(ConsoleKey.S))
                {
                    MoveEntity(hero, 0, 1, entities);
                }
                if (Controls.IsKeyPressed(ConsoleKey.A))
                {
                    MoveEntity(hero, -1, 0, entities);
                }
                if (Controls.IsKeyPressed(ConsoleKey.D))
                {
                    MoveEntity(hero, 1, 0, entities);
                }
                if (Controls.IsKeyPressed(ConsoleKey.R))
                {
                    Program.RespawnPlayer(); // вызов метода RespawnPlayer из Program
                    Controls.ResetKey(ConsoleKey.R);
                }
            }
        }

        public static void MoveEntity(Entity entity, int deltaX, int deltaY, List<Entity> entities)
        {
            int newX = entity.X + deltaX;
            int newY = entity.Y + deltaY;

            // Проверка на столкновение с границами и другими объектами
            bool collision = entities.Any(e => e != entity && entity.IsCollidingInDirection(e, deltaX, deltaY));

            if (!collision)
            {
                entity.X = newX;
                entity.Y = newY;
            }
        }
    }
}




























