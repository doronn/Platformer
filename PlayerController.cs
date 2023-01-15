using System;
using UnityEngine;

namespace Scripts.Player.Platformer
{
    public class PlayerController : MonoBehaviour, IPlayerController, IReadPlayerValues
    {
        public int Id { get; private set; }

        private PlayerProperties _playerProperties; // The scriptable object with the gameplay properties

        private bool _enabled = false;
        private bool _isGrounded = false;         // Flag to track whether the player is grounded
        private int _jumpsRemaining = 0;          // The number of jumps remaining
        private float _jumpForcePercentage = 1f;
        private float _movementSpeedPercentage = 1f;
        private Vector3 _velocity = Vector3.zero; // The current velocity of the 
        private Vector3 _currentVelocity = Vector3.zero;

        private Vector3 _nextWantedPosition = Vector3.zero;

        private bool _didRequestJump = false;
        private int _remainingJumpBuffer = 0;
        private int _remainingGroundBuffer = 0;
        private float _horizontalInput;

        private int _fixedUpdatesToCatchup = 0;
        private float _lastFrameReminder = 0f;
        private Vector3 _constantVelocity;
        private Vector3 _currentConstantVelocity = Vector3.zero;

        private float _consecutiveMovementDistanceWhileGrounded = 0f;

        private Ray _ray;

        private static readonly (Vector2 direction, LayerMask layerMask)[] CollisionDirections = {
            (Vector2.up, 0),
            (Vector2.left, 0),
            (Vector2.right, 0),
            (Vector2.down, 0)
        };

        private float _boostForMovementDistance;


        public void Inject(PlayerProperties playerProperties, int id, Vector3 startPosition)
        {
            _playerProperties = playerProperties;
            Id = id;

            CollisionDirections[0].layerMask = _playerProperties.solidGroundLayer;
            CollisionDirections[1].layerMask = _playerProperties.solidGroundLayer;
            CollisionDirections[2].layerMask = _playerProperties.solidGroundLayer;
            CollisionDirections[3].layerMask = _playerProperties.groundLayer;

            _nextWantedPosition = startPosition;
        }

        private void Update()
        {
            if (!_enabled)
            {
                return;
            }

            CalculateNextUpdatesCatchup();

            if (_fixedUpdatesToCatchup == 0)
            {
                return;
            }
            
            HorizontalMovementVelocityUpdate();

            CalculateBoostDueToConsecutiveMovement();
            
            HandleJumpRequested();

            ApplyGravity();
            
            CalculateCurrentVelocity();

            CheckCollisions();
            
            UpdateNextPosition();
            // AccountForGroundMovement();
            _currentConstantVelocity = Vector3.zero;
        }

        private void CalculateBoostDueToConsecutiveMovement()
        {
            _boostForMovementDistance = 1 + _playerProperties.JumpBoostForGroundedMovementDistance.Evaluate(_consecutiveMovementDistanceWhileGrounded);
        }

        private void CalculateNextUpdatesCatchup()
        {
            var elapsedFixedUpdates = (Time.deltaTime / Time.fixedDeltaTime) + _lastFrameReminder;
            _fixedUpdatesToCatchup = (int)Math.Floor(elapsedFixedUpdates);
            _lastFrameReminder = elapsedFixedUpdates - _fixedUpdatesToCatchup;
            _currentConstantVelocity += Time.deltaTime * _constantVelocity;
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
            _ray.origin = CurrentPlayerLocalPosition;
            for (var i = 0; i < CollisionDirections.Length; i++)
            {
                var collisionDirection = CollisionDirections[i];
                _ray.direction = collisionDirection.direction;
                PlayerCollisionCheck(collisionDirection.layerMask);
            }

            if (!_isGrounded && _remainingGroundBuffer > 0)
            {
                _isGrounded = true;
                _remainingGroundBuffer -= _fixedUpdatesToCatchup;
            }
        }

        private void UpdateNextPosition()
        {
            _nextWantedPosition += _currentVelocity;

            if (_isGrounded)
            {
                _consecutiveMovementDistanceWhileGrounded += Math.Abs(_currentVelocity.x);
            }
        }

        private void CalculateCurrentVelocity()
        {
            _currentVelocity = _velocity * Time.fixedDeltaTime + _currentConstantVelocity;
        }

        private void ApplyGravity()
        {
            if (!_isGrounded)
            {
                // Apply gravity to the player
                _velocity += Vector3.down * (_playerProperties.gravity * _fixedUpdatesToCatchup *
                                             (_jumpForcePercentage / 2f));

                _consecutiveMovementDistanceWhileGrounded -=
                    Time.fixedDeltaTime * _fixedUpdatesToCatchup * _velocity.y < 0
                        ? _playerProperties.ConsecutiveMovementDampingWhileFalling
                        : _playerProperties.ConsecutiveMovementDampingWhileJumping;
                
                if (_consecutiveMovementDistanceWhileGrounded < 1)
                {
                    _consecutiveMovementDistanceWhileGrounded = 0;
                }
            }
        }

