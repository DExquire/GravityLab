using System;
using UnityEngine;

namespace GravityLab.Audio
{
    public enum SoundId
    {
        Click, Hover, Footstep, Jump, Land, Grab, Drop, Launch, Deny, Flip, Freeze, Unfreeze, Impact,
        PlateOn, PlateOff, DoorOpen, DoorClose, LaserOn, Dissolve, Respawn, FieldZap, FanOn, Blip,
        Complete, Fail, Elevator, RankStamp
    }

    /// <summary>
    /// Procedurally synthesised audio: every effect is a few oscillators, filtered noise and exponential envelopes,
    /// plus a seamless held-object hum and a 16-second ambient music loop.
    /// </summary>
    public static class SynthClips
    {
        const int Rate = 44100;
        const float TwoPi = Mathf.PI * 2f;
        static readonly System.Random _rng = new System.Random(7);

        public static AudioClip Create(SoundId id)
        {
            switch (id)
            {
                case SoundId.Click: return Build("click", 0.07f, (t, s) => Sine(1500f - t * 8000f, t) * Env(t, 70f) * 0.45f);
                case SoundId.Hover: return Build("hover", 0.05f, (t, s) => Sine(2400f, t) * Env(t, 90f) * 0.15f);
                case SoundId.Footstep: return Build("footstep", 0.16f, Footstep);
                case SoundId.Jump: return Build("jump", 0.22f, (t, s) => (Sine(160f + t * 500f, t) * 0.4f + s.LowNoise(0.2f) * 0.5f) * Env(t, 16f));
                case SoundId.Land: return Build("land", 0.25f, (t, s) => (Sine(90f - t * 120f, t) * 0.8f + s.LowNoise(0.15f) * 0.7f) * Env(t, 18f));
                case SoundId.Grab: return Build("grab", 0.32f, Grab);
                case SoundId.Drop: return Build("drop", 0.25f, (t, s) => Sine(700f - t * 1800f, t) * Env(t, 14f) * 0.4f);
                case SoundId.Launch: return Build("launch", 0.45f, Launch);
                case SoundId.Deny: return Build("deny", 0.26f, (t, s) => Square(140f, t) * (t < 0.1f || t > 0.14f ? 1f : 0f) * Env(t, 9f) * 0.22f);
                case SoundId.Flip: return Build("flip", 0.55f, Flip);
                case SoundId.Freeze: return Build("freeze", 0.7f, Freeze);
                case SoundId.Unfreeze: return Build("unfreeze", 0.4f, (t, s) => (Sine(2600f - t * 3000f, t) + Sine(3900f - t * 4200f, t) * 0.5f) * Env(t, 9f) * 0.18f);
                case SoundId.Impact: return Build("impact", 0.22f, (t, s) => (Sine(140f - t * 160f, t) * 0.7f + s.LowNoise(0.25f) * 0.6f) * Env(t, 26f));
                case SoundId.PlateOn: return Build("plateOn", 0.3f, (t, s) => Sine(t < 0.09f ? 520f : 780f, t) * Env(t, 10f) * 0.35f + s.Noise() * Env(t, 120f) * 0.2f);
                case SoundId.PlateOff: return Build("plateOff", 0.3f, (t, s) => Sine(t < 0.09f ? 600f : 400f, t) * Env(t, 10f) * 0.3f);
                case SoundId.DoorOpen: return Build("doorOpen", 0.8f, (t, s) => Door(t, s, 0.8f, true));
                case SoundId.DoorClose: return Build("doorClose", 0.7f, (t, s) => Door(t, s, 0.7f, false));
                case SoundId.LaserOn: return Build("laserOn", 0.6f, (t, s) => (Sine(880f, t) + Sine(1320f, t) * 0.6f + Sine(1760f, t) * 0.3f) * Env(t, 6f) * Attack(t, 0.01f) * 0.2f);
                case SoundId.Dissolve: return Build("dissolve", 0.8f, Dissolve);
                case SoundId.Respawn: return Build("respawn", 0.5f, (t, s) => (Sine(400f + t * 1600f, t) * 0.5f + s.HighNoise(0.5f) * 0.25f) * Attack(t, 0.15f) * Env(t, 5f) * 0.45f);
                case SoundId.FieldZap: return Build("fieldZap", 0.22f, (t, s) => (Square(90f, t) * 0.3f + s.HighNoise(0.4f) * 0.4f) * Env(t, 14f) * 0.5f);
                case SoundId.FanOn: return Build("fanOn", 0.7f, (t, s) => s.LowNoise(0.06f + t * 0.1f) * Attack(t, 0.25f) * Env(t, 3f) * 1.2f);
                case SoundId.Blip: return Build("blip", 0.08f, (t, s) => Sine(1100f, t) * Env(t, 45f) * 0.22f);
                case SoundId.Complete: return Build("complete", 1.3f, (t, s) => Arpeggio(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 0.45f));
                case SoundId.Fail: return Build("fail", 1.1f, (t, s) => Arpeggio(t, new[] { 392f, 311.13f, 261.63f, 196f }, 0.16f, 0.4f));
                case SoundId.Elevator: return Build("elevator", 1.8f, (t, s) => (Sine(55f + t * 12f, t) * 0.6f + s.LowNoise(0.03f) * 0.8f) * Attack(t, 0.4f) * Mathf.Clamp01((1.8f - t) * 2f) * 0.6f);
                case SoundId.RankStamp: return Build("rankStamp", 0.6f, (t, s) => (Sine(110f - t * 80f, t) * Env(t, 12f) * 0.8f + (Sine(1568f, t) + Sine(2093f, t) * 0.5f) * Env(t, 5f) * 0.18f));
                default: return Build("silence", 0.05f, (t, s) => 0f);
            }
        }

