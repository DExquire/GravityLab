using GravityLab.Audio;
using GravityLab.Core;
using GravityLab.Puzzle;
using GravityLab.Rendering;
using UnityEngine;

namespace GravityLab.Player
{
    /// <summary>
    /// CharacterController-based first-person controller: smoothed acceleration, sprint, crouch with headroom check,
    /// coyote time + jump buffering, moving-platform carry, conveyor/updraft zones, head bob, footsteps, landing dip
    /// and camera shake.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public const float StandHeight = 1.8f;
        public const float CrouchHeight = 1.1f;
        public const float Radius = 0.35f;

        const float WalkSpeed = 4.6f;
        const float SprintSpeed = 6.8f;
        const float CrouchSpeed = 2.4f;
        const float GroundAccel = 42f;
        const float AirAccel = 11f;
        const float GravityAccel = 20f;
        const float JumpSpeed = 6.6f;
        const float CoyoteTime = 0.12f;
        const float JumpBuffer = 0.12f;
        const float EyeBelowTop = 0.15f;

        public static PlayerController Active { get; private set; }

        CharacterController _controller;
        Transform _head;
        Camera _camera;
        float _yaw, _pitch;
        Vector3 _planarVelocity;
        float _verticalVelocity;
        bool _grounded;
        float _lastGroundedTime = -10f;
        float _jumpPressedTime = -10f;
        float _height = StandHeight;
        bool _crouching;
        float _bobPhase, _bobWeight, _landDip, _landDipVelocity;
        int _lastStepIndex;
        Transform _platform;
        Vector3 _platformLocalPoint, _platformWorldPoint;
        Vector3 _zoneVelocity;
        float _liftAccel, _liftMaxSpeed;

        public Camera Camera { get { return _camera; } }
        public Transform Head { get { return _head; } }
        public bool InputEnabled { get; set; }
        public bool CarryingHeavy { get; set; }
        public Vector3 Velocity { get { return _controller.velocity; } }
        public Vector3 Center { get { return transform.position + Vector3.up * (_height * 0.5f); } }
        public float Yaw { get { return _yaw; } }

        public void Init(Camera camera)
        {
            _controller = GetComponent<CharacterController>();
            _controller.radius = Radius;
            _controller.height = StandHeight;
            _controller.center = Vector3.up * (StandHeight * 0.5f);
            _controller.stepOffset = 0.35f;
            _controller.slopeLimit = 50f;
            _controller.skinWidth = 0.04f;
            _controller.minMoveDistance = 0f;
            gameObject.layer = Layers.Player;

            _head = new GameObject("Head").transform;
            _head.SetParent(transform, false);
            _head.localPosition = Vector3.up * (StandHeight - EyeBelowTop);
            _camera = camera;
            _camera.transform.SetParent(_head, false);
            _camera.transform.localPosition = Vector3.zero;
            _camera.transform.localRotation = Quaternion.identity;
        }

