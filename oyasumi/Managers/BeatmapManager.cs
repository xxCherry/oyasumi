using System.Linq;
using System.Threading.Tasks;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Extensions;
using oyasumi.Objects;
using oyasumi.Utilities;

namespace oyasumi.Managers
{
    public struct BeatmapTitle
    {
        public string Title;
        public string Artist;
        public string Creator;
        public string Difficulty;
        }

    public static class BeatmapManager
    {
        public static MultiKeyDictionary<int, string, Beatmap> Beatmaps = new();

        /// <summary>
        ///  Getting beatmap by fastest method available
        /// </summary>
        /// <param name="checksum">MD5 checksum of beatmap</param>
        /// <param name="fileName">Name of osu beatmap file</param>
        /// <param name="setId">Beatmap set id</param>
        /// <param name="leaderboard">Leaderboard</param>
        /// <param name="mode">GameMode</param>
        public static async Task<Beatmap> Get
        (
            string checksum = "", string fileName = "", int setId = 0,
            bool leaderboard = true, PlayMode mode = PlayMode.Osu
        )
        {
            if (string.IsNullOrEmpty(checksum) && string.IsNullOrEmpty(fileName) && setId == 0)
                return null;

            var beatmap = DbContext.Beatmaps[checksum];
            var cachedBeatmap = Beatmaps[checksum];

            if (cachedBeatmap is null && beatmap is not null)
                Beatmaps.Add(beatmap.BeatmapId, beatmap.BeatmapMd5, beatmap.FromDb(leaderboard, mode));

            if (beatmap is null)
            {
                if (setId == -1)
                {
                    var apiBeatmap = await Beatmap.Get(checksum, fileName, leaderboard, mode);

                    if (apiBeatmap is null)
                        return null;

                    setId = apiBeatmap.SetId;
                }

                // Let's try to find beatmap by set id
                beatmap = DbContext.Beatmaps.Values.Where(x => x.BeatmapSetId == setId).FirstOrDefault();

                var beatmapSet = Beatmap.GetBeatmapSet(setId, fileName, leaderboard, mode);

                if (beatmap is not null) // If beatmap is already exist then we need to set status to NeedUpdate
                {
                    DbContext.Beatmaps.ExecuteWhere(x => x.BeatmapSetId == setId, (beatmap) => 
                    {
                        beatmap.Status = RankedStatus.NeedUpdate;
                        return beatmap;
                    });

                    Beatmaps.ExecuteWhere(x => x.SetId == setId, (beatmap) => 
                    {
                        beatmap.Status = RankedStatus.NeedUpdate;
                        return beatmap;
                    });
                }

                await foreach (var b in beatmapSet)
                {
                    if (b is null)
                        return null;

                    DbContext.Beatmaps.Add(b.Id, b.FileChecksum, b.ToDb());
                    Beatmaps.Add(b.Id, b.FileChecksum, b);
                }
            }

            return Beatmaps[checksum];
        }

        public static Beatmap FromDb(this DbBeatmap b, bool leaderboard, PlayMode mode)
        {
            var metadata = new BeatmapMetadata
            {
                Artist = b.Artist,
                Title = b.Title,
                DifficultyName = b.DifficultyName,
                Creator = b.Creator,
                ApproachRate = b.ApproachRate,
                CircleSize = b.CircleSize,
                OverallDifficulty = b.OverallDifficulty,
                HPDrainRate = b.HPDrainRate,
                BPM = b.BPM,
                Stars = b.Stars
            };
            return new (b.BeatmapMd5, b.FileName, b.BeatmapId, b.BeatmapSetId, metadata,
                b.Status, false, 0, 0, 0, 0, leaderboard, mode);
        }

        public static DbBeatmap ToDb(this Beatmap b)
        {
            return new()
            {
                Artist = b.Metadata.Artist,
                Title = b.Metadata.Title,
                DifficultyName = b.Metadata.DifficultyName,
                Creator = b.Metadata.Creator,
                ApproachRate = b.Metadata.ApproachRate,
                CircleSize = b.Metadata.CircleSize,
                OverallDifficulty = b.Metadata.OverallDifficulty,
                HPDrainRate = b.Metadata.HPDrainRate,
                BPM = b.Metadata.BPM,
                Stars = b.Metadata.Stars,
                BeatmapMd5 = b.FileChecksum,
                BeatmapId = b.Id,
                BeatmapSetId = b.SetId,
                Status = b.Status,
                Frozen = b.Frozen,
                PlayCount = b.PlayCount,
                PassCount = b.PassCount,
                FileName = b.FileName
            };
        }
    }
}