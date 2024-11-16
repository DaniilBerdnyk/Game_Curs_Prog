namespace Game_Curs_Prog
{
    public class Hero : Entity
    {
        public int JumpHeight { get; set; }
        public bool IsJumping { get; set; }
        public bool CanJump { get; set; } = false;
        private int jumpStartY;
        private const int MaxJumpHeight = 7;
        private const int GroundTimeThreshold = 30;
        private DateTime? firstGroundTime;
        public double jumpVelocity; // Публичное поле
        private const double initialJumpVelocity = 0.2;
        private const double gravity = 0.01;
        private const double acceleration = 0.1; // Ускорение
        public const double speed = 0.15; // Время шага
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
            jumpVelocity = initialJumpVelocity;
            velocityX = 0;
            slideSpeed = 0;
            lastMoveTime = DateTime.Now;
            lastJumpMoveTime = DateTime.Now;
            lastMoveDirection = "right";
        }

        public void Jump(List<Entity> entities)
        {
            if (!IsJumping && CanJump)
            {
                jumpStartY = Y;
                IsJumping = true;
                CanJump = false;
                jumpVelocity = initialJumpVelocity * Program.game_speed;
            }
        }

        public void Update(List<Entity> entities)
        {
            UpdateJump(entities);
            UpdateMovement(entities);
            CheckCollisions(entities);
            UpdateGroundTime(entities);
        }

        private void UpdateJump(List<Entity> entities)
        {
            if (IsJumping)
            {
                double newY = Y - jumpVelocity;
                jumpVelocity -= gravity;

                bool isColliding = entities.Any(e => e != this && IsCollidingInDirection(e, 0, (int)newY - Y));
                if (isColliding || Y <= jumpStartY - MaxJumpHeight)
                {
                    IsJumping = false;
                    jumpVelocity = 0;
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

        private void UpdateMovement(List<Entity> entities)
        {
            DateTime currentTime = DateTime.Now;
            double timeSinceLastMove = (currentTime - lastMoveTime).TotalMilliseconds;

            if (timeSinceLastMove >= speed * 1000) // Преобразуем скорость в миллисекунды
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
                }
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

        private bool IsCollidingInDirection(Entity entity, int directionX, int directionY)
        {
            int projectedX = X + directionX;
            int projectedY = Y + directionY;

            return projectedX < entity.X + entity.Width &&
                   projectedX + Width > entity.X &&
                   projectedY < entity.Y + entity.Height &&
                   projectedY + Height > entity.Y;
        }
    }
}



