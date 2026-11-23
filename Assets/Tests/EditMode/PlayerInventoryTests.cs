using Core.Data.Boosters;
using Core.Data.Level;
using Gameplay.Economy;
using NUnit.Framework;
using Signals;

namespace Tests
{
    public sealed class PlayerInventoryTests
    {
        private InMemoryKeyValueStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryKeyValueStore();
        }

        [TearDown]
        public void TearDown()
        {
            EconomySignals.Reset();
        }

        [Test]
        public void EmptyStore_UsesDefaults()
        {
            PlayerInventory inventory = new PlayerInventory(_store);

            Assert.AreEqual(PlayerInventory.DEFAULT_COINS, inventory.Coins);
            Assert.AreEqual(PlayerInventory.DEFAULT_ITEM_COUNT, inventory.GetBoosterCount(BoosterType.Hammer));
            Assert.AreEqual(PlayerInventory.DEFAULT_ITEM_COUNT, inventory.GetPreBoosterCount(PreGameBoosterFlags.ColorBomb));
        }

        [Test]
        public void TrySpendCoins_NotEnough_LeavesBalance()
        {
            PlayerInventory inventory = new PlayerInventory(_store);

            bool spent = inventory.TrySpendCoins(PlayerInventory.DEFAULT_COINS + 1);

            Assert.IsFalse(spent);
            Assert.AreEqual(PlayerInventory.DEFAULT_COINS, inventory.Coins);
        }

        [Test]
        public void TrySpendCoins_Enough_PersistsAndSignals()
        {
            PlayerInventory inventory = new PlayerInventory(_store);
            int signalled = -1;
            EconomySignals.OnCoinsChanged += value => signalled = value;

            bool spent = inventory.TrySpendCoins(250);

            Assert.IsTrue(spent);
            Assert.AreEqual(1000, inventory.Coins);
            Assert.AreEqual(1000, signalled);
            Assert.AreEqual(1000, new PlayerInventory(_store).Coins);
        }

        [Test]
        public void TryConsumeBooster_Empty_ReturnsFalse()
        {
            PlayerInventory inventory = new PlayerInventory(_store);
            for (int i = 0; i < PlayerInventory.DEFAULT_ITEM_COUNT; i++)
            {
                Assert.IsTrue(inventory.TryConsumeBooster(BoosterType.Glove));
            }

            Assert.IsFalse(inventory.TryConsumeBooster(BoosterType.Glove));
            Assert.AreEqual(0, inventory.GetBoosterCount(BoosterType.Glove));
        }

        [Test]
        public void AddBooster_ThenConsume_KeepsPurchasedStock()
        {
            PlayerInventory inventory = new PlayerInventory(_store);

            inventory.AddBooster(BoosterType.Hammer, 2);
            inventory.TryConsumeBooster(BoosterType.Hammer);

            Assert.AreEqual(PlayerInventory.DEFAULT_ITEM_COUNT + 1, inventory.GetBoosterCount(BoosterType.Hammer));
            Assert.AreEqual(PlayerInventory.DEFAULT_ITEM_COUNT + 1, new PlayerInventory(_store).GetBoosterCount(BoosterType.Hammer));
        }

        [Test]
        public void ConsumePreBoosters_GrantsOnlyStockedFlags()
        {
            PlayerInventory inventory = new PlayerInventory(_store);
            for (int i = 0; i < PlayerInventory.DEFAULT_ITEM_COUNT; i++)
            {
                inventory.ConsumePreBoosters(PreGameBoosterFlags.ColorBomb);
            }

            PreGameBoosterFlags granted = inventory.ConsumePreBoosters(PreGameBoosterFlags.ColorBomb | PreGameBoosterFlags.ExtraMoves);

            Assert.AreEqual(PreGameBoosterFlags.ExtraMoves, granted);
            Assert.AreEqual(0, inventory.GetPreBoosterCount(PreGameBoosterFlags.ColorBomb));
            Assert.AreEqual(PlayerInventory.DEFAULT_ITEM_COUNT - 1, inventory.GetPreBoosterCount(PreGameBoosterFlags.ExtraMoves));
        }

        [Test]
        public void ConsumePreBoosters_None_DoesNotSave()
        {
            PlayerInventory inventory = new PlayerInventory(_store);

            PreGameBoosterFlags granted = inventory.ConsumePreBoosters(PreGameBoosterFlags.None);

            Assert.AreEqual(PreGameBoosterFlags.None, granted);
            Assert.AreEqual(0, _store.SaveCount);
        }
    }
}
