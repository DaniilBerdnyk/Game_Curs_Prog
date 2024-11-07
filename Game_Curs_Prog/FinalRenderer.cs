using System;
using System.Text;
using System.Collections.Generic;

namespace Game_Curs_Prog
{
    public static class FinalRenderer
    {
        public static void Draw(int consoleWidth, int consoleHeight, List<Entity> entities, Background background, int cameraX, int cameraY)
        {
            // Инициализация буфера символами фона
            char[,] backgroundBuffer = new char[consoleWidth, consoleHeight];
            char[,] objectBuffer = new char[consoleWidth, consoleHeight];
            ConsoleColor[,] colorBuffer = new ConsoleColor[consoleWidth, consoleHeight];

            // Генерация полного фона
            char[,] fullBackground = background.GenerateFullBackground(1000, 40);
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    backgroundBuffer[x, y] = fullBackground[cameraX + x, cameraY + y];
                    colorBuffer[x, y] = ConsoleColor.Gray;
                }
            }

            // Отрисовка объектов поверх фона
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
                            objectBuffer[drawX, drawY] = entity.Symbol;
                            colorBuffer[drawX, drawY] = ConsoleColor.White;
                        }
                    }
                }
            }

            // Отображение буфера на консоли с использованием встроенных методов C#
            Console.Clear();
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    Console.SetCursorPosition(x, y);
                    if (objectBuffer[x, y] == '\0')
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write(backgroundBuffer[x, y]);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write(objectBuffer[x, y]);
                    }
                }
            }
            Console.ResetColor();
        }
    }
}

























