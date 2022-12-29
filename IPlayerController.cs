namespace Scripts.Player.Platformer
{
    public interface IPlayerController
    {
        void ConnectController();
        void SetHorizontalInput(float horizontalInput);
        void RequestJump();
    }
}