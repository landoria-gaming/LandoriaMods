using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Serialization
{
    internal static class CharacterSchema
    {
        internal static void Profile(BinaryJson io, JObject obj)
        {
            io.Version(obj, 46);
            int stats = io.Int(obj, "statCount");
            io.List(obj, "statGroups", group => Stats(io, group, stats));
            io.Bool(obj, "firstSpawn");
            io.List(obj, "worlds", world => World(io, world));
            io.String(obj, "playerName");
            io.Long(obj, "playerId");
            io.String(obj, "startSeed");
            io.Bool(obj, "usedCheats");
            io.Long(obj, "dateCreated");
            if (io.Bool(obj, "hasPlayerData")) io.Blob(obj, "playerData", Player);
        }

        private static void Stats(BinaryJson io, JObject obj, int count)
        {
            if (count < 0 || count > 100000) throw new System.IO.InvalidDataException("Invalid stat count.");
            io.List(obj, "values", value => io.Float(value, "value"), fixedCount: count);
            Floats(io, obj, "knownWorlds");
            Floats(io, obj, "knownWorldKeys");
            Floats(io, obj, "knownCommands");
            io.List(obj, "enemyStats", group => Floats(io, group, "values"));
            foreach (string name in new[] { "itemPickupStats", "itemCraftStats", "pickableStats", "foodEatenStats", "piecesPlacedStats" })
                Floats(io, obj, name);
        }

        private static void World(BinaryJson io, JObject obj)
        {
            io.Long(obj, "worldId");
            io.Bool(obj, "haveCustomSpawnPoint");
            io.Vector(obj, "spawnPoint");
            io.Bool(obj, "haveLogoutPoint");
            io.Vector(obj, "logoutPoint");
            io.Bool(obj, "haveDeathPoint");
            io.Vector(obj, "deathPoint");
            io.Vector(obj, "homePoint");
            if (io.Bool(obj, "hasMapData")) io.Blob(obj, "mapData");
        }

        internal static void Player(BinaryJson io, JObject obj)
        {
            io.Version(obj, 33);
            foreach (string name in new[] { "maxHealth", "health", "maxStamina", "timeSinceDeath" }) io.Float(obj, name);
            io.String(obj, "guardianPower");
            io.Float(obj, "guardianPowerCooldown");
            io.Object(obj, "inventory", inventory => Inventory(io, inventory));
            Strings(io, obj, "knownRecipes");
            io.List(obj, "knownStations", entry => { io.String(entry, "key"); io.Int(entry, "value"); });
            foreach (string name in new[] { "knownMaterial", "shownTutorials", "uniques", "trophies", "knownBiome" })
                Strings(io, obj, name);
            Pairs(io, obj, "knownTexts");
            io.String(obj, "beardItem");
            io.String(obj, "hairItem");
            io.Vector(obj, "skinColor");
            io.Vector(obj, "hairColor");
            io.Int(obj, "modelIndex");
            io.List(obj, "foods", food => { io.String(food, "name"); io.Float(food, "time"); });
            io.Object(obj, "skills", skills => Skills(io, skills));
            Pairs(io, obj, "customData");
            foreach (string name in new[] { "stamina", "maxEitr", "eitr" }) io.Float(obj, name);
            io.Blob(obj, "buildUi");
        }

        internal static void Inventory(BinaryJson io, JObject obj)
        {
            io.Version(obj, 109);
            io.List(obj, "items", item => Item(io, item), shortCount: true);
        }

        internal static void Item(BinaryJson io, JObject obj)
        {
            io.Int(obj, "durabilityHundredths");
            io.Byte(obj, "slotX");
            io.Byte(obj, "slotY");
            io.Byte(obj, "worldLevel");
            byte flags = io.Byte(obj, "flags");
            if ((flags & 4) != 0) io.UShort(obj, "quality");
            if ((flags & 8) != 0) io.UShort(obj, "quantity");
            if ((flags & 16) != 0) io.Int(obj, "variant");
            if ((flags & 32) != 0) { io.Long(obj, "crafterId"); io.String(obj, "crafterName"); }
            if ((flags & 64) != 0) io.Int(obj, "prefabHash");
            if ((flags & 128) != 0)
            {
                if (io.Writing) obj["customDataCount"] = ((JArray)BinaryJson.Required(obj, "customData")).Count;
                int count = io.CompactCount(obj, "customDataCount");
                io.List(obj, "customData", entry => { io.String(entry, "key"); io.String(entry, "value"); }, fixedCount: count);
            }
            io.Byte(obj, "extraFlags");
        }

        internal static void Skills(BinaryJson io, JObject obj)
        {
            io.Version(obj, 2);
            io.List(obj, "values", skill =>
            {
                io.Int(skill, "type");
                io.Float(skill, "level");
                io.Float(skill, "accumulator");
            });
        }

        private static void Strings(BinaryJson io, JObject obj, string name)
            => io.List(obj, name, entry => io.String(entry, "value"));
        private static void Floats(BinaryJson io, JObject obj, string name)
            => io.List(obj, name, entry => { io.String(entry, "key"); io.Float(entry, "value"); });
        private static void Pairs(BinaryJson io, JObject obj, string name)
            => io.List(obj, name, entry => { io.String(entry, "key"); io.String(entry, "value"); });
    }
}
