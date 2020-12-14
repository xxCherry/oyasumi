using System;
using System.Collections.Concurrent;
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
        [JsonProperty("artist")] public string Artist { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("difficulty_name")] public string DifficultyName { get; set; }
        [JsonProperty("creator")] public string Creator { get; set; }
        [JsonProperty("bpm")] public float BPM { get; set; }
        [JsonProperty("cs")] public float CircleSize { get; set; }
        [JsonProperty("od")]public float OverallDifficulty { get; set; }
        [JsonProperty("ar")] public float ApproachRate { get; set; }
        [JsonProperty("hp")] public float HPDrainRate { get; set; }
        [JsonProperty("stars")] public float Stars { get; set; }
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

        public static Dictionary<int, APIRankedStatus> DirectToApiRankedStatus = new()
        {
            [0] = APIRankedStatus.Ranked,
            [2] = APIRankedStatus.LatestPending,
            [3] = APIRankedStatus.Qualified,
            [5] = APIRankedStatus.LatestPending,
            [7] = APIRankedStatus.Ranked,
            [8] = APIRankedStatus.Loved
        };

        [JsonProperty("file_checksum")] public string FileChecksum;
        [JsonProperty("file_name")] public string FileName;
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("set_id")] public int SetId { get; set; }
        [JsonProperty("status")] public RankedStatus Status { get; set; }
        [JsonProperty("frozen")] public bool Frozen;
        [JsonProperty("play_count")] public int PlayCount;
        
        [JsonProperty("pass_count")] public int PassCount;
        [JsonProperty("online_offset")] public int OnlineOffset;
        [JsonProperty("rating")] public int Rating;
        
        [JsonIgnore]
        public ConcurrentDictionary<LeaderboardMode, ConcurrentDictionary<PlayMode, List<string>>> LeaderboardFormatted = new();
        
        [JsonIgnore]
        public ConcurrentDictionary<LeaderboardMode, ConcurrentDictionary<PlayMode, ConcurrentDictionary<int, Score>>> LeaderboardCache = new(); // int is user id

        [JsonProperty("metadata")] public BeatmapMetadata Metadata { get; set; }
        public string BeatmapName => $"{Metadata.Artist} - {Metadata.Title} [{Metadata.DifficultyName}]" ;
        public Beatmap
        (
            string md5, string fileName, int id, int setId, BeatmapMetadata metadata, RankedStatus status,
            bool frozen, int playCount, int passCount, int onlineOffset, int mapRating, bool leaderboard, 
            PlayMode mode, LeaderboardMode lbMode, OyasumiDbContext context
        )
        {
            FileChecksum = md5;
            FileName = fileName;
            Id = id;
            SetId = setId;
            Metadata = metadata;
            Status = status;
            Frozen = frozen;
            PlayCount = playCount;
            PassCount = passCount;
            OnlineOffset = onlineOffset;
            Rating = mapRating;

            if (leaderboard)
            {
                Task.WaitAll(Task.Run(async () =>
                {
                    var vanillaScores = await Score.GetRawScores(context, md5, mode, LeaderboardMode.Vanilla);
                    var relaxScores = await Score.GetRawScores(context, md5, mode, LeaderboardMode.Relax);

                    for (var i = 0; i < 3; i++)
                    {
                        LeaderboardCache.TryAdd((LeaderboardMode)i, new());
                        LeaderboardFormatted.TryAdd((LeaderboardMode)i, new());

                        for (var j = 0; j < 4; j++)
                        {
                            LeaderboardCache[(LeaderboardMode)i].TryAdd((PlayMode)j, new());
                            LeaderboardFormatted[(LeaderboardMode)i].TryAdd((PlayMode)j, new());
                        }
                    }

                    foreach (var score in vanillaScores)
                        LeaderboardCache[LeaderboardMode.Vanilla][mode].TryAdd(score.UserId, score);
                    foreach (var score in relaxScores)
                        LeaderboardCache[LeaderboardMode.Relax][mode].TryAdd(score.UserId, score);

                    LeaderboardFormatted[LeaderboardMode.Vanilla][mode] = Score.FormatScores(vanillaScores, mode);
                    LeaderboardFormatted[LeaderboardMode.Relax][mode] = Score.FormatScores(relaxScores, mode);

                }));
            }
        }
        public static async Task<Beatmap> Get(string md5, string fileName, bool leaderboard, PlayMode mode, LeaderboardMode lbMode, OyasumiDbContext context)
        {
            using var client = new HttpClient();

            var resp = await client.GetAsync($"{Config.Properties.BeatmapMirror}/api/md5/{md5}");

            if (!resp.IsSuccessStatusCode) // if map not found or mirror is down then set status to NotSubmitted
                return new Beatmap(md5, fileName, -1, -1, new BeatmapMetadata(),
                    RankedStatus.NotSubmitted, false, 0, 0, 0, 0, leaderboard, mode, lbMode, context);

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

            return new Beatmap(md5, fileName, requestedDifficulty.BeatmapID, requestedDifficulty.ParentSetID, beatmapMetadata,
                status, false, 0, 0, 0, 0, leaderboard, mode, lbMode, context);
        }

        public void ClearLeaderboard()
        {
            for (var i = 0; i < 3; i++)
            {
                LeaderboardCache[(LeaderboardMode)i] = new();
                LeaderboardFormatted[(LeaderboardMode)i] = new();

                for (var j = 0; j < 4; j++)
                {
                    LeaderboardCache[(LeaderboardMode)i][(PlayMode)j] = new();
                    LeaderboardFormatted[(LeaderboardMode)i][(PlayMode)j] = new();
                }
            }
        }

        public string ToString(PlayMode mode, LeaderboardMode lbMode)
        {
            return $"{(int)Status}|false|{Id}|{SetId}|{LeaderboardFormatted[lbMode][mode].Count}\n" +
                   $"{OnlineOffset}\n" +
                   $"{BeatmapName}\n" +
                   $"{Rating}";
        }
    }
}