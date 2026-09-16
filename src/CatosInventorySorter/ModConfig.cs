using BepInEx.Configuration;

namespace CatosInventorySorter
{
    internal static class ModConfig
    {
        internal static ConfigEntry<string> SortMode;
        internal static ConfigEntry<bool> Descending;
        internal static ConfigEntry<bool> WeightDescending;

        internal static void Bind(ConfigFile config)
        {
            SortMode = config.Bind("Sorting", "SortMode", "Type",
                "Sort criterion: Type, Name, Weight, or Quantity. Changes are picked up while the game is running.");
            Descending = config.Bind("Sorting", "Descending", false,
                "Reverse the selected sort order for Type, Name, and Quantity. Changes are picked up while the game is running.");
            WeightDescending = config.Bind("Sorting", "WeightDescending", true,
                "Sort heaviest total stacks first by default. Set false for lightest first. Changes are picked up while the game is running.");
            InventorySortButton.Configure(config);
        }
    }
}
