namespace MultiRecipePin
{
    internal class PinnedRecipe
    {
        public readonly CraftDefinition Craft;

        // Workstation the recipe was pinned at; its zone storages count as available. May get destroyed.
        public readonly WorldGameObject Craftery;

        // Monotonic counter: smaller = pinned earlier.
        public readonly int Order;

        public bool Collapsed;

        public bool AutoCollapsed;

        public string Id => Craft.id;

        public PinnedRecipe(CraftDefinition craft, WorldGameObject craftery, int order)
        {
            Craft = craft;
            Craftery = craftery;
            Order = order;
        }
    }
}
