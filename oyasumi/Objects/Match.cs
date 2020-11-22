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
        public bool Loaded;
    }

    public class Match
    {
        public const int MAX_PLAYERS = 16; 
        public bool PasswordRequired => GamePassword != null;

        public string GameName;
        public int Id { get; set; }
        public Slot[] Slots = new Slot[MAX_PLAYERS];
        public Beatmap Beatmap;
        public string BeatmapChecksum;
        public int BeatmapId = -1;
        public bool InProgress;
        public Mods ActiveMods;
        public Presence Host;
        public int Seed;

        public int NeedLoad;

        public Channel Channel;
        public List<Presence> Presences = new List<Presence>();

        public MatchTypes Type;
        public PlayMode PlayMode;
        public MatchScoringTypes ScoringType;
        public MatchTeamTypes TeamType;
        public MultiSpecialModes SpecialModes;

        public string GamePassword;
        public bool SendPassword;

        public bool FreeMods;

        public Match()
        {
            for (var i = 0; i < MAX_PLAYERS; i++)
                Slots[i] = new Slot();
        }

        public Slot FreeSlot
        {
            get 
            {
                foreach (var slot in Slots)
                    if (slot.Status == SlotStatus.Open)
                        return slot;
                return null;
            }
        }

        public void Unready(SlotStatus status)
        {
            foreach (var slot in Slots)
                if (slot.Status == status)
                    slot.Status = SlotStatus.NotReady;
        }

        public void UnreadyEveryone()
        {
            foreach (var slot in Slots)
                if (slot.Status == SlotStatus.Ready)  // if player ready
                    slot.Status = SlotStatus.NotReady; // then unready him
        }

        public void Start()
        {
            var hasBeatmapPrs = new List<Presence>();

            foreach (var slot in Slots)
            {
                if ((slot.Status & SlotStatus.HasPlayer) > 0)
                {
                    if (slot.Status != SlotStatus.NoMap)
                    {
                        slot.Status = SlotStatus.Playing;
                        ++NeedLoad;
                        hasBeatmapPrs.Add(slot.Presence);
                    }
                }
            }

            InProgress = true;

            foreach (var presence in hasBeatmapPrs)
                presence.MatchStart(this);
        }
    }
}
