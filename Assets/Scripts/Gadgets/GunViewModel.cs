using GravityLab.Core;
using GravityLab.Player;
using GravityLab.Rendering;
using UnityEngine;

namespace GravityLab.Gadgets
{
    /// <summary>First-person gun model built from primitives: mouse sway, walk bob, recoil spring, glowing core and opening prongs.</summary>
    public sealed class GunViewModel : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly Vector3 RestPosition = new Vector3(0.27f, -0.24f, 0.46f);

        Renderer[] _glowParts;
        Transform[] _prongs;
        MaterialPropertyBlock _mpb;
        GravityGun _gun;
        PlayerController _player;
        Vector2 _sway;
        float _kick, _kickVelocity;
        float _flash;
        Color _flashColor = Color.white;
        float _prongOpen;
        float _bobPhase;

        public Transform Muzzle { get; private set; }

        public static GunViewModel Create(Transform cameraTransform)
        {
            GameObject root = new GameObject("ViewModel");
            root.transform.SetParent(cameraTransform, false);
            root.transform.localPosition = RestPosition;
            GunViewModel vm = root.AddComponent<GunViewModel>();
            vm.Build();
            return vm;
        }

        public void Bind(GravityGun gun, PlayerController player)
        {
            _gun = gun;
            _player = player;
        }

        void Build()
        {
            _mpb = new MaterialPropertyBlock();
            Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.11f, 0.12f, 0.34f), Vector3.zero, MatLib.Plastic);
            Part(PrimitiveType.Cube, new Vector3(0f, 0.072f, -0.03f), new Vector3(0.07f, 0.03f, 0.22f), Vector3.zero, MatLib.Metal);
            Part(PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.1f), new Vector3(0.06f, 0.14f, 0.07f), new Vector3(15f, 0f, 0f), MatLib.Metal);
            Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.2f), new Vector3(0.075f, 0.07f, 0.075f), new Vector3(90f, 0f, 0f), MatLib.Metal);
            Renderer stripeL = Part(PrimitiveType.Cube, new Vector3(0.056f, 0.015f, 0.01f), new Vector3(0.006f, 0.022f, 0.24f), Vector3.zero, MatLib.AccentBlue);
            Renderer stripeR = Part(PrimitiveType.Cube, new Vector3(-0.056f, 0.015f, 0.01f), new Vector3(0.006f, 0.022f, 0.24f), Vector3.zero, MatLib.AccentBlue);
            Renderer core = Part(PrimitiveType.Sphere, new Vector3(0f, 0f, 0.28f), Vector3.one * 0.06f, Vector3.zero, MatLib.AccentBlue);

            _prongs = new Transform[3];
            Renderer[] prongTips = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                Transform pivot = new GameObject("Prong" + i).transform;
                pivot.SetParent(transform, false);
                pivot.localPosition = new Vector3(0f, 0f, 0.25f);
                pivot.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                _prongs[i] = pivot;
                Part(PrimitiveType.Cube, new Vector3(0f, 0.055f, 0.05f), new Vector3(0.018f, 0.018f, 0.12f), Vector3.zero, MatLib.Metal, pivot);
                prongTips[i] = Part(PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.11f), new Vector3(0.022f, 0.022f, 0.02f), Vector3.zero, MatLib.AccentBlue, pivot);
            }

            _glowParts = new[] { stripeL, stripeR, core, prongTips[0], prongTips[1], prongTips[2] };
            Muzzle = new GameObject("Muzzle").transform;
            Muzzle.SetParent(transform, false);
            Muzzle.localPosition = new Vector3(0f, 0f, 0.3f);
        }

        Renderer Part(PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Material mat, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        /// <summary>Recoil impulse (0..1).</summary>
        public void Kick(float amount) { _kickVelocity -= amount * 1.6f; }

        public void Flash(Color color)
        {
            _flash = 1f;
            _flashColor = color;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _gun == null) return;

            Vector2 look = _player.InputEnabled ? GameInput.LookDelta : Vector2.zero;
            _sway = Vector2.Lerp(_sway, Vector2.ClampMagnitude(-look * 0.012f, 0.05f), dt * 8f);

            Vector3 v = _player.Velocity;
            float speed = new Vector2(v.x, v.z).magnitude;
            _bobPhase += dt * speed * 1.7f;
            float bobK = Mathf.Clamp01(speed / 6f);
            Vector3 bob = new Vector3(Mathf.Cos(_bobPhase) * 0.012f, Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.012f, 0f) * bobK;

            _kickVelocity += (-_kick * 260f - _kickVelocity * 22f) * dt;
            _kick += _kickVelocity * dt;

            transform.localPosition = RestPosition + new Vector3(_sway.x, _sway.y, 0f) + bob + new Vector3(0f, 0f, _kick * 0.12f);
            transform.localRotation = Quaternion.Euler(_kick * 18f + _sway.y * 80f, _sway.x * -60f, _sway.x * 40f);

            bool holding = _gun.State == AimState.Holding;
            _prongOpen = Mathf.MoveTowards(_prongOpen, holding ? 1f : (_gun.State == AimState.Grabbable ? 0.4f : 0f), dt * 5f);
            for (int i = 0; i < _prongs.Length; i++) _prongs[i].localRotation = Quaternion.Euler(-_prongOpen * 22f, 0f, i * 120f);

            Color baseColor = holding ? Palette.OrangeGlow * (0.8f + 0.3f * Mathf.Sin(Time.time * 8f))
                : _gun.State == AimState.Grabbable ? Palette.BlueGlow * 1.3f : Palette.BlueGlow * 0.75f;
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 3f);
            _mpb.SetColor(ColorId, Color.Lerp(baseColor, _flashColor * 2f, _flash));
            for (int i = 0; i < _glowParts.Length; i++) _glowParts[i].SetPropertyBlock(_mpb);
        }
    }
}
