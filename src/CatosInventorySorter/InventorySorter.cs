using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CatosInventorySorter
{
    internal enum SortCriterion { Weight, Quantity }

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
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || item.m_shared == null) return false;
                int x = item.m_gridPos.x;
                int y = item.m_gridPos.y;
                if (x < 0 || y < 0 || x >= width || y >= height) return false;
                long key = PositionKey(x, y);
                if (!occupied.Add(key)) return false;

                // Keep the hotbar row, equipped items, and game-bound items fixed.
                if (y == 0 || locked.Contains(item) || player.IsItemEquiped(item))
                    reservedPositions.Add(key);
                else
                    movable.Add(item);
            }

            movable.Sort((a, b) => Compare(a, b, criterion, descending));
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
            SortCriterion criterion, bool descending)
        {
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

        internal static SortCriterion GetCriterion()
        {
            return Enum.TryParse(ModConfig.SortMode.Value, true, out SortCriterion criterion)
                ? criterion
                : SortCriterion.Weight;
        }
    }
}
