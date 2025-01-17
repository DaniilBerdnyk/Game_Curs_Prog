using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NAudio.Wave.SampleProviders;
using System.Numerics;

//SAVE NOTE
namespace Game_Curs_Prog
{
    class Program
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)] 
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_MAXIMIZE = 3;


        // Размер игрового поля
        const int defaultWidth = Global.defaultWidth;
        const int defaultHeight = Global.defaultHeight;

        const int consoleWidth = Global.consoleWidth;
        const int consoleHeight = Global.consoleHeight;

        static TcpClient client;
        static NetworkStream stream;
        static Thread receiveThread;
        static bool running = true;
        public static Hero player;
        static List<Hero> teammates = new List<Hero>(); // Список напарников

        static StaticEntity topWall = new StaticEntity(1, 0, defaultWidth, 1, '▄'); // Изменяем ширину на 500
        static StaticEntity bottomWall = new StaticEntity(0, defaultHeight - 2, defaultWidth, 2, '▄'); // Нижняя граница
        static StaticEntity leftWall = new StaticEntity(0, 0, 1, defaultHeight, ' '); // Изменяем высоту на 500
        static StaticEntity rightWall = new StaticEntity(defaultWidth - 1, 0, 1, defaultHeight, ' '); // Изменяем позицию и высоту
        static StaticEntity ground = new StaticEntity(0, defaultHeight - 2, defaultWidth, 1, '▀'); // Изменяем позицию и ширину

        static List<Entity> entities = new List<Entity> { topWall, leftWall, rightWall, bottomWall, ground };
        static Background background;

        static List<VisualEntity> visualEntities = new List<VisualEntity> { }; // Список визуальных объектов
        static List<Texture> textures = new List<Texture>(); // Список текстур

        static double gravity = 0.03; // Уменьшенное значение гравитации для более плавного прыжка
        static Timer gravityTimer;
        static int gravityStepTime = 125; // Время на смещение на один символ вниз (в миллисекундах)

        public const int game_speed = 5;  //default 5
        public const int framesPerSecond = 120;
        static int frameCounter = 0;

        static int cameraX = 0;
        static int cameraY = 0;

        // Пути к файлам
        public static string backgroundFilePath;
        public static string audioDirectoryPath;
        public static List<string> audioFiles;
        public static int currentTrackIndex = 0;
        // Пути к текстурам
        public static string heroTexturePath;
        public static string enemyTexturePath;
        public static string visualEntityTexturePath;
        // Текстуры
        public static Texture heroTexture;
        public static Texture enemyTexture;
        public static Texture visualEntityTexture;

        // Звук
        public static VolumeSampleProvider volumeProvider;
        public static WasapiOut waveOutEvent; // для воспроизведения аудио
        public static AudioFileReader audioFile; // для чтения аудио файлов

        public static void LoadResources()
        {
            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            backgroundFilePath = Path.Combine(projectDirectory, "Backgrounds", "background.txt");
            audioDirectoryPath = Path.Combine(projectDirectory, "Music");

            // Пути к текстурам
            heroTexturePath = Path.Combine(projectDirectory, "Textures", "hero.txt");
            enemyTexturePath = Path.Combine(projectDirectory, "Textures", "enemy.txt");
            visualEntityTexturePath = Path.Combine(projectDirectory, "Textures", "visualEntity.txt");

            // Загрузка текстур
            heroTexture = new Texture(heroTexturePath, ' ', 10, 10);
            enemyTexture = new Texture(enemyTexturePath, ' ', 10, 10);
            visualEntityTexture = new Texture(visualEntityTexturePath, ' ', 10, 10);

            // Загрузите и используйте файлы с использованием этих путей
            if (File.Exists(backgroundFilePath))
            {
                string backgroundContent = File.ReadAllText(backgroundFilePath);
                // Используйте содержимое фона
            }

            if (Directory.Exists(audioDirectoryPath))
            {
                audioFiles = new List<string>(Directory.GetFiles(audioDirectoryPath, "*.mp3"));

                if (audioFiles.Count > 0)
                {
                    Console.WriteLine($"Loading track: {audioFiles[currentTrackIndex]}");
                    audioFile = new AudioFileReader(audioFiles[currentTrackIndex]);
                    VolumeSampleProvider volumeProvider = new VolumeSampleProvider(audioFile) { Volume = 0.2f }; // Установите громкость (0.0 - без звука, 1.0 - максимум)
                    waveOutEvent.Init(volumeProvider);
                    waveOutEvent.PlaybackStopped += OnPlaybackStopped;
                }
                else
                {
                    Console.WriteLine("No valid audio files found.");
                }
            }
            else
            {
                Console.WriteLine($"Directory not found: {audioDirectoryPath}");
            }
        }

        public static void IncreaseVolume()
        {

            if (volumeProvider != null)
            {
                volumeProvider.Volume = Math.Min(1.0f, volumeProvider.Volume + 0.1f); // Увеличиваем громкость на 10%
                waveOutEvent.Init(volumeProvider);
#if DEBUG
        Console.WriteLine($"Volume increased to: {volumeProvider.Volume}");
#endif
            }
        }

        public static void DecreaseVolume()
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume = Math.Max(0.0f, volumeProvider.Volume - 0.1f); // Уменьшаем громкость на 10%
                waveOutEvent.Init(volumeProvider);
