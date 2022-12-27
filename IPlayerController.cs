namespace Scripts.PlayerController.Platformer
{
    public interface IPlayerController
    {
        void SetHorizontalInput(float horizontalInput);

        void RequestJump();
    }
}