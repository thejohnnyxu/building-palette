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

Click any tag chip to remove it. Clicking outside the panel unfocuses it — **Enter** and **Esc** behave normally until you click back inside. The editor also works on items in open chests and Magic Storage.

**Valid tag names:** lowercase letters, numbers, and hyphens — e.g. `warm-stone`, `cave-ceiling`, `palette-a`

---

## Bulk tagging a chest

1. **Close your inventory first**
2. Hover a chest tile in the world
3. Press **T**

The panel opens in bulk mode. Any tag you add is applied to every item in the chest.

---

## Tag Manager

Click **Manage Tags** in the tag editor, or open it directly to manage all your tags in one place.

- **Left column** — all your tags. Click one to select it.
- **Right top** — items in the selected tag. Click **×** to remove an item from the tag.
- **Right bottom** — search all vanilla and modded items by name. Click **+** to add to the selected tag.

Scroll with the mouse wheel or drag the scrollbar thumb in any list.

---

## Area Scan

Press **[** or type `/scan` to enter scan mode. Left-click two points in the world to define a box — a selection rectangle shows as you drag. The scan collects every unique foreground tile and wall inside the box and opens the Tag Manager with the results pre-loaded. Select or deselect items, type a tag name, and click **Tag Selected**.

The selection border turns orange when the box exceeds the 100×100 tile limit.

---

## Eye Dropper

Press **F** to pick the block or item under your cursor.

- **Inventory open** — picks the hovered item slot (works in vanilla inventory, chests, and Magic Storage). Moves the item to your hotbar if it's not already there.
- **Inventory closed** — identifies the foreground tile or wall under the cursor and selects the matching item from your hotbar or inventory.

---

## Searching in Magic Storage

Type a tag name in Magic Storage's search box. All items with that tag appear in results.

---

## Commands

| Command | Description |
|---|---|
| `/tag <n>` | Add a tag to the selected item |
| `/untag <n>` | Remove a tag |
| `/tags` | List tags on the selected item |
| `/tagdone` | Deselect the current item |
| `/renametag <old> <new>` | Rename a tag across every item that has it |
| `/alltags` | List every tag and its items |
| `/exporttag <name>` | Export a tag as a shareable string |
| `/importtag <string>` | Import a tag string |
| `/scan` | Start or cancel area scan mode |

Multi-word tag names are joined with hyphens — `/tag warm stone` adds `warm-stone`.

---

## Import / Export

Share tags between characters or with other players.

**Export:** Select a tag in the Tag Manager and click **Export**, or use `/exporttag <name>`. The result appears in chat:
```
warm-stone:Sandstone Block,Smooth Sandstone,Palm Wood
```

**Import:** Click **Import** in the Tag Manager (opens chat with the format hint), or use `/importtag warm-stone:Sandstone Block,Smooth Sandstone`. If an item name matches multiple items it is skipped with a warning.

---

## Notes

- Tags are saved to your **character file** and travel across worlds
- Works on vanilla and modded items, including modded walls
- Tags are case-insensitive (`Warm-Stone` and `warm-stone` are the same)
- Autocomplete suggests all matching existing tags as you type

---

## Installation

Drop the `BuildingPalette` folder into:
```
Documents/My Games/Terraria/tModLoader/ModSources/
```
Then build and enable it from the tModLoader Mod Sources menu.