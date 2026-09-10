using Landoria.RavenWatch.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Landoria.RavenWatch.Client
{
    internal static class InventoryEntryDates
    {
        internal const string DateKey = "Landoria.RavenWatch.lastAddedUtc";
        internal const string CharacterKey = "Landoria.RavenWatch.lastAddedCharacterId";
        internal static int Suppressed;
        internal static int InternalMove;

        internal sealed class State
        {
            internal long CharacterId;
            internal Dictionary<ItemDrop.ItemData, int> Quantities;
            internal bool Internal;
            internal string SourceDate;
            internal string SourceCharacter;
        }

        internal static State Before(global::Inventory inventory, ItemDrop.ItemData input)
        {
            try
            {
                var player = Player.m_localPlayer;
                if (Suppressed != 0 || player == null || player.GetInventory() != inventory) return null;
                input.m_customData.TryGetValue(DateKey, out string sourceDate);
                input.m_customData.TryGetValue(CharacterKey, out string sourceCharacter);
                return new State { CharacterId = player.GetPlayerID(),
                    Internal = InternalMove != 0 || inventory.ContainsItem(input),
                    SourceDate = sourceDate, SourceCharacter = sourceCharacter,
                    Quantities = inventory.GetAllItems().ToDictionary(item => item, item => item.m_stack) };
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); return null; }
        }

        internal static void After(global::Inventory inventory, State state)
        {
            if (state == null) return;
            try
            {
                var player = Player.m_localPlayer;
                if (player == null || player.GetInventory() != inventory || player.GetPlayerID() != state.CharacterId) return;
                string utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                foreach (var item in inventory.GetAllItems())
                {
                    state.Quantities.TryGetValue(item, out int before);
                    if (item.m_stack <= before) continue;
                    Stamp(item, state, utc);
                }
            }
            catch (Exception error) { RavenWatchLog.Log.LogError(error); }
        }

        private static void Stamp(ItemDrop.ItemData item, State state, string utc)
        {
            string character = state.CharacterId.ToString(CultureInfo.InvariantCulture);
            if (state.Internal)
            {
                if (state.SourceCharacter != character || !DateTime.TryParseExact(state.SourceDate, "O",
                    CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var source)) return;
                item.m_customData.TryGetValue(DateKey, out string currentDate);
                if (DateTime.TryParseExact(currentDate, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var current) && current >= source) return;
                utc = state.SourceDate;
            }
            item.m_customData[DateKey] = utc;
            item.m_customData[CharacterKey] = character;
        }
    }
}
