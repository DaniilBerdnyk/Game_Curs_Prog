using System;

namespace Game_Curs_Prog
{
    class Menu
    {
        private const int ConsoleWidth = 120;
        private const int ConsoleHeight = 30;
        private static string background =
        @"
. .     .     o   o     .         .  o    .    o        .    o    .    .
    .   .       .      .      o       .          o   .      .         . 
.         o      .       .       .        o   .      .   o      o   o   
      .          o         o         .         .           o         .  
    .   .       .      .      o       .          o   .      .         . 
.         o      .       .       .        o   .      .   o      o   o    
      .          o         o         .         .           o         .
. .     .     o   o     .         .  o    .    o        .    o    .    .
    .   .       .      .      o       .          o   .      .         . 
.         o      .       .       .        o   .      .   o      o   o   
      .          o         o         .         .           o         .  
    .   .       .      .      o       .          o   .      .         . 
.         o      .       .       .        o   .      .   o      o   o    
      .          o         o         .         .           o         .
. .     .     o   o     .         .  o    .    o        .    o    .    .
    .   .       .      .      o       .          o   .      .         . 
.         o      .       .       .        o   .      .   o      o   o   
      .          o         o         .         .           o         .  
    .   .       .      .      o       .          o   .      .         . 
.         o      .       .       .        o   .      .   o      o   o    
      .          o         o         .         .           o         .
        ";

        public static void DisplayMenu()
        {
            bool isRunning = true;

            while (isRunning) // Цикл для постоянного отображения меню
            {
                Console.Clear();
                DrawBackground();

                Console.ForegroundColor = ConsoleColor.Cyan;
                WriteCenteredText("WELCOME TO THE GAME", ConsoleHeight / 2 - 2);
                Console.ForegroundColor = ConsoleColor.Green;
                WriteCenteredText("1. PLAY", ConsoleHeight / 2);
                WriteCenteredText("2. EXIT", ConsoleHeight / 2 + 2);
                Console.ResetColor();

                ConsoleKeyInfo keyInfo = Console.ReadKey(); // Ждём нажатия клавиши
                switch (keyInfo.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        StartGame(); // Логика начала игры
                        isRunning = false;
                        break;

                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        Console.Clear();
                        Environment.Exit(0);
                        break;

                    default:
                        WriteCenteredText("Invalid choice, press 1 or 2", ConsoleHeight / 2 + 4);
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void StartGame()
        {
            Console.Clear();
            WriteCenteredText("The game is starting... ", ConsoleHeight / 2);
        }

        private static void DrawBackground()
        {
            Console.Clear();
            var lines = background.Split('\n');

            int backgroundLinesCount = lines.Length;

            for (int i = 0; i < ConsoleHeight; i++)
            {
                string line = lines[i % backgroundLinesCount]; 
                Console.WriteLine(line.PadRight(ConsoleWidth));
            }
        }

        private static void WriteCenteredText(string text, int row)
        {
            int leftPosition = Math.Max((ConsoleWidth - text.Length) / 2, 0);
            if (row >= 0 && row < ConsoleHeight)
            {
                Console.SetCursorPosition(leftPosition, row);
                Console.WriteLine(text);
            }
        }
    }
}
