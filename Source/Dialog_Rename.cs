namespace WeaponStorage
{
    class Dialog_Rename : Verse.Dialog_Rename
    {
        private Building_WeaponStorage WeaponStorage;

        public Dialog_Rename(Building_WeaponStorage weaponStorage) : base()
        {
            this.WeaponStorage = weaponStorage;
            base.curName = weaponStorage.Name;
        }

        protected override void SetName(string name)
        {
            WeaponStorage.Name = name;
        }
    }
}
