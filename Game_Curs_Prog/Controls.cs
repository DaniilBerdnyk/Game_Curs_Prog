using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
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
            { ConsoleKey.R, 0x52 },
            { ConsoleKey.LeftArrow, 0x25 },  // Для предыдущего трека
            { ConsoleKey.RightArrow, 0x27 }, // Для следующего трека
            { ConsoleKey.UpArrow, 0x26 },    // Для увеличения громкости
            { ConsoleKey.DownArrow, 0x28 },  // Для уменьшения громкости
            { ConsoleKey.Escape, 0x1B }
        };

        private static readonly Dictionary<ConsoleKey, bool> keyStates = new Dictionary<ConsoleKey, bool>
        {
            { ConsoleKey.W, false },
            { ConsoleKey.S, false },
            { ConsoleKey.A, false },
            { ConsoleKey.D, false },
            { ConsoleKey.R, false },
            { ConsoleKey.LeftArrow, false },
            { ConsoleKey.RightArrow, false },
            { ConsoleKey.UpArrow, false },
            { ConsoleKey.DownArrow, false },
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

                    if (IsKeyPressed(ConsoleKey.R))
                    {
                        Global.currentCameraMode = (Global.CameraMode)(((int)Global.currentCameraMode + 1) % Enum.GetNames(typeof(Global.CameraMode)).Length);
                        Console.WriteLine($"Switched Camera Mode to: {Global.currentCameraMode}");
                        Thread.Sleep(200);
                    }
                }

                if (IsKeyPressed(ConsoleKey.RightArrow)) // Следующий трек
                {
                    MusicPlayer.NextTrack();
                    Thread.Sleep(200); // Небольшая задержка для предотвращения многократного срабатывания переключения
                }

                if (IsKeyPressed(ConsoleKey.LeftArrow)) // Предыдущий трек
                {
                    MusicPlayer.PreviousTrack();
                    Thread.Sleep(200); // Небольшая задержка для предотвращения многократного срабатывания переключения
                }

                if (IsKeyPressed(ConsoleKey.UpArrow)) // Увеличение громкости
                {
                    MusicPlayer.IncreaseVolume();
                    Thread.Sleep(200); // Небольшая задержка для предотвращения многократного срабатывания переключения
                }

                if (IsKeyPressed(ConsoleKey.DownArrow)) // Уменьшение громкости
                {
                    MusicPlayer.DecreaseVolume();
                    Thread.Sleep(200); // Небольшая задержка для предотвращения многократного срабатывания переключения
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

    public static class MusicPlayer
    {
        public static void NextTrack()
        {
            if (Program.audioFiles != null && Program.audioFiles.Count > 0)
            {
                Program.StopBackgroundMusic(); // Останавливаем воспроизведение текущего трека

                Program.currentTrackIndex = (Program.currentTrackIndex + 1) % Program.audioFiles.Count; // Переходим к следующему треку
                Program.audioFile = new AudioFileReader(Program.audioFiles[Program.currentTrackIndex]); // Загружаем следующий трек

                // Создаем новый экземпляр WasapiOut
                Program.waveOutEvent = new WasapiOut();
                Program.waveOutEvent.Init(Program.audioFile); // Инициализируем воспроизведение
                Program.PlayBackgroundMusic(); // Начинаем воспроизведение
                Console.WriteLine($"Playing next track: {Program.audioFiles[Program.currentTrackIndex]}");
            }
        }

        public static void PreviousTrack()
        {
            if (Program.audioFiles != null && Program.audioFiles.Count > 0)
            {
                Program.StopBackgroundMusic(); // Останавливаем воспроизведение текущего трека

                Program.currentTrackIndex = (Program.currentTrackIndex - 1 + Program.audioFiles.Count) % Program.audioFiles.Count; // Возвращаемся к предыдущему треку
                Program.audioFile = new AudioFileReader(Program.audioFiles[Program.currentTrackIndex]); // Загружаем предыдущий трек

                // Создаем новый экземпляр WasapiOut
                Program.waveOutEvent = new WasapiOut();
                Program.waveOutEvent.Init(Program.audioFile); // Инициализируем воспроизведение
                Program.PlayBackgroundMusic(); // Начинаем воспроизведение
                Console.WriteLine($"Playing previous track: {Program.audioFiles[Program.currentTrackIndex]}");
            }
        }

        public static void IncreaseVolume()
        {
            if (Program.waveOutEvent != null)
            {
                Program.waveOutEvent.Volume = Math.Min(1.0f, Program.waveOutEvent.Volume + 0.1f); // Увеличиваем громкость на 10%
                Console.WriteLine($"Volume increased to: {Program.waveOutEvent.Volume}");
            }
        }

        public static void DecreaseVolume()
        {
            if (Program.waveOutEvent != null)
            {
                Program.waveOutEvent.Volume = Math.Max(0.0f, Program.waveOutEvent.Volume - 0.1f); // Уменьшаем громкость на 10%
                Console.WriteLine($"Volume decreased to: {Program.waveOutEvent.Volume}");
            }
        }
    }
}

