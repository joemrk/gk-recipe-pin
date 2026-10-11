using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MultiRecipePin
{
    // The only place that touches game internals. See NOTES.md for where each call comes from.
    internal static class GameApi
    {
        private const string GRATITUDE_ITEM_ID = "gratitude_as_item";

        public static bool IsGameStarted()
        {
            return MainGame.game_started && MainGame.me != null && MainGame.me.player != null;
        }

        public static string GameVersion()
        {
            try
            {
                return LazyConsts.VERSION.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                return "unknown (" + ex.GetType().Name + ")";
            }
        }

        // Recipe under the mouse in the craft/build window (both are CraftGUI) and the workstation it belongs to.
        public static bool TryGetHoveredCraft(out CraftDefinition craft, out WorldGameObject craftery)
        {
            craft = null;
            craftery = null;
            try
            {
                GUIElements gui = GUIElements.me;
                if (gui == null || gui.craft == null || !gui.craft.is_shown)
                {
                    return false;
                }
                CraftItemGUI overed = CraftItemGUI.current_overed;
                if (overed == null || !overed.gameObject.activeInHierarchy)
                {
                    return false;
                }
                craft = overed.current_craft;
                craftery = gui.craft.GetCrafteryWGO();
                return craft != null;
            }
            catch (Exception ex)
            {
                Log.Error("TryGetHoveredCraft failed: " + ex);
                return false;
            }
        }

        // Inventories the game would use when crafting at this workstation:
        // player + toolbelt + storages of the workstation's zone. Bags are counted inside their owner,
        // so standalone bag entries are skipped (as MultiInventory.GetTotalCount does).
        public static List<Item> GetSources(WorldGameObject craftery)
        {
            List<Item> list = new List<Item>();
            try
            {
                if (craftery != null)
                {
                    MultiInventory multiInventory = craftery.GetMultiInventory(null, "", MultiInventory.PlayerMultiInventory.IncludePlayer, include_toolbelt: true);
                    foreach (Inventory item in multiInventory.all)
                    {
                        if (item != null && item.data != null && !item.data.is_bag && !list.Contains(item.data))
                        {
                            list.Add(item.data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Dbg("GetSources failed: " + ex.Message);
                list.Clear();
            }
            if (list.Count == 0 && MainGame.me != null && MainGame.me.player != null)
            {
                list.Add(MainGame.me.player.data);
            }
            return list;
        }

        // Same per-item logic as BaseItemCellGUI.DrawIngredient.
        public static int CountItem(Item need, List<Item> sources)
        {
            try
            {
                if (need == null || MainGame.me == null || MainGame.me.player == null)
                {
                    return 0;
                }
                if (need.id == GRATITUDE_ITEM_ID)
                {
                    return (int)MainGame.me.player.gratitude_points;
                }
                int num = 0;
                foreach (Item source in sources)
                {
                    if (need.multiquality_items != null && need.multiquality_items.Count > 0)
                    {
                        foreach (string multiquality_item in need.multiquality_items)
                        {
                            num += source.GetTotalCount(multiquality_item);
                        }
                    }
                    else
                    {
                        num += source.GetTotalCount(need.id);
                    }
                }
                return num;
            }
            catch (Exception ex)
            {
                Log.Error("CountItem failed: " + ex);
                return 0;
            }
        }

        public static string GetCraftName(CraftDefinition craft)
        {
            try
            {
                return GJL.L(craft.GetNameNonLocalized());
            }
            catch (Exception ex)
            {
                Log.Error("GetCraftName failed: " + ex);
                return craft?.id ?? "?";
            }
        }

        // Name plus output amount, e.g. "Nails x2".
        public static string GetCraftTitle(CraftDefinition craft)
        {
            string text = GetCraftName(craft);
            try
            {
                if (!(craft is ObjectCraftDefinition))
                {
                    Item firstRealOutput = craft.GetFirstRealOutput();
                    if (firstRealOutput != null && firstRealOutput.value > 1)
                    {
                        text = text + " x" + firstRealOutput.value;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Dbg("GetCraftTitle failed: " + ex.Message);
            }
            return text;
        }

        public static Sprite GetCraftSprite(CraftDefinition craft)
        {
            try
            {
                return GetSpriteOrNull(craft.GetCraftIcon());
            }
            catch (Exception ex)
            {
                Log.Dbg("GetCraftSprite failed for " + craft?.id + ": " + ex.Message);
                return null;
            }
        }

        // Same definition lookup as BaseItemCellGUI.DrawItem.
        private static ItemDefinition FindDefinition(Item item, out string id)
        {
            id = (item.multiquality_items != null && item.multiquality_items.Count > 0) ? item.multiquality_items[0] : item.id;
            ItemDefinition itemDefinition = GameBalance.me.GetDataOrNull<ItemDefinition>(id);
            if (itemDefinition == null)
            {
                List<string> itemsOfBaseName = GameBalance.me.GetItemsOfBaseName(id);
                if (itemsOfBaseName.Count > 0)
                {
                    itemDefinition = GameBalance.me.GetDataOrNull<ItemDefinition>(itemsOfBaseName[0]);
                }
            }
            return itemDefinition;
        }

        // The inventory tooltip of an item; it already has the vanilla "crafted at" line.
        public static List<BubbleWidgetData> GetItemTooltip(Item item)
        {
            try
            {
                ItemDefinition itemDefinition = FindDefinition(item, out var _);
                if (itemDefinition == null)
                {
                    return null;
                }
                return itemDefinition.GetTooltipData(new Item(itemDefinition.id, 1));
            }
            catch (Exception ex)
            {
                Log.Error("GetItemTooltip failed for " + item?.id + ": " + ex);
                return null;
            }
        }

        public static Sprite GetItemSprite(Item item)
        {
            try
            {
                ItemDefinition itemDefinition = FindDefinition(item, out var text);
                Sprite sprite = (itemDefinition == null) ? null : GetSpriteOrNull(itemDefinition.GetIcon());
                return sprite ?? GetSpriteOrNull("i_" + text);
            }
            catch (Exception ex)
            {
                Log.Dbg("GetItemSprite failed for " + item?.id + ": " + ex.Message);
                return null;
            }
        }

        private static Sprite GetSpriteOrNull(string sprite_name)
        {
            if (string.IsNullOrEmpty(sprite_name))
            {
                return null;
            }
            return EasySpritesCollection.GetSprite(sprite_name, not_found_is_valid: true);
        }

        // HUD follows the vanilla HUD (hidden by windows, faded out in cutscenes/dialogs),
        // plus stays visible while the craft/build window is the top window.
        public static bool IsHudAllowed(out float alpha)
        {
            alpha = 1f;
            if (!IsGameStarted())
            {
                return false;
            }
            GUIElements gui = GUIElements.me;
            if (gui == null)
            {
                return false;
            }
            if (BaseGUI.all_guis_closed)
            {
                HUD hud = gui.hud;
                if (hud == null || !hud.gameObject.activeInHierarchy)
                {
                    return false;
                }
                if (hud.panel != null)
                {
                    alpha = hud.panel.alpha;
                }
                return alpha > 0.01f;
            }
            return gui.craft != null && gui.craft.is_shown_and_top;
        }

        public static string GetItemName(Item item)
        {
            try
            {
                return item.GetItemName();
            }
            catch (Exception)
            {
                return item?.id ?? "?";
            }
        }

        public static string DescribeCraft(CraftDefinition craft, WorldGameObject craftery)
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<Item> sources = GetSources(craftery);
            stringBuilder.Append(craft.id).Append(" \"").Append(GetCraftName(craft)).Append("\" [").Append(craft.GetType().Name).Append("] needs:");
            foreach (Item need in craft.needs)
            {
                stringBuilder.Append(' ').Append(need.id).Append('=').Append(CountItem(need, sources)).Append('/').Append(need.value);
            }
            stringBuilder.Append(" @ ").Append((craftery == null) ? "none" : craftery.obj_id).Append(" (inventories: ").Append(sources.Count).Append(')');
            return stringBuilder.ToString();
        }

        public static void PlayClick()
        {
            try
            {
                Sounds.OnGUIClick();
            }
            catch (Exception ex)
            {
                Log.Dbg("PlayClick failed: " + ex.Message);
            }
        }

        public static void PlayDeny()
        {
            try
            {
                Sounds.OnGUIHover();
            }
            catch (Exception ex)
            {
                Log.Dbg("PlayDeny failed: " + ex.Message);
            }
        }
    }
}