        // ---- building blocks ---------------------------------------------------------------

        /// <summary>Per-clip filter state so filtered noise stays continuous within one clip.</summary>
        sealed class State
        {
            float _lp, _hpIn, _hpOut;
            public float Noise() { return (float)(_rng.NextDouble() * 2.0 - 1.0); }
            public float LowNoise(float k) { _lp += (Noise() - _lp) * k; return _lp * 2f; }
            public float HighNoise(float k)
            {
                float x = Noise();
                _hpOut = k * (_hpOut + x - _hpIn);
                _hpIn = x;
                return _hpOut;
            }
        }

        static AudioClip Build(string name, float seconds, Func<float, State, float> synth)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
            float[] data = new float[n];
            State state = new State();
            int fadeIn = Mathf.Min(n / 4, Rate / 500);
            int fadeOut = Mathf.Min(n / 4, Rate / 80);
            for (int i = 0; i < n; i++)
            {
                float g = 1f;
                if (i < fadeIn) g = i / (float)fadeIn;
                if (i > n - fadeOut) g *= (n - i) / (float)fadeOut;
                data[i] = SoftClip(synth(i / (float)Rate, state) * g);
            }
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float SoftClip(float x) { return x / (1f + Mathf.Abs(x)) * 1.25f; }
        static float Sine(float freq, float t) { return Mathf.Sin(TwoPi * freq * t); }
        static float Square(float freq, float t) { return Mathf.Sin(TwoPi * freq * t) >= 0f ? 1f : -1f; }
        static float Env(float t, float decay) { return Mathf.Exp(-t * decay); }
        static float Attack(float t, float time) { return Mathf.Clamp01(t / time); }

        static float Footstep(float t, State s)
        {
            float thump = Sine(95f - t * 160f, t) * Env(t, 30f) * 0.55f;
            float scuff = s.LowNoise(0.35f) * Env(t, 38f) * 0.35f;
            return thump + scuff;
        }

        static float Grab(float t, State s)
        {
            float f = 300f + 700f * (1f - Env(t, 9f));
            float body = (Sine(f, t) + Sine(f * 1.5f, t) * 0.35f) * Env(t, 7f) * Attack(t, 0.02f);
            return body * 0.32f + s.HighNoise(0.6f) * Env(t, 25f) * 0.12f;
        }

        static float Launch(float t, State s)
        {
            float zap = Sine(1300f * Env(t, 7f) + 120f, t) * Env(t, 8f) * 0.45f;
            float air = s.LowNoise(0.3f) * Env(t, 6f) * 0.55f;
            float punch = Sine(70f, t) * Env(t, 25f) * 0.6f;
            return zap + air + punch;
        }

        static float Flip(float t, State s)
        {
            float f = 180f + 1100f * t / 0.55f;
            float trem = 0.6f + 0.4f * Mathf.Sin(TwoPi * 18f * t);
            return (Sine(f, t) * 0.4f + Sine(f * 0.5f, t) * 0.3f) * trem * Attack(t, 0.03f) * Env(t, 4f) * 0.6f;
        }

