using System.Collections.Generic;
using Core.Data.Tiles;
using UnityEngine;

namespace Gameplay.Board
{
    /// <summary>
    /// Cell backdrops are kept alive between levels and only toggled, so starting a level
    /// allocates nothing once the largest board has been seen.
    /// </summary>
    public sealed class BoardBackgroundPlacer
    {
        private static readonly Color EVEN_CELL_COLOR = new Color(0.11f, 0.19f, 0.34f, 0.88f);
        private static readonly Color ODD_CELL_COLOR = new Color(0.15f, 0.25f, 0.43f, 0.88f);
        private static readonly Color BORDER_COLOR = new Color(0.06f, 0.11f, 0.22f, 0.96f);
        private const float CELL_SCALE_FACTOR = 0.96f;
        private const float BORDER_PAD_FACTOR = 1.14f;
        private const int BORDER_SORTING_ORDER = -2;
        private const int CELL_SORTING_ORDER = -1;
        private const int INITIAL_CAPACITY = 162;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>(INITIAL_CAPACITY);
        private readonly Transform _parent;
        private int _usedCount;

        public BoardBackgroundPlacer(Transform parent)
        {
            _parent = parent;
        }

        public void SpawnGridBackground(BoardModel model, GridToWorldConverter converter, Sprite cellSprite, float cellSize)
        {
            Clear();

            if (cellSprite == null)
            {
                return;
            }

            byte width = model.Width;
            byte height = model.Height;
            Vector3 cellScale = new Vector3(cellSize * CELL_SCALE_FACTOR, cellSize * CELL_SCALE_FACTOR, 1f);
            Vector3 borderScale = new Vector3(cellSize * BORDER_PAD_FACTOR, cellSize * BORDER_PAD_FACTOR, 1f);

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    if (!model.GetCell(coord).IsPlayable)
                    {
                        continue;
                    }

                    Vector3 worldPos = converter.ToWorldPosition(coord);

                    Place("CellBorder", worldPos, borderScale, cellSprite, BORDER_COLOR, BORDER_SORTING_ORDER);

                    bool isEven = ((x + y) & 1) == 0;
                    Color cellColor = isEven ? EVEN_CELL_COLOR : ODD_CELL_COLOR;
                    Place("CellBg", worldPos, cellScale, cellSprite, cellColor, CELL_SORTING_ORDER);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _usedCount; i++)
            {
                _renderers[i].gameObject.SetActive(false);
            }

            _usedCount = 0;
        }

        private void Place(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder)
        {
            SpriteRenderer sr;

            if (_usedCount < _renderers.Count)
            {
                sr = _renderers[_usedCount];
            }
            else
            {
                GameObject go = new GameObject(objectName);
                go.transform.SetParent(_parent, false);
                sr = go.AddComponent<SpriteRenderer>();
                _renderers.Add(sr);
            }

            _usedCount++;

            Transform tr = sr.transform;
            tr.localPosition = position;
            tr.localScale = scale;
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.gameObject.SetActive(true);
        }
    }
}
