using System;
using System.Collections.Generic;

namespace MultiRecipePin
{
    internal enum ToggleResult
    {
        Added,
        Removed,
        LimitReached,
        Ignored
    }

    internal class PinManager
    {
        private const string REMOVE_BUILDING_CRAFT_ID = "_remove_";

        private readonly List<PinnedRecipe> _pins = new List<PinnedRecipe>();

        private int _next_order;

        public IReadOnlyList<PinnedRecipe> Pins => _pins;

        public int Count => _pins.Count;

        public event Action Changed;

        public event Action LimitHit;

        public ToggleResult Toggle(CraftDefinition craft, WorldGameObject craftery, int max_pins)
        {
            if (craft == null || string.IsNullOrEmpty(craft.id) || craft.id == REMOVE_BUILDING_CRAFT_ID || craft.needs == null || craft.needs.Count == 0)
            {
                return ToggleResult.Ignored;
            }
            int num = IndexOf(craft.id);
            if (num != -1)
            {
                _pins.RemoveAt(num);
                Changed?.Invoke();
                return ToggleResult.Removed;
            }
            if (_pins.Count >= max_pins)
            {
                LimitHit?.Invoke();
                return ToggleResult.LimitReached;
            }
            _pins.Add(new PinnedRecipe(craft, craftery, _next_order++));
            Changed?.Invoke();
            return ToggleResult.Added;
        }

        public bool Remove(string craft_id)
        {
            int num = IndexOf(craft_id);
            if (num == -1)
            {
                return false;
            }
            _pins.RemoveAt(num);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_pins.Count == 0)
            {
                return;
            }
            _pins.Clear();
            Changed?.Invoke();
        }

        private int IndexOf(string craft_id)
        {
            for (int i = 0; i < _pins.Count; i++)
            {
                if (_pins[i].Id == craft_id)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
