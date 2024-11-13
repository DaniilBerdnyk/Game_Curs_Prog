using System;
using System.Text;
using System.Collections.Generic;

namespace Game_Curs_Prog
{
    public static class FinalRenderer
    {
        public static void Draw(int consoleWidth, int consoleHeight, List<Entity> entities, Background background, int cameraX, int cameraY)
        {
            // Инициализация буфера символами фона и объектов
            char[,] frameBuffer = new char[consoleWidth, consoleHeight];
            ConsoleColor[,] colorBuffer = new ConsoleColor[consoleWidth, consoleHeight];

            // Генерация фона с параллаксом
            char[,] parallaxBackground = background.GenerateParallaxBackground(1000, 100, cameraX, cameraY, 0.3);

            // Заполнение буфера фона
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    frameBuffer[x, y] = parallaxBackground[x, y];
                    colorBuffer[x, y] = ConsoleColor.Gray; // Используем серый цвет для фона
                }
            }

            // Заполнение буфера объектов
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
                            frameBuffer[drawX, drawY] = entity.Symbol;
                            colorBuffer[drawX, drawY] = ConsoleColor.White; // Устанавливаем белый цвет для объектов
                        }
                    }
                }
            }

            RenderFinalFrame(frameBuffer, colorBuffer, consoleWidth, consoleHeight);
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
            Console.SetCursorPosition(0, 0);
            Console.Write(sb.ToString());
            Console.ResetColor();
        }

        public static void SetBackgroundColor(ConsoleColor color)
        {
            Console.BackgroundColor = color;
        }

        public static void SetForegroundColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        public static void ClearScreen()
        {
            Console.Clear();
        }
    }
}















































