# CatosInventorySorter

CatosInventorySorter adds a **Sort** button to the local player's inventory panel. It sorts real inventory slots by item type, name, per-stack weight, or stack quantity. The chosen criterion and direction are configurable; F6 is also available while the inventory screen is open.

The sorter keeps the hotbar row, equipped items, and bound items in place. It changes only `ItemData.m_gridPos`, then calls Valheim's `Inventory.Changed` callback so the game's normal inventory-change listeners update. It does not open containers, move items between inventories, or add a server plugin or custom network messages. Weight sorting defaults to heaviest stacks first.

Edits to the BepInEx config file are detected while the game is running and applied after the file stops changing. The sort button label and behavior update with the new sort mode.

This is a client plugin for `valheim.exe`; a dedicated server does not install it. Multiplayer behavior still needs in-game verification.

## Build

Refresh references as documented in [AGENTS.md](AGENTS.md), then run:

```powershell
.
scripts\build.ps1
```

Or directly:

```powershell
dotnet build src/CatosInventorySorter/CatosInventorySorter.csproj -c Release
```

See [DEVPLAN.md](DEVPLAN.md) for scope and verification status.
