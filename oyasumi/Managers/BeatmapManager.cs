using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
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
        public static MultiKeyDictionary<int, string, string, Beatmap> Beatmaps = new ();

        private const string BEATMAP_INSERT_SQL =
            "INSERT INTO Beatmaps " +
            "(" +
                "BeatmapId, FileName, BeatmapSetId, Status, Frozen, " +
                "PlayCount, PassCount, Artist, Title, DifficultyName, " +
                "Creator, BPM, CircleSize, OverallDifficulty, ApproachRate, " +
                "HPDrainRate, Stars" +
            ") " +
            "VALUES " +
            "(" +
                "@BeatmapId, '@FileName', @BeatmapSetId, @Status, @Frozen, " +
                "@PlayCount, @PassCount, '@Artist', '@Title', '@DifficultyName', " +
                "'@Creator', @BPM, @CircleSize, @OverallDifficulty, @ApproachRate, " +
                "@HPDrainRate, @Stars" +
            ")";
        
        /// <summary>
        ///  Getting beatmap by fastest method available
        /// </summary>
        /// <param name="checksum">MD5 checksum of beatmap</param>
        /// <param name="fileName">Name of osu beatmap file</param>
        /// <param name="setId">Beatmap set id</param>
        /// <param name="context">Database instance</param>
        public static async Task<(RankedStatus, Beatmap)> Get
        (
            string checksum, string fileName = "", int setId = 0,
            bool leaderboard = true, PlayMode mode = PlayMode.Osu,
            LeaderboardMode lbMode = LeaderboardMode.Vanilla
        )
        {
            var beatmap = Beatmaps[checksum]; // try get beatmap from local cache

            if (beatmap is not null)
                return beatmap.Id == -1 ? (RankedStatus.NotSubmitted, beatmap) : (RankedStatus.Approved, beatmap);
            
            DbBeatmap dbBeatmap = null;
            await using (var db = MySqlProvider.GetDbConnection())
                dbBeatmap = await db.QueryFirstOrDefaultAsync<DbBeatmap>($"SELECT * From Beatmaps WHERE BeatmapMd5 = '{checksum}'");

            if (fileName.Length > 0)
            {
                await using (var db = MySqlProvider.GetDbConnection())
                {
                    dbBeatmap = await db.QueryFirstOrDefaultAsync<DbBeatmap>(
                        $"SELECT * From Beatmaps WHERE FileName = '{fileName}'");

                    return (RankedStatus.Approved, dbBeatmap.FromDb(leaderboard, mode));
                }
            }

            // if beatmap exists in db we'll add it to local cache
            if (dbBeatmap is not null)
            {
                beatmap = dbBeatmap.FromDb(leaderboard, mode);

                Beatmaps.Add(beatmap.Id, beatmap.FileChecksum, beatmap.BeatmapOsuName, beatmap);
                return (RankedStatus.Approved, beatmap);
            }

            beatmap = await Beatmap.Get(checksum, fileName, leaderboard, mode); // try fetch beatmap from osu!api

            // if beatmap is not submitted, not found or mirror is down
            if (beatmap.Id == -1)
            {
                using var client = new HttpClient();

                var result = await client.GetAsync($"{Config.Properties.BeatmapMirror}/api/s/{setId}");
                
                // try check by beatmap set id
                if (result.IsSuccessStatusCode)
                {
                    // if this set exists, lets add the whole set to database and say client that they need update the map
                    var beatmaps = Beatmap.GetBeatmapSet(setId, fileName, true, mode);
                    await foreach (var b in beatmaps)
                    {
                        await using (var db = MySqlProvider.GetDbConnection())
                        {
                            dbBeatmap = await db.QueryFirstOrDefaultAsync<DbBeatmap>($"SELECT * From Beatmaps WHERE BeatmapMd5 = '{b.FileChecksum}'");
                            if (dbBeatmap is null)
                                await db.ExecuteAsync(BEATMAP_INSERT_SQL, b.ToDb());
                        }
                        Beatmaps.Add(b.Id, b.FileChecksum, b.BeatmapOsuName, b);
                    }
                    return (RankedStatus.NeedUpdate, null);
                }

                Beatmaps.Add(beatmap.Id, beatmap.FileChecksum, beatmap.BeatmapOsuName, beatmap);
                return (RankedStatus.NotSubmitted, null);
            }
            
            // if beatmap exists in api we'll add it to local cache and db
            Beatmaps.Add(beatmap.Id, beatmap.FileChecksum, beatmap.BeatmapOsuName, beatmap);
            
            await using (var db = MySqlProvider.GetDbConnection())
                await db.ExecuteAsync(BEATMAP_INSERT_SQL, beatmap.ToDb());

            return (RankedStatus.Approved, beatmap);
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