        private void HorizontalMovementVelocityUpdate()
        {
            if (_isGrounded || Math.Abs(_horizontalInput) > 0.5f || _velocity.y < 0)
            {
                _velocity.x = _horizontalInput * _playerProperties.moveSpeed * _fixedUpdatesToCatchup * _boostForMovementDistance;
            }
        }

        private void HandleJumpRequested()
        {
            var didJump = false;
            if (_isGrounded)
            {
                _jumpsRemaining = _playerProperties.maxJumps;
            }
            else if (_jumpsRemaining >= _playerProperties.maxJumps)
            {
                _jumpsRemaining = _playerProperties.maxJumps - 1;
            }
            
            if (_didRequestJump && (_isGrounded || _jumpsRemaining > 0))
            {
                didJump = true;
                _velocity.y = _playerProperties.jumpForce * _jumpForcePercentage * _boostForMovementDistance;
                _jumpsRemaining--;
                _remainingGroundBuffer = 0;
            }

            if (!didJump && _remainingJumpBuffer > 0)
            {
                _remainingJumpBuffer -= _fixedUpdatesToCatchup;
            }
            else
            {
                _didRequestJump = false;
                _remainingJumpBuffer = 0;
            }
        }

        private void PlayerCollisionCheck(int collisionLayerMask)
        {
            var checkDirection = _ray.direction;
            // Check if the player is grounded by casting a ray down from the player's position
            var velocityInDirection = Vector3.Dot(checkDirection, _currentVelocity);
            var velocityFactorToAdd = Math.Abs(velocityInDirection);

            var groundCheckDistance = Math.Abs(Vector3.Dot(checkDirection, _playerProperties.CharacterSize) * 0.5f);
            var rayDistance = groundCheckDistance + velocityFactorToAdd;
            var rayEndPosition = CurrentPlayerLocalPosition + checkDirection * rayDistance;
            var directionCheckIsDown = checkDirection.y < 0;
            if (Physics.Raycast(CurrentPlayerLocalPosition, checkDirection, out var hit, rayDistance, collisionLayerMask))
            {
                // Set the player as grounded if it is falling onto the platform from above
                Debug.DrawLine(CurrentPlayerLocalPosition, hit.point, Color.blue);
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

                    if (!wasAlreadyGrounded && _isGrounded)
                    {
                        _remainingGroundBuffer = _playerProperties.GroundBuffer;
                    }

                    // Set the player's X, Y positions to the X, Y positions of the collision point
                    var collisionForVelocityInCheckDirection = checkDirection.x < 0 == _velocity.x < 0;
                    var position = CurrentPlayerLocalPosition;
                    var nextXPosition = hitHorizontalForHorizontalDirectionCheck && collisionForVelocityInCheckDirection
                        ? hit.collider.ClosestPointOnBounds(hit.point).x + -checkDirection.x * groundCheckDistance
                        : position.x;

                    if (hitHorizontalForHorizontalDirectionCheck && collisionForVelocityInCheckDirection)
                    {
                        _velocity.x = 0f;
                        _currentVelocity.x = 0;
                    }

                    var shouldBlockOnVerticalCollision = hitVerticalForVerticalDirectionCheck && (!directionCheckIsDown || _isGrounded);
                    var nextYPosition = shouldBlockOnVerticalCollision
                        ? hit.collider.ClosestPointOnBounds(hit.point).y + -checkDirection.y * groundCheckDistance
                        : position.y;
                    position =
                        new Vector3(nextXPosition - (position.x - _nextWantedPosition.x), nextYPosition - (position.y - _nextWantedPosition.y), position.z);
                    _nextWantedPosition = position;

                    if (shouldBlockOnVerticalCollision)
                    {
                        _velocity.y = 0f;
                        _currentVelocity.y = _currentConstantVelocity.y;
                    }
                }
                else if (directionCheckIsDown)
                {
                    _isGrounded = false;
                }
            }
            else
            {
                Debug.DrawLine(CurrentPlayerLocalPosition, rayEndPosition, Color.blue);
                if (directionCheckIsDown)
                {
                    _isGrounded = false;
                }
            }
        }
        
        public void ConnectController()
        {
            _enabled = true;
        }
        
        public void SetHorizontalInput(float horizontalInput)
        {
            _horizontalInput = Math.Clamp(horizontalInput, -_movementSpeedPercentage, _movementSpeedPercentage);
        }

        public void RequestJump()
        {
            _didRequestJump = true;
            _remainingJumpBuffer = _playerProperties.JumpBuffer;
        }

        public void SetConstantVerticalSpeed(float speed)
        {
            _constantVelocity = Vector3.down * speed;
        }

        public void SetJumpForcePercentage(float jumpForcePercentage)
        {
            _jumpForcePercentage = jumpForcePercentage;
        }

        public void SetMovementSpeedPercentage(float movementSpeedPercentage)
        {
            _movementSpeedPercentage = movementSpeedPercentage;
        }

        public Vector3 CurrentPlayerLocalPosition => _nextWantedPosition;
    }
}