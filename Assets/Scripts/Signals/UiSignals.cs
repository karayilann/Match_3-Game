using System;
using System.Collections.Generic;
using Core.Data.Level;
using UnityEngine;

namespace Signals
{
    public static class UiSignals
    {
        public static event Action OnOpenSettings;
        public static event Action OnOpenShop;
        public static event Action OnOpenDailyReward;
        public static event Action OnOpenInventory;
        public static event Action<bool> OnGoalPanelToggled;
        public static event Action<bool> OnSfxToggled;
        public static event Action<bool> OnMusicToggled;
        public static event Action<bool> OnHapticsToggled;
        public static event Action<int> OnLevelNodeClicked;
        public static event Action<int> OnStartLevel;
        public static event Action<int, PreGameBoosterFlags> OnStartLevelWithBoosters;
        public static event Action OnReturnToMenu;
        public static event Action OnLeaveLevelRequested;
        public static event Action OnLeaveLevelConfirmRequested;
        public static event Action<int, IReadOnlyList<byte>> OnProgressChanged;
        public static event Action<bool, bool, bool> OnSettingsLoaded;

        public static void RaiseOpenSettings()
        {
            OnOpenSettings?.Invoke();
        }

        public static void RaiseOpenShop()
        {
            OnOpenShop?.Invoke();
        }

        public static void RaiseOpenDailyReward()
        {
            OnOpenDailyReward?.Invoke();
        }

        public static void RaiseOpenInventory()
        {
            OnOpenInventory?.Invoke();
        }

        public static void RaiseGoalPanelToggled(bool isOpen)
        {
            OnGoalPanelToggled?.Invoke(isOpen);
        }

        public static void RaiseSfxToggled(bool isEnabled)
        {
            OnSfxToggled?.Invoke(isEnabled);
        }

        public static void RaiseMusicToggled(bool isEnabled)
        {
            OnMusicToggled?.Invoke(isEnabled);
        }

        public static void RaiseHapticsToggled(bool isEnabled)
        {
            OnHapticsToggled?.Invoke(isEnabled);
        }

        public static void RaiseLevelNodeClicked(int levelIndex)
        {
            OnLevelNodeClicked?.Invoke(levelIndex);
        }

        public static void RaiseStartLevel(int levelIndex)
        {
            OnStartLevel?.Invoke(levelIndex);
        }

        public static void RaiseStartLevelWithBoosters(int levelIndex, PreGameBoosterFlags boosters)
        {
            OnStartLevelWithBoosters?.Invoke(levelIndex, boosters);
        }

        public static void RaiseReturnToMenu()
        {
            OnReturnToMenu?.Invoke();
        }

        public static void RaiseLeaveLevelRequested()
        {
            OnLeaveLevelRequested?.Invoke();
        }

        public static void RaiseLeaveLevelConfirmRequested()
        {
            OnLeaveLevelConfirmRequested?.Invoke();
        }

        public static void RaiseProgressChanged(int unlockedLevel, IReadOnlyList<byte> starsByLevel)
        {
            OnProgressChanged?.Invoke(unlockedLevel, starsByLevel);
        }

        public static void RaiseSettingsLoaded(bool isSfxEnabled, bool isMusicEnabled, bool isHapticsEnabled)
        {
            OnSettingsLoaded?.Invoke(isSfxEnabled, isMusicEnabled, isHapticsEnabled);
        }

        /// <summary>
        /// Enter Play Mode runs without a domain reload, so static subscribers would survive into the next
        /// session and fire on destroyed objects. Clearing them before the first scene load prevents that.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            OnOpenSettings = null;
            OnOpenShop = null;
            OnOpenDailyReward = null;
            OnOpenInventory = null;
            OnGoalPanelToggled = null;
            OnSfxToggled = null;
            OnMusicToggled = null;
            OnHapticsToggled = null;
            OnLevelNodeClicked = null;
            OnStartLevel = null;
            OnStartLevelWithBoosters = null;
            OnReturnToMenu = null;
            OnLeaveLevelRequested = null;
            OnLeaveLevelConfirmRequested = null;
            OnProgressChanged = null;
            OnSettingsLoaded = null;
        }
    }
}
