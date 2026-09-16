using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CatosInventorySorter
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.catosaur.catosinventorysorter";
        public const string Name = "Catos Inventory Sorter";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);
            ConfigHotReload.Initialize(Config);
            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(InventoryGuiPatches));
            Logger.LogInfo($"{Name} {Version} client-only loaded.");
        }

        private void OnDestroy()
        {
            InventorySortButton.Destroy();
            _harmony?.UnpatchAll(Guid);
        }

        private void Update()
        {
            ConfigHotReload.Update();
            InventorySortButton.Update();
        }
    }
}
