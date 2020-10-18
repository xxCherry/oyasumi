using oyasumi.Enums;

namespace oyasumi.Objects
{
    public class PresenceStatus
    {
        public ActionStatuses Status { get; set; }
        public string StatusText { get; set; }
        public string BeatmapChecksum { get; set; }
        public Mods CurrentMods { get; set; }
        public PlayMode CurrentPlayMode { get; set; }
        public int BeatmapId { get; set; }
    }
}