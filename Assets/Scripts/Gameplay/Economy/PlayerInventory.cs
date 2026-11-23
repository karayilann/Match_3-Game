using Core.Data.Boosters;
using Core.Data.Level;
using Gameplay.Persistence;
using Signals;

namespace Gameplay.Economy
{
    /// <summary>
    /// Single owner of coins and booster stock. Every screen used to read and write these keys on its own, so an
    /// in-memory copy elsewhere could overwrite a purchase. Keys are kept from the old layout so existing saves load.
    /// </summary>
    public sealed class PlayerInventory
    {
        public const int DEFAULT_COINS = 1250;
        public const int DEFAULT_ITEM_COUNT = 3;

        private const string COINS_KEY = "Player_Coins";
        private const string HAMMER_KEY = "Booster_Hammer";
        private const string GLOVE_KEY = "Booster_Glove";
        private const string SHUFFLE_KEY = "Booster_Shuffle";
        private const string ROCKET_BOMB_KEY = "PreBooster_RocketBomb";
        private const string COLOR_BOMB_KEY = "PreBooster_ColorBomb";
        private const string EXTRA_MOVES_KEY = "PreBooster_ExtraMoves";

        private readonly IKeyValueStore _store;
        private int _coins;
        private int _hammerCount;
        private int _gloveCount;
        private int _shuffleCount;
        private int _rocketBombCount;
        private int _colorBombCount;
        private int _extraMovesCount;

        public int Coins => _coins;

        public PlayerInventory(IKeyValueStore store)
        {
            _store = store;
            _coins = store.GetInt(COINS_KEY, DEFAULT_COINS);
            _hammerCount = store.GetInt(HAMMER_KEY, DEFAULT_ITEM_COUNT);
            _gloveCount = store.GetInt(GLOVE_KEY, DEFAULT_ITEM_COUNT);
            _shuffleCount = store.GetInt(SHUFFLE_KEY, DEFAULT_ITEM_COUNT);
            _rocketBombCount = store.GetInt(ROCKET_BOMB_KEY, DEFAULT_ITEM_COUNT);
            _colorBombCount = store.GetInt(COLOR_BOMB_KEY, DEFAULT_ITEM_COUNT);
            _extraMovesCount = store.GetInt(EXTRA_MOVES_KEY, DEFAULT_ITEM_COUNT);
        }

        public void BroadcastAll()
        {
            EconomySignals.RaiseCoinsChanged(_coins);
            EconomySignals.RaiseBoosterCountChanged(BoosterType.Hammer, _hammerCount);
            EconomySignals.RaiseBoosterCountChanged(BoosterType.Glove, _gloveCount);
            EconomySignals.RaiseBoosterCountChanged(BoosterType.Shuffle, _shuffleCount);
            EconomySignals.RaisePreBoosterCountChanged(PreGameBoosterFlags.RocketBomb, _rocketBombCount);
            EconomySignals.RaisePreBoosterCountChanged(PreGameBoosterFlags.ColorBomb, _colorBombCount);
            EconomySignals.RaisePreBoosterCountChanged(PreGameBoosterFlags.ExtraMoves, _extraMovesCount);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetCoins(_coins + amount);
            _store.Save();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || _coins < amount)
            {
                return false;
            }

            SetCoins(_coins - amount);
            _store.Save();
            return true;
        }

        public int GetBoosterCount(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hammer:
                    return _hammerCount;
                case BoosterType.Glove:
                    return _gloveCount;
                case BoosterType.Shuffle:
                    return _shuffleCount;
                default:
                    return 0;
            }
        }

        public void AddBooster(BoosterType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetBoosterCount(type, GetBoosterCount(type) + amount);
            _store.Save();
        }

        public bool TryConsumeBooster(BoosterType type)
        {
            int count = GetBoosterCount(type);
            if (count <= 0)
            {
                return false;
            }

            SetBoosterCount(type, count - 1);
            _store.Save();
            return true;
        }

        /// <summary>
        /// Expects a single flag. Combined flags have no single count, so they read as zero.
        /// </summary>
        public int GetPreBoosterCount(PreGameBoosterFlags flag)
        {
            switch (flag)
            {
                case PreGameBoosterFlags.RocketBomb:
                    return _rocketBombCount;
                case PreGameBoosterFlags.ColorBomb:
                    return _colorBombCount;
                case PreGameBoosterFlags.ExtraMoves:
                    return _extraMovesCount;
                default:
                    return 0;
            }
        }

        public void AddPreBooster(PreGameBoosterFlags flag, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetPreBoosterCount(flag, GetPreBoosterCount(flag) + amount);
            _store.Save();
        }

        /// <summary>
        /// Consumes one of each requested pre-game booster that is in stock and returns only the flags that were
        /// paid for, so a stale selection from the start popup can never grant a booster the player does not own.
        /// </summary>
        public PreGameBoosterFlags ConsumePreBoosters(PreGameBoosterFlags requested)
        {
            PreGameBoosterFlags granted = PreGameBoosterFlags.None;
            granted |= TryTakePreBooster(requested, PreGameBoosterFlags.RocketBomb);
            granted |= TryTakePreBooster(requested, PreGameBoosterFlags.ColorBomb);
            granted |= TryTakePreBooster(requested, PreGameBoosterFlags.ExtraMoves);

            if (granted != PreGameBoosterFlags.None)
            {
                _store.Save();
            }

            return granted;
        }

        private PreGameBoosterFlags TryTakePreBooster(PreGameBoosterFlags requested, PreGameBoosterFlags flag)
        {
            if ((requested & flag) == 0)
            {
                return PreGameBoosterFlags.None;
            }

            int count = GetPreBoosterCount(flag);
            if (count <= 0)
            {
                return PreGameBoosterFlags.None;
            }

            SetPreBoosterCount(flag, count - 1);
            return flag;
        }

        private void SetCoins(int value)
        {
            _coins = value;
            _store.SetInt(COINS_KEY, value);
            EconomySignals.RaiseCoinsChanged(value);
        }

        private void SetBoosterCount(BoosterType type, int value)
        {
            switch (type)
            {
                case BoosterType.Hammer:
                    _hammerCount = value;
                    _store.SetInt(HAMMER_KEY, value);
                    break;
                case BoosterType.Glove:
                    _gloveCount = value;
                    _store.SetInt(GLOVE_KEY, value);
                    break;
                case BoosterType.Shuffle:
                    _shuffleCount = value;
                    _store.SetInt(SHUFFLE_KEY, value);
                    break;
                default:
                    return;
            }

            EconomySignals.RaiseBoosterCountChanged(type, value);
        }

        private void SetPreBoosterCount(PreGameBoosterFlags flag, int value)
        {
            switch (flag)
            {
                case PreGameBoosterFlags.RocketBomb:
                    _rocketBombCount = value;
                    _store.SetInt(ROCKET_BOMB_KEY, value);
                    break;
                case PreGameBoosterFlags.ColorBomb:
                    _colorBombCount = value;
                    _store.SetInt(COLOR_BOMB_KEY, value);
                    break;
                case PreGameBoosterFlags.ExtraMoves:
                    _extraMovesCount = value;
                    _store.SetInt(EXTRA_MOVES_KEY, value);
                    break;
                default:
                    return;
            }

            EconomySignals.RaisePreBoosterCountChanged(flag, value);
        }
    }
}
