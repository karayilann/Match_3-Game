using System;
using Core.Data.Boosters;
using Core.Data.Level;
using UnityEngine;

namespace Signals
{
    public static class EconomySignals
    {
        public static event Action<BoosterType> OnBoosterSelected;
        public static event Action<BoosterType, int> OnBoosterCountChanged;
        public static event Action<PreGameBoosterFlags, int> OnPreBoosterCountChanged;
        public static event Action<BoosterType> OnBoosterUsed;
        public static event Action<BoosterType> OnBoosterPurchaseRequested;
        public static event Action<BoosterType> OnBoosterButtonClicked;
        public static event Action<BoosterType, int> OnQuickBuyRequested;
        public static event Action<int> OnCoinPackPurchaseRequested;
        public static event Action<int, int> OnBundlePurchaseRequested;
        public static event Action<int, int> OnDailyRewardClaimRequested;
        public static event Action<int> OnCoinsChanged;
        public static event Action<int, int> OnLivesChanged;
        public static event Action<int> OnLivesTimerChanged;
        public static event Action OnNoLivesLeft;

        public static void RaiseBoosterSelected(BoosterType type)
        {
            OnBoosterSelected?.Invoke(type);
        }

        public static void RaiseBoosterCountChanged(BoosterType type, int count)
        {
            OnBoosterCountChanged?.Invoke(type, count);
        }

        public static void RaisePreBoosterCountChanged(PreGameBoosterFlags flag, int count)
        {
            OnPreBoosterCountChanged?.Invoke(flag, count);
        }

        public static void RaiseBoosterUsed(BoosterType type)
        {
            OnBoosterUsed?.Invoke(type);
        }

        public static void RaiseBoosterPurchaseRequested(BoosterType type)
        {
            OnBoosterPurchaseRequested?.Invoke(type);
        }

        public static void RaiseBoosterButtonClicked(BoosterType type)
        {
            OnBoosterButtonClicked?.Invoke(type);
        }

        public static void RaiseQuickBuyRequested(BoosterType type, int price)
        {
            OnQuickBuyRequested?.Invoke(type, price);
        }

        public static void RaiseCoinPackPurchaseRequested(int coins)
        {
            OnCoinPackPurchaseRequested?.Invoke(coins);
        }

        public static void RaiseBundlePurchaseRequested(int coins, int itemCount)
        {
            OnBundlePurchaseRequested?.Invoke(coins, itemCount);
        }

        public static void RaiseDailyRewardClaimRequested(int coins, int hammers)
        {
            OnDailyRewardClaimRequested?.Invoke(coins, hammers);
        }

        public static void RaiseCoinsChanged(int coins)
        {
            OnCoinsChanged?.Invoke(coins);
        }

        public static void RaiseLivesChanged(int lives, int maxLives)
        {
            OnLivesChanged?.Invoke(lives, maxLives);
        }

        public static void RaiseLivesTimerChanged(int secondsToNextLife)
        {
            OnLivesTimerChanged?.Invoke(secondsToNextLife);
        }

        public static void RaiseNoLivesLeft()
        {
            OnNoLivesLeft?.Invoke();
        }

        /// <summary>
        /// Enter Play Mode runs without a domain reload, so static subscribers would survive into the next
        /// session and fire on destroyed objects. Clearing them before the first scene load prevents that.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            OnBoosterSelected = null;
            OnBoosterCountChanged = null;
            OnPreBoosterCountChanged = null;
            OnBoosterUsed = null;
            OnBoosterPurchaseRequested = null;
            OnBoosterButtonClicked = null;
            OnQuickBuyRequested = null;
            OnCoinPackPurchaseRequested = null;
            OnBundlePurchaseRequested = null;
            OnDailyRewardClaimRequested = null;
            OnCoinsChanged = null;
            OnLivesChanged = null;
            OnLivesTimerChanged = null;
            OnNoLivesLeft = null;
        }
    }
}
