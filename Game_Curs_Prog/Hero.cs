namespace Game_Curs_Prog
{
    public class Hero : Entity
    {
        public int JumpHeight { get; set; }
        public bool IsJumping { get; set; }
        public bool CanJump { get; set; } = false;
        private int jumpStartY;
        private const int MaxJumpHeight = 5;
        private const int GroundTimeThreshold = 50;
        private DateTime? firstGroundTime;
        private double jumpVelocity;
        private const double initialJumpVelocity = 0.3;
        private const double gravity = 0.1;
        private double velocityX;
        private const double friction = 0.9;
        private const double acceleration = 1.0; // Увеличено значение для быстрого разгона
        private const double maxSpeed = 3.0; // Максимальная скорость

        public Hero(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol)
        {
            JumpHeight = MaxJumpHeight;
            IsJumping = false;
            jumpVelocity = initialJumpVelocity;
            velocityX = 0;
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
            }
        }

        private void UpdateMovement(List<Entity> entities)
        {
            if (Controls.IsKeyPressed(ConsoleKey.A))
            {
                velocityX -= acceleration * Program.game_speed; // Ускорение влево
            }
            if (Controls.IsKeyPressed(ConsoleKey.D))
            {
                velocityX += acceleration * Program.game_speed; // Ускорение вправо
            }

            if (velocityX > maxSpeed) velocityX = maxSpeed;
            if (velocityX < -maxSpeed) velocityX = -maxSpeed;

            velocityX *= friction; // Применение трения для уменьшения инерции

            int newX = X + (int)velocityX;
            if (!entities.Any(e => e != this && IsCollidingInDirection(e, (int)velocityX, 0)))
            {
                X = newX;
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
    }
}
