# CatosInventorySorter development plan

> **Status:** New client-side project scaffold and first sort implementation are in source; the local Release build succeeds. Runtime and gameplay behavior still need verification.
>
> **Purpose:** Let a player reorder their own inventory with an in-game button, sorting by total stack weight or quantity.

## Locked scope

- Runs only in `valheim.exe`; the dedicated server does not install the mod.
- Sorts the local player's inventory only. Container inventories are out of MVP scope.
- Reorders existing item records by grid position. Does not alter stack counts, item identity, equipment state, hotbar row, or bound-item positions.
- Notifies the native inventory through `Inventory.Changed(true, false)` so its normal change listeners run.
- Has no custom network messages and no item transfer operations.
- Adds a button to the native player inventory panel and F6 shortcut while the inventory is visible.
- Sorting choices: Weight (shared unit weight times stack size) and Quantity. Both default to descending order: heaviest or largest stack first.
- Config changes made by an external editor are polled and reloaded on the main thread after the file write settles.

## Native API evidence

The local `assembly_valheim.dll` was inspected with Mono.Cecil. `Inventory` exposes `GetAllItems`, `GetWidth`, `GetHeight`, and `GetBoundItems`; `Changed(bool,bool)` is private, so the implementation resolves that exact method with Harmony reflection and fails closed if it is missing. `ItemDrop.ItemData` has `m_gridPos`, stack count, and shared item type/name/weight fields. `InventoryGui` has `m_player`, `m_stackAllButton`, `m_takeAllButton`, `m_dropButton`, and static `IsVisible()`. The project has not yet been loaded in Valheim; the runtime placement and button behavior are not validated.

## Sort behavior

1. Snapshot current item references and validate every item and occupied grid position.
2. Reserve the first row (hotbar), equipped-item slots, and bound-item slots.
3. Sort the remaining items by configured criterion, with deterministic tie-breakers.
4. Place them into available slots row-major from the second row, preserving all item data.
5. Call `Inventory.Changed(true, false)` once if any grid position changed.

If validation fails, do not mutate the inventory. The algorithm does not merge stacks or split items.

## Test setup

Use the existing `C:\Users\magni\Downloads\Dedicated` world through `C:\Users\magni\Downloads\worlds_local\Dedicated`, with `-savedir C:\Users\magni\Downloads`, world `Dedicated`, port `2462`, password `696969`, and `-public 0`. The server is only the multiplayer endpoint and must never receive this client DLL. The test launcher deploys to the separate r2modman profile `CatosInventorySorter`.

## Verification checklist

- [x] Release build succeeds against the installed local references.
- [ ] External config edits reload while in-game and update the sort button label.
- [ ] Sort button is visible beside the upper-right edge of the player inventory grid.
- [ ] Button appears in a clean client and invokes the configured sort.
- [ ] Weight and quantity modes produce the expected descending order, including stable ties.
- [ ] Hotbar, equipped items, and bound items remain in their original positions.
- [ ] Repeated sort is idempotent; empty and one-item inventories are safe.
- [ ] Sorting while connected to the dedicated test endpoint persists after reconnect/reload.
- [ ] No repeated client or server BepInEx errors; no sorter DLL on server.
- [ ] Test launcher uses the existing world junction and correct active-save admin list.
