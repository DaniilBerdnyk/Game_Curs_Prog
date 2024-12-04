using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

namespace Game_Curs_Prog
{
    public static class FinalRenderer
    {
        public static void Draw(int consoleWidth, int consoleHeight, List<Entity> entities, List<VisualEntity> visualEntities, List<Hero> teammates, Background background, int cameraX, int cameraY, double parallaxFactor)
        {
            // Инициализация буфера символами фона и объектов
            char[,] frameBuffer = new char[consoleWidth, consoleHeight];
            char[,] objectFrame = new char[consoleWidth, consoleHeight];
            char[,] visualFrame = new char[consoleWidth, consoleHeight];
            char[,] textureFrame = new char[consoleWidth, consoleHeight];

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

            // Заполнение буфера визуальных объектов
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
                            visualFrame[drawX, drawY] = visualEntity.Symbol;
                        }
                    }
                }
            }

            // Заполнение буфера текстур
            foreach (var gameEntity in entities)
            {
                if (gameEntity is Hero hero)
                {
                    Texture texture = hero.GetCurrentTexture(); // Получаем текущую текстуру для героя

                    // Проверка успешности загрузки текстуры и null
                    if (texture != null && texture.Image != null)
                    {
                        // Смещение текстуры относительно героя
                        int offsetX = (texture.Width - gameEntity.Width) / 2; // Центрируем текстуру по горизонтали
                        int offsetY = (texture.Height - gameEntity.Height) / 2; // Центрируем текстуру по вертикали

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
                    else
                    {
                        // Отладочный вывод для случая, если текстура не загружена
                        Console.WriteLine($"Texture not loaded for hero.");
                    }
                }
            }

            // Заполнение буфера игровых объектов
            foreach (var gameEntity in entities)
            {
                for (int y = 0; y < gameEntity.Height; y++)
                {
                    for (int x = 0; x < gameEntity.Width; x++)
                    {
                        int drawX = gameEntity.X - cameraX + x;
                        int drawY = gameEntity.Y - cameraY + y;

                        if (drawX >= 0 && drawX < consoleWidth && drawY >= 0 && drawY < consoleHeight)
                        {
                            objectFrame[drawX, drawY] = gameEntity.Symbol;
                        }
                    }
                }
            }

            // Комбинирование всех слоев в финальный буфер
            for (int y = 0; y < consoleHeight; y++)
            {
                for (int x = 0; x < consoleWidth; x++)
                {
                    frameBuffer[x, y] = parallaxBackground[x, y]; // Сначала фон
                    if (objectFrame[x, y] != '\0')
                    {
                        frameBuffer[x, y] = objectFrame[x, y]; // Затем игровые объекты
                    }
                    if (textureFrame[x, y] != '\0')
                    {
                        frameBuffer[x, y] = textureFrame[x, y]; // Потом текстуры
                    }
                    if (visualFrame[x, y] != '\0')
                    {
                        frameBuffer[x, y] = visualFrame[x, y]; // И в конце визуальные объекты
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
