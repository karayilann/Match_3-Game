using System.Runtime.CompilerServices;
using Core.Data.Boosters;
using Gameplay.Economy;
using Signals;

namespace Gameplay.Boosters
{
    /// <summary>
    /// Holds only the in-level selection. Stock lives in PlayerInventory, so a purchase made in the shop or the
    /// daily reward is seen here at once instead of being overwritten by a copy cached at startup.
    /// </summary>
    public sealed class BoosterService
    {
        private readonly PlayerInventory _inventory;
        private BoosterType _activeBooster = BoosterType.None;

        public BoosterType ActiveBooster
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _activeBooster;
        }

        public BoosterService(PlayerInventory inventory)
        {
            _inventory = inventory;
        }

        public void ResetForLevel()
        {
            _activeBooster = BoosterType.None;
            EconomySignals.RaiseBoosterCountChanged(BoosterType.Hammer, _inventory.GetBoosterCount(BoosterType.Hammer));
            EconomySignals.RaiseBoosterCountChanged(BoosterType.Glove, _inventory.GetBoosterCount(BoosterType.Glove));
            EconomySignals.RaiseBoosterCountChanged(BoosterType.Shuffle, _inventory.GetBoosterCount(BoosterType.Shuffle));
            EconomySignals.RaiseBoosterSelected(BoosterType.None);
        }

        public void SelectBooster(BoosterType type)
        {
            if (type != BoosterType.None && GetCount(type) <= 0)
            {
                EconomySignals.RaiseBoosterPurchaseRequested(type);
                return;
            }

            if (_activeBooster == type)
            {
                _activeBooster = BoosterType.None;
            }
            else
            {
                _activeBooster = type;
            }

            EconomySignals.RaiseBoosterSelected(_activeBooster);
        }

        public void Deselect()
        {
            if (_activeBooster == BoosterType.None)
            {
                return;
            }

            _activeBooster = BoosterType.None;
            EconomySignals.RaiseBoosterSelected(BoosterType.None);
        }

        public bool TryConsumeBooster(BoosterType type)
        {
            if (!_inventory.TryConsumeBooster(type))
            {
                return false;
            }

            EconomySignals.RaiseBoosterUsed(type);

            if (_activeBooster == type)
            {
                _activeBooster = BoosterType.None;
                EconomySignals.RaiseBoosterSelected(BoosterType.None);
            }

            return true;
        }

        public int GetCount(BoosterType type)
        {
            return _inventory.GetBoosterCount(type);
        }
    }
}
