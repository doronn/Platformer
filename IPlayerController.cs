namespace Scripts.Player.Platformer
{
    public interface IPlayerController
    {
        public int Id { get; }
        void ConnectController();
        void SetHorizontalInput(float horizontalInput);
        void RequestJump();
    }
}