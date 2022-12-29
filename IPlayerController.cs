namespace Scripts.Player.Platformer
{
    public interface IPlayerController
    {
        void SetHorizontalInput(float horizontalInput);

        void RequestJump();
    }
}