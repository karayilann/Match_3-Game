using Core.Data.Boosters;
using Signals;
using UnityEngine;

namespace View.HUD
{
    public sealed class BoosterPanelView : MonoBehaviour
    {
        [SerializeField] private BoosterItemView[] _boosterItems;

        private void OnEnable()
        {
            EconomySignals.OnBoosterCountChanged += HandleBoosterCountChanged;
            EconomySignals.OnBoosterSelected += HandleBoosterSelected;
        }

        private void OnDisable()
        {
            EconomySignals.OnBoosterCountChanged -= HandleBoosterCountChanged;
            EconomySignals.OnBoosterSelected -= HandleBoosterSelected;
        }

        private void HandleBoosterCountChanged(BoosterType type, int count)
        {
            if (_boosterItems == null)
            {
                return;
            }

            for (int i = 0; i < _boosterItems.Length; i++)
            {
                BoosterItemView item = _boosterItems[i];
                if (item != null && item.Type == type)
                {
                    item.SetCount(count);
                }
            }
        }

        private void HandleBoosterSelected(BoosterType selectedType)
        {
            if (_boosterItems == null)
            {
                return;
            }

            for (int i = 0; i < _boosterItems.Length; i++)
            {
                BoosterItemView item = _boosterItems[i];
                if (item != null)
                {
                    item.SetSelected(item.Type == selectedType);
                }
            }
        }
    }
}
