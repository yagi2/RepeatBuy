using Xunit;

namespace RepeatBuy.Tests;

public class PurchaseMathTests
{
    [Fact]
    public void FixedCount_IsCappedByGilAndBagSpace()
    {
        var limits = PurchaseMath.Compute(
            StopMode.FixedCount,
            fixedCount: 10,
            untilOwned: 0,
            owned: 0,
            delivered: 0,
            gil: 250,
            gilFloor: 0,
            unitPrice: 100,
            stackRoom: 4,
            shopStack: 0,
            maxPerTransaction: int.MaxValue);

        Assert.Equal(2, limits.Result);
        Assert.True(limits.WasCapped);
    }

    [Fact]
    public void EmptyBag_DoesNotForceABuyOfOne()
    {
        var limits = PurchaseMath.Compute(
            StopMode.FixedCount,
            fixedCount: 4,
            untilOwned: 0,
            owned: 0,
            delivered: 0,
            gil: 10_000,
            gilFloor: 0,
            unitPrice: 10,
            stackRoom: 0,
            shopStack: 99);

        Assert.Equal(0, limits.Result);
    }

    [Fact]
    public void PartialStack_AddsRoomBeyondFreeSlots()
    {
        long room = PurchaseMath.StackCapacity(freeSlots: 0, maxStack: 99, partialRoom: 5);
        var limits = PurchaseMath.Compute(
            StopMode.FixedCount,
            fixedCount: 20,
            untilOwned: 0,
            owned: 94,
            delivered: 0,
            gil: 100_000,
            gilFloor: 0,
            unitPrice: 1,
            stackRoom: room,
            shopStack: 99);

        Assert.Equal(5, limits.Result);
    }

    [Fact]
    public void DeliveredCount_ReducesTheNextFixedBatch()
    {
        var limits = PurchaseMath.Compute(
            StopMode.FixedCount,
            fixedCount: 10,
            untilOwned: 0,
            owned: 3,
            delivered: 6,
            gil: 100_000,
            gilFloor: 0,
            unitPrice: 1,
            stackRoom: 99,
            shopStack: 99);

        Assert.Equal(4, limits.Result);
    }

    [Fact]
    public void ShopStack_CapsASingleBatch()
    {
        var limits = PurchaseMath.Compute(
            StopMode.FixedCount,
            fixedCount: 50,
            untilOwned: 0,
            owned: 0,
            delivered: 0,
            gil: 100_000,
            gilFloor: 0,
            unitPrice: 1,
            stackRoom: 50,
            shopStack: 1);

        Assert.Equal(1, limits.Result);
    }

    [Fact]
    public void UntilOwned_BuysOnlyTheGap()
    {
        var limits = PurchaseMath.Compute(
            StopMode.UntilOwned,
            fixedCount: 0,
            untilOwned: 10,
            owned: 7,
            delivered: 0,
            gil: 100_000,
            gilFloor: 0,
            unitPrice: 5,
            stackRoom: 99,
            shopStack: 0,
            maxPerTransaction: int.MaxValue);

        Assert.Equal(3, limits.Result);
    }

    [Fact]
    public void GilFloor_StopsAtTheThreshold()
    {
        var limits = PurchaseMath.Compute(
            StopMode.GilFloor,
            fixedCount: 0,
            untilOwned: 0,
            owned: 0,
            delivered: 0,
            gil: 1_000,
            gilFloor: 400,
            unitPrice: 100,
            stackRoom: 99,
            shopStack: 0,
            maxPerTransaction: int.MaxValue);

        Assert.Equal(6, limits.Result);
        Assert.True(PurchaseMath.GoalMet(
            StopMode.GilFloor, 0, 0, 0, 6, gil: 400, gilFloor: 400, stackRoom: 99));
    }

    [Fact]
    public void InventoryFull_StopsWhenNothingFits()
    {
        Assert.True(PurchaseMath.GoalMet(
            StopMode.InventoryFull, 0, 0, 10, 10, gil: 500, gilFloor: 0, stackRoom: 0));
        Assert.False(PurchaseMath.GoalMet(
            StopMode.InventoryFull, 0, 0, 10, 10, gil: 500, gilFloor: 0, stackRoom: 1));
    }
}
