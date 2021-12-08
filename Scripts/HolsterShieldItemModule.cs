using ThunderRoad;

namespace HolsterShield
{
    /// <summary>
    /// Entry class for the item module.
    /// </summary>
    public class HolsterShieldItemModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            // Load the base class.
            base.OnItemLoaded(item);

            // Is the current level the character selection screen?
            if (string.CompareOrdinal(Level.current.data.id, "CharacterSelection") == 0)
            {
                // Yes, do not initialize further.
                return;
            }

            // Add the HolsterShield component for initialization and set the module.
            item.gameObject.AddComponent<HolsterShield>().ItemModule = this;
        }
    }
}