using Verse;

namespace WeaponStorage.UI
{
    struct WeaponSelected
    {
        public ThingWithComps thing;
        public bool isChecked;

        public WeaponSelected(ThingWithComps thing, bool isChecked)
        {
            this.thing = thing;
            this.isChecked = isChecked;
        }
    }
}
