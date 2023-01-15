using UnityEngine;

namespace Scripts.Player.Platformer
{
    [CreateAssetMenu(menuName = "Player Properties")]
    public class PlayerProperties : ScriptableObject
    {
        public float moveSpeed = 10f;            // The speed at which the player moves
        public float jumpForce = 10f;            // The force of the player's jump
        public float gravity = 9.81f;            // The gravitational force applied to the player
        public float groundCheckDistance = 0.5f; // The distance at which the player is considered to be grounded
        public LayerMask groundLayer;            // The layer mask for the ground colliders
        public LayerMask solidGroundLayer;       // The layer mask for the non passable ground colliders (that cannot be jumped through)
        public int maxJumps = 1;                 // The maximum number of jumps allowed
        public Vector3 CharacterSize = Vector3.one;       // The size of the character
        [Tooltip("The amount of fixed updates to let a jump press be valid afterwards")]
        public int JumpBuffer;
        [Tooltip("The amount of fixed updates to let the player be considered grounded when falling off a ledge")]
        public int GroundBuffer;

        public AnimationCurve JumpBoostForGroundedMovementDistance;
        public float ConsecutiveMovementDampingWhileJumping;
        public float ConsecutiveMovementDampingWhileFalling;
    }
}