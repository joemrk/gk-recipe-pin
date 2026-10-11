using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiRecipePin.Hud
{
    // Vanilla item tooltip (same data as in the inventory) shown by hand:
    // TooltipsManager ignores tooltips while no window is open.
    internal static class ItemTooltip
    {
        private static TooltipBubbleGUI _bubble;

        public static void Show(Item need, Collider2D collider)
        {
            Hide();
            try
            {
                List<BubbleWidgetData> itemTooltip = GameApi.GetItemTooltip(need);
                if (itemTooltip == null || itemTooltip.Count == 0)
                {
                    Log.Dbg("No tooltip data for " + need.id);
                    return;
                }
                BubbleWidgetDataContainer bubbleWidgetDataContainer = new BubbleWidgetDataContainer(WidgetsBubbleGUI.Alignment.Center);
                bubbleWidgetDataContainer.SetData(itemTooltip);
                _bubble = TooltipBubbleGUI.Show(bubbleWidgetDataContainer, for_gamepad: false, collider);
                Log.Dbg($"Tooltip shown for {need.id}: widgets={itemTooltip.Count} bubble={_bubble != null}");
            }
            catch (Exception ex)
            {
                Log.Error("ItemTooltip.Show failed: " + ex);
            }
        }

        public static void Hide()
        {
            try
            {
                if (_bubble != null)
                {
                    _bubble.DestroyBubble();
                }
            }
            catch (Exception ex)
            {
                Log.Dbg("ItemTooltip.Hide failed: " + ex.Message);
            }
            _bubble = null;
        }
    }
}
