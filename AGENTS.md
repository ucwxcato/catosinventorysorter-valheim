# CatosInventorySorter development

This is a client-only Valheim/BepInEx mod. The dedicated server does not install this DLL. Keep `[BepInProcess("valheim.exe")]` on the plugin.

## Environment

Use Valheim 1.0.7 / network 39 / Unity 6000.0.75.2503836, BepInExPack Valheim 5.4.2350 / BepInEx 5.4.23.5, and .NET Framework 4.8. Recheck installed assemblies before implementation changes. Keep game-managed references and BepInEx core references in separate source directories; never commit `lib/` DLLs.

## Build and local test endpoint

Build with `scripts/build.ps1`. The `TEST_SERVER/` launcher follows the CatosChestViewer harness: port 2462, password `696969`, `-public 0`, save root `C:\Users\magni\Downloads`, world `Dedicated`, and existing world source `C:\Users\magni\Downloads\Dedicated`, exposed through `worlds_local\Dedicated`. Do not copy or regenerate the world. Copy `TEST_SERVER/adminlist.txt` into the active save root before launching.

Deploy the DLL only to the client profile named `CatosInventorySorter`; never deploy it to the dedicated server. The launcher rebuilds, refuses to run while Valheim is open, and rejects a dedicated server assembly older than the refreshed client reference. Runtime files, world state, local profiles, logs, and reference DLLs are not committed.

Sorting settings are watched for external file edits and reloaded on the Unity main thread after the file stops changing. Weight sort defaults to heaviest stack first.

## Safety and verification

The MVP sorts only the local player's inventory. Do not sort container inventories or introduce custom networking without separately verifying their ownership and synchronization behavior. A sort changes slot positions only, preserves the hotbar row and equipped/bound items, and must notify Valheim via `Inventory.Changed`. Never claim gameplay verification until clean-client multiplayer and failure-path checks pass.
