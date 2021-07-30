using Org.BouncyCastle.Asn1.Esf;
using oyasumi.Enums;
using oyasumi.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Objects
{
    public class Slot
    {
        public SlotStatus Status = SlotStatus.Open;
        public SlotTeams Team = SlotTeams.Neutral;
        public Mods Mods = Mods.None;
        public Presence Presence;
        public bool Skipped;

        public void CopyFrom(Slot source)
        {
            Mods = source.Mods;
            Presence = source.Presence;
            Status = source.Status;
            Team = source.Team;
        }
         
        public void Clear()
        {
            Mods = Mods.None;
            Presence = null;
            Status = SlotStatus.Open;
            Team = SlotTeams.Neutral;
        }
    }

    public class Match
    {
        public const int MAX_PLAYERS = 16; 
        public bool PasswordRequired => GamePassword is not null;

        public string GameName;
        public int Id { get; set; }
        public readonly Slot[] Slots = new Slot[MAX_PLAYERS];
        public Beatmap Beatmap;
        public string BeatmapChecksum;
        public int BeatmapId = -1;
        public bool InProgress;
        public Mods ActiveMods;
        public Presence Host;
        public int Seed;

        public int NeedLoad;

        public Channel Channel;
        public readonly List<Presence> Presences = new ();

        public MatchTypes Type;
        public PlayMode PlayMode;
        public MatchScoringTypes ScoringType;
        public MatchTeamTypes TeamType;
        public MultiSpecialModes SpecialModes;

        public string GamePassword;

        public bool FreeMods;

        public Match()
        {
            for (var i = 0; i < MAX_PLAYERS; i++)
                Slots[i] = new ();
        }

        public Slot FreeSlot => Slots.FirstOrDefault(slot => slot.Status == SlotStatus.Open);
        
        public void Unready(SlotStatus status)
        {
            foreach (var slot in Slots)
                if (slot.Status == status)
                    slot.Status = SlotStatus.NotReady;
        }

        public async Task Start()
        {
            var hasBeatmapPrs = new List<Presence>();

            foreach (var slot in Slots)
            {
                if ((slot.Status & SlotStatus.HasPlayer) != 0 && slot.Status != SlotStatus.NoMap)
                {
                    slot.Status = SlotStatus.Playing;
                    
                    ++NeedLoad;
                    hasBeatmapPrs.Add(slot.Presence);
                }
            }

            InProgress = true;

            foreach (var presence in hasBeatmapPrs)
                await presence.MatchStart(this);
        }
    }
}
