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

    public struct OsuJsonBeatmap
    {
        public int beatmapset_id { get; set; }
        public int beatmap_id { get; set; }
        public int approved { get; set; }
        public int total_length { get; set; }
        public int hit_length { get; set; }
        [JsonProperty(PropertyName = "version")] public string Difficulty { get; set; }
        [JsonProperty(PropertyName = "file_md5")] public string MD5 { get; set; }
        [JsonProperty(PropertyName = "diff_size")] public float CS { get; set; }
        [JsonProperty(PropertyName = "diff_overall")] public float OD { get; set; }
        [JsonProperty(PropertyName = "diff_approach")] public float AR { get; set; }
        [JsonProperty(PropertyName = "diff_drain")] public float HP { get; set; }
        public int mode { get; set; }
        public DateTime? submit_date { get; set; }
        public DateTime? approved_date { get; set; }
        public DateTime last_update { get; set; }
        public string artist { get; set; }
        public string title { get; set; }
        public string creator { get; set; }
        public string creator_id { get; set; }
        public float bpm { get; set; }
        public string source { get; set; }
        public string tags { get; set; }
        public string genre_id { get; set; }
        public string language_id { get; set; }
        public int favourite_count { get; set; }
        public float rating { get; set; }
        public int playcount { get; set; }
        public int passcount { get; set; }
        public int? max_combo { get; set; }
        [JsonProperty(PropertyName = "difficultyrating")] public float StarRating { get; set; }
        [JsonProperty(PropertyName = "diff_aim")] public float? AimRating { get; set; }
        [JsonProperty(PropertyName = "diff_speed")] public float? SpeedRating { get; set; }
        [JsonProperty(PropertyName = "count_normal")] public int CountNormal { get; set; }
        [JsonProperty(PropertyName = "count_slider")] public int CountSlider { get; set; }
        [JsonProperty(PropertyName = "count_spinner")] public int CountSpinner { get; set; }
        [JsonProperty(PropertyName = "artist_unicode")] public string ArtistUnicode { get; set; }
        [JsonProperty(PropertyName = "title_unicode")] public string TitleUnicode { get; set; }
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
        public ConcurrentDictionary<LeaderboardMode, ConcurrentDictionary<PlayMode, List<string>>>
            LeaderboardFormatted = new();

        [JsonIgnore]
        public ConcurrentDictionary<LeaderboardMode, ConcurrentDictionary<PlayMode, ConcurrentDictionary<int, Score>>>
            LeaderboardCache = new(); // int is user id

        [JsonProperty("metadata")] public BeatmapMetadata Metadata { get; set; }
        [JsonProperty("beatmap_name")] public string Name 
            => $"{Metadata.Artist} - {Metadata.Title} ({Metadata.Creator}) [{Metadata.DifficultyName}]";
        [JsonIgnore] public string OsuFileName => $"{Name}.osu";

        public Beatmap
        (
            string md5, string fileName, int id, int setId, BeatmapMetadata metadata, RankedStatus status,
            bool frozen, int playCount, int passCount, int onlineOffset, int mapRating, bool leaderboard,
            PlayMode mode
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
                InitializeLeaderboard(mode);
        }

        public static async Task<Beatmap> Get(string md5, string fileName, bool leaderboard, PlayMode mode)
        {
            using var client = new HttpClient();

            var resp = await client.GetAsync($"{Config.Properties.OsuApiUrl}/api/get_beatmaps?h={md5}&k={Config.Properties.OsuApiKey}");

            if (!resp.IsSuccessStatusCode)
                return null;

            var beatmaps = JsonConvert.DeserializeObject<OsuJsonBeatmap[]>(await resp.Content.ReadAsStringAsync());

            var requestedDifficulty = beatmaps.FirstOrDefault(x => x.MD5 == md5);

            var beatmapMetadata = new BeatmapMetadata
            {
                Artist = requestedDifficulty.artist,
                Title = requestedDifficulty.title,
                Creator = requestedDifficulty.creator,
                DifficultyName = requestedDifficulty.Difficulty,
                CircleSize = requestedDifficulty.CS,
                ApproachRate = requestedDifficulty.AR,
                OverallDifficulty = requestedDifficulty.OD,
                HPDrainRate = requestedDifficulty.HP,
                Stars = requestedDifficulty.StarRating
            };

            var status = ApiToOsuRankedStatus[(APIRankedStatus)requestedDifficulty.approved];

            return new (md5, fileName, requestedDifficulty.beatmap_id, requestedDifficulty.beatmapset_id, beatmapMetadata,
                status, false, 0, 0, 0, 0, leaderboard, mode);
        }

        public static async IAsyncEnumerable<Beatmap> GetBeatmapSet(int setId, string fileName, bool leaderboard, PlayMode mode)
        {
            using var client = new HttpClient();

            var resp = await client.GetAsync($"{Config.Properties.BeatmapMirror}/s/{setId}");

            var respContent = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode || respContent == "null")
                yield return null;

            var beatmap = JsonConvert.DeserializeObject<JsonBeatmap>(respContent);

            foreach (var b in beatmap.ChildrenBeatmaps)
            {
                var beatmapMetadata = new BeatmapMetadata
                {
                    Artist = beatmap.Artist,
                    Title = beatmap.Title,
                    Creator = beatmap.Creator,
                    DifficultyName = b.DiffName,
                    CircleSize = b.CS,
                    ApproachRate = b.AR,
                    OverallDifficulty = b.OD,
                    HPDrainRate = b.HP,
                    Stars = b.DifficultyRating
                };

                var status = ApiToOsuRankedStatus[(APIRankedStatus)beatmap.RankedStatus];

                yield return new(b.FileMD5, fileName, b.BeatmapID, b.ParentSetID, beatmapMetadata,
                    status, false, 0, 0, 0, 0, leaderboard, mode);
            }
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

        public void UpdateLeaderboard(LeaderboardMode lbMode, PlayMode mode)
        {
            var scores = Score.GetRawScores(FileChecksum, mode, Status, lbMode);

            var leaderboard = LeaderboardCache[lbMode][mode];
            leaderboard.Clear(); // Clear the cache

            foreach (var score in scores)
                leaderboard.TryAdd(score.UserId, score);

            LeaderboardFormatted[lbMode][mode] = Score.FormatScores(scores, Status);
        }

        public void InitializeLeaderboard(PlayMode mode)
        {
            var vanillaScores = Score.GetRawScores(FileChecksum, mode, Status, LeaderboardMode.Vanilla);
            var relaxScores = Score.GetRawScores(FileChecksum, mode, Status, LeaderboardMode.Relax);

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

            LeaderboardFormatted[LeaderboardMode.Vanilla][mode] = Score.FormatScores(vanillaScores, Status);
            LeaderboardFormatted[LeaderboardMode.Relax][mode] = Score.FormatScores(relaxScores, Status);
        }

        public string ToString(PlayMode mode, LeaderboardMode lbMode)
        {
            return $"{(int)Status}|false|{Id}|{SetId}|{(Status == RankedStatus.LatestPending ? 0 : LeaderboardFormatted[lbMode][mode].Count)}\n" +
                   $"{OnlineOffset}\n" +
                   $"{Name}\n" +
                   $"{Rating}";
        }
    }
}