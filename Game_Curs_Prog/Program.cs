using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NAudio.Wave;

namespace Game_Curs_Prog
{
    class Program
    {
        // Размер игрового поля
        const int defaultWidth = Global.defaultWidth;
        const int defaultHeight = Global.defaultHeight;

        const int consoleWidth = Global.consoleWidth;
        const int consoleHeight = Global.consoleHeight;

        static TcpClient client;
        static NetworkStream stream;
        static Thread receiveThread;
        static bool running = true;
        static Hero player;
        static List<Hero> teammates = new List<Hero>(); // Список напарников
        static Enemy enemy = new Enemy(18, defaultHeight - 15, 3, 3, 'E'); // Изменяем позицию
        static VisualEntity visual = new VisualEntity(18, defaultHeight - 15, 3, 3, 'V'); // Изменяем позицию

        static StaticEntity Platform = new StaticEntity(25, defaultHeight - 5, 10, 2, '#'); // Платформа выше земли
        static StaticEntity Platform2 = new StaticEntity(30, defaultHeight - 10, 10, 2, '#'); // Платформа выше земли
        static StaticEntity Platform3 = new StaticEntity(35, defaultHeight - 15, 10, 2, '#'); // Платформа выше земли
        static StaticEntity Platform4 = new StaticEntity(40, defaultHeight - 20, 10, 2, '#'); // Платформа выше земли

        static StaticEntity topWall = new StaticEntity(1, 0, defaultWidth, 1, '▄'); // Изменяем ширину на 500
        static StaticEntity bottomWall = new StaticEntity(0, defaultHeight - 2, defaultWidth, 1, '▄'); // Нижняя граница
        static StaticEntity leftWall = new StaticEntity(0, 0, 1, defaultHeight, ' '); // Изменяем высоту на 500
        static StaticEntity rightWall = new StaticEntity(defaultWidth - 1, 0, 1, defaultHeight, ' '); // Изменяем позицию и высоту
        static StaticEntity ground = new StaticEntity(0, defaultHeight - 2, defaultWidth, 1, '▀'); // Изменяем позицию и ширину

        static List<Entity> entities = new List<Entity> { enemy, Platform, Platform2, Platform3, Platform4, topWall, leftWall, rightWall, bottomWall, ground };
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



        // Создаем объекты вне методов
        static WaveOutEvent waveOutEvent; // для воспроизведения аудио
        static AudioFileReader audioFile; // для чтения аудио файлов

        static void Main(string[] args)
        {
            const double parallaxFactor = 0.5;
            const string backgroundFilePath = "background.txt";
            const string audioFilePath = "background_music.wav"; // Путь к вашему WAV файлу

            background = new Background(backgroundFilePath, defaultWidth, defaultHeight);

            try
            {
                client = new TcpClient("127.0.0.1", 8000);
                stream = client.GetStream();
                receiveThread = new Thread(ReceiveData);
                receiveThread.Start();
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
            // Инициализация героя с текстурами и состояниями
            player = new Hero(3, defaultHeight - 10, 3, 3, '█'); // Герой появляется внизу слева
            entities.Add(player);



            // Воспроизведение фоновой музыки с выбранного устройства
            PlayBackgroundMusic(audioFilePath);

            Thread gameThread = new Thread(() => GameLoop(framesPerSecond, parallaxFactor, player));
            gameThread.Start();

            // Запуск проверки клавиш
            Thread controlsThread = new Thread(() => Controls.StartKeyChecking(framesPerSecond, entities));
            controlsThread.Start();

            // Запуск гравитационного таймера
            StartGravityLoop(consoleHeight);

            gameThread.Join();
            controlsThread.Join();
            gravityTimer.Dispose(); // Остановка таймера при завершении игры

            // Остановка воспроизведения музыки
            StopBackgroundMusic();
        }




        static void PlayBackgroundMusic(string audioFilePath)
        {
            waveOutEvent = new WaveOutEvent();
            audioFile = new AudioFileReader(audioFilePath);
            waveOutEvent.Init(audioFile);
            waveOutEvent.Play();
        }

        static void StopBackgroundMusic()
        {
            waveOutEvent?.Stop();
            waveOutEvent?.Dispose();
            audioFile?.Dispose();
        }

        public static void RespawnPlayer()
        {
            if (player != null)
            {
                player.X = 2;
                player.Y = defaultHeight - 5;
                player.IsJumping = false;
                player.CanJump = true;
                FinalRenderer.Draw(consoleWidth , consoleHeight , entities, visualEntities, teammates, background, cameraX, cameraY, 0.5);
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

        static void ApplyGravity(Entity entity, int fieldHeight)
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

        }
        enum CameraMode
        {
            Basic,
            Advanced,
            Hybrid
        }

        static CameraMode currentCameraMode = CameraMode.Hybrid; // Для отладки используем только Basic

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
                    UpdateAdvancedCamera(player , consoleWidth, consoleHeight);
                    break;
                case Global.CameraMode.Hybrid:
                    UpdateHybridCamera(player, consoleWidth, consoleHeight);
                    break;
            }
        }


