using System;
using Core.Data.Boosters;
using Core.Data.Level;
using Signals;

namespace Gameplay.Economy
{
    /// <summary>
    /// Turns purchase and reward requests raised by the views into inventory changes, so the View assembly never
    /// writes economy data itself. Prices and reward sizes still come from the views until the store is built.
    /// </summary>
    public sealed class EconomyRequestHandler
    {
        private readonly PlayerInventory _inventory;
        private readonly Action<BoosterType, int> _onQuickBuyRequested;
        private readonly Action<int> _onCoinPackPurchaseRequested;
        private readonly Action<int, int> _onBundlePurchaseRequested;
        private readonly Action<int, int> _onDailyRewardClaimRequested;

        public EconomyRequestHandler(PlayerInventory inventory)
        {
            _inventory = inventory;
            _onQuickBuyRequested = HandleQuickBuyRequested;
            _onCoinPackPurchaseRequested = HandleCoinPackPurchaseRequested;
            _onBundlePurchaseRequested = HandleBundlePurchaseRequested;
            _onDailyRewardClaimRequested = HandleDailyRewardClaimRequested;
        }

        public void Subscribe()
        {
            EconomySignals.OnQuickBuyRequested += _onQuickBuyRequested;
            EconomySignals.OnCoinPackPurchaseRequested += _onCoinPackPurchaseRequested;
            EconomySignals.OnBundlePurchaseRequested += _onBundlePurchaseRequested;
            EconomySignals.OnDailyRewardClaimRequested += _onDailyRewardClaimRequested;
        }

        public void Unsubscribe()
        {
            EconomySignals.OnQuickBuyRequested -= _onQuickBuyRequested;
            EconomySignals.OnCoinPackPurchaseRequested -= _onCoinPackPurchaseRequested;
            EconomySignals.OnBundlePurchaseRequested -= _onBundlePurchaseRequested;
            EconomySignals.OnDailyRewardClaimRequested -= _onDailyRewardClaimRequested;
        }

        private void HandleQuickBuyRequested(BoosterType type, int price)
        {
            if (!_inventory.TrySpendCoins(price))
            {
                UiSignals.RaiseOpenShop();
                return;
            }

            _inventory.AddBooster(type, 1);
        }

        private void HandleCoinPackPurchaseRequested(int coins)
        {
            _inventory.AddCoins(coins);
        }

        private void HandleBundlePurchaseRequested(int coins, int itemCount)
        {
            _inventory.AddCoins(coins);
            _inventory.AddBooster(BoosterType.Hammer, itemCount);
            _inventory.AddBooster(BoosterType.Glove, itemCount);
            _inventory.AddBooster(BoosterType.Shuffle, itemCount);
            _inventory.AddPreBooster(PreGameBoosterFlags.RocketBomb, itemCount);
            _inventory.AddPreBooster(PreGameBoosterFlags.ColorBomb, itemCount);
            _inventory.AddPreBooster(PreGameBoosterFlags.ExtraMoves, itemCount);
        }

        private void HandleDailyRewardClaimRequested(int coins, int hammers)
        {
            _inventory.AddCoins(coins);
            _inventory.AddBooster(BoosterType.Hammer, hammers);
        }
    }
}
