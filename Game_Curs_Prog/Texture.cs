using System;
using System.IO;
using System.Collections.Generic;

namespace Game_Curs_Prog
{
    public class Texture
    {
        public char[,] Image { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private string currentFilePath;
        private char defaultSymbol;

        public static Dictionary<Type, string> TypeTextureMapping = new Dictionary<Type, string>
        {
            { typeof(Hero), "Textures/hero.txt" },
            { typeof(Enemy), "Textures/enemy.txt" },
            { typeof(VisualEntity), "Textures/visualEntity.txt" }
        };

        public Texture(string filePath, char defaultSymbol, int width, int height)
        {
            this.defaultSymbol = defaultSymbol;
            Width = width;
            Height = height;

            // Попробуем загрузить текстуру, если не удастся, назначим дефолтную текстуру
            if (!LoadImage(filePath))
            {
                SetDefaultImage();
            }
        }

        public bool LoadImage(string filePath)
        {
            currentFilePath = filePath;
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
                {
                    return false; // Файл пустой или содержит только пробелы
                }
                else
                {
                    // Определяем фактическую ширину и высоту текстуры, исключая излишние пробелы
                    int maxWidth = 0;
                    int maxHeight = lines.Length;

                    for (int y = 0; y < lines.Length; y++)
                    {
                        int width = lines[y].TrimEnd().Length;
                        if (width > maxWidth)
                        {
                            maxWidth = width;
                        }
                    }

                    Width = maxWidth;
                    Height = maxHeight;
                    Image = new char[Width, Height];

                    // Заполняем массив символов
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            if (x < lines[y].Length)
                            {
                                Image[x, y] = lines[y][x];
                            }
                            else
                            {
                                Image[x, y] = ' '; // Заполняем пробелами
                            }
                        }
                    }
                    return true; // Успешно загружено
                }
            }
            return false; // Файл не найден
        }

        private void SetDefaultImage()
        {
            Image = new char[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Image[x, y] = defaultSymbol;
                }
            }
        }
    }
}






