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

        public static void StartKeyChecking(int framesPerSecond, List<Entity> entities)
        {
            while (true)
            {
                // Обновляем состояния клавиш
                UpdateKeyStates();

                Hero player = (Hero)entities.FirstOrDefault(e => e is Hero);

                if (player != null)
                {
                    if (IsKeyPressed(ConsoleKey.W))
                    {
                        player.UpdateState("jump");
                    }
                    else if (IsKeyPressed(ConsoleKey.S))
                    {
                        player.UpdateState("crouch");
                    }
                    else if (IsKeyPressed(ConsoleKey.A) || IsKeyPressed(ConsoleKey.D))
                    {
                        player.UpdateState("run");
                    }
                    else
                    {
                        player.UpdateState("idle");
                    }

                    if (IsKeyPressed(ConsoleKey.R)) // Добавим переключение режима камеры по клавише R
                    {
                        Global.currentCameraMode = (Global.CameraMode)(((int)Global.currentCameraMode + 1) % Enum.GetNames(typeof(Global.CameraMode)).Length);
                        Console.WriteLine($"Switched Camera Mode to: {Global.currentCameraMode}");
                        Thread.Sleep(200); // Небольшая задержка для предотвращения многократного срабатывания переключения
                    }
                }

                Thread.Sleep(1000 / framesPerSecond);
            }
        }


        private static void UpdateKeyStates()
        {
            lock (keyStates)
            {
                foreach (var keyMapping in keyMappings)
                {
                    keyStates[keyMapping.Key] = GetAsyncKeyState(keyMapping.Value) < 0;
                }
            }
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


















