using Game_Curs_Prog;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;

class Program
{
    static bool running = true;
    static Hero player = new Hero(2, 1, 3, 2, '@');
    static Enemy enemy = new Enemy(10, 3, 4, 2, 'E');
    static StaticEntity Platform = new StaticEntity(4, 16, 4, 1, '#');
    static StaticEntity topWall = new StaticEntity(1, 1, 1000, 1, ' ');
    static StaticEntity leftWall = new StaticEntity(0, 0, 1, 40, ' ');
    static StaticEntity rightWall = new StaticEntity(999, 0, 1, 40, ' ');
    static StaticEntity ground = new StaticEntity(0, 18, 1000, 2, '#');

    static List<Entity> entities = new List<Entity> { player, enemy, Platform, topWall, leftWall, rightWall, ground };
    static Background background = new Background("background.txt");

    static int gravity = 1;
    static Thread gravityThread;

    const int game_speed = 10;
    const int framesPerSecond = 120;
    static int frameCounter = 0;

    // Координаты камеры
    static int cameraX = 0;
    static int cameraY = 0;

    static void Main(string[] args)
    {
        const int consoleWidth = 80;
        const int consoleHeight = 20;
        Console.OutputEncoding = Encoding.UTF8;
        Console.BackgroundColor = ConsoleColor.Black; // Установить фоновый цвет
        Console.Clear(); // Очистить консоль и применить цвет фона
        Console.ForegroundColor = ConsoleColor.White; // Установить цвет текста по умолчанию
        Console.SetWindowSize(consoleWidth, consoleHeight);
        Console.SetBufferSize(1000, consoleHeight);
        Thread.Sleep(100);

        Thread gameThread = new Thread(() => GameLoop(consoleWidth, consoleHeight, framesPerSecond));
        gameThread.Start();

        gravityThread = new Thread(() => GravityLoop(consoleHeight));
        gravityThread.Start();

        Controls.StartKeyChecking(framesPerSecond);

        gameThread.Join();
        gravityThread.Join();
    }

    static void RespawnPlayer()
    {
        player.X = 2;
        player.Y = 1;
        player.IsJumping = false;
        player.CanJump = true;
    }

    static void GravityLoop(int consoleHeight)
    {
        while (running)
        {
            ApplyGravity(player, consoleHeight);
            ApplyGravity(enemy, consoleHeight);
            Thread.Sleep(1000 / framesPerSecond);
        }
    }

    static void ApplyGravity(Entity entity, int consoleHeight)
    {
        if (entity is Hero hero && hero.IsJumping)
            return;

        int newY = entity.Y + gravity;

        bool isOnGround = entities.Any(e => e != entity && entity.IsCollidingBottom(e));

        if (isOnGround)
        {
            foreach (var e in entities)
            {
                if (e != entity && entity.IsCollidingBottom(e))
                {
                    entity.Y = e.Y - entity.Height;
                    if (entity is Hero h)
                    {
                        h.Land();
                    }
                    break;
                }
            }
        }
        else
        {
            entity.Y = newY;
        }

        if (entity.Y >= consoleHeight - entity.Height)
        {
            entity.Y = 0;
        }
    }

    static void UpdateCamera(int consoleWidth, int consoleHeight)
    {
        const int horizontalCameraMoveThreshold = 5;
        const int verticalCameraMoveThreshold = 2;

        // Смещение камеры, если герой приближается к границам экрана
        if (player.X > cameraX + consoleWidth - horizontalCameraMoveThreshold)
        {
            cameraX = player.X - consoleWidth + horizontalCameraMoveThreshold;
        }
        if (player.X < cameraX + horizontalCameraMoveThreshold)
        {
            cameraX = player.X - horizontalCameraMoveThreshold;
        }
        if (player.Y > cameraY + consoleHeight - verticalCameraMoveThreshold)
        {
            cameraY = player.Y - consoleHeight + verticalCameraMoveThreshold;
        }
        if (player.Y < cameraY + verticalCameraMoveThreshold)
        {
            cameraY = player.Y - verticalCameraMoveThreshold;
        }

        // Ограничение камеры границами карты
        cameraX = Math.Max(0, Math.Min(cameraX, 1000 - consoleWidth)); // Предполагаемая ширина карты 1000
        cameraY = Math.Max(0, Math.Min(cameraY, 100 - consoleHeight)); // Предполагаемая высота карты 100
    }

