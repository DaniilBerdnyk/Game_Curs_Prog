using Game_Curs_Prog;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class Program
{
    static bool running = true;
    static Hero player = new Hero(2, 5, 4, 4, '█');
    static Enemy enemy = new Enemy(18, 3, 3, 4, 'E');
    static StaticEntity Platform = new StaticEntity(10, 15, 20, 1, '#');
    static StaticEntity topWall = new StaticEntity(1, 0, 1000, 1, '▄');
    static StaticEntity leftWall = new StaticEntity(0, 0, 1, 40, ' ');
    static StaticEntity rightWall = new StaticEntity(999, 0, 1, 40, ' ');
    static StaticEntity ground = new StaticEntity(0, 19, 1000, 1, '▀');
    

    static List<Entity> entities = new List<Entity> { player, enemy, Platform, topWall, leftWall, rightWall, ground };
    static Background background;

    static int gravity = 1;
    static Thread gravityThread;

    public const int game_speed = 10;
    const int framesPerSecond = 120;
    static int frameCounter = 0;

    static int cameraX = 0;
    static int cameraY = 0;

    static void Main(string[] args)
    {
        const int consoleWidth = 120;
        const int consoleHeight = 20;
        const int defaultWidth = 1000;
        const int defaultHeight = 40;
        const double parallaxFactor = 0.5;
        const string backgroundFilePath = "background.txt";

        background = new Background(backgroundFilePath, defaultWidth, defaultHeight);

        Console.OutputEncoding = Encoding.UTF8;
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;
        Console.SetWindowSize(consoleWidth, consoleHeight);
        Console.SetBufferSize(2000, 1000);
        Thread.Sleep(100);

        Thread gameThread = new Thread(() => GameLoop(consoleWidth, consoleHeight, framesPerSecond, parallaxFactor));
        gameThread.Start();

        gravityThread = new Thread(() => GravityLoop(consoleHeight));
        gravityThread.Start();

        Controls.StartKeyChecking(framesPerSecond);

        gameThread.Join();
        gravityThread.Join();
    }

    public static void RespawnPlayer()
    {
        player.X = 2;
        player.Y = 1;
        player.IsJumping = false;
        player.CanJump = true;
        RedrawScreen();
    }

    static void RedrawScreen()
    {
        const int consoleWidth = 80;
        const int consoleHeight = 20;
        const double parallaxFactor = 0.5;
        char[,] backgroundFrame = background.GenerateParallaxBackground(1000, 40, cameraX, cameraY, parallaxFactor);

        char[,] objectFrame = new char[consoleWidth, consoleHeight];
        ConsoleColor[,] colorFrame = new ConsoleColor[consoleWidth, consoleHeight];

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
                        colorFrame[drawX, drawY] = ConsoleColor.White;
                    }
                }
            }
        }

        char[,] finalFrame = new char[consoleWidth, consoleHeight];
        ConsoleColor[,] finalColorFrame = new ConsoleColor[consoleWidth, consoleHeight];

        for (int y = 0; y < consoleHeight; y++)
        {
            for (int x = 0; x < consoleWidth; x++)
            {
                if (objectFrame[x, y] != '\0')
                {
                    finalFrame[x, y] = objectFrame[x, y];
                    finalColorFrame[x, y] = colorFrame[x, y];
                }
                else
                {
                    finalFrame[x, y] = backgroundFrame[cameraX + x, cameraY + y];
                    finalColorFrame[x, y] = ConsoleColor.Gray;
                }
            }
        }

        RenderFinalFrame(finalFrame, finalColorFrame, consoleWidth, consoleHeight);
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
        const double cameraInertia = 0.1; // Коэффициент инерции камеры

        // Цель позиции камеры
        int targetCameraX = player.X - consoleWidth / 2;
        int targetCameraY = player.Y - consoleHeight / 2;

        // Применение инерции к движению камеры
        cameraX += (int)((targetCameraX - cameraX) * cameraInertia);
        cameraY += (int)((targetCameraY - cameraY) * cameraInertia);

        // Ограничение позиции камеры в пределах игрового поля
        cameraX = Math.Max(0, Math.Min(cameraX, 1000 - consoleWidth));
        cameraY = Math.Max(0, Math.Min(cameraY, 100 - consoleHeight));
    }

    static void GameLoop(int consoleWidth, int consoleHeight, int framesPerSecond, double parallaxFactor)
    {
        while (running)
        {
            frameCounter++;
            PreRenderer.Update(entities, consoleWidth, consoleHeight);

            UpdateCamera(consoleWidth, consoleHeight);

            char[,] objectFrame = new char[consoleWidth, consoleHeight];
            ConsoleColor[,] colorFrame = new ConsoleColor[consoleWidth, consoleHeight];

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
                            colorFrame[drawX, drawY] = ConsoleColor.White;
                        }
                    }
                }
            }

            char[,] backgroundFrame = background.GenerateParallaxBackground(consoleWidth, consoleHeight, cameraX, cameraY, parallaxFactor);

            char[,] finalFrame = new char[consoleWidth, consoleHeight];
            ConsoleColor[,] finalColorFrame = new ConsoleColor[consoleWidth, consoleHeight];

            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    if (objectFrame[x, y] != '\0')
                    {
                        finalFrame[x, y] = objectFrame[x, y];
                        finalColorFrame[x, y] = colorFrame[x, y];
                    }
                    else
                    {
                        finalFrame[x, y] = backgroundFrame[x, y];
                        finalColorFrame[x, y] = ConsoleColor.Gray;
                    }
                }
            }

            RenderFinalFrame(finalFrame, finalColorFrame, consoleWidth, consoleHeight);

            Thread.Sleep(1000 / framesPerSecond);
        }
    }
    public static void RenderFinalFrame(char[,] frame, ConsoleColor[,] colorFrame, int width, int height)
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
        Console.SetCursorPosition(0, 0); // Устанавливаем курсор в начало перед выводом
        Console.Write(sb.ToString());   // Выводим всю строку
        Console.ResetColor();           // Сбрасываем цвет после вывода
    }



}




