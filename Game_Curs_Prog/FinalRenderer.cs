using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Game_Curs_Prog
{
    public static class FinalRenderer
    {
        public static void Draw(int consoleWidth, int consoleHeight, List<Entity> entities, List<Hero> teammates, Background background, int cameraX, int cameraY, double parallaxFactor)
        {
            // Инициализация буфера символами фона и объектов
            char[,] frameBuffer = new char[consoleWidth, consoleHeight];
            char[,] objectFrame = new char[consoleWidth, consoleHeight];
            char[,] visualFrame = new char[consoleWidth, consoleHeight];

            // Генерация фона с параллаксом
            char[,] parallaxBackground = background.GenerateParallaxBackground(1000, 40, cameraX, cameraY, parallaxFactor);

            // Заполнение буфера фона
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    frameBuffer[x, y] = parallaxBackground[x, y];
                }
            }

            // Заполнение буфера объектов и визуальных элементов
            foreach (var entity in entities)
            {
                if (entity is VisualEntity)
                {
                    for (int y = 0; y < entity.Height; y++)
                    {
                        for (int x = 0; x < entity.Width; x++)
                        {
                            int drawX = entity.X - cameraX + x;
                            int drawY = entity.Y - cameraY + y;

                            if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight)
                            {
                                visualFrame[drawX, drawY] = entity.Symbol;
                            }
                        }
                    }
                }
                else
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
            }

            // Добавление напарников в отрисовку
            foreach (var teammate in teammates)
            {
                for (int y = 0; y < teammate.Height; y++)
                {
                    for (int x = 0; x < teammate.Width; x++)
                    {
                        int drawX = teammate.X - cameraX + x;
                        int drawY = teammate.Y - cameraY + y;

                        if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight)
                        {
                            objectFrame[drawX, drawY] = teammate.Symbol;
                        }
                    }
                }
            }

            // Комбинирование всех слоев в финальный буфер
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    if (visualFrame[x, y] != '\0')
                    {
                        frameBuffer[x, y] = visualFrame[x, y];
                    }
                    else if (objectFrame[x, y] != '\0')
                    {
                        frameBuffer[x, y] = objectFrame[x, y];
                    }
                }
            }

            RenderFinalFrame(frameBuffer, consoleWidth, consoleHeight);
        }

        public static void RenderFinalFrame(char[,] frame, int width, int height)
        {
            var sb = new StringBuilder();
            Console.SetCursorPosition(0, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sb.Append(frame[x, y]);
                }
                if (y < height - 1) // Удаление лишних линий
                {
                    sb.AppendLine();
                }
            }
            Console.SetCursorPosition(0, 0); // Устанавливаем курсор в начало перед выводом
            Console.Write(sb.ToString());   // Выводим всю строку

            // Небольшая задержка для контроля частоты обновления
            int Delay = Program.game_speed;
            Thread.Sleep(Delay); // Задержка для плавности
        }

        public static void ClearScreen()
        {
            Console.Clear();
        }
    }
}























































