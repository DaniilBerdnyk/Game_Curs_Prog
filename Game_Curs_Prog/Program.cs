using Game_Curs_Prog;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class Program
{
    static TcpClient client;
    static NetworkStream stream;
    static Thread receiveThread;
    static bool running = true;
    static Hero player = new Hero(2, 5, 4, 4, '█');
    static List<Hero> teammates = new List<Hero>(); // Список напарников
    static Enemy enemy = new Enemy(18, 3, 3, 4, 'E');
    static VisualEntity visual = new VisualEntity(18, 3, 3, 4, 'V');
    static StaticEntity Platform = new StaticEntity(10, 15, 20, 10, '#');
    static StaticEntity topWall = new StaticEntity(1, 0, 1000, 1, '▄');
    static StaticEntity leftWall = new StaticEntity(0, 0, 1, 40, ' ');
    static StaticEntity rightWall = new StaticEntity(999, 0, 1, 40, ' ');
    static StaticEntity ground = new StaticEntity(0, 19, 1000, 1, '▀');

    static List<Entity> entities = new List<Entity> { visual, player, enemy, Platform, topWall, leftWall, rightWall, ground };
    static Background background;

    static double gravity = 0.05; // Уменьшенное значение гравитации для более плавного прыжка
    static Timer gravityTimer;
    static int gravityStepTime = 100; // Время на смещение на один символ вниз (в миллисекундах)

    public const int game_speed = 5;  //default 5
    public const int framesPerSecond = 120;
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

        try
        {
            client = new TcpClient("127.0.0.1", 8000);
            stream = client.GetStream();
            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
            Console.WriteLine("Connected to the server.");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Could not connect to server: {ex.Message}");
        }

        Console.OutputEncoding = Encoding.UTF8;
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;
        Console.SetWindowSize(consoleWidth, consoleHeight);
        Console.SetBufferSize(2000, 1000);
        Thread.Sleep(100);

        Thread gameThread = new Thread(() => GameLoop(consoleWidth, consoleHeight, framesPerSecond, parallaxFactor));
        gameThread.Start();

        // Запуск гравитационного таймера
        StartGravityLoop(consoleHeight);

        Controls.StartKeyChecking(framesPerSecond);

        gameThread.Join();
        gravityTimer.Dispose(); // Остановка таймера при завершении игры
    }


    public static void RespawnPlayer()
    {
        player.X = 2;
        player.Y = 1;
        player.IsJumping = false;
        player.CanJump = true;
        FinalRenderer.Draw(80, 20, entities, teammates, background, cameraX, cameraY, 0.5);
    }

static void StartGravityLoop(int consoleHeight)
{
    gravityTimer = new Timer(ApplyGravityToAllEntities, consoleHeight, 0, (int)(gravity * 1000));
}

static void ApplyGravityToAllEntities(object state)
{
    int consoleHeight = (int)state;

    // Применение гравитации ко всем объектам типа Hero
    foreach (var entity in entities.OfType<Hero>())
    {
        ApplyGravity(entity, consoleHeight);
    }

    // Применение гравитации ко всем объектам типа Enemy
    foreach (var entity in entities.OfType<Enemy>())
    {
        ApplyGravity(entity, consoleHeight);
    }

    // Применение гравитации ко всем объектам типа PhysicsEntity
    foreach (var entity in entities.OfType<PhysicsEntity>())
    {
        ApplyGravity(entity, consoleHeight);
    }

    // Применение гравитации ко всем объектам типа Teammate
    foreach (var teammate in teammates)
    {
        ApplyGravity(teammate, consoleHeight);
    }
}

