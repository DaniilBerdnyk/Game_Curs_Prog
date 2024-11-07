using System;
using System.IO;

namespace Game_Curs_Prog
{
    public class Background
    {
        private char[,] background;

        public Background(string filePath)
        {
            LoadBackground(filePath);
        }

        private void LoadBackground(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            int width = lines[0].Length;
            int height = lines.Length;
            background = new char[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    background[x, y] = lines[y][x];
                }
            }
        }

        public char GetChar(int x, int y)
        {
            int bgX = x % background.GetLength(0);
            int bgY = y % background.GetLength(1);
            return background[bgX, bgY];
        }

        public char[,] GenerateFullBackground(int mapWidth, int mapHeight)
        {
            char[,] fullBackground = new char[mapWidth, mapHeight];
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    fullBackground[x, y] = GetChar(x, y);
                }
            }
            return fullBackground;
        }
    }
}


