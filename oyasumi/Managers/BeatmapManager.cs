using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using osu.Game.Screens.Edit.Components;
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
        public static TwoKeyDictionary<int, string, Beatmap> Beatmaps = new ();
        
        /// <summary>
        ///  Getting beatmap by fastest method available
        /// </summary>
        /// <param name="checksum">MD5 checksum of beatmap</param>
        /// <param name="fileName">Name of osu beatmap file</param>
        /// <param name="setId">Beatmap set id</param>
        /// <param name="context">Database instance</param>
        public static async Task<(RankedStatus, Beatmap)> Get(string checksum, string fileName, int setId, OyasumiDbContext context, bool leaderboard = true, PlayMode mode = PlayMode.Osu, LeaderboardMode lbMode = LeaderboardMode.Vanilla)
        {
            var beatmap = Beatmaps[checksum]; // try get beatmap from local cache

            if (beatmap is not null)
            {
                return beatmap.Id == -1 ? (RankedStatus.NotSubmitted, beatmap) : (RankedStatus.Approved, beatmap);
            }

            var dbBeatmap = context.Beatmaps.AsNoTracking().FirstOrDefault(x => x.BeatmapMd5 == checksum); // try get beatmap from db

            // if beatmap exists in db we'll add it to local cache
            if (dbBeatmap is not null)
            {
                beatmap = dbBeatmap.FromDb(context, leaderboard, mode, lbMode);

                Beatmaps.Add(beatmap.Id, beatmap.FileChecksum, beatmap);
                return (RankedStatus.Approved, beatmap);
            }

            beatmap = await Beatmap.Get(checksum, fileName, leaderboard, mode, lbMode, context); // try get beatmap from osu!api

            if (beatmap.Id == -1)
            {
                dbBeatmap = context.Beatmaps.AsNoTracking().FirstOrDefault(x => x.FileName == fileName);

                if (dbBeatmap is not null)
                    return (RankedStatus.NeedUpdate, null);
                else
                {
                    using var client = new HttpClient();
                    var result = await client.GetAsync($"{Config.Properties.BeatmapMirror}/api/s/{setId}");

                    if (result.IsSuccessStatusCode)
                        return (RankedStatus.NeedUpdate, null);
                    else
                    {
                        Beatmaps.Add(beatmap.Id, beatmap.FileChecksum, beatmap);
                        return (RankedStatus.NotSubmitted, null);
                    }
                }
            }

            // if beatmap exists in api we'll add it to local cache and db
            Beatmaps.Add(beatmap.Id, beatmap.FileChecksum, beatmap);
            await context.Beatmaps.AddAsync(beatmap.ToDb());

            await context.SaveChangesAsync();
            
            return (RankedStatus.Approved, beatmap);
        }
        public static Beatmap FromDb(this DbBeatmap b, OyasumiDbContext context, bool leaderboard, PlayMode mode, LeaderboardMode lbMode)
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
            return new Beatmap(b.BeatmapMd5, b.FileName, b.BeatmapId, b.BeatmapSetId, metadata,
                b.Status, false, 0, 0, 0, 0, leaderboard, mode, lbMode, context);
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