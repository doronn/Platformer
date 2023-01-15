namespace Scripts.Player.Platformer
{
    public interface IPlayerController
    {
        public int Id { get; }
        void ConnectController();
        void SetHorizontalInput(float horizontalInput);
        void RequestJump();
        void SetConstantVerticalSpeed(float speed);
        void SetJumpForcePercentage(float jumpForcePercentage);
        void SetMovementSpeedPercentage(float movementSpeedPercentage);
    }
}