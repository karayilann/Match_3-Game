using System.Collections.Generic;
using PrimeTween;
using Signals;
using UnityEngine;
using UnityEngine.UI;

namespace View.LevelMap
{
    public sealed class LevelMapView : MonoBehaviour
    {
        [Header("Scroll & Content")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;

        [Header("Prefabs")]
        [SerializeField] private LevelNodeView _nodePrefab;
        [SerializeField] private GameObject _pathDotPrefab;

        [Header("Curved Path Configuration")]
        [SerializeField] private int _totalLevels = 15;
        [SerializeField] private float _nodeSpacingY = 195f;
        [SerializeField] private float _bottomPadding = 240f;
        [SerializeField] private float _curveAmplitude = 260f;
        [SerializeField] private float _curveFrequency = 0.95f;
        [SerializeField] private int _dotsBetweenNodes = 3;

        private static readonly Vector2 BOTTOM_CENTER = new Vector2(0.5f, 0f);

        private readonly List<LevelNodeView> _spawnedNodes = new List<LevelNodeView>(24);
        private int _currentUnlockedLevel = 1;
        private IReadOnlyList<byte> _starsByLevel;

        private void Awake()
        {
            EnsureDragSurface();
        }

        private void OnEnable()
        {
            UiSignals.OnProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            UiSignals.OnProgressChanged -= HandleProgressChanged;
        }

        private void HandleProgressChanged(int unlockedLevel, IReadOnlyList<byte> starsByLevel)
        {
            _currentUnlockedLevel = unlockedLevel;
            _starsByLevel = starsByLevel;

            if (_spawnedNodes.Count > 0)
            {
                RefreshProgress();
            }
        }

        private int GetStars(int level)
        {
            return _starsByLevel != null && level < _starsByLevel.Count ? _starsByLevel[level] : 0;
        }

        /// <summary>
        /// ScrollRect only receives drag events through raycast-hit graphics; without a transparent
        /// graphic on the viewport, dragging on empty map areas does nothing.
        /// </summary>
        private void EnsureDragSurface()
        {
            if (_scrollRect == null)
            {
                return;
            }

            RectTransform surface = _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)_scrollRect.transform;
            Graphic graphic = surface.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = surface.gameObject.AddComponent<Image>();
                image.color = Color.clear;
                graphic = image;
            }
            graphic.raycastTarget = true;
        }

        private void Start()
        {
            BuildCurvedMap();
        }

        public void BuildCurvedMap()
        {
            if (_content == null || _nodePrefab == null)
            {
                return;
            }

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }
            _spawnedNodes.Clear();

            float totalHeight = (_totalLevels - 1) * _nodeSpacingY + _bottomPadding * 2f;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            Vector2 prevPos = Vector2.zero;

            for (int level = 1; level <= _totalLevels; level++)
            {
                float y = (level - 1) * _nodeSpacingY + _bottomPadding;
                float x = Mathf.Sin(level * _curveFrequency) * _curveAmplitude;
                Vector2 currentPos = new Vector2(x, y);

                if (level > 1 && _pathDotPrefab != null)
                {
                    SpawnPathDots(prevPos, currentPos, _dotsBetweenNodes);
                }

                LevelNodeView node = Instantiate(_nodePrefab, _content);
                RectTransform rt = node.GetComponent<RectTransform>();
                AnchorToBottomCenter(rt);
                rt.anchoredPosition = currentPos;

                bool isUnlocked = level <= _currentUnlockedLevel;
                bool isCurrent = level == _currentUnlockedLevel;
                node.Bind(level, isUnlocked, isCurrent, GetStars(level));
                _spawnedNodes.Add(node);

                prevPos = currentPos;
            }

            ScrollToCurrentLevel();
        }

        private void RefreshProgress()
        {
            for (int i = 0; i < _spawnedNodes.Count; i++)
            {
                int level = i + 1;
                bool isUnlocked = level <= _currentUnlockedLevel;
                bool isCurrent = level == _currentUnlockedLevel;

                _spawnedNodes[i].Bind(level, isUnlocked, isCurrent, GetStars(level));
            }

            ScrollToCurrentLevel();
        }

        private void SpawnPathDots(Vector2 startPos, Vector2 endPos, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                float t = (float)i / (count + 1);
                Vector2 pos = Vector2.Lerp(startPos, endPos, t);

                GameObject dot = Instantiate(_pathDotPrefab, _content);
                dot.transform.SetAsFirstSibling();
                RectTransform rt = dot.GetComponent<RectTransform>();
                AnchorToBottomCenter(rt);
                rt.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Positions are computed from the content bottom, so children must anchor there;
        /// the prefabs' centered anchors would shift everything up by half the content height.
        /// </summary>
        private static void AnchorToBottomCenter(RectTransform rt)
        {
            rt.anchorMin = BOTTOM_CENTER;
            rt.anchorMax = BOTTOM_CENTER;
            rt.pivot = BOTTOM_CENTER;
        }

        private void ScrollToCurrentLevel()
        {
            if (_scrollRect == null || _totalLevels <= 1)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float scrollRange = _content.rect.height - _scrollRect.viewport.rect.height;
            if (scrollRange <= 0f)
            {
                return;
            }

            float nodeY = (_currentUnlockedLevel - 1) * _nodeSpacingY + _bottomPadding;
            float centeredOffset = nodeY - _scrollRect.viewport.rect.height * 0.5f;
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(centeredOffset / scrollRange);
        }
    }
}
