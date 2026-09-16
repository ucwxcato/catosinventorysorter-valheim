using BepInEx.Configuration;

namespace CatosInventorySorter
{
    internal static class ModConfig
    {
        internal static ConfigEntry<string> SortMode;
        internal static ConfigEntry<bool> WeightDescending;
        internal static ConfigEntry<bool> QuantityDescending;

        internal static void Bind(ConfigFile config)
        {
            SortMode = config.Bind("Sorting", "SortMode", "Weight",
                "Sort criterion: Weight or Quantity. Changes are picked up while the game is running.");
            WeightDescending = config.Bind("Sorting", "WeightDescending", true,
                "Sort heaviest total stacks first by default. Set false for lightest first. Changes are picked up while the game is running.");
            QuantityDescending = config.Bind("Sorting", "QuantityDescending", true,
                "Sort highest stack quantities first by default. Set false for lowest first. Changes are picked up while the game is running.");
            InventorySortButton.Configure(config);
        }
    }
}
