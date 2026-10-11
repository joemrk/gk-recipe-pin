using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiRecipePin.Hud
{
    // Vanilla sprites, fonts and colors picked from live GUI prefabs (names taken from the debug GUI dump).
    internal static class VanillaStyle
    {
        private const string SPRITE_PAPER = "back_paper02_rounded";

        private const string SPRITE_CROSS = "main_window_cross_inside";

        private const string COUNTER_LABEL_NAME = "n label";

        public static Sprite Paper;

        public static Sprite Cross;

        // Outlined tiny label used by vanilla ingredient cells for "have/need".
        public static UILabel CounterTemplate;

        public static Color Enough = new Color(0.42f, 0.98f, 0.19f);

        public static Color NotEnough = new Color(0.96f, 0.2f, 0.05f);

        // Our colliders copy the trigger flag of vanilla GUI colliders.
        public static bool ColliderIsTrigger;

        public static bool Ready { get; private set; }

        public static bool Init()
        {
            if (Ready)
            {
                return true;
            }
            try
            {
                GUIElements gui = GUIElements.me;
                Dictionary<string, Sprite> dictionary = new Dictionary<string, Sprite>();
                CollectSprites(gui.craft.transform, dictionary);
                CollectSprites(gui.tooltip_bubble.transform, dictionary);
                dictionary.TryGetValue(SPRITE_PAPER, out Paper);
                dictionary.TryGetValue(SPRITE_CROSS, out Cross);
                CraftItemGUI craft_item_prefab = gui.craft.craft_item_prefab;
                foreach (UILabel uILabel in craft_item_prefab.GetComponentsInChildren<UILabel>(includeInactive: true))
                {
                    if (uILabel.name == COUNTER_LABEL_NAME)
                    {
                        CounterTemplate = uILabel;
                        break;
                    }
                }
                foreach (BaseItemCellGUI baseItemCellGUI in craft_item_prefab.GetComponentsInChildren<BaseItemCellGUI>(includeInactive: true))
                {
                    if (baseItemCellGUI != craft_item_prefab.item_gui && baseItemCellGUI.colors != null && baseItemCellGUI.colors.enough_res.a > 0f)
                    {
                        Enough = baseItemCellGUI.colors.enough_res;
                        NotEnough = baseItemCellGUI.colors.not_enough_res;
                        break;
                    }
                }
                BoxCollider2D componentInChildren = craft_item_prefab.GetComponentInChildren<BoxCollider2D>(includeInactive: true);
                if (componentInChildren != null)
                {
                    ColliderIsTrigger = componentInChildren.isTrigger;
                }
                Ready = Paper != null && CounterTemplate != null;
                Log.Dbg($"VanillaStyle: paper={Paper != null} cross={Cross != null} counter={CounterTemplate != null} enough={ColorUtility.ToHtmlStringRGBA(Enough)} not_enough={ColorUtility.ToHtmlStringRGBA(NotEnough)}");
                if (!Ready)
                {
                    Log.Error("VanillaStyle: required vanilla GUI resources not found, HUD disabled");
                }
            }
            catch (Exception ex)
            {
                Log.Error("VanillaStyle.Init failed: " + ex);
            }
            return Ready;
        }

        private static void CollectSprites(Transform root, Dictionary<string, Sprite> sprites)
        {
            foreach (UI2DSprite uI2DSprite in root.GetComponentsInChildren<UI2DSprite>(includeInactive: true))
            {
                Sprite sprite2D = uI2DSprite.sprite2D;
                if (sprite2D != null && !sprites.ContainsKey(sprite2D.name))
                {
                    sprites.Add(sprite2D.name, sprite2D);
                }
            }
        }
    }
}
