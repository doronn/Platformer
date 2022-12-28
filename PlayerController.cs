using System;
using UnityEngine;

namespace Scripts.PlayerController.Platformer
{
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        public PlayerProperties playerProperties; // The scriptable object with the gameplay properties
        public float _movementInterpolationSpeed;

        [SerializeField]
        private Transform _playerTransform;      // Reference to the player's transform component
        private Transform _originalParent;
        private bool _isGrounded = false;        // Flag to track whether the player is grounded
        private int _jumpsRemaining = 0;         // The number of jumps remaining
        private Vector3 _velocity = Vector3.zero; // The current velocity of the 
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _nextWantedPosition = Vector3.zero;
        // private Vector3 _myGroundInitialPosition = Vector3.zero;
        // private Vector3 _myGroundPositionOffset = Vector3.zero;

        private bool _didRequestJump = false;
        private float _horizontalInput;

        private void Start()
        {
            _nextWantedPosition = _playerTransform.localPosition;
            _originalParent = _playerTransform.parent;
        }

        private void FixedUpdate()
        {
            HorizontalMovementVelocityUpdate();

            HandleJumpRequested();

            ApplyGravity();
            
            UpdateVelocityAndNextPosition();

            CheckCollisions();

            // AccountForGroundMovement();
        }

        /*private void AccountForGroundMovement()
        {
            if (_isGrounded)
            {
                _nextWantedPosition += _myGroundPositionOffset;
            }
        }*/

        private void CheckCollisions()
        {
            PlayerCollisionCheck(Vector3.up, playerProperties.solidGroundLayer);
            PlayerCollisionCheck(Vector3.left, playerProperties.solidGroundLayer);
            PlayerCollisionCheck(Vector3.right, playerProperties.solidGroundLayer);
            PlayerCollisionCheck(Vector3.down, playerProperties.groundLayer);
        }

        private void UpdateVelocityAndNextPosition()
        {
            _currentVelocity = _velocity * Time.deltaTime;
            _nextWantedPosition += _currentVelocity;
            // _myGroundPositionOffset = Vector3.zero;
        }

        private void ApplyGravity()
        {
            if (!_isGrounded)
            {
                // Apply gravity to the player
                _velocity += Vector3.down * playerProperties.gravity;
            }
        }

        private void HorizontalMovementVelocityUpdate()
        {
            if (_isGrounded || Math.Abs(_horizontalInput) > 0.5f || _velocity.y < 0)
            {
                _velocity.x = _horizontalInput * playerProperties.moveSpeed;
            }
        }

        private void HandleJumpRequested()
        {
            if (_didRequestJump && (_isGrounded || _jumpsRemaining > 0))
            {
                _velocity.y = playerProperties.jumpForce;
                _jumpsRemaining--;
            }

            _didRequestJump = false;
        }

        private void Update()
        {
            var currentTransformPosition = _playerTransform.localPosition;
            currentTransformPosition = Vector3.Lerp(currentTransformPosition, _nextWantedPosition, _movementInterpolationSpeed * Time.deltaTime);
            _playerTransform.localPosition = currentTransformPosition;
        }

        private void PlayerCollisionCheck(Vector3 checkDirection, int collisionLayerMask)
        {
            // Check if the player is grounded by casting a ray down from the player's position
            var velocityInDirection = Vector3.Dot(checkDirection, _velocity);
            var velocityFactorToAdd = velocityInDirection < 0 ? velocityInDirection * Time.deltaTime : 0;

            var groundCheckDistance = Math.Abs(Vector3.Dot(checkDirection, playerProperties.CharacterSize) * 0.5f);
            var rayDistance = groundCheckDistance + velocityFactorToAdd;
            var rayEndPosition = _nextWantedPosition + checkDirection * rayDistance;
            var directionCheckIsDown = checkDirection.y < 0;
            if (Physics.Raycast(_nextWantedPosition, checkDirection, out var hit, rayDistance, collisionLayerMask))
            {
                // Set the player as grounded if it is falling onto the platform from above
                Debug.DrawLine(_nextWantedPosition, hit.point, Color.blue);
                Debug.DrawLine(hit.point, rayEndPosition, Color.red);
                var hitHorizontalForHorizontalDirectionCheck = hit.normal.x < 0 != checkDirection.x < 0;
                var hitVerticalForVerticalDirectionCheck = hit.normal.y < 0 != directionCheckIsDown;
                if (hitHorizontalForHorizontalDirectionCheck || hitVerticalForVerticalDirectionCheck)
                {
                    var wasAlreadyGrounded = _isGrounded;
                    if (directionCheckIsDown)
                    {
                        _isGrounded = _velocity.y <= 0;
                    }

                    if (_isGrounded)
                    {
                        _jumpsRemaining = playerProperties.maxJumps;
                        /*if (!wasAlreadyGrounded)
                        {
                            _myGroundInitialPosition = hit.transform.position;
                        }
                        else
                        {
                            var curentGroundPosition = hit.transform.position;
                            _myGroundPositionOffset = curentGroundPosition - _myGroundInitialPosition;
                            _myGroundInitialPosition = curentGroundPosition;
                        }*/
                    }

                    // Set the player's X, Y positions to the X, Y positions of the collision point
                    var position = _nextWantedPosition;
                    var nextXPosition = hitHorizontalForHorizontalDirectionCheck
                        ? hit.collider.ClosestPointOnBounds(hit.point).x + -checkDirection.x * groundCheckDistance
                        : position.x;
                    
                    var shouldBlockOnVerticalCollision = hitVerticalForVerticalDirectionCheck && (!directionCheckIsDown || _isGrounded);
                    var nextYPosition = shouldBlockOnVerticalCollision
                        ? hit.collider.ClosestPointOnBounds(hit.point).y + -checkDirection.y * groundCheckDistance
                        : position.y;
                    position =
                        new Vector3(nextXPosition, nextYPosition, position.z);
                    _nextWantedPosition = position;

                    if (shouldBlockOnVerticalCollision)
                    {
                        _velocity.y = 0f;
                        _currentVelocity.y = 0;
                    }
                }
                else if (directionCheckIsDown)
                {
                    _isGrounded = false;
                }
            }
            else
            {
                Debug.DrawLine(_nextWantedPosition, rayEndPosition, Color.blue);
                if (directionCheckIsDown)
                {
                    _isGrounded = false;
                }
            }
        }

        private void SetPlayerParentObject(Transform platformTransform)
        {
            if (_playerTransform.parent == platformTransform)
            {
                return;
            }

            var lastLocalPosition = _playerTransform.localPosition;
            _playerTransform.SetParent(platformTransform, true);
        }

        public void SetHorizontalInput(float horizontalInput)
        {
            _horizontalInput = horizontalInput;
        }

        public void RequestJump()
        {
            _didRequestJump = true;
        }
    }
}