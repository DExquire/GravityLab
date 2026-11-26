using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GravityLab.Core
{
    /// <summary>
    /// Keyboard + mouse facade. The only file that touches UnityEngine.Input or the Input System package:
    /// the legacy manager is used whenever it is enabled ("Both" included), otherwise the Input System.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Converts Input System pixel deltas to the legacy "Mouse X/Y" axis scale.</summary>
        const float PixelToAxis = 0.1f;

        static readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        static PointerEventData _eventData;
        static EventSystem _eventDataOwner;

        public static Vector2 Move
        {
            get
            {
                float x = 0f, y = 0f;
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
#elif ENABLE_INPUT_SYSTEM
                if (Held(Key.D) || Held(Key.RightArrow)) x += 1f;
                if (Held(Key.A) || Held(Key.LeftArrow)) x -= 1f;
                if (Held(Key.W) || Held(Key.UpArrow)) y += 1f;
                if (Held(Key.S) || Held(Key.DownArrow)) y -= 1f;
#endif
                Vector2 v = new Vector2(x, y);
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        /// <summary>Mouse movement this frame in legacy axis units (~0.1 per pixel).</summary>
        public static Vector2 LookDelta
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#elif ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.delta.ReadValue() * PixelToAxis : Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }

        /// <summary>-1, 0 or +1 wheel notches.</summary>
        public static float Scroll
        {
            get
            {
                float raw = 0f;
#if ENABLE_LEGACY_INPUT_MANAGER
                raw = Input.GetAxisRaw("Mouse ScrollWheel");
#elif ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) raw = Mouse.current.scroll.ReadValue().y;
#endif
                return Mathf.Abs(raw) < 0.0001f ? 0f : Mathf.Sign(raw);
            }
        }

        public static Vector2 PointerPosition
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        public static bool JumpDown { get { return Input.GetKeyDown(KeyCode.Space); } }
        public static bool CrouchHeld { get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C); } }
        public static bool SprintHeld { get { return Input.GetKey(KeyCode.LeftShift); } }
        public static bool GrabDown { get { return Input.GetMouseButtonDown(0); } }
        public static bool LaunchDown { get { return Input.GetMouseButtonDown(1); } }
        public static bool FlipDown { get { return Input.GetKeyDown(KeyCode.F); } }
        public static bool FreezeDown { get { return Input.GetKeyDown(KeyCode.Q); } }
        public static bool PauseDown { get { return Input.GetKeyDown(KeyCode.Escape); } }
        public static bool RestartDown { get { return Input.GetKeyDown(KeyCode.R); } }
        public static bool ConfirmDown { get { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); } }
#elif ENABLE_INPUT_SYSTEM
        public static bool JumpDown { get { return Pressed(Key.Space); } }
        public static bool CrouchHeld { get { return Held(Key.LeftCtrl) || Held(Key.C); } }
        public static bool SprintHeld { get { return Held(Key.LeftShift); } }
        public static bool GrabDown { get { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; } }
        public static bool LaunchDown { get { return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame; } }
        public static bool FlipDown { get { return Pressed(Key.F); } }
        public static bool FreezeDown { get { return Pressed(Key.Q); } }
        public static bool PauseDown { get { return Pressed(Key.Escape); } }
        public static bool RestartDown { get { return Pressed(Key.R); } }
        public static bool ConfirmDown { get { return Pressed(Key.Enter) || Pressed(Key.NumpadEnter); } }
#else
        public static bool JumpDown { get { return false; } }
        public static bool CrouchHeld { get { return false; } }
        public static bool SprintHeld { get { return false; } }
        public static bool GrabDown { get { return false; } }
        public static bool LaunchDown { get { return false; } }
        public static bool FlipDown { get { return false; } }
        public static bool FreezeDown { get { return false; } }
        public static bool PauseDown { get { return false; } }
        public static bool RestartDown { get { return false; } }
        public static bool ConfirmDown { get { return false; } }
#endif

        /// <summary>Returns 1..9 when a number key was pressed this frame, otherwise 0.</summary>
        public static int NumberDown()
        {
            for (int i = 1; i <= 9; i++)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) return i;
#elif ENABLE_INPUT_SYSTEM
                if (Pressed(Key.Digit1 + (i - 1)) || Pressed(Key.Numpad1 + (i - 1))) return i;
#endif
            }
            return 0;
        }

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
        static bool Held(Key key)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }

        static bool Pressed(Key key)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb[key].wasPressedThisFrame;
        }
#endif

        /// <summary>True when the mouse is above a raycast-target UI element.</summary>
        public static bool IsPointerOverUI()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            if (_eventData == null || _eventDataOwner != es)
            {
                _eventData = new PointerEventData(es);
                _eventDataOwner = es;
            }
            _eventData.Reset();
            _eventData.position = PointerPosition;
            _uiHits.Clear();
            es.RaycastAll(_eventData, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
