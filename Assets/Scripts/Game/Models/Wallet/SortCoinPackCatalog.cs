namespace AllSorted
{
    /// <summary>Display prices for the no-charge simulated Shop packs.</summary>
    public static class SortCoinPackCatalog
    {
        public const int Count = 6;
        private static readonly int[] Coins = { 1000, 5000, 10000, 25000, 50000, 100000 };
        private static readonly string[] Prices = { "$1.99", "$8.99", "$16.99", "$34.99", "$64.99", "$99.99" };
        private static readonly string[] DisplayAmounts = { "1.000", "5.000", "10.000", "25.000", "50.000", "100.000" };

        public static int GetCoins(int index) { return IsValid(index) ? Coins[index] : 0; }
        public static string GetPrice(int index) { return IsValid(index) ? Prices[index] : string.Empty; }
        public static string GetDisplayAmount(int index) { return IsValid(index) ? DisplayAmounts[index] : string.Empty; }
        private static bool IsValid(int index) { return index >= 0 && index < Count; }
    }
}
