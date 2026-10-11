using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiRecipePin.Hud
{
    // NGUI panel in the bottom-left screen corner, built from vanilla sprites/fonts.
    // Entries stack upwards: the newest pin is at the bottom, older ones move up.
    // All sizes are NGUI units (1 unit = 2 screen px at 1080p).
    internal class PinHud
    {
        private class Cell
        {
            public Item need;

            public UILabel counter;

            public int last_have = -1;
        }

        private class Entry
        {
            public PinnedRecipe pin;

            public GameObject go;

            public UI2DSprite back;

            public int height;

            public List<Cell> cells = new List<Cell>();
        }

        private const string ROOT_NAME = "MultiRecipePin HUD";

        private const int PANEL_DEPTH = 2;

        private const int MARGIN_X = 2;

        private const int MARGIN_Y = 4;

        // Share of screen height the stack may take; the rest is left for the vanilla top-left HUD.
        private const float MAX_HEIGHT_K = 0.7f;

        private const int GAP = 2;

        private const int PAD = 4;

        private const int ICON = 24;

        private const int CELL_W = 30;

        private const int CELL_H = 30;

        private const int CLOSE = 12;

        private const int COUNTER_H = 16;

        private const int NAME_MAX_LINES = 3;

        private const float REFRESH_PERIOD = 0.25f;

        private const float FLASH_TIME = 0.6f;

        private const float FLASH_BLINK = 0.15f;

        private readonly PinManager _pins;

        private readonly List<Entry> _entries = new List<Entry>();

        private GameObject _root;

        private UIPanel _panel;

        private int _width;

        private int _screen_w;

        private int _screen_h;

        private bool _dirty = true;

        private float _next_refresh;

        private float _flash_until;

        private bool _flash_on;

        // Entry the user expanded by hand; overflow logic doesn't auto-collapse it.
        private PinnedRecipe _keep_expanded;

        public PinHud(PinManager pins)
        {
            _pins = pins;
            _pins.Changed += delegate
            {
                _dirty = true;
            };
            _pins.LimitHit += delegate
            {
                _flash_until = Time.unscaledTime + FLASH_TIME;
            };
        }

        public void Tick()
        {
            try
            {
                float alpha = 1f;
                if (_pins.Count == 0 || !GameApi.IsHudAllowed(out alpha))
                {
                    if (_root != null && _root.activeSelf)
                    {
                        ItemTooltip.Hide();
                        _root.SetActive(value: false);
                    }
                    return;
                }
                if (!EnsureRoot())
                {
                    return;
                }
                if (!_root.activeSelf)
                {
                    _root.SetActive(value: true);
                }
                _panel.alpha = alpha;
                UIWidget screen_size_widget = GUIElements.me.screen_size_widget;
                if (screen_size_widget.width != _screen_w || screen_size_widget.height != _screen_h)
                {
                    _screen_w = screen_size_widget.width;
                    _screen_h = screen_size_widget.height;
                    _dirty = true;
                }
                if (_dirty)
                {
                    _dirty = false;
                    Rebuild();
                    _next_refresh = 0f;
                }
                if (Time.unscaledTime >= _next_refresh)
                {
                    _next_refresh = Time.unscaledTime + REFRESH_PERIOD;
                    RefreshCounters();
                }
                UpdateFlash();
            }
            catch (Exception ex)
            {
                Log.Error("PinHud.Tick failed: " + ex);
                _dirty = false;
            }
        }

        private bool EnsureRoot()
        {
            if (_root != null)
            {
                return true;
            }
            if (!VanillaStyle.Init())
            {
                return false;
            }
            GUIElements gui = GUIElements.me;
            _root = NGUITools.AddChild(gui.gameObject);
            _root.name = ROOT_NAME;
            _root.layer = gui.hud.gameObject.layer;
            _panel = _root.AddComponent<UIPanel>();
            _panel.depth = PANEL_DEPTH;
            _entries.Clear();
            _dirty = true;
            Log.Dbg("HUD root created under " + gui.name);
            return true;
        }

        private void Rebuild()
        {
            ItemTooltip.Hide();
            foreach (Entry entry in _entries)
            {
                DestroyEntry(entry);
            }
            _entries.Clear();
            _flash_on = false;
            _width = Mathf.Max(60, _screen_w * Plugin.HudWidthPercent.Value / 100);
            IReadOnlyList<PinnedRecipe> pins = _pins.Pins;
            if (_keep_expanded != null && (_keep_expanded.Collapsed || !Contains(pins, _keep_expanded)))
            {
                _keep_expanded = null;
            }
            int num = -GAP;
            foreach (PinnedRecipe item in pins)
            {
                item.AutoCollapsed = false;
                Entry entry2 = BuildEntry(item);
                _entries.Add(entry2);
                num += entry2.height + GAP;
            }
            // Overflow: collapse the oldest expanded entries until the stack fits. The newest stays expanded.
            int num2 = Mathf.RoundToInt(_screen_h * MAX_HEIGHT_K);
            for (int i = 0; i < _entries.Count - 1 && num > num2; i++)
            {
                PinnedRecipe pin = _entries[i].pin;
                if (!pin.Collapsed && pin != _keep_expanded)
                {
                    pin.AutoCollapsed = true;
                    num -= _entries[i].height;
                    DestroyEntry(_entries[i]);
                    _entries[i] = BuildEntry(pin);
                    num += _entries[i].height;
                }
            }
            // Bottom-left corner; the newest entry is the lowest one.
            _root.transform.localPosition = new Vector3(-_screen_w / 2 + MARGIN_X, -_screen_h / 2 + MARGIN_Y, 0f);
            int num3 = 0;
            for (int num4 = _entries.Count - 1; num4 >= 0; num4--)
            {
                Entry entry3 = _entries[num4];
                entry3.go.transform.localPosition = new Vector3(0f, num3 + entry3.height, 0f);
                num3 += entry3.height + GAP;
            }
            GJL.EnsureChildLabelsHasCorrectFont(_root, do_cache: false);
            Log.Dbg($"HUD rebuilt: entries={_entries.Count} width={_width} height={num} max={num2}");
        }

        private static bool Contains(IReadOnlyList<PinnedRecipe> pins, PinnedRecipe pin)
        {
            foreach (PinnedRecipe pin2 in pins)
            {
                if (pin2 == pin)
                {
                    return true;
                }
            }
            return false;
        }

        private static void DestroyEntry(Entry entry)
        {
            entry.go.SetActive(value: false);
            UnityEngine.Object.Destroy(entry.go);
        }

        // Entry origin is its top-left corner; children go right (+x) and down (-y).
        private Entry BuildEntry(PinnedRecipe pin)
        {
            Entry entry = new Entry
            {
                pin = pin,
                go = NGUITools.AddChild(_root)
            };
            entry.go.name = "pin " + pin.Id;
            GameObject go = entry.go;
            bool flag = pin.Collapsed || pin.AutoCollapsed;
            UILabel uILabel = go.AddWidget<UILabel>(3);
            UITextStyles.Set(uILabel, UITextStyles.TextStyle.TinyDescription);
            uILabel.name = "name";
            uILabel.pivot = UIWidget.Pivot.TopLeft;
            uILabel.alignment = NGUIText.Alignment.Left;
            uILabel.overflowMethod = UILabel.Overflow.ResizeHeight;
            uILabel.maxLineCount = NAME_MAX_LINES;
            uILabel.width = _width - PAD - ICON - 2 - CLOSE - PAD;
            uILabel.text = GameApi.GetCraftTitle(pin.Craft);
            uILabel.ProcessText();
            uILabel.transform.localPosition = new Vector3(PAD + ICON + 2, -PAD, 0f);
            int num = Mathf.Max(ICON, uILabel.height) + PAD * 2;

            AddIcon(go, "icon", GameApi.GetCraftSprite(pin.Craft), PAD + ICON / 2, -(PAD + ICON / 2), 2);

            UIWidget uIWidget = go.AddWidget<UIWidget>(1);
            uIWidget.name = "header";
            uIWidget.pivot = UIWidget.Pivot.TopLeft;
            uIWidget.width = _width - CLOSE - PAD * 2;
            uIWidget.height = num;
            uIWidget.transform.localPosition = Vector3.zero;
            AddCollider(uIWidget.gameObject, uIWidget.width, uIWidget.height, new Vector2(uIWidget.width / 2f, -uIWidget.height / 2f));
            UIEventListener.Get(uIWidget.gameObject).onClick = delegate
            {
                if (pin.Collapsed || pin.AutoCollapsed)
                {
                    pin.Collapsed = false;
                    _keep_expanded = pin;
                }
                else
                {
                    pin.Collapsed = true;
                }
                GameApi.PlayClick();
                _dirty = true;
            };

            UI2DSprite uI2DSprite = go.AddWidget<UI2DSprite>(5);
            uI2DSprite.name = "close";
            uI2DSprite.sprite2D = VanillaStyle.Cross;
            uI2DSprite.pivot = UIWidget.Pivot.Center;
            uI2DSprite.width = CLOSE;
            uI2DSprite.height = CLOSE;
            uI2DSprite.transform.localPosition = new Vector3(_width - PAD - CLOSE / 2, -(PAD + CLOSE / 2), 0f);
            AddCollider(uI2DSprite.gameObject, CLOSE + 4, CLOSE + 4, Vector2.zero);
            string id = pin.Id;
            UIEventListener.Get(uI2DSprite.gameObject).onClick = delegate
            {
                GameApi.PlayClick();
                Log.Info("Unpinned from HUD: " + id);
                _pins.Remove(id);
            };

            int num2 = num;
            if (!flag)
            {
                List<Item> needs = pin.Craft.needs;
                int num3 = Mathf.Max(1, (_width - PAD * 2) / CELL_W);
                int num4 = (_width - PAD * 2) / num3;
                for (int i = 0; i < needs.Count; i++)
                {
                    int num5 = PAD + i % num3 * num4 + num4 / 2;
                    int num6 = -(num + i / num3 * CELL_H);
                    entry.cells.Add(BuildCell(go, needs[i], num5, num6, num4));
                }
                num2 += (needs.Count + num3 - 1) / num3 * CELL_H + PAD;
            }

            UI2DSprite uI2DSprite2 = go.AddWidget<UI2DSprite>(0);
            uI2DSprite2.name = "back";
            uI2DSprite2.sprite2D = VanillaStyle.Paper;
            uI2DSprite2.type = UIBasicSprite.Type.Sliced;
            uI2DSprite2.border = new Vector4(14f, 14f, 14f, 14f);
            uI2DSprite2.pivot = UIWidget.Pivot.TopLeft;
            uI2DSprite2.width = _width;
            uI2DSprite2.height = Mathf.Max(num2, 30);
            uI2DSprite2.transform.localPosition = Vector3.zero;
            entry.back = uI2DSprite2;
            entry.height = uI2DSprite2.height;
            return entry;
        }

        // Vanilla-like ingredient cell: icon with an outlined "have/need" counter over its bottom edge.
        private Cell BuildCell(GameObject parent, Item need, int center_x, int top_y, int cell_w)
        {
            UI2DSprite uI2DSprite = AddIcon(parent, "need " + need.id, GameApi.GetItemSprite(need), center_x, top_y - ICON / 2, 2);
            if (uI2DSprite.sprite2D == null)
            {
                Log.Dbg("No icon for ingredient " + need.id);
            }
            // Hover over the whole cell shows the vanilla item tooltip.
            AddCollider(uI2DSprite.gameObject, cell_w, CELL_H, new Vector2(0f, (ICON - CELL_H) / 2f));
            UIEventListener.Get(uI2DSprite.gameObject).onHover = delegate(GameObject go, bool state)
            {
                if (state)
                {
                    ItemTooltip.Show(need, go.GetComponent<Collider2D>());
                }
                else
                {
                    ItemTooltip.Hide();
                }
            };
            UILabel counterTemplate = VanillaStyle.CounterTemplate;
            UILabel uILabel = parent.AddWidget<UILabel>(4);
            uILabel.name = "count " + need.id;
            uILabel.bitmapFont = counterTemplate.bitmapFont;
            uILabel.fontSize = counterTemplate.fontSize;
            uILabel.effectStyle = counterTemplate.effectStyle;
            uILabel.effectColor = counterTemplate.effectColor;
            uILabel.effectDistance = counterTemplate.effectDistance;
            uILabel.spacingY = counterTemplate.spacingY;
            uILabel.pivot = UIWidget.Pivot.Bottom;
            uILabel.alignment = NGUIText.Alignment.Center;
            uILabel.overflowMethod = UILabel.Overflow.ShrinkContent;
            uILabel.width = cell_w;
            uILabel.height = COUNTER_H;
            uILabel.transform.localPosition = new Vector3(center_x, top_y - CELL_H, 0f);
            return new Cell
            {
                need = need,
                counter = uILabel
            };
        }

        private static UI2DSprite AddIcon(GameObject parent, string name, Sprite sprite, int center_x, int center_y, int depth)
        {
            UI2DSprite uI2DSprite = parent.AddWidget<UI2DSprite>(depth);
            uI2DSprite.name = name;
            uI2DSprite.sprite2D = sprite;
            uI2DSprite.pivot = UIWidget.Pivot.Center;
            int num = ICON;
            if (sprite != null && sprite.rect.width > sprite.rect.height)
            {
                // Wide (two-cell) item icons keep their aspect ratio.
                num = Mathf.Max(2, Mathf.RoundToInt(ICON * sprite.rect.height / sprite.rect.width));
            }
            uI2DSprite.width = ICON;
            uI2DSprite.height = num;
            uI2DSprite.transform.localPosition = new Vector3(center_x, center_y, 0f);
            return uI2DSprite;
        }

        private static void AddCollider(GameObject go, float width, float height, Vector2 offset)
        {
            BoxCollider2D boxCollider2D = go.AddComponent<BoxCollider2D>();
            boxCollider2D.isTrigger = VanillaStyle.ColliderIsTrigger;
            boxCollider2D.size = new Vector2(width, height);
            boxCollider2D.offset = offset;
        }

        private void RefreshCounters()
        {
            foreach (Entry entry in _entries)
            {
                if (entry.cells.Count == 0)
                {
                    continue;
                }
                List<Item> sources = GameApi.GetSources(entry.pin.Craftery);
                foreach (Cell cell in entry.cells)
                {
                    int num = GameApi.CountItem(cell.need, sources);
                    if (num != cell.last_have)
                    {
                        cell.last_have = num;
                        cell.counter.text = num + "/" + cell.need.value;
                        cell.counter.color = (num >= cell.need.value) ? VanillaStyle.Enough : VanillaStyle.NotEnough;
                    }
                }
            }
        }

        // Pin limit reached: the whole stack blinks red for a moment.
        private void UpdateFlash()
        {
            float num = _flash_until - Time.unscaledTime;
            bool flag = num > 0f && (int)(num / FLASH_BLINK) % 2 == 0;
            if (flag == _flash_on)
            {
                return;
            }
            _flash_on = flag;
            Color color = flag ? Color.Lerp(Color.white, VanillaStyle.NotEnough, 0.6f) : Color.white;
            foreach (Entry entry in _entries)
            {
                entry.back.color = color;
            }
        }
    }
}
