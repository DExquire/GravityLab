using GravityLab.Audio;
using GravityLab.Core;
using GravityLab.Player;
using GravityLab.Puzzle;
using GravityLab.Rendering;
using UnityEngine;

namespace GravityLab.Gadgets
{
    public enum AimState { None, Grabbable, Holding }

    /// <summary>
    /// The gravity gun. LMB grabs/drops (physics hold: a critically-damped spring pulls the prop to a point in front of
    /// the camera, so it still collides with the world and is dropped when it lags too far or loses line of sight),
    /// RMB launches (or punts), F flips the target's gravity, Q puts it in stasis, the wheel changes hold distance.
    /// </summary>
    public sealed class GravityGun : MonoBehaviour
    {
        public const float Range = 8.5f;
        public const float FreezeDuration = 5f;
        const float MinHoldDistance = 1.7f;
        const float MaxHoldDistance = 4f;
        const float DropDistance = 2f;
        const int BeamPoints = 14;

        PlayerController _player;
        Transform _cam;
        GunViewModel _view;
        LineRenderer _beam;
        PhysicsProp _held;
        PhysicsProp _aimed;
        PhysicsProp _frozen;
        float _holdDistance;
        float _yawOffset;
        float _tooFarTimer;
        float _blockedTimer;
        float _strain;
        bool _inputWasEnabled;

        public AimState State { get; private set; }
        public PhysicsProp Held { get { return _held; } }
        public PhysicsProp Aimed { get { return _aimed; } }
        /// <summary>Remaining stasis of the currently frozen prop, 0..1.</summary>
        public float FreezeRemaining01 { get { return _frozen != null && _frozen.IsFrozen ? _frozen.FreezeTimeLeft / FreezeDuration : 0f; } }
        /// <summary>Time of the last action, so the HUD can pulse the crosshair.</summary>
        public float LastActionTime { get; private set; }
        public Color LastActionColor { get; private set; }

        public void Init(PlayerController player, GunViewModel view)
        {
            _player = player;
            _cam = player.Camera.transform;
            _view = view;

            GameObject beamGo = new GameObject("HoldBeam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.sharedMaterial = MatLib.Beam;
            _beam.useWorldSpace = true;
            _beam.positionCount = BeamPoints;
            _beam.widthMultiplier = 0.05f;
            _beam.widthCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.6f));
            _beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _beam.receiveShadows = false;
            _beam.enabled = false;
        }

        /// <summary>Drops whatever is held and forgets frozen targets (chamber change / restart).</summary>
        public void ResetState()
        {
            if (_held != null) Release(false);
            _held = null;
            _aimed = null;
            _frozen = null;
            State = AimState.None;
            _beam.enabled = false;
            AudioManager.SetHum(false);
        }

        void Update()
        {
            if (_held != null && (_held.IsDissolving || !_held.gameObject.activeInHierarchy)) Release(false);

            // Skip the frame input comes back (e.g. the click on "Resume") so it doesn't fire the gun.
            bool inputEnabled = _player.InputEnabled;
            bool skip = !inputEnabled || !_inputWasEnabled;
            _inputWasEnabled = inputEnabled;
            if (skip)
            {
                UpdateBeam();
                return;
            }

            UpdateAim();

            if (GameInput.GrabDown)
            {
                if (_held != null) Release(true);
                else if (_aimed != null) Grab(_aimed);
                else Deny();
            }

            if (GameInput.LaunchDown)
            {
                if (_held != null) Launch();
                else if (_aimed != null) Punt(_aimed);
                else Deny();
            }

            if (GameInput.FlipDown)
            {
                PhysicsProp target = _held != null ? _held : _aimed;
                if (target != null)
                {
                    if (_held != null) Release(false);
                    target.FlipGravity();
                    Pulse(Palette.Violet);
                }
                else Deny();
            }

            if (GameInput.FreezeDown)
            {
                PhysicsProp target = _held != null ? _held : _aimed;
                if (target != null) ToggleFreeze(target);
                else Deny();
            }

            float scroll = GameInput.Scroll;
            if (_held != null && scroll != 0f) _holdDistance = Mathf.Clamp(_holdDistance + scroll * 0.35f, MinHoldDistance, MaxHoldDistance);

            State = _held != null ? AimState.Holding : (_aimed != null ? AimState.Grabbable : AimState.None);
            _player.CarryingHeavy = _held != null && _held.IsHeavy;
            UpdateBeam();
        }

        void UpdateAim()
        {
            _aimed = null;
            if (_held != null) return;
            RaycastHit hit;
            if (!Physics.Raycast(_cam.position, _cam.forward, out hit, Range, Layers.GunMask, QueryTriggerInteraction.Ignore)) return;
            PhysicsProp prop = hit.collider.GetComponent<PhysicsProp>();
            if (prop == null || prop.IsDissolving) return;
            _aimed = prop;
            prop.Highlight();
        }

        void Grab(PhysicsProp prop)
        {
            if (prop.IsFrozen)
            {
                prop.Unfreeze();
                if (_frozen == prop) _frozen = null;
            }
            _held = prop;
            prop.SetHeld(true);
            _holdDistance = Mathf.Clamp(Vector3.Distance(_cam.position, prop.transform.position), MinHoldDistance, MaxHoldDistance);
            float relative = Mathf.DeltaAngle(_player.Yaw, prop.transform.eulerAngles.y);
            _yawOffset = Mathf.Round(relative / 90f) * 90f;
            _tooFarTimer = 0f;
            _blockedTimer = 0f;
            AudioManager.Play(SoundId.Grab, 0.8f);
            Pulse(Palette.Orange);
            _view.Kick(0.25f);
        }

