using GravityLab.Core;
using GravityLab.Gadgets;
using UnityEngine;
using UnityEngine.UI;

namespace GravityLab.UI
{
    /// <summary>
    /// Reactive crosshair: a dot when idle, closing blue brackets over grabbable objects, an orange ring while holding,
    /// a cyan arc showing remaining stasis, a pulse on every gun action, plus a context hint line.
    /// </summary>
    public sealed class Crosshair : MonoBehaviour
    {
        static readonly Vector2[] BracketDirections = { new Vector2(-0.7f, 0.7f), new Vector2(0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f) };

        Image _dot, _ring, _freeze;
        RectTransform[] _brackets;
        Image[] _bracketImages;
        Text _hint;
        GravityGun _gun;
        float _grab, _hold, _pulse;
        float _lastAction;
        AimState _shownState = (AimState)(-1);

        public static Crosshair Create(Transform parent, GravityGun gun)
        {
            RectTransform root = UIFactory.Place(UIFactory.Rect(parent, "Crosshair"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
            Crosshair c = root.gameObject.AddComponent<Crosshair>();
            c._gun = gun;

            c._freeze = UIFactory.Image(root, "Freeze", UISprites.ThickRing, Palette.Cyan);
            c._freeze.rectTransform.sizeDelta = new Vector2(64f, 64f);
            c._freeze.type = Image.Type.Filled;
            c._freeze.fillMethod = Image.FillMethod.Radial360;
            c._freeze.fillOrigin = 2;
            c._freeze.fillClockwise = false;

            c._ring = UIFactory.Image(root, "Ring", UISprites.Ring, Color.white);
            c._ring.rectTransform.sizeDelta = new Vector2(40f, 40f);
            c._dot = UIFactory.Image(root, "Dot", UISprites.Circle, Color.white);
            c._dot.rectTransform.sizeDelta = new Vector2(7f, 7f);
            Shadow s = c._dot.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.5f);
            s.effectDistance = new Vector2(1f, -1f);

            c._brackets = new RectTransform[4];
            c._bracketImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                Image b = UIFactory.Image(root, "Bracket" + i, UISprites.Bracket, Palette.Blue);
                b.rectTransform.sizeDelta = new Vector2(16f, 16f);
                b.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f * i);
                c._brackets[i] = b.rectTransform;
                c._bracketImages[i] = b;
            }

            c._hint = UIFactory.Label(parent, "", Fonts.Bold, 22, new Color(1f, 1f, 1f, 0.85f));
            UIFactory.Place(c._hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(900f, 34f));
            Outline o = c._hint.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.45f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
            return c;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            AimState state = _gun.State;
            _grab = Mathf.MoveTowards(_grab, state == AimState.Grabbable ? 1f : 0f, dt * 8f);
            _hold = Mathf.MoveTowards(_hold, state == AimState.Holding ? 1f : 0f, dt * 8f);
            if (_gun.LastActionTime != _lastAction)
            {
                _lastAction = _gun.LastActionTime;
                _pulse = 1f;
            }
            _pulse = Mathf.MoveTowards(_pulse, 0f, dt * 4f);

            float ringSize = 26f + _hold * 18f + _pulse * 14f;
            _ring.rectTransform.sizeDelta = new Vector2(ringSize, ringSize);
            Color ringColor = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), Palette.Orange, _hold);
            _ring.color = Color.Lerp(ringColor, _gun.LastActionColor, _pulse * 0.8f);
            _dot.color = Color.Lerp(Color.white, Palette.Orange, _hold);

            float spread = Mathf.Lerp(34f, 17f, _grab) + Mathf.Sin(Time.unscaledTime * 6f) * 2f * _grab;
            for (int i = 0; i < 4; i++)
            {
                _brackets[i].anchoredPosition = BracketDirections[i] * spread;
                Color bc = Palette.Blue;
                bc.a = _grab;
                _bracketImages[i].color = bc;
            }

            float freeze = _gun.FreezeRemaining01;
            _freeze.fillAmount = freeze;
            _freeze.enabled = freeze > 0f;

            if (state != _shownState)
            {
                _shownState = state;
                _hint.text = state == AimState.Holding ? "[LMB] Drop    [RMB] Launch    [F] Flip    [Q] Freeze    [Wheel] Distance"
                    : state == AimState.Grabbable ? "[LMB] Grab    [RMB] Push    [F] Flip gravity    [Q] Freeze" : "";
            }
        }
    }
}
