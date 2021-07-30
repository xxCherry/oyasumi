using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using oyasumi.Database.Models;
using oyasumi.Enums;
using oyasumi.Objects;

namespace oyasumi.Utilities
{
    public static class Calculator
    {
        public static async Task<string> GetBeatmap(string md5, int id = 0)
        {
            var file = $"./data/beatmaps/{md5}.osu";

            if (File.Exists(file))
                return md5;

            using var httpClient = new HttpClient();
            var data = await httpClient.GetByteArrayAsync($"https://osu.ppy.sh/osu/{id}");

            md5 = Crypto.ComputeHash(data); // probably md5 got updated, so re-compute it.

            await File.WriteAllBytesAsync($"./data/beatmaps/{md5}.osu", data);
            return md5;
        }

        public static async Task<string> GetBeatmapByMd5(string md5)
        {
            var file = $"./data/beatmaps/{md5}.osu";

            if (File.Exists(file))
                return md5;

            using var client = new HttpClient();

            var resp = await client.GetAsync($"{Config.Properties.BeatmapMirror}/api/md5/{md5}");

            if (!resp.IsSuccessStatusCode) // if map not found or mirror is down then set status to NotSubmitted
                return string.Empty;

            var beatmap = JsonConvert.DeserializeObject<JsonBeatmap>(await resp.Content.ReadAsStringAsync());

            var requestedDifficulty = beatmap.ChildrenBeatmaps.FirstOrDefault(x => x.FileMD5 == md5);
            var data = await client.GetByteArrayAsync($"https://osu.ppy.sh/osu/{requestedDifficulty.BeatmapID}");

            md5 = Crypto.ComputeHash(data); // probably md5 got updated, so re-compute it.

            await File.WriteAllBytesAsync($"./data/beatmaps/{md5}.osu", data);

            return md5;
        }

        public static async Task<double> CalculatePerformancePoints(DbScore score)
        {
            var beatmapMd5 = await GetBeatmapByMd5(score.FileChecksum);

            if (beatmapMd5 == string.Empty)
                return 0.0;

            var workingBeatmap = new ProcessorWorkingBeatmap($"./data/beatmaps/{beatmapMd5}.osu");

            var psp = new ProcessorScoreDecoder(workingBeatmap);
            var parsedScore = psp.Parse(score);

            var categoryAttribs = new Dictionary<string, double>(System.StringComparer.Ordinal);
            return (double)parsedScore.ScoreInfo.Ruleset
                .CreateInstance()
                .CreatePerformanceCalculator(workingBeatmap, parsedScore.ScoreInfo)
                .Calculate(categoryAttribs);
        }

        public static async Task<double> CalculatePerformancePoints(Score score)
        {
            var beatmapMd5 = await GetBeatmap(score.FileChecksum, score.Beatmap.Id);

            var beatmap = new ProcessorWorkingBeatmap($"./data/beatmaps/{beatmapMd5}.osu");

            var psp = new ProcessorScoreDecoder(beatmap);
            var parsedScore = psp.Parse(score, $"./data/osr/{score.ReplayChecksum}.osr");

            var categoryAttribs = new Dictionary<string, double>(StringComparer.Ordinal);
            return (double)parsedScore.ScoreInfo.Ruleset
                .CreateInstance()
                .CreatePerformanceCalculator(beatmap, parsedScore.ScoreInfo)
                .Calculate(categoryAttribs);
        }

        public static string CalculateRank(DbScore score)
        {
            var tHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss;
            var ratio300 = (float)score.Count300 / tHits;
            var ratio50 = (float)score.Count50 / tHits;
            if (ratio300 == 1)
            {
                return (score.Mods & Mods.Hidden) > 0 ||
                       (score.Mods & Mods.Flashlight) > 0
                    ? "SSHD"
                    : "SS";
            }

            if (ratio300 > 0.9 && ratio50 <= 0.01 && score.CountMiss == 0)
            {
                return (score.Mods & Mods.Hidden) > 0 ||
                       (score.Mods & Mods.Flashlight) > 0
                    ? "SHD"
                    : "S";
            }

            if ((ratio300 > 0.8 && score.CountMiss == 0) || ratio300 > 0.9)
                return "A";
            if ((ratio300 > 0.7 && score.CountMiss == 0) || ratio300 > 0.8)
                return "B";
            return ratio300 > 0.6 ? "C" : "D";
        }

        public static double CalculateAccuracy(Score score)
        {
            int totalHits;
            float accuracy = 0;

            switch (score.PlayMode)
            {
                case PlayMode.Osu:
                    totalHits = score.Count300 + score.Count100 + score.Count50 + score.CountMiss;

                    if (totalHits > 0)
                        accuracy = (float)(((score.Count50 * 50.0) + (score.Count100 * 100.0) + (score.Count300 * 300.0)) / (totalHits * 300.0));

                    return accuracy;

                case PlayMode.Taiko:
                    totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss;
                    return totalHits > 0
                        ? (double)(score.Count100 * 150 + score.Count300 * 300) / (totalHits * 300)
                        : 1;

                case PlayMode.CatchTheBeat:
                    totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss + score.CountKatu;
                    return totalHits > 0 ? (double)(score.Count50 + score.Count100 + score.Count300) / totalHits : 1;

                case PlayMode.OsuMania:
                    totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss + score.CountGeki +
                                score.CountKatu;
                    return totalHits > 0
                        ? (double)(score.Count50 * 50 + score.Count100 * 100 + score.CountKatu * 200 +
                                    (score.Count300 + score.CountGeki) * 300) / (totalHits * 300)
                        : 1;
                default:
                    return 0;
            }
        }
    }
}