        static void UpdateBasicCamera(Hero player, int consoleWidth, int consoleHeight)
        {
            if (player == null)
            {
                return;
            }

            const double cameraInertiaX = 0.05; // Коэффициент инерции камеры по горизонтали
            const double cameraInertiaY = 0.3; // Увеличенный коэффициент инерции камеры по вертикали для большей скорости
            int triggerThresholdX = consoleWidth / 2; // Порог для реакции камеры по горизонтали
            int triggerThresholdY = consoleHeight / 3; // Порог для реакции камеры по вертикали, оптимизированный

            // Цель позиции камеры
            int targetCameraX = player.X - consoleWidth / 2;
            int targetCameraY = player.Y - consoleHeight / 2; // Смещение на 2 символа вниз

            // Применение инерции к движению камеры по горизонтали
            if (player.X < cameraX + triggerThresholdX)
            {
                cameraX -= (int)((cameraX + triggerThresholdX - player.X) * cameraInertiaX);
            }
            else if (player.X > cameraX + consoleWidth - triggerThresholdX)
            {
                cameraX += (int)((player.X - (cameraX + consoleWidth - triggerThresholdX)) * cameraInertiaX);
            }

            // Применение инерции к движению камеры по вертикали
            if (player.Y < cameraY + triggerThresholdY)
            {
                cameraY -= (int)((cameraY + triggerThresholdY-1 - player.Y) * cameraInertiaY);
            }
            else if (player.Y > cameraY + consoleHeight - triggerThresholdY)
            {
                cameraY += (int)((player.Y - (cameraY + consoleHeight - triggerThresholdY-1)) * cameraInertiaY);
            }

            // Ограничение позиции камеры в пределах игрового поля
            cameraX = Math.Max(0, Math.Min(cameraX, 1000 - consoleWidth));
            cameraY = Math.Max(0, Math.Min(cameraY, 1000 - consoleHeight));
        }




        static void UpdateAdvancedCamera(Hero player, int consoleWidth, int consoleHeight)
        {
            if (player == null)
            {
                return;
            }

            const double cameraInertiaX = 1; // Коэффициент инерции камеры по горизонтали
            const double cameraInertiaY = 0.3; // Коэффициент инерции камеры по вертикали
            int triggerThresholdX = consoleWidth / 3; // Порог для реакции камеры по горизонтали
            int triggerThresholdY = consoleHeight / 3; // Порог для реакции камеры по вертикали
            int verticalOffset = 2; // Смещение на 2 символа вниз

            // Цель позиции камеры
            int targetCameraX = player.X - consoleWidth / 2;
            int targetCameraY = player.Y - consoleHeight / 2;

            // Применение инерции к движению камеры по горизонтали
            if (player.X < cameraX + triggerThresholdX)
            {
                cameraX -= (int)((cameraX + triggerThresholdX - player.X) * cameraInertiaX);
            }
            else if (player.X > cameraX + consoleWidth - triggerThresholdX)
            {
                cameraX += (int)((player.X - (cameraX + consoleWidth - triggerThresholdX)) * cameraInertiaX);
            }

            // Применение инерции к движению камеры по вертикали
            if (player.Y < cameraY + triggerThresholdY)
            {
                cameraY -= (int)((cameraY + triggerThresholdY - player.Y) * cameraInertiaY);
            }
            else if (player.Y > cameraY + consoleHeight - triggerThresholdY)
            {
                cameraY += (int)((player.Y - (cameraY + consoleHeight - triggerThresholdY)) * cameraInertiaY);
            }

            // Ограничение позиции камеры в пределах игрового поля
            cameraX = Math.Max(0, Math.Min(cameraX, 1000 - consoleWidth));
            cameraY = Math.Max(0, Math.Min(cameraY, 1000 - consoleHeight));
        }


        static char previousCameraMode = 'B'; // 'B' для базового режима, 'A' для продвинутого режима

        static void UpdateHybridCamera(Hero player, int consoleWidth, int consoleHeight)
        {
            if (player == null)
            {
                return;
            }

            const double cameraInertiaX = 0.05; // Коэффициент инерции камеры по горизонтали
            const double cameraInertiaY = 0.3; // Коэффициент инерции камеры по вертикали
            int triggerThresholdX = consoleWidth / 2; // Порог для реакции камеры по горизонтали
            int triggerThresholdY = consoleHeight / 2; // Порог для реакции камеры по вертикали
           

            // Проверка Advanced режима
            if (previousCameraMode == 'A')
            {
                // Продолжаем использовать Advanced режим, если удерживаются клавиши движения
                if (Controls.IsKeyPressed(ConsoleKey.A) || Controls.IsKeyPressed(ConsoleKey.D))
                {
                    UpdateAdvancedCamera(player, consoleWidth, consoleHeight);
                }
                else
                {
                    previousCameraMode = 'B';
                    UpdateBasicCamera(player, consoleWidth, consoleHeight);
                }
            }
            else if (previousCameraMode == 'B')
            {
                // Проверка на активацию Advanced режима
                if (player.X < cameraX + triggerThresholdX || player.X > cameraX + consoleWidth - triggerThresholdX ||
                    player.Y < cameraY + triggerThresholdY || player.Y > cameraY + consoleHeight - triggerThresholdY)
                {
                    previousCameraMode = 'A';
                    UpdateAdvancedCamera(player, consoleWidth, consoleHeight);
                }
                else
                {
                    UpdateBasicCamera(player, consoleWidth, consoleHeight);
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

        static void GameLoop(int framesPerSecond, double parallaxFactor, Hero player)
        {
            while (running)
            {
                frameCounter++;
                PreRenderer.Update(entities, Global.defaultWidth, Global.defaultHeight);

                if (player != null)
                {
                    UpdateCamera(player, Global.consoleWidth, Global.consoleHeight);
                }

                // Отрисовка финального массива с указанием ширины и высоты камеры
                FinalRenderer.Draw(consoleWidth, consoleHeight, entities, visualEntities, teammates, background, cameraX, cameraY, 0.5);

                // Отправляем координаты игрока на сервер
                if (player != null)
                {
                    SendData($"{player.X},{player.Y}");
                }

                Thread.Sleep(1000 / framesPerSecond);
            }
        }


    }
}