    static void HandleInput(Entity entity, List<Entity> entities)
    {
        if (entity is Hero hero)
        {
            // Проверка всех клавиш и вызов соответствующих методов
            if (Controls.IsKeyPressed(ConsoleKey.W))
            {
                hero.Jump(entities);
                Controls.ResetKey(ConsoleKey.W);
            }
            if (Controls.IsKeyPressed(ConsoleKey.S))
            {
                PreRenderer.MoveEntity(hero, 0, 1, entities);
            }
            if (Controls.IsKeyPressed(ConsoleKey.A))
            {
                PreRenderer.MoveEntity(hero, -1, 0, entities);
            }
            if (Controls.IsKeyPressed(ConsoleKey.D))
            {
                PreRenderer.MoveEntity(hero, 1, 0, entities);
            }
            if (Controls.IsKeyPressed(ConsoleKey.R)) // клавиша респауна
            {
                RespawnPlayer();
                Controls.ResetKey(ConsoleKey.R);
            }
        }
    }

    static void GameLoop(int consoleWidth, int consoleHeight, int framesPerSecond)
    {
        while (running)
        {
            frameCounter++;
            PreRenderer.Update(entities, consoleWidth, consoleHeight); // Обновление состояния объектов, включая ввод

            // Обновление позиции камеры
            UpdateCamera(consoleWidth, consoleHeight);

            // Создание псевдо кадра для объектов
            char[,] objectFrame = new char[consoleWidth, consoleHeight];
            ConsoleColor[,] objectColorFrame = new ConsoleColor[consoleWidth, consoleHeight];

            foreach (var entity in entities)
            {
                for (int y = 0; y < entity.Height; y++)
                {
                    for (int x = 0; x < entity.Width; x++)
                    {
                        int drawX = entity.X - cameraX + x;
                        int drawY = entity.Y - cameraY + y;

                        if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight)
                        {
                            objectFrame[drawX, drawY] = entity.Symbol;
                            objectColorFrame[drawX, drawY] = ConsoleColor.White;
                        }
                    }
                }
            }

            // Генерация псевдо кадра для фона
            char[,] backgroundFrame = background.GenerateFullBackground(1000, 40);

            // Формирование финального кадра
            char[,] finalFrame = new char[consoleWidth, consoleHeight];
            ConsoleColor[,] finalColorFrame = new ConsoleColor[consoleWidth, consoleHeight];

            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    if (objectFrame[x, y] != '\0')
                    {
                        finalFrame[x, y] = objectFrame[x, y];
                        finalColorFrame[x, y] = objectColorFrame[x, y];
                    }
                    else
                    {
                        finalFrame[x, y] = backgroundFrame[cameraX + x, cameraY + y];
                        finalColorFrame[x, y] = ConsoleColor.Gray;
                    }
                }
            }

            // Отправка финального кадра в рендер
            RenderFinalFrame(finalFrame, finalColorFrame, consoleWidth, consoleHeight);

            Thread.Sleep(1000 / framesPerSecond);
        }
    }

    static void RenderFinalFrame(char[,] frame, ConsoleColor[,] colorFrame, int width, int height)
    {
        var sb = new StringBuilder();
        Console.SetCursorPosition(0, 0);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Console.ForegroundColor = colorFrame[x, y];
                sb.Append(frame[x, y]);
            }
            if (y < height - 1) // Удаление лишних линий
            {
                sb.AppendLine();
            }
        }
        Console.SetCursorPosition(0, 0);
        Console.Write(sb.ToString());
        Console.ResetColor();
    }
}





