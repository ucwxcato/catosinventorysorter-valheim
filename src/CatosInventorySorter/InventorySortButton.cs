using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatosInventorySorter
{
    internal static class InventorySortButton
    {
        private static Button _button;
        private static TMP_Text _label;
        private static ConfigEntry<KeyboardShortcut> _sortShortcut;
        private static readonly Vector3[] _corners = new Vector3[4];

        internal static void Attach(InventoryGui gui)
        {
            if (!gui || _button) return;

            Transform playerPanel = AccessTools.Field(typeof(InventoryGui), "m_player")?.GetValue(gui) as Transform;
            Transform overlayRoot = AccessTools.Field(typeof(InventoryGui), "m_inventoryRoot")?.GetValue(gui) as Transform;
            if (!playerPanel || !overlayRoot) return;

            var buttonObject = new GameObject("CatosInventorySorterButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(overlayRoot, false);
            _button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.23f, 0.17f, 0.98f);
            _button.targetGraphic = image;
            _button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = _button.colors;
            colors.normalColor = new Color(0.82f, 0.92f, 0.82f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.6f, 0.8f, 0.6f, 1f);
            colors.selectedColor = Color.white;
            _button.colors = colors;
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            _button.onClick.AddListener(SortPlayerInventory);

            RectTransform rect = _button.transform as RectTransform;
            if (rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(150f, 34f);
                rect.localScale = Vector3.one;
            }
            _button.transform.SetAsLastSibling();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(_button.transform, false);
            RectTransform labelRect = labelObject.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _label = labelObject.GetComponent<TextMeshProUGUI>();
            TMP_Text nativeText = AccessTools.Field(typeof(InventoryGui), "m_containerName")?.GetValue(gui) as TMP_Text;
            if (nativeText)
            {
                _label.font = nativeText.font;
                _label.fontSharedMaterial = nativeText.fontSharedMaterial;
                _label.fontSize = nativeText.fontSize;
            }
            else
            {
                _label.font = TMP_Settings.defaultFontAsset;
                _label.fontSize = 16f;
            }
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.text = "Sort: " + InventorySorter.GetCriterion();
            PositionBesideInventory(gui, playerPanel, overlayRoot as RectTransform, rect);
            Plugin.Log?.LogInfo("Inventory sort button attached beside the player inventory grid.");
        }

        internal static void Configure(ConfigFile config)
        {
            _sortShortcut = config.Bind("Controls", "SortShortcut", new KeyboardShortcut(KeyCode.F6),
                "Sort the local player's inventory while the inventory screen is open.");
        }

        internal static void Update()
        {
            InventoryGui gui = InventoryGui.instance;
            if (!gui) return;
            // Awake can run before this plugin's patch is applied on some UI
            // creation paths, so retry while the native inventory is available.
            if (!_button) Attach(gui);
            if (_button)
            {
                Transform playerPanel = AccessTools.Field(typeof(InventoryGui), "m_player")?.GetValue(gui) as Transform;
                Transform overlayRoot = AccessTools.Field(typeof(InventoryGui), "m_inventoryRoot")?.GetValue(gui) as Transform;
                PositionBesideInventory(gui, playerPanel, overlayRoot as RectTransform, _button.transform as RectTransform);
                RefreshLabel();
            }
            if (!InventoryGui.IsVisible()) return;
            if (_sortShortcut != null && _sortShortcut.Value.IsDown()) SortPlayerInventory();
        }

        internal static void Destroy()
        {
            if (_button) Object.Destroy(_button.gameObject);
            _button = null;
            _label = null;
        }

        private static void SortPlayerInventory()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (!player) return;
                SortCriterion criterion = InventorySorter.GetCriterion();
                bool descending = criterion == SortCriterion.Weight
                    ? ModConfig.WeightDescending.Value
                    : ModConfig.Descending.Value;
                bool changed = InventorySorter.Sort(player.GetInventory(), player, criterion, descending);
                if (_label) _label.text = "Sort: " + criterion;
                Plugin.Log?.LogDebug(changed
                    ? $"Sorted player inventory by {criterion} (descending={descending})."
                    : "Player inventory already matches the selected sort order.");
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogError($"Inventory sort failed safely: {ex}");
            }
        }

        private static void RefreshLabel()
        {
            if (!_label) return;
            string desired = "Sort: " + InventorySorter.GetCriterion();
            if (_label.text != desired) _label.text = desired;
        }

        private static void PositionBesideInventory(InventoryGui gui, Transform playerPanel,
            RectTransform overlayRoot, RectTransform buttonRect)
        {
            if (!gui || !playerPanel || !overlayRoot || !buttonRect) return;

            RectTransform reference = AccessTools.Field(typeof(InventoryGui), "m_playerGrid")?.GetValue(gui) is InventoryGrid grid
                ? AccessTools.Field(typeof(InventoryGrid), "m_gridRoot")?.GetValue(grid) as RectTransform
                : null;
            if (!reference) reference = playerPanel as RectTransform;
            if (!reference) return;

            reference.GetWorldCorners(_corners);
            Vector3 topRight = overlayRoot.InverseTransformPoint(_corners[2]);
            Rect bounds = overlayRoot.rect;
            float width = buttonRect.rect.width > 0f ? buttonRect.rect.width : 150f;
            float height = buttonRect.rect.height > 0f ? buttonRect.rect.height : 34f;
            float x = Mathf.Clamp(topRight.x + 12f, bounds.xMin + 8f, bounds.xMax - width - 8f);
            float y = Mathf.Clamp(topRight.y, bounds.yMin + height + 8f, bounds.yMax - 8f);
            buttonRect.localPosition = new Vector3(x, y, 0f);
        }
    }
}