        void Release(bool playSound)
        {
            if (_held == null) return;
            PhysicsProp prop = _held;
            _held = null;
            prop.SetHeld(false);
            prop.Body.velocity = Vector3.ClampMagnitude(prop.Body.velocity, 6f);
            prop.Body.angularVelocity = Vector3.zero;
            _player.CarryingHeavy = false;
            if (playSound) AudioManager.Play(SoundId.Drop, 0.6f);
        }

        void Launch()
        {
            PhysicsProp prop = _held;
            Release(false);
            Vector3 v = _player.Velocity;
            prop.Body.velocity = _cam.forward * (prop.IsHeavy ? 8f : 15f) + new Vector3(v.x, 0f, v.z) * 0.5f;
            prop.Body.angularVelocity = Random.insideUnitSphere * 2f;
            AudioManager.Play(SoundId.Launch, 0.9f, prop.IsHeavy ? 0.8f : 1f);
            CameraShake.Add(0.18f);
            _view.Kick(1f);
            Pulse(Palette.Orange);
            FxManager.Burst(_view.Muzzle.position + _cam.forward * 0.2f, Palette.Orange, 10, 2.5f, 0.1f, 0.3f, 0.05f);
        }

        void Punt(PhysicsProp prop)
        {
            if (prop.IsFrozen) return;
            prop.Body.AddForce(_cam.forward * (prop.IsHeavy ? 4f : 8f), ForceMode.VelocityChange);
            AudioManager.Play(SoundId.Launch, 0.6f, 1.2f);
            _view.Kick(0.6f);
            Pulse(Palette.Orange);
        }

        void ToggleFreeze(PhysicsProp target)
        {
            if (_held == target) Release(false);
            if (target.IsFrozen)
            {
                target.Unfreeze();
                if (_frozen == target) _frozen = null;
                Pulse(Palette.Cyan);
                return;
            }
            if (_frozen != null && _frozen != target) _frozen.Unfreeze();
            target.Freeze(FreezeDuration);
            _frozen = target;
            Pulse(Palette.Cyan);
            _view.Kick(0.35f);
        }

        void Deny()
        {
            AudioManager.Play(SoundId.Deny, 0.45f);
        }

        void Pulse(Color color)
        {
            LastActionTime = Time.time;
            LastActionColor = color;
            _view.Flash(color);
        }

        void FixedUpdate()
        {
            if (_held == null) return;
            float dt = Time.fixedDeltaTime;
            Rigidbody rb = _held.Body;
            Vector3 eye = _cam.position;
            Vector3 forward = _cam.forward;
            Vector3 target = eye + forward * _holdDistance;

            // Never ask the prop to sit inside a wall: stop the hold point in front of geometry.
            RaycastHit wall;
            if (Physics.SphereCast(eye, 0.3f, forward, out wall, _holdDistance, Layers.SightBlockMask, QueryTriggerInteraction.Ignore))
            {
                target = eye + forward * Mathf.Max(0.6f, wall.distance - 0.2f);
            }

            Vector3 toTarget = target - rb.position;
            float k = _held.IsHeavy ? 90f : 170f;
            float damping = 2f * Mathf.Sqrt(k) * 0.95f;
            Vector3 relativeVelocity = rb.velocity - _player.Velocity;
            Vector3 accel = toTarget * k - relativeVelocity * damping;
            accel = Vector3.ClampMagnitude(accel, _held.IsHeavy ? 140f : 280f);
            rb.AddForce(accel, ForceMode.Acceleration);

            Quaternion targetRot = Quaternion.Euler(0f, _player.Yaw + _yawOffset, 0f);
            Quaternion delta = targetRot * Quaternion.Inverse(rb.rotation);
            float angle;
            Vector3 axis;
            delta.ToAngleAxis(out angle, out axis);
            if (angle > 180f) angle -= 360f;
            if (Mathf.Abs(angle) > 0.1f && axis.sqrMagnitude > 0.5f) rb.angularVelocity = axis * (angle * Mathf.Deg2Rad * 9f);
            else rb.angularVelocity *= 0.5f;

            float distance = toTarget.magnitude;
            _strain = Mathf.Clamp01(distance / DropDistance);
            _tooFarTimer = distance > DropDistance ? _tooFarTimer + dt : 0f;
            bool blocked = Physics.Linecast(eye, rb.position, Layers.SightBlockMask, QueryTriggerInteraction.Ignore);
            _blockedTimer = blocked ? _blockedTimer + dt : 0f;
            if (_tooFarTimer > 0.3f || _blockedTimer > 0.35f)
            {
                Release(true);
                Pulse(Palette.Blue);
            }
        }

        void UpdateBeam()
        {
            bool show = _held != null;
            _beam.enabled = show;
            AudioManager.SetHum(show, 0.9f + _strain * 0.5f);
            if (!show) return;

            Vector3 a = _view.Muzzle.position;
            Vector3 b = _held.transform.position;
            Vector3 dir = b - a;
            Vector3 side = Vector3.Cross(dir.normalized, _cam.up).normalized;
            Vector3 up = Vector3.Cross(side, dir.normalized);
            float t0 = Time.time * 14f;
            for (int i = 0; i < BeamPoints; i++)
            {
                float t = i / (float)(BeamPoints - 1);
                float envelope = Mathf.Sin(t * Mathf.PI) * (0.03f + _strain * 0.08f);
                Vector3 wobble = side * Mathf.Sin(t * 11f - t0) * envelope + up * Mathf.Cos(t * 7f - t0 * 1.3f) * envelope;
                _beam.SetPosition(i, Vector3.Lerp(a, b, t) + wobble);
            }
        }
    }
}