#if DEBUG
        Console.WriteLine($"Volume decreased to: {volumeProvider.Volume}");
#endif
            }
        }


        public static void Platform4_2(int a, int b)
        {
            StaticEntity platform = new StaticEntity(a, defaultHeight - b, 4, 2, '#');
            entities.Add(platform);
        }

        public static void Platform10_2(int a, int b)
        {
            StaticEntity platform = new StaticEntity(a, defaultHeight - b, 10, 2, '#');
            entities.Add(platform);
        }

        public static void Platform208_2(int a, int b)
        {
            StaticEntity platform = new StaticEntity(a, defaultHeight - b, 208, 2, '#');
            entities.Add(platform);
        }

        public static void Platform5_4(int a, int b)
        {
            StaticEntity platform = new StaticEntity(a, defaultHeight - b, 5, 4, '#');
            entities.Add(platform);
        }

        public static void Platform4_3(int a, int b)
        {
            StaticEntity platform = new StaticEntity(a, defaultHeight - b, 4, 3, '#');
            entities.Add(platform);
        }

        public static void Platform2_2(int a, int b)
        {
            StaticEntity platform = new StaticEntity(a, defaultHeight - b, 2, 2, '#');
            entities.Add(platform);
        }

        public static void Chain2_8(int a, int b)
        {
            VisualEntity chain1_1 = new VisualEntity(a, defaultHeight - b, 1, 1, '/');
            VisualEntity chain1_2 = new VisualEntity(a, (defaultHeight - b) - 1, 1, 1, '\\');
            VisualEntity chain1_3 = new VisualEntity(a, (defaultHeight - b) - 2, 1, 1, '/');
            VisualEntity chain1_4 = new VisualEntity(a, (defaultHeight - b) - 3, 1, 1, '\\');
            VisualEntity chain1_5 = new VisualEntity(a, (defaultHeight - b) - 4, 1, 1, '/');
            VisualEntity chain1_6 = new VisualEntity(a, (defaultHeight - b) - 5, 1, 1, '\\');
            VisualEntity chain1_7 = new VisualEntity(a, (defaultHeight - b) - 6, 1, 1, '/');
            VisualEntity chain1_8 = new VisualEntity(a, (defaultHeight - b) - 7, 1, 1, '\\');

            VisualEntity chain2_1 = new VisualEntity(a - 1, defaultHeight - b, 1, 1, '\\');
            VisualEntity chain2_2 = new VisualEntity(a - 1, (defaultHeight - b) - 1, 1, 1, '/');
            VisualEntity chain2_3 = new VisualEntity(a - 1, (defaultHeight - b) - 2, 1, 1, '\\');
            VisualEntity chain2_4 = new VisualEntity(a - 1, (defaultHeight - b) - 3, 1, 1, '/');
            VisualEntity chain2_5 = new VisualEntity(a - 1, (defaultHeight - b) - 4, 1, 1, '\\');
            VisualEntity chain2_6 = new VisualEntity(a - 1, (defaultHeight - b) - 5, 1, 1, '/');
            VisualEntity chain2_7 = new VisualEntity(a - 1, (defaultHeight - b) - 6, 1, 1, '\\');
            VisualEntity chain2_8 = new VisualEntity(a - 1, (defaultHeight - b) - 7, 1, 1, '/');

            visualEntities.Add(chain1_1);
            visualEntities.Add(chain1_2);
            visualEntities.Add(chain1_3);
            visualEntities.Add(chain1_4);
            visualEntities.Add(chain1_5);
            visualEntities.Add(chain1_6);
            visualEntities.Add(chain1_7);
            visualEntities.Add(chain1_8);

            visualEntities.Add(chain2_1);
            visualEntities.Add(chain2_2);
            visualEntities.Add(chain2_3);
            visualEntities.Add(chain2_4);
            visualEntities.Add(chain2_5);
            visualEntities.Add(chain2_6);
            visualEntities.Add(chain2_7);
            visualEntities.Add(chain2_8);
        }

        public static void Chain2_10(int a, int b)
        {
            VisualEntity chain1_1 = new VisualEntity(a, defaultHeight - b, 1, 1, '/');
            VisualEntity chain1_2 = new VisualEntity(a, (defaultHeight - b) - 1, 1, 1, '\\');
            VisualEntity chain1_3 = new VisualEntity(a, (defaultHeight - b) - 2, 1, 1, '/');
            VisualEntity chain1_4 = new VisualEntity(a, (defaultHeight - b) - 3, 1, 1, '\\');
            VisualEntity chain1_5 = new VisualEntity(a, (defaultHeight - b) - 4, 1, 1, '/');
            VisualEntity chain1_6 = new VisualEntity(a, (defaultHeight - b) - 5, 1, 1, '\\');
            VisualEntity chain1_7 = new VisualEntity(a, (defaultHeight - b) - 6, 1, 1, '/');
            VisualEntity chain1_8 = new VisualEntity(a, (defaultHeight - b) - 7, 1, 1, '\\');
            VisualEntity chain1_9 = new VisualEntity(a, (defaultHeight - b) - 8, 1, 1, '/');
            VisualEntity chain1_10 = new VisualEntity(a, (defaultHeight - b) - 9, 1, 1, '\\');

            VisualEntity chain2_1 = new VisualEntity(a - 1, defaultHeight - b, 1, 1, '\\');
            VisualEntity chain2_2 = new VisualEntity(a - 1, (defaultHeight - b) - 1, 1, 1, '/');
            VisualEntity chain2_3 = new VisualEntity(a - 1, (defaultHeight - b) - 2, 1, 1, '\\');
            VisualEntity chain2_4 = new VisualEntity(a - 1, (defaultHeight - b) - 3, 1, 1, '/');
            VisualEntity chain2_5 = new VisualEntity(a - 1, (defaultHeight - b) - 4, 1, 1, '\\');
            VisualEntity chain2_6 = new VisualEntity(a - 1, (defaultHeight - b) - 5, 1, 1, '/');
            VisualEntity chain2_7 = new VisualEntity(a - 1, (defaultHeight - b) - 6, 1, 1, '\\');
            VisualEntity chain2_8 = new VisualEntity(a - 1, (defaultHeight - b) - 7, 1, 1, '/');
            VisualEntity chain2_9 = new VisualEntity(a - 1, (defaultHeight - b) - 8, 1, 1, '\\');
            VisualEntity chain2_10 = new VisualEntity(a - 1, (defaultHeight - b) - 9, 1, 1, '/');

            visualEntities.Add(chain1_1);
            visualEntities.Add(chain1_2);
            visualEntities.Add(chain1_3);
            visualEntities.Add(chain1_4);
            visualEntities.Add(chain1_5);
            visualEntities.Add(chain1_6);
            visualEntities.Add(chain1_7);
            visualEntities.Add(chain1_8);
            visualEntities.Add(chain1_9);
            visualEntities.Add(chain1_10);

            visualEntities.Add(chain2_1);
            visualEntities.Add(chain2_2);
            visualEntities.Add(chain2_3);
            visualEntities.Add(chain2_4);
            visualEntities.Add(chain2_5);
            visualEntities.Add(chain2_6);
            visualEntities.Add(chain2_7);
            visualEntities.Add(chain2_8);
            visualEntities.Add(chain2_9);
            visualEntities.Add(chain2_10);
        }

        public static void Spike3_2(int a, int b)
        {
            Enemy spikeLeft = new Enemy(a, defaultHeight - b, 1, 1, '/');
            Enemy spikeMiddle1 = new Enemy(a + 1, defaultHeight - b, 1, 1, '\\');
            Enemy spikeMiddle2 = new Enemy(a + 2, defaultHeight - b, 1, 1, '/');
            Enemy spikeRight = new Enemy(a + 3, defaultHeight - b, 1, 1, '\\');

            Enemy spikeUpLeft = new Enemy(a + 1, (defaultHeight - b) - 1, 1, 1, '/');
            Enemy spikeUpRight = new Enemy(a + 2, (defaultHeight - b) - 1, 1, 1, '\\');

            entities.Add(spikeLeft);
            entities.Add(spikeMiddle1);
            entities.Add(spikeMiddle2);
            entities.Add(spikeRight);

            entities.Add(spikeUpLeft);
            entities.Add(spikeUpRight);
        }

        public static void Stairs(int a, int b)
        {
            VisualEntity stairs1 = new VisualEntity(a, defaultHeight - b, 4, 2, '#');
            VisualEntity stairs2 = new VisualEntity(a + 4, (defaultHeight - b) - 2, 4, 5, '#');
            VisualEntity stairs3 = new VisualEntity(a + 8, (defaultHeight - b) - 4, 4, 7, '#');
            VisualEntity stairs4 = new VisualEntity(a + 12, (defaultHeight - b) - 6, 4, 8, '#');
            VisualEntity stairs5 = new VisualEntity(a + 16, (defaultHeight - b) - 8, 4, 11, '#');
            VisualEntity stairs6 = new VisualEntity(a + 20, (defaultHeight - b) - 10, 4, 13, '#');

            entities.Add(stairs1);
            entities.Add(stairs2);
            entities.Add(stairs3);
            entities.Add(stairs4);
            entities.Add(stairs5);
            entities.Add(stairs6);
        }

        static void Main(string[] args)
        {


            IntPtr consoleWindow = GetConsoleWindow(); if (consoleWindow != IntPtr.Zero) { ShowWindow(consoleWindow, SW_MAXIMIZE); }
            Menu.DisplayMenu();
            SelectPlaybackDevice();

            Chain2_10(10, 14);
            Chain2_10(16, 14);
            Chain2_8(24, 16);
            Chain2_8(30, 16);
            Chain2_10(45, 14);
            Chain2_8(50, 16);
            Chain2_10(62, 14);
            Chain2_10(67, 14);
            Chain2_8(79, 16);
            Chain2_10(88, 14);
            Chain2_10(92, 14);
            Chain2_8(95, 16);
            Chain2_10(105, 14);
            Chain2_10(115, 14);
            Chain2_10(125, 14);
            Chain2_10(134, 14);
            Chain2_10(139, 14);
            Chain2_8(136, 16);
            Chain2_8(150, 16);
            Chain2_10(154, 14);
            Chain2_8(159, 16);
            Chain2_10(165, 14);
            Chain2_8(170, 16);
            Chain2_10(185, 14);
            Chain2_8(190, 16);
            Chain2_10(195, 14);
            Chain2_8(209, 16);

            Platform10_2(18, 4);
            Platform10_2(25, 8);
            Spike3_2(31, 10);
            Spike3_2(28, 4);
            Spike3_2(32, 4);
            Spike3_2(36, 4);
            Spike3_2(40, 4);
            Spike3_2(44, 4);
            Spike3_2(48, 4);
            Spike3_2(52, 4);
            Spike3_2(56, 4);
            Spike3_2(60, 4);
            Spike3_2(64, 4);
            Spike3_2(68, 4);
            Spike3_2(72, 4);
            Spike3_2(76, 4);
            Spike3_2(80, 4);
            Platform10_2(40, 10);
            Spike3_2(50, 12);
            Platform10_2(50, 10);
            Platform10_2(60, 13);
            Platform10_2(70, 13);
            Platform10_2(82, 9);
            Spike3_2(84, 4);
            Platform10_2(88, 4);
            Spike3_2(99, 4);
            Platform5_4(103, 6);
            Spike3_2(108, 4);
            Spike3_2(112, 4);
            Spike3_2(116, 4);
            Spike3_2(120, 4);
            Platform5_4(124, 6);
            Spike3_2(129, 4);
            Spike3_2(133, 4);
            Spike3_2(137, 4);
            Spike3_2(141, 4);
            Spike3_2(145, 4);
            Platform5_4(149, 6);
            Platform10_2(154, 6);
            Spike3_2(160, 8);
            Platform10_2(164, 6);
            Spike3_2(174, 8);
            Platform10_2(174, 6);
            Spike3_2(188, 8);
            Platform10_2(184, 6);
            Spike3_2(192, 8);
            Spike3_2(206, 8);
            Platform10_2(194, 6);
            Platform10_2(204, 6);
            Platform10_2(214, 6);
            Platform5_4(220, 6);
            Spike3_2(225, 6);
            Spike3_2(229, 6);
            Spike3_2(233, 6);
            Spike3_2(237, 6);
            Spike3_2(241, 6);
            Spike3_2(245, 6);
            Platform10_2(225, 10);
            Platform10_2(239, 14);
            Platform10_2(225, 19);
            Platform10_2(212, 22);

            Platform208_2(2, 25);
            Platform10_2(190, 27);
            Spike3_2(186, 27);
            Spike3_2(182, 27);
            Spike3_2(178, 27);
            Spike3_2(174, 27);
            Spike3_2(170, 27);
            Spike3_2(166, 27);
            Spike3_2(162, 27);
            Spike3_2(158, 27);
            Spike3_2(154, 27);
            Spike3_2(154, 27);
            Spike3_2(150, 27);
            Spike3_2(146, 27);
            Spike3_2(142, 27);
            Spike3_2(138, 27);
            Spike3_2(134, 27);
            Spike3_2(130, 27);
            Spike3_2(126, 27);
            Spike3_2(122, 27);
            Spike3_2(118, 27);
            Spike3_2(114, 27);
            Spike3_2(110, 27);
            Spike3_2(106, 27);
            Spike3_2(102, 27);
            Spike3_2(98, 27);
            Spike3_2(94, 27);
            Spike3_2(90, 27);
            Spike3_2(86, 27);
            Spike3_2(82, 27);
            Spike3_2(78, 27);
            Spike3_2(74, 27);
            Spike3_2(70, 27);
            Spike3_2(66, 27);
            Spike3_2(62, 27);
            Spike3_2(58, 27);
            Spike3_2(54, 27);
            Spike3_2(50, 27);
            Spike3_2(46, 27);
            Spike3_2(42, 27);
            Spike3_2(38, 27);
            Spike3_2(34, 27);
            Spike3_2(30, 27);
            Spike3_2(26, 27);
            Spike3_2(22, 27);
            Spike3_2(18, 27);
            Spike3_2(14, 27);
            Spike3_2(10, 27);
            Spike3_2(6, 27);
            Spike3_2(2, 27);

            Spike3_2(154, 27);
            Platform10_2(170, 30);
            Platform4_3(155, 33);
            Platform4_3(140, 33);
            Platform4_3(125, 33);
            Platform4_3(110, 33);
            Platform2_2(95, 33);
            Platform2_2(80, 33);
            Platform2_2(65, 33);
            Platform2_2(50, 33);
            Platform2_2(35, 33);
            Platform2_2(20, 33);

            Platform4_3(5, 37);
            Platform4_3(1, 37);
            Platform4_3(20, 42);
            Platform4_3(35, 47);

            Platform208_2(40, 50);
            Chain2_10(42, 39);
            Chain2_8(46, 41);
            Chain2_10(49, 39);
            Chain2_8(51, 41);
            Chain2_10(55, 39);
            Chain2_10(59, 39);
            Chain2_8(63, 41);
            Chain2_8(69, 41);
            Chain2_10(75, 39);
            Chain2_10(79, 39);
            Chain2_10(85, 39);
            Chain2_8(89, 41);
            Chain2_10(91, 39);
            Chain2_10(98, 39);
            Chain2_8(104, 41);
            Chain2_8(108, 41);
            Chain2_8(113, 41);
            Chain2_10(117, 39);
            Chain2_10(121, 39);
            Chain2_10(125, 39);
            Chain2_10(130, 39);
            Chain2_8(134, 41);
            Chain2_8(140, 41);
            Chain2_10(150, 39);
            Chain2_10(156, 39);
            Chain2_8(160, 41);
            Chain2_10(164, 39);
            Chain2_10(168, 39);
            Chain2_8(172, 41);
            Chain2_8(176, 41);
            Chain2_10(183, 39);
            Chain2_10(190, 39);
            Chain2_8(197, 41);
            Chain2_10(210, 39);
            Chain2_10(214, 39);
            Chain2_8(218, 41);
            Chain2_8(224, 41);
            Chain2_10(228, 39);
            Chain2_10(232, 39);
            Chain2_8(238, 41);

            Stairs(45, 51);
            Spike3_2(71, 51);
            Spike3_2(75, 51);
            Spike3_2(79, 51);
            Spike3_2(83, 51);
            Spike3_2(87, 51);
            Spike3_2(91, 51);
            Spike3_2(95, 51);
            Spike3_2(99, 51);
            Spike3_2(103, 51);
            Spike3_2(107, 51);
            Spike3_2(111, 51);
            Spike3_2(115, 51);
            Spike3_2(119, 51);
            Spike3_2(123, 51);
            Spike3_2(127, 51);
            Spike3_2(131, 51);
            Spike3_2(135, 51);
            Spike3_2(139, 51);
            Spike3_2(143, 51);
            Spike3_2(147, 51);
            Spike3_2(151, 51);
            Spike3_2(155, 51);
            Spike3_2(159, 51);
            Spike3_2(163, 51);
            Spike3_2(167, 51);
            Spike3_2(171, 51);
            Spike3_2(175, 51);
            Spike3_2(179, 51);
            Spike3_2(183, 51);
            Spike3_2(187, 51);
            Spike3_2(191, 51);
            Spike3_2(195, 51);
            Spike3_2(199, 51);
            Spike3_2(203, 51);
            Spike3_2(207, 51);
            Spike3_2(211, 51);
            Spike3_2(215, 51);
            Spike3_2(219, 51);
            Spike3_2(223, 51);
            Spike3_2(227, 51);
            Spike3_2(231, 51);
            Spike3_2(235, 51);
            Spike3_2(239, 51);
            Spike3_2(243, 51);

            Platform10_2(80, 61);
            Spike3_2(86, 63);
            Platform10_2(100, 61);
            Spike3_2(106, 63);
            Platform10_2(120, 61);
            Spike3_2(126, 63);
            Platform10_2(140, 61);
            Spike3_2(146, 63);
            Platform10_2(160, 61);
            Spike3_2(166, 63);
            Platform10_2(180, 61);
            Spike3_2(186, 63);
            Platform10_2(200, 61);
            Spike3_2(206, 63);
            Platform10_2(220, 61);
            Spike3_2(226, 63);

            // Загрузка ресурсов
            LoadResources();

            const double parallaxFactor = 0.5;

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
            player = new Hero(3, defaultHeight - 10, 3, 3, '█'); // Герой появляется внизу слева
            entities.Add(player);

            // Воспроизведение фоновой музыки с выбранного устройства
            PlayBackgroundMusic();

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


        public static void PlayBackgroundMusic()
        {
            waveOutEvent.Play();
        }

        public static void StopBackgroundMusic()
        {
            waveOutEvent?.Stop();
            waveOutEvent?.Dispose();
            audioFile?.Dispose();
        }

        public static void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                if (audioFiles != null && audioFiles.Count > 0)
                {
                    currentTrackIndex = (currentTrackIndex + 1) % audioFiles.Count;
                    Console.WriteLine($"Loading track: {audioFiles[currentTrackIndex]}");

                    audioFile.Dispose();
                    audioFile = new AudioFileReader(audioFiles[currentTrackIndex]);
                    waveOutEvent.Init(audioFile);

                    PlayBackgroundMusic();
                }
                else
                {
                    Console.WriteLine("No audio files to play.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading track: {ex.Message}");
            }
        }


        public static void SelectPlaybackDevice()
        {
            Console.WriteLine("Available playback devices:");
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            int index = 0;
            foreach (var device in devices)
            {
                Console.WriteLine($"{index++}: {device.FriendlyName}");
            }

            Console.Write("Select a device (by number): ");
            int deviceNumber = int.Parse(Console.ReadLine());
            var selectedDevice = devices[deviceNumber];

            try
            {
                waveOutEvent = new WasapiOut(selectedDevice, AudioClientShareMode.Shared, false, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing device: {ex.Message} (HRESULT: {ex.HResult})");
            }
        }




        public static void RespawnPlayer()
        {
            if (player != null)
            {
#if DEBUG
                Console.WriteLine("Respawning player...");
#endif
                player.X = 2;
                player.Y = defaultHeight - 8;
                player.IsJumping = false;
                player.CanJump = true;
                FinalRenderer.Draw(consoleWidth, consoleHeight, entities, visualEntities, teammates, background, cameraX, cameraY, 0.5);
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
        Hybrid,
        Static
    }

    static CameraMode currentCameraMode = CameraMode.Hybrid; // Для отладки используем только Basic

    static DateTime lastMovementTime = DateTime.Now;
    static bool isMoving = false;
    static Timer movementTimer;

    static int targetCameraX;
    static int targetCameraY;
    static int additionalOffsetY = 0; // Переменная для дополнительного смещения по Y

    static void InitializeMovementTimer()
    {
        movementTimer = new Timer(CheckForMovement, null, 0, 1000); // Проверка каждые 1 секунду
    }

    static void CheckForMovement(object state)
    {
        if (!isMoving && (DateTime.Now - lastMovementTime).TotalSeconds >= 3)
        {
            // Плавно смещаем таргет поинт вверх
            additionalOffsetY = Math.Min(additionalOffsetY + 1, 50); // Ограничим смещение 50 символами
        }
        else
        {
            additionalOffsetY = 0; // Сбрасываем смещение
        }

        isMoving = false; // Сброс движения для следующей проверки
    }

    static void UpdateCamera(Hero player, int consoleWidth, int consoleHeight)
    {
        if (player == null)
        {
            return;
        }

        // Проверка нажатия клавиш WASD
        if (Controls.IsKeyPressed(ConsoleKey.W) || Controls.IsKeyPressed(ConsoleKey.A) || Controls.IsKeyPressed(ConsoleKey.S) || Controls.IsKeyPressed(ConsoleKey.D))
        {
            isMoving = true;
            lastMovementTime = DateTime.Now;
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
                UpdateHybridModeCamera(player, consoleWidth, consoleHeight);
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
        targetCameraX = player.X - consoleWidth;
        targetCameraY = player.Y - consoleHeight;

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
            cameraY -= (int)((cameraY + triggerThresholdY + 2 - player.Y) * cameraInertiaY);
        }
        else if (player.Y > cameraY + consoleHeight - triggerThresholdY)
        {
            cameraY += (int)((player.Y - (cameraY + consoleHeight - triggerThresholdY)) * cameraInertiaY);
        }

        // Ограничение позиции камеры в пределах игрового поля
        cameraX = Math.Max(0, Math.Min(cameraX, Global.defaultWidth));
        cameraY = Math.Max(0, Math.Min(cameraY, Global.defaultHeight-consoleHeight-1));
    }

    static void UpdateAdvancedCamera(int consoleWidth, int consoleHeight)
    {
        const double cameraInertia = 0.05; // Коэффициент инерции камеры
        const double followSpeed = 0.05; // Скорость следования камеры за персонажем
        const double overtakeSpeed = 0.02; // Скорость обгона камеры
        int centerX = consoleWidth / 4; // Центр экрана по горизонтали
        int centerY = consoleHeight / 4; // Центр экрана по вертикали

        // Цель позиции камеры
        targetCameraX = player.X - centerX;
        targetCameraY = player.Y - centerY; 

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
        cameraX = Math.Max(0, Math.Min(cameraX, Global.defaultWidth));
        cameraY = Math.Max(0, Math.Min(cameraY, Global.defaultHeight));
    }

    static char previousCameraMode = 'B'; // 'B' для базового режима, 'A' для продвинутого режима
    static int StatM = 0;
    static void UpdateHybridModeCamera(Hero player, int consoleWidth, int consoleHeight)
    {
            if (player == null)
            {
               return;
            }
            

        // Проверка на отсутствие движения
        if (!isMoving && (DateTime.Now - lastMovementTime).TotalSeconds >= 5)
        {
                StatM += 1;
                return;
        }else { StatM = 0; }

            int triggerThresholdX = consoleWidth / 4; // Порог для реакции камеры по горизонтали ближе к середине
        int borderThreshold = 50; // Порог для границы карты

        // Ограничение позиции камеры в пределах игрового поля
        cameraX = Math.Max(0, Math.Min(cameraX, Global.defaultWidth));
        cameraY = Math.Max(0, Math.Min(cameraY, Global.defaultHeight - consoleHeight-StatM));

        // Если персонаж рядом с границей карты, используем только Basic режим
        if (cameraX < borderThreshold || cameraX > Global.defaultWidth - consoleWidth - borderThreshold)
        {
            UpdateBasicCamera(player, consoleWidth, consoleHeight);
            previousCameraMode = 'B';
            return;
        }

        // Проверка касания границы базового режима
        if (previousCameraMode == 'B')
        {
            if (player.X < cameraX + triggerThresholdX || player.X > cameraX + consoleWidth - triggerThresholdX)
            {
                previousCameraMode = 'A';
                UpdateAdvancedCamera(consoleWidth, consoleHeight);
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

        private static void ReceiveData()
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
                        if (!teammates.Any(t => t.X == x && t.Y == y)) // Убедитесь, что напарник еще не существует
                        {
                            var teammate = new Teammate(x, y, 4, 4, 'T');
                            Texture teammateTexture = new Texture("teammate.txt", 'T', 4, 4);

                            lock (teammates)
                            {
                             
                                entities.Add(teammate); // Добавляем напарника в список объектов игры
                            }

                            lock (textures)
                            {
                                textures.Add(teammateTexture); // Текстуру напарника добавляем в отдельный список текстур
                            }
                        }
                        else
                        {
                            var teammate = teammates.First(t => t.X == x && t.Y == y);
                            lock (teammates)
                            {
                                teammate.X = x;
                                teammate.Y = y;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
            }
        }
        static async Task GameLoop(int framesPerSecond, double parallaxFactor, Hero player)
        {
            try
            {
                while (running)
                {
                    frameCounter++;

                    // Обновление всех сущностей
                    List<Task> checkTasks = new List<Task>();
                    foreach (var entity in entities)
                    {
                        if (entity is Enemy enemy)
                        {
                            // Проверка на нахождение героя рядом
                            checkTasks.Add(enemy.CheckForHeroAsync(entities));
                        }
                    }

                    await Task.WhenAll(checkTasks);

                    PreRenderer.Update(entities, Global.defaultWidth, Global.defaultHeight);

                    if (player != null)
                    {
                        UpdateCamera(player, Global.consoleWidth, Global.consoleHeight);
                    }

                   
                    FinalRenderer.Draw(consoleWidth, consoleHeight, entities, visualEntities, teammates, background, cameraX, cameraY, parallaxFactor);

                    await Task.Delay(1000 / framesPerSecond); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameLoop exception: {ex.Message}");
            }
        }


    }
}
