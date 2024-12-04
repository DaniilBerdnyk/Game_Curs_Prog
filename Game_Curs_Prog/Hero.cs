using System;
using System.Collections.Generic;

   namespace Game_Curs_Prog
    {
        public class Hero : Entity
        {
            private int animationSpeed = 100; // Время одного кадра в миллисекундах
            private DateTime lastAnimationTime;
            private bool isAnimating = false;
            private string currentDirection = "right"; // Текущее направление (right, left)

            public string State { get; private set; } = "idle";
            private Dictionary<string, List<Texture>> textures;
            private int currentFrame = 0;
            private DateTime lastFrameChangeTime;
            private int frameDuration = 100; // Длительность каждого кадра в миллисекундах
            public int JumpHeight { get; set; }
            public bool IsJumping { get; set; }
            public bool CanJump { get; set; } = false;
            public bool IsCrouching { get; set; } = false;
            private int originalHeight;
            private const int CrouchHeight = 2; // Высота героя при приседании
            private int jumpStartY;
            private const int MaxJumpHeight = 6;
            private const int GroundTimeThreshold = 30;
            private DateTime? firstGroundTime;
            private const int apexDelay = 50; // Задержка в апексе прыжка (в миллисекундах)
            private bool apexReached = false; // Флаг достижения апекса прыжка
            private DateTime apexReachedTime; // Время достижения апекса прыжка
            public double jumpVelocity; // Публичное поле
            private const double initialJumpVelocity = 0.1; // Уменьшенное значение начальной скорости прыжка
            private const double gravity = 0.002; // Уменьшенное значение гравитации
            private const double acceleration = 0.05; // Уменьшенное значение ускорения
            public const double speed = 0.20; // Время шага
            public double velocityX; // Публичное поле
            private double slideSpeed; // Скорость скольжения
            private const double friction = 0.03; // Трение
            public DateTime lastMoveTime { get; private set; } // Время последнего движения
            public DateTime lastJumpMoveTime { get; private set; } // Время последнего движения в прыжке
            public string lastMoveDirection { get; private set; } // Направление последнего движения

            public Hero(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol)
            {
                JumpHeight = MaxJumpHeight;
                IsJumping = false;
                IsCrouching = false;
                originalHeight = height;
                jumpVelocity = initialJumpVelocity;
                velocityX = 0;
                slideSpeed = 0;
                lastMoveTime = DateTime.Now;
                lastJumpMoveTime = DateTime.Now;
                lastMoveDirection = "right";

                textures = new Dictionary<string, List<Texture>>
            {
                { "idle", LoadTextures("Textures/hero_idle_", 3, ' ', width, height) },
                { "run_right", LoadTextures("Textures/hero_run_right_", 3, ' ', width, height) },
                { "run_left", LoadTextures("Textures/hero_run_left_", 3, ' ', width, height) },
                { "jump_right", LoadTextures("Textures/hero_jump_right_", 3, ' ', width, height) },
                { "jump_left", LoadTextures("Textures/hero_jump_left_", 3, ' ', width, height) },
                { "crouch", LoadTextures("Textures/hero_crouch_", 3, ' ', width, height) }
            };

                lastFrameChangeTime = DateTime.Now;
            }

        private List<Texture> LoadTextures(string basePath, int frameCount, char defaultSymbol, int width, int height)
        {
            List<Texture> textures = new List<Texture>();
            for (int i = 0; i < frameCount; i++)
            {
                string filePath = $"{basePath}{i}.txt";
                textures.Add(new Texture(filePath, defaultSymbol, width, height));
                // Отладочный вывод для проверки загрузки текстур
                Console.WriteLine($"Texture loaded: {filePath}");
            }
            return textures;
        }


        private void UpdateTexture()
        {
            string texturePath = textures[State][currentFrame].currentFilePath;
            textures[State][currentFrame].LoadImage(texturePath);
            // Отладочный вывод для проверки обновления текстуры
            Console.WriteLine($"Texture updated: {texturePath}");
        }

        public void SetDirection(string direction)
            {
                if (direction == "right")
                {
                    UpdateState(IsJumping ? "jump_right" : "run_right");
                }
                else if (direction == "left")
                {
                    UpdateState(IsJumping ? "jump_left" : "run_left");
                }
            }

            public void ChangeDirection(string direction)
            {
                if (currentDirection != direction)
                {
                    currentDirection = direction;
                    StartAnimation(); // Запуск анимации при смене направления
                }
            }

            public void StartAnimation()
            {
                isAnimating = true;
                lastAnimationTime = DateTime.Now;
            }

            public void StopAnimation()
            {
                isAnimating = false;
                currentFrame = 0; // Сбросить кадр анимации на начальный
            }

        public void UpdateAnimation()
        {
            if (isAnimating)
            {
                DateTime currentTime = DateTime.Now;
                if ((currentTime - lastAnimationTime).TotalMilliseconds >= animationSpeed)
                {
                    currentFrame = (currentFrame + 1) % textures[State].Count;
                    lastAnimationTime = currentTime;
                    UpdateTexture(); // Обновление текстуры
                }
            }
        }


        public (int centerX, int centerY) GetCenter()
            {
                int centerX = X + Width / 2;
                int centerY = Y + Height / 2;
                return (centerX, centerY);
            }

        public void UpdateState(string newState)
        {
            string stateWithDirection = newState;
            if (newState != "idle")
            {
                stateWithDirection += (currentDirection == "right" ? "_right" : "_left");
            }

            if (textures.ContainsKey(stateWithDirection))
            {
                State = stateWithDirection;
                currentFrame = 0;
                lastFrameChangeTime = DateTime.Now;
                StartAnimation(); // Запуск анимации при смене состояния

                // Отладочный вывод для проверки смены состояния
                Console.WriteLine($"State updated to: {State}");
            }
            else
            {
                // Отладочный вывод для случая, если состояние не найдено
                Console.WriteLine($"State not found: {stateWithDirection}");
            }
        }


        public Texture GetCurrentTexture()
        {
            // Отладочный вывод для проверки текущего состояния и кадра
            Console.WriteLine($"Getting texture for state: {State}, frame: {currentFrame}");
            return textures[State][currentFrame];
        }



        public void Update(List<Entity> entities)
            {
                UpdateJump(entities);
                UpdateMovement(entities);
                UpdateCrouch(entities); // Обновление приседания
                CheckCollisions(entities);
                UpdateGroundTime(entities);
                UpdateAnimation(); // Вызов обновления анимации
            }

            public void Jump(List<Entity> entities)
            {
                if (!IsJumping && CanJump && !IsCrouching)
                {
                    jumpStartY = Y;
                    IsJumping = true;
                    CanJump = false;
                    jumpVelocity = initialJumpVelocity * 0.5; // Уменьшенное значение начальной скорости прыжка для более медленного подъема
                    StartAnimation(); // Запуск анимации прыжка
                    SetDirection(lastMoveDirection); // Установить направление прыжка в зависимости от последнего движения
                }
            }

            private void UpdateMovement(List<Entity> entities)
            {
                DateTime currentTime = DateTime.Now;
                double timeSinceLastMove = (currentTime - lastMoveTime).TotalMilliseconds;

                if (timeSinceLastMove >= speed * 1000) // Проверка времени шага
                {
                    if (Controls.IsKeyPressed(ConsoleKey.A))
                    {
                        int newX = X - 1; // Движение влево на 1 символ
                        if (!entities.Any(e => e != this && IsCollidingInDirection(e, -1, 0)))
                        {
                            X = newX;
                        }
                        lastMoveTime = currentTime;
                        lastMoveDirection = "left";
                        ChangeDirection("left"); // Изменить направление на left
                    }
                    else if (Controls.IsKeyPressed(ConsoleKey.D))
                    {
                        int newX = X + 1; // Движение вправо на 1 символ
                        if (!entities.Any(e => e != this && IsCollidingInDirection(e, 1, 0)))
                        {
                            X = newX;
                        }
                        lastMoveTime = currentTime;
                        lastMoveDirection = "right";
                        ChangeDirection("right"); // Изменить направление на right
                    }
                    else
                    {
                        UpdateState("idle"); // Установить состояние idle при отсутствии движения
                    }
                }
            }
        
    




        public void Crouch()
        {
            if (!IsJumping)
            {
                IsCrouching = true;
                Height = CrouchHeight;
            }
        }

        public void StandUp(List<Entity> entities)
        {
            if (IsCrouching)
            {
                IsCrouching = false;

                // Проверка, что герой не провалится под землю
                if (IsOnGround(entities))
                {
                    Y -= 2; // Поднять героя на два символа перед увеличением высоты
                }

                Height = originalHeight;
            }
        }
        private void UpdateJump(List<Entity> entities)
        {
            if (IsJumping)
            {
                // Если герой достиг апекса и задержка еще не завершена, ждем
                if (apexReached && (DateTime.Now - apexReachedTime).TotalMilliseconds < apexDelay)
                {
                    return;
                }

                double newY = Y - jumpVelocity;
                jumpVelocity -= gravity;

                bool isColliding = entities.Any(e => e != this && IsCollidingInDirection(e, 0, (int)newY - Y));
                if (isColliding || Y <= jumpStartY - MaxJumpHeight)
                {
                    // Если герой достиг апекса прыжка
                    if (!apexReached)
                    {
                        apexReached = true;
                        apexReachedTime = DateTime.Now;
                        jumpVelocity = 0; // Останавливаем вертикальную скорость
                        return;
                    }
                    else
                    {
                        apexReached = false; // Сбрасываем флаг после задержки
                        IsJumping = false;
                    }
                }
                else
                {
                    Y = (int)newY;
                }

                if (jumpVelocity <= 0)
                {
                    IsJumping = false;
                    jumpVelocity = 0;
                }

                // Диагональный прыжок при нажатии клавиши направления
                DateTime currentTime = DateTime.Now;
                double timeSinceLastJumpMove = (currentTime - lastJumpMoveTime).TotalMilliseconds;
                double jumpMoveSpeed = speed * 0.5; // Уменьшение времени ввода при прыжке

                if (timeSinceLastJumpMove >= jumpMoveSpeed * 100) // Преобразуем скорость в миллисекунды
                {
                    if (Controls.IsKeyPressed(ConsoleKey.A))
                    {
                        int newX = X - 1; // Движение влево на 1 символ
                        if (!entities.Any(e => e != this && IsCollidingInDirection(e, -1, 0)))
                        {
                            X = newX;
                        }
                        lastJumpMoveTime = currentTime;
                        lastMoveDirection = "left";
                    }
                    else if (Controls.IsKeyPressed(ConsoleKey.D))
                    {
                        int newX = X + 1; // Движение вправо на 1 символ
                        if (!entities.Any(e => e != this && IsCollidingInDirection(e, 1, 0)))
                        {
                            X = newX;
                        }
                        lastJumpMoveTime = currentTime;
                        lastMoveDirection = "right";
                    }
                }
            }
        }
        

        private void UpdateCrouch(List<Entity> entities)
        {
            if (Controls.IsKeyPressed(ConsoleKey.S))
            {
                Crouch();
            }
            else
            {
                StandUp(entities);
            }
        }

        public void Land()
        {
            IsJumping = false;
            if (firstGroundTime == null)
            {
                firstGroundTime = DateTime.Now;
            }
        }

        private void UpdateGroundTime(List<Entity> entities)
        {
            if (IsOnGround(entities))
            {
                if (firstGroundTime != null && (DateTime.Now - firstGroundTime.Value).TotalMilliseconds >= GroundTimeThreshold)
                {
                    CanJump = true;
                    firstGroundTime = null;
                }
            }
            else
            {
                firstGroundTime = null;
                CanJump = false;
            }
        }

        private void CheckCollisions(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity != this && IsCollidingBottom(entity))
                {
                    Y = entity.Y - Height;
                    Land();
                }
                if (entity != this && (IsCollidingLeft(entity) || IsCollidingRight(entity)))
                {
                    if (IsCollidingLeft(entity))
                    {
                        X = entity.X + entity.Width;
                    }
                    if (IsCollidingRight(entity))
                    {
                        X = entity.X - Width;
                    }
                }
            }
        }

        private bool IsOnGround(List<Entity> entities)
        {
            return entities.Any(e => e != this && IsCollidingBottom(e));
        }

        public bool IsCollidingInDirection(Entity entity, int directionX, int directionY)
        {
            if (entity == null)
            {
                // Если entity равен null, возвращаем false, так как столкновение невозможно
                return false;
            }

            int projectedX = X + directionX;
            int projectedY = Y + directionY;

            return projectedX < entity.X + entity.Width &&
                   projectedX + Width > entity.X &&
                   projectedY < entity.Y + entity.Height &&
                   projectedY + Height > entity.Y;
        }

    }
}
