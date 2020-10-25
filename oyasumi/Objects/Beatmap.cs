using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using oyasumi.Database;
using oyasumi.Enums;

namespace oyasumi.Objects
{
    public struct BeatmapMetadata
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string DifficultyName { get; set; }
        public string Creator { get; set; }
        public float BPM { get; set; }
        public float CircleSize { get; set; }
        public float OverallDifficulty { get; set; }
        public float ApproachRate { get; set; }
        public float HPDrainRate { get; set; }
        public float Stars { get; set; }
    }

    public struct JsonBeatmap
    {
        public int SetID { get; set; }
        public List<ChildBeatmap> ChildrenBeatmaps { get; set; }
        public int RankedStatus { get; set; }
        public string ApprovedDate { get; set; }
        public string LastUpdate { get; set; }
        public string LastChecked { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Creator { get; set; }
        public string Source { get; set; }
        public string Tags { get; set; }
        public bool HasVideo { get; set; }
        public int Genre { get; set; }
        public int Language { get; set; }
        public int Favourites { get; set; }
    }

    public struct ChildBeatmap
    {
        public int BeatmapID { get; set; }
        public int ParentSetID { get; set; }
        public string DiffName { get; set; }
        public string FileMD5 { get; set; }
        public int Mode { get; set; }
        public float BPM { get; set; }
        public float AR { get; set; }
        public float OD { get; set; }
        public float CS { get; set; }
        public float HP { get; set; }
        public int TotalLength { get; set; }
        public int HitLength { get; set; }
        public int Playcount { get; set; }
        public int Passcount { get; set; }
        public int MaxCombo { get; set; }
        public float DifficultyRating { get; set; }
    }

    public class Beatmap
    {
        public static Dictionary<APIRankedStatus, RankedStatus> ApiToOsuRankedStatus = new()
        {
            [APIRankedStatus.Graveyard] = RankedStatus.LatestPending,
            [APIRankedStatus.WorkInProgress] = RankedStatus.LatestPending,
            [APIRankedStatus.LatestPending] = RankedStatus.LatestPending,
            [APIRankedStatus.Ranked] = RankedStatus.Ranked,
            [APIRankedStatus.Approved] = RankedStatus.Approved,
            [APIRankedStatus.Qualified] = RankedStatus.Qualified,
            [APIRankedStatus.Loved] = RankedStatus.Loved
        };

        public string MD5;
        public int BeatmapId;
        public int BeatmapSetId;
        public RankedStatus Status;
        public bool Frozen;
        public int PlayCount;
        public int PassCount;
        public int OnlineOffset;
        public int MapRating;

        public string Leaderboard;

        public BeatmapMetadata Metadata;
        public string BeatmapName => $"{Metadata.Artist} - {Metadata.Title} [{Metadata.DifficultyName}]";
        public Beatmap(string md5, int id, int setId, BeatmapMetadata metadata, RankedStatus status,
            bool frozen, int playCount, int passCount, int onlineOffset, int mapRating, bool leaderboard, OyasumiDbContext context)
        {
            MD5 = md5;
            BeatmapId = id;
            BeatmapSetId = setId;
            Metadata = metadata;
            Status = status;
            Frozen = frozen;
            PlayCount = playCount;
            PassCount = passCount;
            OnlineOffset = onlineOffset;
            MapRating = mapRating;

            if (leaderboard)
                Leaderboard = Score.GetFormattedScores(context, md5).Result;

        }
        public static async Task<Beatmap> GetBeatmap(string md5, bool leaderboard, OyasumiDbContext context)
        {
            using var client = new HttpClient();

            var resp = await client.GetAsync($"{Config.Properties.BeatmapMirror}/api/md5/{md5}");

            if (!resp.IsSuccessStatusCode) // if map not found or mirror is down then set status to NotSubmitted
                return new Beatmap(md5, -1, -1, new BeatmapMetadata(),
                    RankedStatus.NotSubmitted, false, 0, 0, 0, 0, leaderboard, context);

            var beatmap = JsonConvert.DeserializeObject<JsonBeatmap>(await resp.Content.ReadAsStringAsync());

            var requestedDifficulty = beatmap.ChildrenBeatmaps.FirstOrDefault(x => x.FileMD5 == md5);

            var beatmapMetadata = new BeatmapMetadata
            {
                Artist = beatmap.Artist,
                Title = beatmap.Title,
                Creator = beatmap.Creator,
                DifficultyName = requestedDifficulty.DiffName,
                CircleSize = requestedDifficulty.CS,
                ApproachRate = requestedDifficulty.AR,
                OverallDifficulty = requestedDifficulty.OD,
                HPDrainRate = requestedDifficulty.HP,
                Stars = requestedDifficulty.DifficultyRating
            };

            var status = ApiToOsuRankedStatus[(APIRankedStatus)beatmap.RankedStatus];

            return new Beatmap(md5, requestedDifficulty.BeatmapID, requestedDifficulty.ParentSetID, beatmapMetadata,
                status, false, 0, 0, 0, 0, leaderboard, context);
        }

        public override string ToString()
        {
            return $"{(int)Status}|false|{BeatmapId}|{BeatmapSetId}|50\n" +
                   $"{OnlineOffset}\n" +
                   $"{BeatmapName}\n" +
                   $"{MapRating}\n";
        }
    }
}