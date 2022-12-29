using UnityEngine;

namespace Scripts.Player.Platformer
{
    public interface IReadPlayerValues
    {
        Vector3 CurrentPlayerLocalPosition { get; }
    }
}