        static float Freeze(float t, State s)
        {
            float v = Sine(2093f, t) * Env(t, 7f) + Sine(2637f, t + 0.01f) * Env(t, 8f) * 0.7f + Sine(3136f, t) * Env(t, 10f) * 0.5f;
            float crackle = s.HighNoise(0.7f) * Env(t, 18f) * 0.35f;
            return v * 0.16f * Attack(t, 0.005f) + crackle;
        }

        static float Dissolve(float t, State s)
        {
            float sizzle = s.HighNoise(0.55f) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 30f * t)) * Env(t, 3.5f) * 0.5f;
            float tone = Sine(900f - t * 900f, t) * Env(t, 4f) * 0.2f;
            return sizzle + tone;
        }

        static float Door(float t, State s, float length, bool opening)
        {
            float hiss = s.LowNoise(0.08f) * Attack(t, 0.08f) * Mathf.Clamp01((length - t) * 4f) * 0.8f;
            float glide = Sine(opening ? 70f + t * 50f : 120f - t * 60f, t) * 0.25f * Mathf.Clamp01((length - t) * 3f);
            float thunk = opening ? 0f : Sine(60f, t) * Env(Mathf.Max(0f, t - length * 0.6f), 20f) * (t > length * 0.6f ? 0.7f : 0f);
            return hiss + glide + thunk;
        }

        static float Arpeggio(float t, float[] notes, float step, float gain)
        {
            float v = 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                float lt = t - k * step;
                if (lt < 0f) continue;
                float decay = k == notes.Length - 1 ? 2.8f : 6f;
                v += (Sine(notes[k], lt) + Sine(notes[k] * 2f, lt) * 0.25f) * Env(lt, decay) * Attack(lt, 0.006f);
            }
            return v * gain;
        }

        // ---- loops ----------------------------------------------------------------------------

        /// <summary>One-second seamless hum (all partials are whole Hz) played while an object is held.</summary>
        public static AudioClip CreateHum()
        {
            int n = Rate;
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float trem = 0.75f + 0.25f * Mathf.Sin(TwoPi * 6f * t);
                data[i] = (Sine(110f, t) * 0.5f + Sine(220f, t) * 0.25f + Sine(331f, t) * 0.12f) * trem * 0.35f;
            }
            AudioClip clip = AudioClip.Create("hum", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>16-second ambient pad loop (Am9 - Fmaj7 - Cmaj7 - G6) with a soft arpeggio, written with wrap-around so it loops seamlessly.</summary>
        public static AudioClip CreateMusic()
        {
            const int rate = 22050;
            const float chordLen = 4f;
            int n = rate * 16;
            float[] data = new float[n];
            float[][] chords =
            {
                new[] { 110f, 220f, 261.63f, 329.63f, 493.88f },
                new[] { 87.31f, 174.61f, 220f, 261.63f, 329.63f },
                new[] { 65.41f, 196f, 246.94f, 329.63f, 392f },
                new[] { 98f, 196f, 246.94f, 293.66f, 329.63f }
            };

            for (int c = 0; c < chords.Length; c++)
            {
                int start = Mathf.RoundToInt(c * chordLen * rate);
                int length = Mathf.RoundToInt((chordLen + 1.8f) * rate);
                float[] notes = chords[c];
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Clamp01(t / 1.4f) * Mathf.Clamp01((chordLen + 1.8f - t) / 1.8f);
                    float v = Sine(notes[0], t) * 0.16f;
                    for (int k = 1; k < notes.Length; k++)
                    {
                        float f = notes[k];
                        v += (Sine(f, t) + Sine(f * 1.003f, t) * 0.8f) * 0.055f;
                    }
                    data[(start + i) % n] += v * env;
                }

                // Gentle plucked arpeggio an octave up, eight notes per chord.
                for (int a = 0; a < 8; a++)
                {
                    float f = notes[1 + (a * 3) % (notes.Length - 1)] * 2f;
                    int noteStart = start + Mathf.RoundToInt(a * 0.5f * rate);
                    int noteLen = Mathf.RoundToInt(1.6f * rate);
                    for (int i = 0; i < noteLen; i++)
                    {
                        float t = i / (float)rate;
                        float v = (Sine(f, t) + Sine(f * 2f, t) * 0.2f) * Mathf.Exp(-t * 3.2f) * Mathf.Clamp01(t / 0.004f) * 0.045f;
                        data[(noteStart + i) % n] += v;
                    }
                }
            }

            float lp = 0f;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    lp += (data[i] - lp) * 0.35f;
                    if (pass == 1) data[i] = SoftClip(lp * 0.9f);
                }
            }

            AudioClip clip = AudioClip.Create("music", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
