using System.Runtime.CompilerServices;
using Core.Data.Tiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.HUD
{
    public sealed class GoalItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rootTransform;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Image _progressBarFill;

        public RectTransform RootTransform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rootTransform;
        }

        public Image ProgressBarFill
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _progressBarFill;
        }

        public TileType GoalType { get; private set; }

        public Sprite IconSprite => _iconImage != null ? _iconImage.sprite : null;

        public void SetGoalType(TileType type)
        {
            GoalType = type;
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void Bind(Sprite iconSprite, byte currentCount, byte targetCount)
        {
            _iconImage.sprite = iconSprite;
            _progressText.SetText("{0:0}/{1:0}", (float)currentCount, (float)targetCount);
            _progressBarFill.fillAmount = targetCount > 0 ? (float)currentCount / targetCount : 0f;
        }
    }
}
