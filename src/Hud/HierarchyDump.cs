using System;
using System.Text;
using UnityEngine;

namespace MultiRecipePin.Hud
{
    // One-off debug dump of vanilla GUI prefabs: prefab structure lives in assets, not in code,
    // so sprites/fonts/depths for the HUD are picked from this log.
    internal static class HierarchyDump
    {
        private const int MAX_NODES_PER_ROOT = 400;

        private static int _nodes;

        public static void DumpAll()
        {
            try
            {
                GUIElements gui = GUIElements.me;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("==== GUI dump ====");
                stringBuilder.AppendLine($"screen {Screen.width}x{Screen.height}, screen_size_widget {gui.screen_size_widget.width}x{gui.screen_size_widget.height}");
                Camera gui_cam = MainGame.me.gui_cam;
                UICamera uICamera = gui_cam.GetComponent<UICamera>();
                stringBuilder.AppendLine($"gui_cam '{gui_cam.name}' ortho={gui_cam.orthographicSize} cullingMask={gui_cam.cullingMask} depth={gui_cam.depth} UICamera={(uICamera != null ? uICamera.eventType + " mask=" + uICamera.eventReceiverMask.value : "none")}");
                UIRoot uIRoot = gui.GetComponentInParent<UIRoot>();
                if (uIRoot == null)
                {
                    uIRoot = gui.GetComponentInChildren<UIRoot>(includeInactive: true);
                }
                stringBuilder.AppendLine((uIRoot == null) ? "UIRoot: not found near GUIElements" : $"UIRoot '{Path(uIRoot.transform)}' scaling={uIRoot.scalingStyle} activeHeight={uIRoot.activeHeight} manualHeight={uIRoot.manualHeight} pixelSizeAdjustment={uIRoot.pixelSizeAdjustment} scale={uIRoot.transform.localScale}");
                stringBuilder.AppendLine($"GUIElements '{Path(gui.transform)}' layer={gui.gameObject.layer} scale={gui.transform.localScale}");
                stringBuilder.AppendLine("-- text styles --");
                UILabel[] style_labels = UITextStyles.me.style_labels;
                for (int i = 0; i < style_labels.Length; i++)
                {
                    stringBuilder.AppendLine($"style[{i}] {(UITextStyles.TextStyle)i}: {DescribeLabel(style_labels[i])}");
                }
                Log.Info(stringBuilder.ToString());
                DumpRoot("hud", gui.hud.transform, 2);
                DumpRoot("tooltip_bubble", gui.tooltip_bubble.transform, 8);
                DumpRoot("bubble_widgets_container", gui.bubble_widgets_container.transform, 5);
                DumpRoot("craft", gui.craft.transform, 12);
            }
            catch (Exception ex)
            {
                Log.Error("GUI dump failed: " + ex);
            }
        }

        private static void DumpRoot(string title, Transform root, int max_depth)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"-- {title}: {Path(root)} (parent panel depth: {ParentPanelDepth(root)}) --");
            _nodes = 0;
            DumpNode(stringBuilder, root, 0, max_depth);
            if (_nodes >= MAX_NODES_PER_ROOT)
            {
                stringBuilder.AppendLine("... truncated");
            }
            Log.Info(stringBuilder.ToString());
        }

        private static void DumpNode(StringBuilder sb, Transform tf, int depth, int max_depth)
        {
            if (_nodes++ >= MAX_NODES_PER_ROOT)
            {
                return;
            }
            sb.Append(' ', depth * 2).Append(tf.name);
            sb.Append(tf.gameObject.activeSelf ? "" : " (off)");
            sb.Append($" L{tf.gameObject.layer} pos=({tf.localPosition.x:0.#},{tf.localPosition.y:0.#})");
            if (tf.localScale != Vector3.one)
            {
                sb.Append($" scale=({tf.localScale.x:0.##},{tf.localScale.y:0.##})");
            }
            Component[] components = tf.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null || component is Transform)
                {
                    continue;
                }
                sb.Append(" | ").Append(DescribeComponent(component));
            }
            sb.AppendLine();
            if (depth >= max_depth)
            {
                if (tf.childCount > 0)
                {
                    sb.Append(' ', (depth + 1) * 2).AppendLine($"... {tf.childCount} children");
                }
                return;
            }
            for (int j = 0; j < tf.childCount; j++)
            {
                DumpNode(sb, tf.GetChild(j), depth + 1, max_depth);
            }
        }

        private static string DescribeComponent(Component c)
        {
            if (c is UILabel label)
            {
                return "UILabel " + DescribeLabel(label);
            }
            if (c is UI2DSprite sprite)
            {
                Sprite sprite2D = sprite.sprite2D;
                return $"UI2DSprite d{sprite.depth} {sprite.width}x{sprite.height} {sprite.pivot} type={sprite.type} sprite={(sprite2D == null ? "null" : sprite2D.name + " " + sprite2D.rect.width + "x" + sprite2D.rect.height + " border=" + sprite2D.border)} wborder={sprite.border} color={ColorStr(sprite.color)}";
            }
            if (c is UIPanel panel)
            {
                return $"UIPanel depth={panel.depth} clip={panel.clipping} alpha={panel.alpha:0.##} sortingOrder={panel.sortingOrder}";
            }
            if (c is UIWidget widget)
            {
                return $"{c.GetType().Name} d{widget.depth} {widget.width}x{widget.height} {widget.pivot} color={ColorStr(widget.color)}";
            }
            if (c is BoxCollider2D box)
            {
                return $"BoxCollider2D size={box.size} offset={box.offset} enabled={box.enabled}";
            }
            if (c is Behaviour behaviour && !behaviour.enabled)
            {
                return c.GetType().Name + "(disabled)";
            }
            return c.GetType().Name;
        }

        private static string DescribeLabel(UILabel label)
        {
            if (label == null)
            {
                return "null";
            }
            string text = label.text ?? "";
            if (text.Length > 24)
            {
                text = text.Substring(0, 24) + "…";
            }
            return $"d{label.depth} {label.width}x{label.height} {label.pivot} font={(label.bitmapFont != null ? "bmp:" + label.bitmapFont.name : (label.trueTypeFont != null ? "ttf:" + label.trueTypeFont.name : "none"))} size={label.fontSize} overflow={label.overflowMethod} effect={label.effectStyle} spacingY={label.spacingY} color={ColorStr(label.color)} text=\"{text.Replace("\n", "\\n")}\"";
        }

        private static string ColorStr(Color c)
        {
            return ColorUtility.ToHtmlStringRGBA(c);
        }

        private static string ParentPanelDepth(Transform tf)
        {
            UIPanel uIPanel = (tf.parent == null) ? null : tf.parent.GetComponentInParent<UIPanel>();
            return (uIPanel == null) ? "none" : uIPanel.depth + " '" + uIPanel.name + "'";
        }

        private static string Path(Transform tf)
        {
            string text = tf.name;
            while (tf.parent != null)
            {
                tf = tf.parent;
                text = tf.name + "/" + text;
            }
            return text;
        }
    }
}
