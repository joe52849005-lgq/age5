using System.Collections.Concurrent;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Containers;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.NPChar
{
    public class Tagging
    {
        private ConcurrentDictionary<Character, int> _taggers = [];
        //private int _totalDamage;

        public Unit Owner { get; }

        public Tagging(Unit owner)
        {
            Owner = owner;
        }

        public Character Tagger { get; private set; }

        public uint TagTeam { get; private set; }

        public void ClearAllTaggers()
        {
            _taggers = [];
            Tagger = null;
            TagTeam = 0;
            //_totalDamage = 0;
        }

        public void AddTagger(Unit checkUnit, int damage)
        {
            // Check if the character is a pet, if so, propagate its user
            if (checkUnit is Units.Mate mate)
            {
                checkUnit = WorldManager.Instance.GetCharacterByObjId(mate.OwnerObjId) ?? checkUnit;
            }

            if (checkUnit is not Character player)
            {
                return;
            }

            if (_taggers.TryAdd(player, damage))
            {
                Tagger ??= player;
            }
            else
            {
                _taggers[player] += damage;
            }

            //_totalDamage += damage;

            // Check if the character is in a party
            if (player.InParty)
            {
                var checkTeam = TeamManager.Instance.GetTeamByObjId(player.ObjId);
                var partyDamage = 0;
                foreach (var member in checkTeam.Members)
                {
                    if (member?.Character == null)
                        continue;

                    if (!(member.Character.GetDistanceTo(Owner, true) <= LootingContainer.MaxLootingRange))
                    {
                        continue;
                    }

                    // tm is an eligible party member
                    if (_taggers.TryGetValue(member.Character, out var value))
                    {
                        // Tagger is already in the list
                        partyDamage += value;
                    }
                }
                // Did the party do more than 50% of the total HP in damage?
                if (partyDamage > Owner.MaxHp * 0.5)
                {
                    TagTeam = checkTeam.Id;
                }
            }
            else
            {
                if (_taggers[player] > Owner.MaxHp * 0.5)
                {

                    Tagger = player;
                }
            }
            // TODO: packet to set red-but-not-aggro HP bar for taggers, "dull red" HP bar for not-taggers
        }
    }
}
