# Building Palette

> A Terraria tModLoader mod for builders. Tag items with custom labels and find them instantly in Magic Storage.

---

## How it works

Tags appear in item tooltips. Magic Storage indexes tooltip text, so typing a tag name in the search box filters to all matching items — no Magic Storage integration required.

---

## Tagging an item

1. Open your inventory and hover any item
2. Press **T** to open the tag editor
3. Click the input box — it glows blue when focused
4. Type a tag name and press **Enter** or click **Add**
5. Press **Esc** or **Cancel** to close

Click any tag chip to remove it. Clicking outside the panel unfocuses it — **Enter** and **Esc** behave normally in the game until you click back inside.

**Valid tag names:** lowercase letters, numbers, and hyphens — e.g. `warm-stone`, `cave-ceiling`, `palette-a`

---

## Bulk tagging a chest

1. **Close your inventory first**
2. Hover a chest tile in the world
3. Press **T**

The panel opens in bulk mode showing `Chest (X items)`. Any tag you add is applied to every item in the chest. Removing a chip removes that tag from all chest items.

---

## Searching in Magic Storage

Type your tag name in Magic Storage's search box. All items with that tag show up in results.

---

## Commands

All commands operate on the item most recently opened with **T**.

| Command | Description |
|---|---|
| `/tag <n>` | Add a tag to the selected item |
| `/untag <n>` | Remove a tag from the selected item |
| `/tags` | List all tags on the selected item |
| `/tagdone` | Deselect the current item |
| `/renametag <old> <new>` | Rename a tag across every item that has it |
| `/alltags` | List every tracked tag and the items in each |

Multi-word tag names are joined with hyphens automatically — `/tag warm stone` adds `warm-stone`.

### `/alltags` output

```
All tags (3):
  #warm-stone: [icon] Sandstone Block, [icon] Mud Block
  #exterior:   [icon] Granite Block, [icon] Sandstone Block
  #natural:    [icon] Mossy Stone Block
```

Each tag renders in a consistent color. Item icons appear inline in the chat window.

---

## Notes

- Tags are saved to your **character file** — they travel with you across worlds
- Works on vanilla and modded items
- Tags are case-insensitive (`Warm-Stone` and `warm-stone` are the same)
- Autocomplete suggests existing tags as you type (prefix match, up to 3 suggestions)

---

## Installation

Drop the `BuildingPalette` folder into:
```
Documents/My Games/Terraria/tModLoader/ModSources/
```
Then build and enable it from the tModLoader Mod Sources menu.
