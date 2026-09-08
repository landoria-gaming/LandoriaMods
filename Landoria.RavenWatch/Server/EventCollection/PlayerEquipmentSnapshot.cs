namespace Landoria.RavenWatch.Server.EventCollection
{
    internal sealed class PlayerEquipmentSnapshot
    {
        public string leftHand;
        public string rightHand;
        public string leftBack;
        public string rightBack;
        public string chest;
        public string legs;
        public string helmet;
        public string shoulder;
        public string utility;
        public string trinket;
        public bool starterOnly;

        internal static PlayerEquipmentSnapshot Capture(ZDO player)
        {
            if (player == null) return null;
            int leftHand = player.GetInt(ZDOVars.s_leftItem);
            int rightHand = player.GetInt(ZDOVars.s_rightItem);
            int leftBack = player.GetInt(ZDOVars.s_leftBackItem);
            int rightBack = player.GetInt(ZDOVars.s_rightBackItem);
            int chest = player.GetInt(ZDOVars.s_chestItem);
            int legs = player.GetInt(ZDOVars.s_legItem);
            int helmet = player.GetInt(ZDOVars.s_helmetItem);
            int shoulder = player.GetInt(ZDOVars.s_shoulderItem);
            int utility = player.GetInt(ZDOVars.s_utilityItem);
            int trinket = player.GetInt(ZDOVars.s_trinketItem);
            return new PlayerEquipmentSnapshot
            {
                leftHand = Name(leftHand), rightHand = Name(rightHand),
                leftBack = Name(leftBack), rightBack = Name(rightBack),
                chest = Name(chest), legs = Name(legs), helmet = Name(helmet),
                shoulder = Name(shoulder), utility = Name(utility), trinket = Name(trinket),
                starterOnly = IsStarterOnly(leftHand, rightHand, leftBack, rightBack,
                    chest, legs, helmet, shoulder, utility, trinket)
            };
        }

        private static bool IsStarterOnly(int leftHand, int rightHand, int leftBack,
            int rightBack, int chest, int legs, int helmet, int shoulder, int utility,
            int trinket)
        {
            int torch = "Torch".GetStableHashCode();
            int rags = "ArmorRagsChest".GetStableHashCode();
            return IsEmptyOr(leftHand, torch) && IsEmptyOr(rightHand, torch) &&
                IsEmptyOr(leftBack, torch) && IsEmptyOr(rightBack, torch) &&
                IsEmptyOr(chest, rags) && legs == 0 && helmet == 0 && shoulder == 0 &&
                utility == 0 && trinket == 0;
        }

        private static bool IsEmptyOr(int value, int allowed) => value == 0 || value == allowed;

        private static string Name(int hash)
        {
            if (hash == 0) return null;
            UnityEngine.GameObject prefab = ZNetScene.instance?.GetPrefab(hash);
            return prefab ? prefab.name : "hash:" + hash;
        }
    }
}