        void OnEnable() { Active = this; }

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        /// <summary>Places the player (feet) at <paramref name="position"/> facing <paramref name="yaw"/> and clears all motion.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
            _yaw = yaw;
            _pitch = 0f;
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _platform = null;
            _crouching = false;
            _height = StandHeight;
            _landDip = 0f;
            ApplyHeight();
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _head.localRotation = Quaternion.identity;
        }

        /// <summary>Conveyor-style carry velocity for this frame.</summary>
        public void AddZoneVelocity(Vector3 velocity) { _zoneVelocity += velocity; }

        /// <summary>Updraft acceleration for this frame (ignored while carrying a heavy cube).</summary>
        public void AddLift(float acceleration, float maxSpeed)
        {
            _liftAccel += acceleration;
            _liftMaxSpeed = Mathf.Max(_liftMaxSpeed, maxSpeed);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            UpdateLook();
            FollowPlatform();
            UpdateCrouch(dt);
            UpdateMovement(dt);
            UpdateCamera(dt);

            _zoneVelocity = Vector3.zero;
            _liftAccel = 0f;
            _liftMaxSpeed = 0f;
        }

        void UpdateLook()
        {
            if (InputEnabled)
            {
                SaveData s = SaveSystem.Data;
                Vector2 d = GameInput.LookDelta * s.MouseSensitivity;
                _yaw += d.x;
                _pitch += s.InvertY ? d.y : -d.y;
                _pitch = Mathf.Clamp(_pitch, -88f, 88f);
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void FollowPlatform()
        {
            if (_platform == null) return;
            Vector3 now = _platform.TransformPoint(_platformLocalPoint);
            Vector3 delta = now - _platformWorldPoint;
            if (delta.sqrMagnitude < 1f) _controller.Move(delta);
            _platformWorldPoint = now;
        }

        void UpdateCrouch(float dt)
        {
            bool want = InputEnabled && GameInput.CrouchHeld;
            if (want) _crouching = true;
            else if (_crouching && HasHeadroom()) _crouching = false;

            float target = _crouching ? CrouchHeight : StandHeight;
            if (!Mathf.Approximately(_height, target))
            {
                _height = Mathf.MoveTowards(_height, target, dt * 7f);
                ApplyHeight();
            }
        }

        bool HasHeadroom()
        {
            Vector3 bottom = transform.position + Vector3.up * Radius;
            float distance = StandHeight - Radius * 2f + 0.02f;
            return !Physics.SphereCast(bottom, Radius * 0.92f, Vector3.up, out RaycastHit _, distance, Layers.SolidMask, QueryTriggerInteraction.Ignore);
        }

        void ApplyHeight()
        {
            _controller.height = _height;
            _controller.center = Vector3.up * (_height * 0.5f);
        }

        void UpdateMovement(float dt)
        {
            Vector2 input = InputEnabled ? GameInput.Move : Vector2.zero;
            float speed = _crouching ? CrouchSpeed : (InputEnabled && GameInput.SprintHeld ? SprintSpeed : WalkSpeed);
            Vector3 wish = (transform.right * input.x + transform.forward * input.y) * speed;
            float accel = _grounded ? GroundAccel : AirAccel;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, wish, accel * dt);

            if (InputEnabled && GameInput.JumpDown) _jumpPressedTime = Time.time;
            bool canJump = Time.time - _lastGroundedTime <= CoyoteTime;
            if (Time.time - _jumpPressedTime <= JumpBuffer && canJump)
            {
                _verticalVelocity = JumpSpeed;
                _jumpPressedTime = -10f;
                _lastGroundedTime = -10f;
                _grounded = false;
                _platform = null;
                AudioManager.Play(SoundId.Jump, 0.5f);
            }

            if (_grounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            else _verticalVelocity -= GravityAccel * dt;

            if (_liftAccel > 0f && !CarryingHeavy)
            {
                _verticalVelocity = Mathf.Min(_verticalVelocity + _liftAccel * dt, _liftMaxSpeed);
            }

            float fallSpeed = -_verticalVelocity;
            Vector3 motion = (_planarVelocity + _zoneVelocity + Vector3.up * _verticalVelocity) * dt;
            CollisionFlags flags = _controller.Move(motion);

            bool wasGrounded = _grounded;
            _grounded = (flags & CollisionFlags.Below) != 0 || _controller.isGrounded;
            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f) _verticalVelocity = 0f;

            if (_grounded)
            {
                _lastGroundedTime = Time.time;
                if (!wasGrounded && fallSpeed > 3.5f) Land(fallSpeed);
            }
            DetectPlatform();
        }

        void Land(float fallSpeed)
        {
            float k = Mathf.Clamp01((fallSpeed - 3.5f) / 10f);
            _landDipVelocity = -(1.2f + k * 2.5f);
            AudioManager.Play(SoundId.Land, 0.4f + k * 0.6f, 1f - k * 0.2f);
            if (k > 0.3f) CameraShake.Add(k * 0.35f);
        }

        void DetectPlatform()
        {
            _platform = null;
            if (!_grounded) return;
            Vector3 origin = transform.position + Vector3.up * 0.3f;
            RaycastHit hit;
            if (!Physics.SphereCast(origin, Radius * 0.8f, Vector3.down, out hit, 0.6f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return;
            MovingPlatform platform = hit.collider.GetComponentInParent<MovingPlatform>();
            if (platform == null) return;
            _platform = platform.transform;
            _platformWorldPoint = transform.position;
            _platformLocalPoint = _platform.InverseTransformPoint(_platformWorldPoint);
        }

        void UpdateCamera(float dt)
        {
            Vector3 v = _controller.velocity;
            float planarSpeed = new Vector2(v.x, v.z).magnitude;
            bool moving = _grounded && planarSpeed > 0.6f && InputEnabled;
            _bobWeight = Mathf.MoveTowards(_bobWeight, moving ? Mathf.Clamp01(planarSpeed / SprintSpeed) + 0.25f : 0f, dt * 4f);
            if (moving)
            {
                _bobPhase += dt * planarSpeed * 1.7f;
                int stepIndex = Mathf.FloorToInt(_bobPhase / Mathf.PI);
                if (stepIndex != _lastStepIndex)
                {
                    _lastStepIndex = stepIndex;
                    AudioManager.Play(SoundId.Footstep, _crouching ? 0.18f : 0.32f + planarSpeed * 0.03f, 1f, 0.12f);
                }
            }

            // Critically-damped spring for the landing dip.
            _landDipVelocity += (-_landDip * 90f - _landDipVelocity * 16f) * dt;
            _landDip += _landDipVelocity * dt;

            float bobAmount = SaveSystem.Data.HeadBob ? _bobWeight : 0f;
            Vector3 bob = new Vector3(Mathf.Cos(_bobPhase) * 0.035f, Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.05f - 0.025f, 0f) * bobAmount;

            float roll;
            Vector3 shake = CameraShake.Sample(dt, out roll);
            _head.localPosition = Vector3.up * (_height - EyeBelowTop + _landDip);
            _camera.transform.localPosition = bob + shake;
            _camera.transform.localRotation = Quaternion.Euler(0f, 0f, roll + bob.x * 20f * bobAmount);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, SaveSystem.Data.FieldOfView + (planarSpeed > WalkSpeed + 0.5f ? 4f : 0f), dt * 6f);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            Vector3 push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            float strength = 2.2f * Mathf.Clamp(20f / rb.mass, 0.2f, 1f);
            Vector3 target = push * strength;
            Vector3 v = rb.velocity;
            rb.velocity = new Vector3(Mathf.Lerp(v.x, target.x, 0.2f), v.y, Mathf.Lerp(v.z, target.z, 0.2f));
        }
    }
}
