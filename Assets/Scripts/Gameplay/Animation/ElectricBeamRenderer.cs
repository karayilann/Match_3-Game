using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Animation
{
    public sealed class ElectricBeamRenderer
    {
        private const int POOL_SIZE = 36;
        private const int POINT_COUNT = 9;
        private const float BASE_WIDTH = 0.36f;
        private const float JITTER_AMPLITUDE = 0.22f;
        private const float BEAM_Z = -0.2f;
        private const int SORTING_ORDER = 15;

        private struct Slot
        {
            public LineRenderer Renderer;
            public Vector3[] Points;
            public bool InUse;
        }

        private struct Command
        {
            public int SlotIndex;
            public Vector3 StartPos;
            public Vector3 EndPos;
            public float Duration;
            public float Elapsed;
            public float StartDelay;
            public int Seed;
        }

        private readonly List<Command> _active;
        private readonly Slot[] _pool;
        private readonly Material _material;
        private readonly Texture2D _texture;
        private int _seedCounter;

        public ElectricBeamRenderer(Transform parent, Material template)
        {
            _active = new List<Command>(POOL_SIZE);
            _pool = new Slot[POOL_SIZE];
            _texture = CreateTexture();
            _material = new Material(template) { mainTexture = _texture };

            Keyframe[] widthKeys =
            {
                new Keyframe(0f, 0.55f),
                new Keyframe(0.2f, 1f),
                new Keyframe(0.8f, 1f),
                new Keyframe(1f, 0.65f)
            };
            AnimationCurve widthCurve = new AnimationCurve(widthKeys);

            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.92f, 1f), 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(new Color(1f, 0.88f, 0.35f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0.95f, 1f)
                });

            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject go = new GameObject("ElectricBeam_" + i);
                go.transform.SetParent(parent, false);

                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = POINT_COUNT;
                lr.numCapVertices = 4;
                lr.numCornerVertices = 3;
                lr.textureMode = LineTextureMode.Stretch;
                lr.alignment = LineAlignment.View;
                lr.sortingOrder = SORTING_ORDER;
                lr.sharedMaterial = _material;
                lr.widthCurve = widthCurve;
                lr.colorGradient = colorGradient;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;

                _pool[i] = new Slot
                {
                    Renderer = lr,
                    Points = new Vector3[POINT_COUNT],
                    InUse = false
                };
            }
        }

        public bool HasActive => _active.Count > 0;

        public void Queue(Vector3 startWorldPos, Vector3 endWorldPos, float duration, float startDelay)
        {
            int slotIndex = AcquireSlot();
            if (slotIndex < 0)
            {
                return;
            }

            _seedCounter = (_seedCounter + 1) & 0x7FFF;
            _active.Add(new Command
            {
                SlotIndex = slotIndex,
                StartPos = new Vector3(startWorldPos.x, startWorldPos.y, BEAM_Z),
                EndPos = new Vector3(endWorldPos.x, endWorldPos.y, BEAM_Z),
                Duration = duration,
                Elapsed = 0f,
                StartDelay = startDelay,
                Seed = _seedCounter
            });
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Command beam = _active[i];
                beam.Elapsed += deltaTime;

                float activeTime = beam.Elapsed - beam.StartDelay;
                if (activeTime < 0f)
                {
                    _active[i] = beam;
                    continue;
                }

                float t = beam.Duration > 0f ? Mathf.Clamp01(activeTime / beam.Duration) : 1f;
                ref Slot slot = ref _pool[beam.SlotIndex];

                if (t >= 1f)
                {
                    slot.Renderer.enabled = false;
                    slot.InUse = false;

                    int lastIndex = _active.Count - 1;
                    _active[i] = _active[lastIndex];
                    _active.RemoveAt(lastIndex);
                    continue;
                }

                UpdateGeometry(ref slot, beam, activeTime, t);
                _active[i] = beam;
            }
        }

        public void Reset()
        {
            _active.Clear();

            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].Renderer != null)
                {
                    _pool[i].Renderer.enabled = false;
                }
                _pool[i].InUse = false;
            }
        }

        public void Dispose()
        {
            Object.Destroy(_material);
            Object.Destroy(_texture);
        }

        private int AcquireSlot()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].InUse)
                {
                    _pool[i].InUse = true;
                    return i;
                }
            }

            return -1;
        }

        private static void UpdateGeometry(ref Slot slot, Command beam, float activeTime, float t)
        {
            if (!slot.Renderer.enabled)
            {
                slot.Renderer.enabled = true;
            }

            float reach = Mathf.Clamp01(t / 0.16f);
            Vector3 currentEnd = Vector3.Lerp(beam.StartPos, beam.EndPos, reach);
            Vector3 delta = currentEnd - beam.StartPos;
            float length = delta.magnitude;
            Vector3 perp = length > 0.001f ? new Vector3(-delta.y / length, delta.x / length, 0f) : Vector3.up;

            int timeSlice = (int)(activeTime * 48f);
            float invSegments = 1f / (POINT_COUNT - 1);
            Vector3[] points = slot.Points;

            points[0] = beam.StartPos;
            points[POINT_COUNT - 1] = currentEnd;

            for (int p = 1; p < POINT_COUNT - 1; p++)
            {
                float frac = p * invSegments;
                Vector3 basePos = Vector3.Lerp(beam.StartPos, currentEnd, frac);
                float envelope = Mathf.Sin(frac * Mathf.PI);

                int hash = (beam.Seed * 73856093) ^ (p * 19349663) ^ (timeSlice * 83492791);
                float normalizedNoise = ((hash & 0xFFFF) / 32767.5f) - 1f;
                float secondaryWave = Mathf.Sin(activeTime * 55f + p * 2.1f + beam.Seed) * 0.45f;
                float offset = (normalizedNoise * 0.75f + secondaryWave) * JITTER_AMPLITUDE * envelope;

                points[p] = basePos + perp * offset;
            }

            slot.Renderer.SetPositions(points);

            float fadeOut = t > 0.72f ? (1f - t) / 0.28f : 1f;
            float flicker = 1f + 0.22f * Mathf.Sin(activeTime * 70f + beam.Seed);
            slot.Renderer.widthMultiplier = BASE_WIDTH * fadeOut * flicker;
        }

        private static Texture2D CreateTexture()
        {
            const int width = 16;
            const int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[width * height];
            float invHalfH = 2f / (height - 1f);

            for (int y = 0; y < height; y++)
            {
                float dist = Mathf.Abs(y * invHalfH - 1f);
                float core = Mathf.Clamp01(1f - dist * 3.2f);
                float midGlow = Mathf.Clamp01(1f - dist * 1.45f);
                float outerGlow = Mathf.Clamp01(1f - dist);

                core *= core;
                midGlow *= midGlow;
                outerGlow *= outerGlow * outerGlow;

                float r = Mathf.Clamp01(core * 1.0f + midGlow * 0.25f + outerGlow * 0.15f);
                float g = Mathf.Clamp01(core * 1.0f + midGlow * 0.85f + outerGlow * 0.45f);
                float b = Mathf.Clamp01(core * 1.0f + midGlow * 1.0f + outerGlow * 0.95f);
                float a = Mathf.Clamp01(core + midGlow * 0.85f + outerGlow * 0.45f);

                Color32 col = new Color32(
                    (byte)(r * 255f),
                    (byte)(g * 255f),
                    (byte)(b * 255f),
                    (byte)(a * 255f));

                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = col;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
