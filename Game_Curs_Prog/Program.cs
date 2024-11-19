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
   
    static List<Hero> teammates = new List<Hero>(); // Список напарников
    static Enemy enemy = new Enemy(18, 3, 3, 4, 'E');
    static VisualEntity visual = new VisualEntity(18, 3, 3, 4, 'V');
    static StaticEntity Platform = new StaticEntity(10, 15, 20, 10, '#');
    static StaticEntity topWall = new StaticEntity(1, 0, 1000, 1, '▄');
    static StaticEntity leftWall = new StaticEntity(0, 0, 1, 40, ' ');
    static StaticEntity rightWall = new StaticEntity(999, 0, 1, 40, ' ');
    static StaticEntity ground = new StaticEntity(0, 19, 1000, 1, '▀');

    static List<Entity> entities = new List<Entity> {player, enemy, Platform, topWall, leftWall, rightWall, ground };
    static Background background;
    static List<VisualEntity> visualEntities = new List<VisualEntity> { visual }; // Список визуальных объектов
    static List<Texture> textures = new List<Texture>(); // Список текстур

    static double gravity = 0.03; // Уменьшенное значение гравитации для более плавного прыжка
    static Timer gravityTimer;
    static int gravityStepTime = 100; // Время на смещение на один символ вниз (в миллисекундах)

    public const int game_speed = 5;  //default 5
    public const int framesPerSecond = 120;
    static int frameCounter = 0;

    static int cameraX = 0;
    static int cameraY = 0;

    static Hero player;

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

        // Инициализация героя с текстурами и состояниями
        player = new Hero(10, 10, 3, 3, '█');
        entities.Add(player);

        Thread gameThread = new Thread(() => GameLoop(consoleWidth, consoleHeight, framesPerSecond, parallaxFactor, player));
        gameThread.Start();

        // Запуск проверки клавиш
        Thread controlsThread = new Thread(() => Controls.StartKeyChecking(framesPerSecond, entities));
        controlsThread.Start();

        // Запуск гравитационного таймера
        StartGravityLoop(consoleHeight);

        gameThread.Join();
        controlsThread.Join();
        gravityTimer.Dispose(); // Остановка таймера при завершении игры
    }



    public static void RespawnPlayer()
    {
        if (player != null)
        {
            player.X = 2;
            player.Y = 1;
            player.IsJumping = false;
            player.CanJump = true;
            FinalRenderer.Draw(80, 20, entities, visualEntities, teammates, background, cameraX, cameraY, 0.5);
        }
        else
        {
            Console.WriteLine("Player object is null. Cannot respawn.");
        }
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
    static void UpdateCamera(Hero player, int consoleWidth, int consoleHeight)
    {
        if (player == null)
        {
            return;
        }

        // Обновление камеры в зависимости от текущего режима
        switch (Global.currentCameraMode)
        {
            case Global.CameraMode.Basic:
                UpdateBasicCamera(player, consoleWidth, consoleHeight);
                break;
            case Global.CameraMode.Advanced:
                UpdateAdvancedCamera(consoleWidth, consoleHeight);
                break;
            case Global.CameraMode.Hybrid:
                UpdateHybridCamera(consoleWidth, consoleHeight);
                break;
        }
    }



    static void UpdateBasicCamera(Hero player, int consoleWidth, int consoleHeight)
    {
        if (player == null)
        {
            // Если player равен null, выходим из метода, чтобы избежать NullReferenceException
            return;
        }

        const double cameraInertia = 0.05; // Коэффициент инерции камеры
        int triggerThresholdX = consoleWidth / 2; // Порог для реакции камеры по горизонтали ближе к границе
        int triggerThresholdY = consoleHeight / 2; // Порог для реакции камеры по вертикали ближе к границе

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

        // Убедимся, что камера не выходит за границы игрового поля по вертикали
        if (cameraY < 0)
        {
            cameraY = 0;
        }
        if (cameraY + consoleHeight > 40) // Предполагаемая высота игрового поля - 40
        {
            cameraY = 40 - consoleHeight;
        }
    }




    static void UpdateAdvancedCamera(int consoleWidth, int consoleHeight)
    {
        const double cameraInertia = 0.05; // Коэффициент инерции камеры
        const double followSpeed = 0.05; // Скорость следования камеры за персонажем
        const double overtakeSpeed = 0.02; // Скорость обгона камеры
        int centerX = consoleWidth / 4; // Центр экрана по горизонтали
        int centerY = consoleHeight / 4; // Центр экрана по вертикали

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

        // Убедимся, что камера не выходит за границы игрового поля по вертикали
        if (cameraY < 0)
        {
            cameraY = 0;
        }
        if (cameraY + consoleHeight > 40) // Предполагаемая высота игрового поля - 40
        {
            cameraY = 40 - consoleHeight;
        }
    }


    static char previousCameraMode = 'B'; // 'B' для базового режима, 'A' для продвинутого режима

    static bool isSwitching = false; // Переключатель для предотвращения частого переключения
    static void UpdateHybridCamera(int consoleWidth, int consoleHeight)
    {
        int triggerThresholdX = consoleWidth / 4; // Порог для реакции камеры по горизонтали ближе к середине
        int triggerThresholdY = consoleHeight / 4; // Порог для реакции камеры по вертикали ближе к середине

        if (isSwitching)
        {
            return; // Предотвращение частого переключения режимов
        }

        // Проверка касания границы базового режима
        if (previousCameraMode == 'B')
        {
            if (player != null &&
                (player.X < cameraX + triggerThresholdX || player.X > cameraX + consoleWidth - triggerThresholdX ||
                player.Y < cameraY + triggerThresholdY || player.Y > cameraY + consoleHeight - triggerThresholdY))
            {
                // Добавим дополнительное условие для активации Advanced режима только в случае необходимости
                if (player.X < cameraX + triggerThresholdX - 1 || player.X > cameraX + consoleWidth - triggerThresholdX + 1 ||
                    player.Y < cameraY + triggerThresholdY - 1 || player.Y > cameraY + consoleHeight - triggerThresholdY + 1)
                {
                    previousCameraMode = 'A';
                    isSwitching = true;
                    UpdateAdvancedCamera(consoleWidth, consoleHeight);
                    Task.Delay(500).ContinueWith(_ => isSwitching = false); // Задержка переключения на 500 мс
                }
                else
                {
                    UpdateBasicCamera(player, consoleWidth, consoleHeight);
                }
            }
            else
            {
                UpdateBasicCamera(player, consoleWidth, consoleHeight);
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
                isSwitching = true;
                UpdateBasicCamera(player, consoleWidth, consoleHeight);
                Task.Delay(500).ContinueWith(_ => isSwitching = false); // Задержка переключения на 500 мс
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
                    // Добавляем напарника только если его еще нет
                    if (teammates.Count == 0)
                    {
                        var teammate = new Hero(x, y, 4, 4, 'T');
                        Texture teammateTexture = new Texture("teammate.txt", 'T', 4, 4);

                        lock (teammates)
                        {
                            teammates.Add(teammate);
                            entities.Add(teammate); // Добавляем напарника в список объектов игры
                        }

                        lock (textures)
                        {
                            textures.Add(teammateTexture); // Текстуру напарника добавляем в отдельный список текстур
                        }
                    }
                    else
                    {
                        // Обновляем координаты существующего напарника
                        var teammate = teammates[0];
                        teammate.X = x;
                        teammate.Y = y;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }

    static void GameLoop(int consoleWidth, int consoleHeight, int framesPerSecond, double parallaxFactor, Hero player)
    {
        while (running)
        {
            frameCounter++;
            PreRenderer.Update(entities, consoleWidth, consoleHeight);

            if (player != null)
            {
                UpdateCamera(player, consoleWidth, consoleHeight);
            }

            // Обновление позиций героев и врагов
            foreach (var entity in entities.OfType<Hero>())
            {
                entity.Update(entities);
            }

            foreach (var entity in entities.OfType<Enemy>())
            {
                entity.Update(entities);
            }

            foreach (var entity in teammates)
            {
                // Обновление позиции напарника на основе данных, полученных по сети
            }

            // Создание массивов для рендеринга
            char[,] visualEntityFrame = new char[consoleWidth, consoleHeight];
            char[,] gameEntityFrame = new char[consoleWidth, consoleHeight];
            char[,] backgroundFrame = background.GenerateParallaxBackground(consoleWidth, consoleHeight, cameraX, cameraY, parallaxFactor);
            char[,] textureFrame = new char[consoleWidth, consoleHeight];
            char[,] finalFrame = new char[consoleWidth, consoleHeight];

            // Заполнение массива визуальных объектов
            foreach (var visualEntity in visualEntities)
            {
                for (int y = 0; y < visualEntity.Height; y++)
                {
                    for (int x = 0; x < visualEntity.Width; x++)
                    {
                        int drawX = visualEntity.X - cameraX + x;
                        int drawY = visualEntity.Y - cameraY + y;

                        if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight)
                        {
                            visualEntityFrame[drawX, drawY] = visualEntity.Symbol;
                        }
                    }
                }
            }

            // Заполнение массива текстур
            foreach (var gameEntity in entities)
            {
                if (gameEntity is Hero hero)
                {
                    // Получаем текущую текстуру в зависимости от состояния героя
                    Texture texture = hero.GetCurrentTexture();

                    // Смещение текстуры относительно героя
                    int offsetX = (texture.Width - gameEntity.Width) - 1;
                    int offsetY = texture.Height - gameEntity.Height - 1;

                    for (int y = 0; y < texture.Height; y++)
                    {
                        for (int x = 0; x < texture.Width; x++)
                        {
                            int drawX = gameEntity.X - cameraX + x - offsetX;
                            int drawY = gameEntity.Y - cameraY + y - offsetY;

                            if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight && texture.Image[x, y] != ' ')
                            {
                                textureFrame[drawX, drawY] = texture.Image[x, y];
                            }
                        }
                    }
                }
            }

            // Заполнение массива игровых объектов
            foreach (var gameEntity in entities)
            {
                if (gameEntity == null) continue; // Проверка на null

                for (int y = 0; y < gameEntity.Height; y++)
                {
                    for (int x = 0; x < gameEntity.Width; x++)
                    {
                        int drawX = gameEntity.X - cameraX + x;
                        int drawY = gameEntity.Y - cameraY + y;

                        if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight)
                        {
                            gameEntityFrame[drawX, drawY] = gameEntity.Symbol;
                        }
                    }
                }
            }

            // Комбинирование всех слоев в финальный буфер
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    if (visualEntityFrame[x, y] != '\0')
                    {
                        finalFrame[x, y] = visualEntityFrame[x, y];
                    }
                    else if (textureFrame[x, y] != '\0')
                    {
                        finalFrame[x, y] = textureFrame[x, y];
                    }
                    else if (gameEntityFrame[x, y] != '\0')
                    {
                        finalFrame[x, y] = gameEntityFrame[x, y];
                    }
                    else
                    {
                        finalFrame[x, y] = backgroundFrame[x, y];
                    }
                }
            }

            // Отрисовка финального массива
            FinalRenderer.RenderFinalFrame(finalFrame, consoleWidth, consoleHeight);

            // Отправляем координаты игрока на сервер
            if (player != null)
            {
                SendData($"{player.X},{player.Y}");
            }

            Thread.Sleep(1000 / framesPerSecond);
        }
    }

}

