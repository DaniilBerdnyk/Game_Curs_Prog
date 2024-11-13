using System;
using System.IO;

namespace Game_Curs_Prog
{
    public class Background
    {
        private char[,] background;
        private int width;
        private int height;

        public Background(string filePath, int defaultWidth, int defaultHeight)
        {
            if (!LoadBackground(filePath))
            {
                CreateDefaultBackground(defaultWidth, defaultHeight);
            }
        }

        private bool LoadBackground(string filePath)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to read background file: {e.Message}. Using default background.");
                return false;
            }

            if (lines.Length == 0)
            {
                Console.WriteLine("Background file is empty. Using default background.");
                return false;
            }

            width = lines[0].Length;
            height = lines.Length;
            background = new char[width, height];

            for (int y = 0; y < height; y++)
            {
                if (lines[y].Length < width)
                {
                    lines[y] = lines[y].PadRight(width); // Дополняем строку пробелами
                }

                for (int x = 0; x < width; x++)
                {
                    background[x, y] = lines[y][x];
                }
            }

            return true;
        }

        private void CreateDefaultBackground(int width, int height)
        {
            this.width = width;
            this.height = height;
            background = new char[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    background[x, y] = '.';
                }
            }
        }

        public char GetChar(int x, int y)
        {
            int bgX = x % width;
            int bgY = y % height;
            return background[bgX, bgY];
        }

        public char[,] GenerateParallaxBackground(int mapWidth, int mapHeight, int cameraX, int cameraY, double parallaxFactor)
        {
            char[,] parallaxBackground = new char[mapWidth, mapHeight];
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int bgX = (int)((x + cameraX * parallaxFactor) % width);
                    int bgY = (int)((y + cameraY * parallaxFactor) % height);
                    parallaxBackground[x, y] = background[bgX, bgY];
                }
            }
            return parallaxBackground;
        }
    }
}





















