using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Animation
{
    public sealed class RocketTrailRenderer
    {
        private const int POOL_SIZE = 8;
        private const int POINT_COUNT = 3;
        private const float MAX_LENGTH = 2.4f;
        private const float BASE_WIDTH = 0.50f;
        private const float TRAIL_Z = -0.15f;
        private const int SORTING_ORDER = 14;

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
            public Vector3 Direction;
            public float Duration;
            public float Elapsed;
            public float StartDelay;
        }

        private readonly List<Command> _active;
        private readonly Slot[] _pool;
        private readonly Material _material;
        private readonly Texture2D _texture;

        public RocketTrailRenderer(Transform parent, Material template)
        {
            _active = new List<Command>(POOL_SIZE);
            _pool = new Slot[POOL_SIZE];
            _texture = CreateTexture();
            _material = new Material(template) { mainTexture = _texture };

            Keyframe[] widthKeys =
            {
                new Keyframe(0f, 0.08f),
                new Keyframe(0.4f, 0.55f),
                new Keyframe(1f, 1.0f)
            };
            AnimationCurve widthCurve = new AnimationCurve(widthKeys);

            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.25f, 0.02f), 0f),
                    new GradientColorKey(new Color(1f, 0.65f, 0.10f), 0.55f),
                    new GradientColorKey(new Color(1f, 1f, 0.90f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.4f),
                    new GradientAlphaKey(1f, 1f)
                });

            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject go = new GameObject("RocketTrail_" + i);
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

            Vector3 start3D = new Vector3(startWorldPos.x, startWorldPos.y, TRAIL_Z);
            Vector3 end3D = new Vector3(endWorldPos.x, endWorldPos.y, TRAIL_Z);
            Vector3 delta = end3D - start3D;
            Vector3 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.right;

            _active.Add(new Command
            {
                SlotIndex = slotIndex,
                StartPos = start3D,
                EndPos = end3D,
                Direction = dir,
                Duration = duration,
                Elapsed = 0f,
                StartDelay = startDelay
            });
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Command trail = _active[i];
                trail.Elapsed += deltaTime;

                float activeTime = trail.Elapsed - trail.StartDelay;
                if (activeTime < 0f)
                {
                    _active[i] = trail;
                    continue;
                }

                float t = trail.Duration > 0f ? Mathf.Clamp01(activeTime / trail.Duration) : 1f;
                ref Slot slot = ref _pool[trail.SlotIndex];

                if (t >= 1f)
                {
                    slot.Renderer.enabled = false;
                    slot.InUse = false;

                    int lastIndex = _active.Count - 1;
                    _active[i] = _active[lastIndex];
                    _active.RemoveAt(lastIndex);
                    continue;
                }

                UpdateGeometry(ref slot, trail, activeTime, t);
                _active[i] = trail;
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

        /// <summary>
        /// The head follows QuadIn easing so the trail stays glued to the rocket projectile,
        /// which accelerates with the same curve.
        /// </summary>
        private static void UpdateGeometry(ref Slot slot, Command trail, float activeTime, float t)
        {
            if (!slot.Renderer.enabled)
            {
                slot.Renderer.enabled = true;
            }

            float easedT = t * t;
            Vector3 headPos = Vector3.Lerp(trail.StartPos, trail.EndPos, easedT);
            float distFromStart = (headPos - trail.StartPos).magnitude;
            float currentTrailLen = Mathf.Min(distFromStart, MAX_LENGTH);

            if (t > 0.75f)
            {
                currentTrailLen *= (1f - t) / 0.25f;
            }

            Vector3 tailPos = headPos - trail.Direction * currentTrailLen;
            Vector3 midPos = Vector3.Lerp(tailPos, headPos, 0.5f);

            Vector3[] points = slot.Points;
            points[0] = tailPos;
            points[1] = midPos;
            points[2] = headPos;

            slot.Renderer.SetPositions(points);

            float widthFade = t > 0.78f ? (1f - t) / 0.22f : 1f;
            float flicker = 1f + 0.15f * Mathf.Sin(activeTime * 85f);
            slot.Renderer.widthMultiplier = BASE_WIDTH * widthFade * flicker;
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
                float core = Mathf.Clamp01(1f - dist * 2.8f);
                float glow = Mathf.Clamp01(1f - dist * 1.2f);
                core *= core;
                glow *= glow;

                float r = Mathf.Clamp01(core * 1.0f + glow * 1.0f);
                float g = Mathf.Clamp01(core * 1.0f + glow * 0.65f);
                float b = Mathf.Clamp01(core * 0.9f + glow * 0.15f);
                float a = Mathf.Clamp01(core * 1.0f + glow * 0.85f);

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
