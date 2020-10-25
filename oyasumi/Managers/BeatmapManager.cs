using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using oyasumi.Database;
using oyasumi.Database.Models;
using oyasumi.Enums;
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
        // Beatmap local cache
        public static TwoKeyDictionary<int, string, Beatmap> Beatmaps = new TwoKeyDictionary<int, string, Beatmap>();
        
        /// <summary>
        ///  Getting beatmap by fastest method available
        /// </summary>
        /// <param name="checksum">MD5 checksum of beatmap</param>
        /// <param name="title">Beatmap title object</param>
        public static async Task<(RankedStatus, Beatmap)> Get(string checksum, BeatmapTitle title, bool leaderboard)
        {
            var beatmap = Beatmaps[checksum]; // try get beatmap from local cache

            if (beatmap is not null)
            {  
                if (beatmap.BeatmapId == -1)
                    return (RankedStatus.NotSubmitted, beatmap);
                else
                    return (RankedStatus.Approved, beatmap);                  // Approved is not actual ranked status
                                                                              // just for handling them after calling Get()
            }

            var context = new OyasumiDbContext();

            var dbBeatmap = context.Beatmaps.FirstOrDefault(x => x.BeatmapMd5 == checksum); // try get beatmap from db

            // if beatmap exists in db we'll add it to local cache
            if (dbBeatmap is not null)
            {
                beatmap = dbBeatmap.FromDb(leaderboard);
                
                Beatmaps.Add(beatmap.BeatmapId, beatmap.MD5, beatmap);
                return (RankedStatus.Approved, beatmap);
            }

            beatmap = await Beatmap.GetBeatmap(checksum, leaderboard); // try get beatmap from osu!api

            if (beatmap.BeatmapId == -1)
            {
                //var dbBeatmap = context.Beatmaps.FirstOrDefault(x => x.); 
                Beatmaps.Add(beatmap.BeatmapId, beatmap.MD5, beatmap);
                return (RankedStatus.NotSubmitted, beatmap); // beatmap doesn't exist
            }

            // if beatmap exists in api we'll add it to local cache and db
            Beatmaps.Add(beatmap.BeatmapId, beatmap.MD5, beatmap);
            await context.Beatmaps.AddAsync(beatmap.ToDb());
            
            return (RankedStatus.Approved, beatmap);

        }

        public static Beatmap FromDb(this DbBeatmap b, bool leaderboard)
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
            return new Beatmap(b.BeatmapMd5, b.BeatmapId, b.BeatmapSetId, metadata,
                b.Status, false, 0, 0, 0, 0, leaderboard);
        }
        
        public static DbBeatmap ToDb(this Beatmap b)
        {
            return new DbBeatmap
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
                BeatmapMd5 = b.MD5,
                BeatmapId = b.BeatmapId,
                BeatmapSetId = b.BeatmapSetId,
                Status = b.Status,
                Frozen = b.Frozen,
                PlayCount = b.PlayCount,
                PassCount = b.PassCount
            };
        }
    }
}