static void ApplyGravity(Entity entity, int consoleHeight)
{
    if (entity is Hero hero && hero.IsJumping)
        return;

    int newY = entity.Y + 1; // Сдвиг вниз на 1 символ

    bool isOnGround = entities.Any(e => e != entity && entity.IsCollidingBottom(e)) ||
                      entities.OfType<StaticEntity>().Any(t => t != entity && entity.IsCollidingBottom(t));

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
        foreach (var t in entities.OfType<StaticEntity>())
        {
            if (t != entity && entity.IsCollidingBottom(t))
            {
                entity.Y = t.Y - entity.Height;
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



enum CameraMode
    {
        Basic,
        Advanced,
        Hybrid
    }

    static CameraMode currentCameraMode = CameraMode.Hybrid;

    static void UpdateCamera(int consoleWidth, int consoleHeight)
    {
        // Обновление камеры в зависимости от текущего режима
        if (currentCameraMode == CameraMode.Basic)
        {
            UpdateBasicCamera(consoleWidth, consoleHeight);
        }
        else if (currentCameraMode == CameraMode.Advanced)
        {
            UpdateAdvancedCamera(consoleWidth, consoleHeight);
        }
        else if (currentCameraMode == CameraMode.Hybrid)
        {
            UpdateHybridCamera(consoleWidth, consoleHeight);
        }
    }

    static void UpdateBasicCamera(int consoleWidth, int consoleHeight)
    {
        const double cameraInertia = 0.06; // Коэффициент инерции камеры
        int triggerThresholdX = consoleWidth / 3; // Порог для реакции камеры по горизонтали ближе к границе
        int triggerThresholdY = consoleHeight / 3; // Порог для реакции камеры по вертикали ближе к границе

        // Цель позиции камеры
        int targetCameraX = player.X - consoleWidth / 2;
        int targetCameraY = player.Y - consoleHeight / 2;

        // Применение инерции к движению камеры
        if (player.X < cameraX + triggerThresholdX || player.X > cameraX + consoleWidth - triggerThresholdX)
        {
            cameraX += (int)((targetCameraX - cameraX) * cameraInertia);
        }
        if (player.Y < cameraY + triggerThresholdY || player.Y > cameraY + consoleHeight - triggerThresholdY)
        {
            cameraY += (int)((targetCameraY - cameraY) * cameraInertia);
        }

        // Ограничение позиции камеры в пределах игрового поля
        cameraX = Math.Max(0, Math.Min(cameraX, 1000 - consoleWidth));
        cameraY = Math.Max(0, Math.Min(cameraY, 1000 - consoleHeight));
    }




    static void UpdateAdvancedCamera(int consoleWidth, int consoleHeight)
    {
        const double cameraInertia = 0.05; // Коэффициент инерции камеры
        const double followSpeed = 0.05; // Скорость следования камеры за персонажем
        const double overtakeSpeed = 0.02; // Скорость обгона камеры
        int centerX = consoleWidth / 2; // Центр экрана по горизонтали
        int centerY = consoleHeight / 2; // Центр экрана по вертикали

        // Получение скорости персонажа из класса Hero
        double playerSpeed = Hero.speed;

        // Цель позиции камеры
        int targetCameraX = player.X - centerX;
        int targetCameraY = player.Y - centerY;

        // Применение инерции к движению камеры
        cameraX += (int)((targetCameraX - cameraX) * cameraInertia);
        cameraY += (int)((targetCameraY - cameraY) * cameraInertia);

        // Плавное следование камеры за персонажем
        if (Controls.IsKeyPressed(ConsoleKey.A) || Controls.IsKeyPressed(ConsoleKey.D))
        {
            cameraX += (int)((targetCameraX - cameraX) * followSpeed);
        }

        // Плавный обгон камеры при длительном движении в одном направлении
        if (Controls.IsKeyPressed(ConsoleKey.A))
        {
            cameraX -= (int)(overtakeSpeed * consoleWidth);
        }
        else if (Controls.IsKeyPressed(ConsoleKey.D))
        {
            cameraX += (int)(overtakeSpeed * consoleWidth);
        }

        // Ограничение позиции камеры в пределах игрового поля
        cameraX = Math.Max(0, Math.Min(cameraX, 1000 - consoleWidth));
        cameraY = Math.Max(0, Math.Min(cameraY, 1000 - consoleHeight));
    }

    static char previousCameraMode = 'B'; // 'B' для базового режима, 'A' для продвинутого режима

    static void UpdateHybridCamera(int consoleWidth, int consoleHeight)
    {
        int triggerThresholdX = consoleWidth / 4; // Порог для реакции камеры по горизонтали ближе к середине
        int triggerThresholdY = consoleHeight / 4; // Порог для реакции камеры по вертикали ближе к середине

        // Проверка касания границы базового режима
        if (previousCameraMode == 'B')
        {
            if (player.X < cameraX + triggerThresholdX || player.X > cameraX + consoleWidth - triggerThresholdX || player.Y < cameraY + triggerThresholdY || player.Y > cameraY + consoleHeight - triggerThresholdY)
            {
                previousCameraMode = 'A';
                UpdateAdvancedCamera(consoleWidth, consoleHeight);
            }
            else
            {
                UpdateBasicCamera(consoleWidth, consoleHeight);
            }
        }
        else if (previousCameraMode == 'A')
        {
            // Проверка нажатия клавиш движения из класса Controls
            if (Controls.IsKeyPressed(ConsoleKey.A) || Controls.IsKeyPressed(ConsoleKey.D))
            {
                UpdateAdvancedCamera(consoleWidth, consoleHeight);
            }
            else
            {
                previousCameraMode = 'B';
                UpdateBasicCamera(consoleWidth, consoleHeight);
            }
        }
    }


    static void SendData(string message)
    {
        if (stream != null)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }

    static void ReceiveData()
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                var coords = message.Split(',');
                if (coords.Length == 2 && int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                {
                    var teammate = new Hero(x, y, 4, 4, '█');
                    lock (teammates)
                    {
                        teammates.Add(teammate);
                        entities.Add(teammate);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }

    static void GameLoop(int consoleWidth, int consoleHeight, int framesPerSecond, double parallaxFactor)
    {
        while (running)
        {
            frameCounter++;
            PreRenderer.Update(entities, consoleWidth, consoleHeight);

            UpdateCamera(consoleWidth, consoleHeight);

            // Обновление позиций героев и врагов
            foreach (var entity in entities.OfType<Hero>())
            {
                entity.Update(entities);
            }

            foreach (var entity in entities.OfType<Enemy>())
            {
                entity.Update(entities);
            }

            foreach (var entity in entities.OfType<Teammate>())
            {
                // Обновление позиции напарника на основе данных, полученных по сети
                // Например:
                // entity.UpdatePosition(полученные_данные_по_сети.X, полученные_данные_по_сети.Y);
            }

            char[,] objectFrame = new char[consoleWidth, consoleHeight];

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
                        }
                    }
                }
            }

            char[,] backgroundFrame = background.GenerateParallaxBackground(consoleWidth, consoleHeight, cameraX, cameraY, parallaxFactor);

            char[,] finalFrame = new char[consoleWidth, consoleHeight];

            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    if (objectFrame[x, y] != '\0')
                    {
                        finalFrame[x, y] = objectFrame[x, y];
                    }
                    else
                    {
                        finalFrame[x, y] = backgroundFrame[x, y];
                    }
                }
            }

            FinalRenderer.RenderFinalFrame(finalFrame, consoleWidth, consoleHeight);

            // Отправляем координаты игрока на сервер
            SendData($"{entities.OfType<Hero>().First().X},{entities.OfType<Hero>().First().Y}");
            Thread.Sleep(1000 / framesPerSecond);
        }
    }


}

