using System;
using UnityEngine;

namespace PlayerController
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerProperties playerProperties; // The scriptable object with the gameplay properties
        public float _movementInterpolationSpeed;

        private Transform _playerTransform;      // Reference to the player's transform component
        private bool _isGrounded = false;        // Flag to track whether the player is grounded
        private int _jumpsRemaining = 0;         // The number of jumps remaining
        private Vector3 _velocity = Vector3.zero; // The current velocity of the 
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _nextWantedPosition = Vector3.zero;

        private bool _didRequestJump = false;

        private void Start()
        {
            _playerTransform = GetComponent<Transform>();
            _nextWantedPosition = _playerTransform.localPosition;
        }

        private void FixedUpdate()
        {
            // Get input for horizontal movement
            float horizontalInput = Input.GetAxis("Horizontal");
            Vector3 moveDirection = new Vector3(horizontalInput, 0f, 0f);

            if (_isGrounded || Math.Abs(horizontalInput) > 0.5f || _velocity.y < 0)
            {
                _velocity.x = horizontalInput * playerProperties.moveSpeed;
            }
            // _nextWantedPosition += moveDirection * (playerProperties.moveSpeed * Time.deltaTime);

            // Get input for jumping
            if (_didRequestJump && (_isGrounded || _jumpsRemaining > 0))
            {
                _velocity.y = playerProperties.jumpForce;
                _jumpsRemaining--;
            }

            _didRequestJump = false;
            
            if (!_isGrounded)
            {
                // Apply gravity to the player
                _velocity += Vector3.down * playerProperties.gravity;
            }
            
            _currentVelocity = _velocity * Time.deltaTime;
            _nextWantedPosition += _currentVelocity;
            PlayerCollisionCheck(Vector3.up, playerProperties.solidGroundLayer);
            PlayerCollisionCheck(Vector3.left, playerProperties.solidGroundLayer);
            PlayerCollisionCheck(Vector3.right, playerProperties.solidGroundLayer);
            PlayerCollisionCheck(Vector3.down, playerProperties.groundLayer);
        }

        private void Update()
        {
            _didRequestJump |= Input.GetButtonDown("Jump");
            var currentTransformPosition = _playerTransform.localPosition;
            currentTransformPosition = Vector3.Lerp(currentTransformPosition, _nextWantedPosition, _movementInterpolationSpeed * Time.deltaTime);
            _playerTransform.localPosition = currentTransformPosition;
        }

        private void PlayerCollisionCheck(Vector3 direction, int collisionLayerMask)
        {
            // Check if the player is grounded by casting a ray down from the player's position
            var velocityInDirection = Vector3.Dot(direction, _velocity);
            var velocityFactorToAdd = velocityInDirection < 0 ? velocityInDirection * Time.deltaTime : 0;

            var groundCheckDistance = Math.Abs(Vector3.Dot(direction, playerProperties.CharacterSize) * 0.5f);
            var rayDistance = groundCheckDistance + velocityFactorToAdd;
            var rayEndPosition = _nextWantedPosition + direction * rayDistance;
            if (Physics.Raycast(_nextWantedPosition, direction, out var hit, rayDistance, collisionLayerMask))
            {
                // Set the player as grounded if it is falling onto the platform from above
                Debug.DrawLine(_nextWantedPosition, hit.point, Color.blue);
                Debug.DrawLine(hit.point, rayEndPosition, Color.red);
                var hitHorizontalForHorizontalDirectionCheck = hit.normal.x > 0 != direction.x > 0;
                var hitVerticalForVerticalDirectionCheck = hit.normal.y > 0 != direction.y > 0;
                if (hitHorizontalForHorizontalDirectionCheck || hitVerticalForVerticalDirectionCheck)
                {
                    _isGrounded = direction.y < 0;
                    if (_isGrounded)
                    {
                        _jumpsRemaining = playerProperties.maxJumps;
                    }

                    // Set the player's Y position to the Y position of the ground collider
                    var position = _nextWantedPosition;
                    var nextXPosition = hitHorizontalForHorizontalDirectionCheck ? hit.collider.ClosestPointOnBounds(hit.point).x + -direction.x * groundCheckDistance : position.x;
                    var nextYPosition = hitVerticalForVerticalDirectionCheck ? hit.collider.ClosestPointOnBounds(hit.point).y + -direction.y * groundCheckDistance : position.y;
                    position =
                        new Vector3(nextXPosition, nextYPosition, position.z);
                    _nextWantedPosition = position;

                    if (hitVerticalForVerticalDirectionCheck)
                    {
                        _velocity.y = 0f;
                    }
                }
                else
                {
                    _isGrounded = false;
                }
            }
            else
            {
                Debug.DrawLine(_nextWantedPosition, rayEndPosition, Color.blue);
                _isGrounded = false;
            }
        }
    }
}