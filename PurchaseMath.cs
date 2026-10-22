using System;

namespace RepeatBuy;

public enum StopMode
{
    FixedCount = 0,
    UntilOwned = 1,
    GilFloor = 2,
    InventoryFull = 3
}

public readonly record struct PurchaseLimits(long ByMode, long ByGil, long BySpace, int Result)
{
    public bool WasCapped => ByMode > Result;
}

public static class PurchaseMath
{
    public const int MaxPerTransaction = 99;

    public static long StackCapacity(int freeSlots, int maxStack, int partialRoom)
    {
        long stack = Math.Max(1, maxStack);
        return (long)Math.Max(0, freeSlots) * stack + Math.Max(0, partialRoom);
    }

    public static bool GoalMet(
        StopMode mode,
        int fixedCount,
        int untilOwned,
        int owned,
        int delivered,
        long gil,
        long gilFloor,
        long stackRoom)
    {
        return mode switch
        {
            StopMode.FixedCount => delivered >= Math.Max(0, fixedCount),
            StopMode.UntilOwned => owned >= Math.Max(0, untilOwned),
            StopMode.GilFloor => gil <= gilFloor,
            StopMode.InventoryFull => stackRoom <= 0,
            _ => true
        };
    }

    /// <summary>
    /// How many items the next transaction may request.
    /// Pass <paramref name="maxPerTransaction"/> as <see cref="int.MaxValue"/> and
    /// <paramref name="shopStack"/> as 0 when estimating a whole run rather than one batch.
    /// </summary>
    public static PurchaseLimits Compute(
        StopMode mode,
        int fixedCount,
        int untilOwned,
        int owned,
        int delivered,
        long gil,
        long gilFloor,
        long unitPrice,
        long stackRoom,
        int shopStack,
        int maxPerTransaction = MaxPerTransaction)
    {
        long byGil = unitPrice <= 0 ? 0 : gil / unitPrice;
        long bySpace = Math.Max(0, stackRoom);
        long byMode = mode switch
        {
            StopMode.FixedCount => Math.Max(0, (long)fixedCount - Math.Max(0, delivered)),
            StopMode.UntilOwned => Math.Max(0, (long)untilOwned - Math.Max(0, owned)),
            StopMode.GilFloor => unitPrice <= 0 || gil <= gilFloor ? 0 : (gil - gilFloor) / unitPrice,
            StopMode.InventoryFull => bySpace,
            _ => 0
        };

        long limit = Math.Min(byGil, Math.Min(byMode, bySpace));
        if (shopStack > 0)
            limit = Math.Min(limit, shopStack);
        if (maxPerTransaction > 0)
            limit = Math.Min(limit, maxPerTransaction);
        if (limit < 0)
            limit = 0;
        if (limit > int.MaxValue)
            limit = int.MaxValue;

        return new PurchaseLimits(byMode, byGil, bySpace, (int)limit);
    }
}
