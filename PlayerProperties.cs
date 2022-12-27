﻿using UnityEngine;

namespace Scripts.PlayerController.Platformer
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
    }
}