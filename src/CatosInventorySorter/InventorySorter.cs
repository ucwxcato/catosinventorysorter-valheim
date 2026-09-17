using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CatosInventorySorter
{
    internal enum SortCriterion { Weight, Quantity }
    internal enum SpecialItemsMode { Ignore, SortFirst }
    internal enum SpecialItemCategory { Other, Armor, Food, Arrows }

    internal static class InventorySorter
    {
        private static readonly MethodInfo ChangedMethod = AccessTools.Method(
            typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });
        private sealed class Slot
        {
            internal int X;
            internal int Y;
            internal Slot(int x, int y) { X = x; Y = y; }
        }

        internal static bool Sort(Inventory inventory, Player player, SortCriterion criterion, bool descending)
        {
            if (inventory == null || player == null || ChangedMethod == null) return false;
            int width = inventory.GetWidth();
            int height = inventory.GetHeight();
            if (width <= 0 || height <= 1) return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null || items.Count < 2) return false;

            var locked = new HashSet<ItemDrop.ItemData>();
            var bound = new List<ItemDrop.ItemData>();
            inventory.GetBoundItems(bound);
            foreach (ItemDrop.ItemData item in bound)
                if (item != null) locked.Add(item);

            var movable = new List<ItemDrop.ItemData>();
            var reservedPositions = new HashSet<long>();
            var occupied = new HashSet<long>();
            SpecialItemsMode specialItemsMode = GetSpecialItemsMode();
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || item.m_shared == null) return false;
                int x = item.m_gridPos.x;
                int y = item.m_gridPos.y;
                if (x < 0 || y < 0 || x >= width || y >= height) return false;
                long key = PositionKey(x, y);
                if (!occupied.Add(key)) return false;

                // Keep the hotbar row, equipped items, and game-bound items fixed.
                // Special items are either fixed or included in the sort according
                // to the user's config.
                if (y == 0 || locked.Contains(item) || player.IsItemEquiped(item) ||
                    (specialItemsMode == SpecialItemsMode.Ignore && IsSpecialItem(item)))
                    reservedPositions.Add(key);
                else
                    movable.Add(item);
            }

            movable.Sort((a, b) => Compare(a, b, criterion, descending, specialItemsMode));
            var destinations = new List<Slot>();
            for (int y = 1; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!reservedPositions.Contains(PositionKey(x, y)))
                        destinations.Add(new Slot(x, y));
            if (destinations.Count < movable.Count) return false;

            bool changed = false;
            for (int i = 0; i < movable.Count; i++)
            {
                ItemDrop.ItemData item = movable[i];
                Slot target = destinations[i];
                if (item.m_gridPos.x != target.X || item.m_gridPos.y != target.Y)
                {
                    item.m_gridPos = new Vector2i(target.X, target.Y);
                    changed = true;
                }
            }

            if (changed)
            {
                try
                {
                    // Changed is private in the target Valheim build. It is the native
                    // notification path used by AddItem/RemoveItem after inventory edits.
                    ChangedMethod.Invoke(inventory, new object[] { true, false });
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Valheim inventory change notification failed: {ex}");
                    return false;
                }
            }
            return changed;
        }

        private static int Compare(ItemDrop.ItemData left, ItemDrop.ItemData right,
            SortCriterion criterion, bool descending, SpecialItemsMode specialItemsMode)
        {
            if (specialItemsMode == SpecialItemsMode.SortFirst)
            {
                int categoryResult = GetSpecialItemRank(left).CompareTo(GetSpecialItemRank(right));
                if (categoryResult != 0) return categoryResult;
            }

            int result;
            switch (criterion)
            {
                case SortCriterion.Weight:
                    result = TotalWeight(left).CompareTo(TotalWeight(right));
                    break;
                default:
                    result = left.m_stack.CompareTo(right.m_stack);
                    break;
            }

            if (descending) result = -result;
            if (result != 0) return result;
            return left.m_gridPos.y != right.m_gridPos.y
                ? left.m_gridPos.y.CompareTo(right.m_gridPos.y)
                : left.m_gridPos.x.CompareTo(right.m_gridPos.x);
        }

        private static float TotalWeight(ItemDrop.ItemData item) => item.m_shared.m_weight * item.m_stack;
        private static long PositionKey(int x, int y) => ((long)y << 32) | (uint)x;

        private static bool IsSpecialItem(ItemDrop.ItemData item)
        {
            return GetSpecialItemCategory(item) != SpecialItemCategory.Other;
        }

        private static SpecialItemCategory GetSpecialItemCategory(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return SpecialItemCategory.Other;

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Ammo:
                    return SpecialItemCategory.Arrows;
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return SpecialItemCategory.Armor;
                default:
                    // Food is identified by its food value rather than the broad
                    // Consumable category, which also contains non-food items.
                    return item.m_shared.m_food > 0f
                        ? SpecialItemCategory.Food
                        : SpecialItemCategory.Other;
            }
        }

        private static int GetSpecialItemRank(ItemDrop.ItemData item)
        {
            SpecialItemCategory category = GetSpecialItemCategory(item);
            string configuredOrder = ModConfig.SpecialItemsOrder != null
                ? ModConfig.SpecialItemsOrder.Value
                : "Armor,Food,Arrows";
            string[] order = configuredOrder.Split(',');
            int rank = 0;
            foreach (string entry in order)
            {
                SpecialItemCategory configuredCategory;
                if (!Enum.TryParse(entry.Trim(), true, out configuredCategory) ||
                    configuredCategory == SpecialItemCategory.Other)
                    continue;
                if (configuredCategory == category) return rank;
                rank++;
            }

            // Invalid or omitted categories sort after explicitly ordered special
            // categories, while ordinary items always remain last.
            return category == SpecialItemCategory.Other ? int.MaxValue : rank;
        }

        private static SpecialItemsMode GetSpecialItemsMode()
        {
            SpecialItemsMode mode;
            return ModConfig.SpecialItemsMode != null &&
                Enum.TryParse(ModConfig.SpecialItemsMode.Value, true, out mode)
                ? mode
                : SpecialItemsMode.Ignore;
        }

        internal static SortCriterion GetCriterion()
        {
            return Enum.TryParse(ModConfig.SortMode.Value, true, out SortCriterion criterion)
                ? criterion
                : SortCriterion.Weight;
        }
    }
}
