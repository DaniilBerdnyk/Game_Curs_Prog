using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Game_Curs_Prog
{
    public static class Controls
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly Dictionary<ConsoleKey, int> keyMappings = new Dictionary<ConsoleKey, int>
        {
            { ConsoleKey.W, 0x57 },
            { ConsoleKey.S, 0x53 },
            { ConsoleKey.A, 0x41 },
            { ConsoleKey.D, 0x44 },
            { ConsoleKey.R, 0x52 }, // Добавляем клавишу R
            { ConsoleKey.Escape, 0x1B }
        };

        private static readonly Dictionary<ConsoleKey, bool> keyStates = new Dictionary<ConsoleKey, bool>
        {
            { ConsoleKey.W, false },
            { ConsoleKey.S, false },
            { ConsoleKey.A, false },
            { ConsoleKey.D, false },
            { ConsoleKey.R, false }, // Добавляем клавишу R
            { ConsoleKey.Escape, false }
        };

        public static void StartKeyChecking(int framesPerSecond)
        {
            int delay = 1000 / framesPerSecond;
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    foreach (var keyMapping in keyMappings)
                    {
                        short keyState = GetAsyncKeyState(keyMapping.Value);
                        lock (keyStates)
                        {
                            keyStates[keyMapping.Key] = (keyState & 0x8000) != 0;
                        }
                    }
                    Thread.Sleep(delay); // Минимальная задержка между проверками
                }
            });
            thread.Start();
        }

        public static bool IsKeyPressed(ConsoleKey key)
        {
            lock (keyStates)
            {
                return keyStates.ContainsKey(key) && keyStates[key];
            }
        }

        public static void ResetKey(ConsoleKey key)
        {
            lock (keyStates)
            {
                if (keyStates.ContainsKey(key))
                {
                    keyStates[key] = false;
                }
            }
        }
    }
}


















