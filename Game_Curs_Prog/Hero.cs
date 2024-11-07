namespace Game_Curs_Prog
{
   
    public class Hero : Entity
    {
        public int JumpHeight { get; set; }
        public bool IsJumping { get; set; }
        public bool CanJump { get; set; } = false;
        private int jumpStartY;
        private const int MaxJumpHeight = 5;
        private const int GroundTimeThreshold = 50; // Время в миллисекундах, которое нужно провести на земле
        private DateTime? firstGroundTime; // Время первого сигнала приземления

        public Hero(int x, int y, int width, int height, char symbol) : base(x, y, width, height, symbol)
        {
            JumpHeight = MaxJumpHeight; // Начальная высота прыжка
            IsJumping = false; // Состояние прыжка
        }

        public void Jump(List<Entity> entities)
        {
            if (!IsJumping && CanJump)
            {
                jumpStartY = Y; // Запоминаем начальную позицию прыжка
                IsJumping = true; // Начало прыжка
                CanJump = false; // Отключение возможности прыжка

                // Рассчитываем высоту прыжка с учетом объектов над головой
                JumpHeight = MaxJumpHeight;
                for (int i = 1; i <= MaxJumpHeight; i++)
                {
                    if (CheckCollisionAbove(i, entities))
                    {
                        JumpHeight = i - 1;
                        break;
                    }
                }
            }
        }

        private bool CheckCollisionAbove(int distance, List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity != this && IsCollidingInDirection(entity, 0, -distance))
                {
                    return true;
                }
            }
            return false;
        }

        public void Update(List<Entity> entities)
        {
            UpdateJump(entities);
            CheckCollisions(entities);
            UpdateGroundTime(entities);
        }

        private void UpdateJump(List<Entity> entities)
        {
            if (IsJumping)
            {
                // Игнорируем коллизии при движении вверх во время прыжка
                if (Y > jumpStartY - JumpHeight)
                {
                    Y -= 1; // Поднимаем героя на высоту прыжка
                }
                else
                {
                    IsJumping = false; // Завершение прыжка
                }

                // Ограничение по верхней границе
                if (Y < 1)
                {
                    Y = 1;
                    IsJumping = false;
                }

                // Обновление позиции по горизонтали во время прыжка
                if (Controls.IsKeyPressed(ConsoleKey.A))
                {
                    if (!entities.Any(e => IsCollidingInDirection(e, -1, 0)))
                    {
                        X -= 1;
                    }
                }
                if (Controls.IsKeyPressed(ConsoleKey.D))
                {
                    if (!entities.Any(e => IsCollidingInDirection(e, 1, 0)))
                    {
                        X += 1;
                    }
                }
            }
        }

        public void Land()
        {
            IsJumping = false; // Завершение прыжка
            if (firstGroundTime == null)
            {
                firstGroundTime = DateTime.Now; // Устанавливаем время первого сигнала приземления
            }
        }

        private void UpdateGroundTime(List<Entity> entities)
        {
            if (IsOnGround(entities))
            {
                if (firstGroundTime != null && (DateTime.Now - firstGroundTime.Value).TotalMilliseconds >= GroundTimeThreshold)
                {
                    CanJump = true; // Разрешаем прыжок после того, как герой пробыл на земле 500 миллисекунд
                    firstGroundTime = null; // Сбрасываем время первого сигнала приземления
                }
            }
            else
            {
                firstGroundTime = null; // Сбрасываем время первого сигнала, если герой не на земле
                CanJump = false; // Запрещаем прыжок, если герой не на земле
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
                // Проверка коллизий слева и справа
                if (entity != this && (IsCollidingLeft(entity) || IsCollidingRight(entity)))
                {
                    // Предотвращаем прохождение через объект
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
























