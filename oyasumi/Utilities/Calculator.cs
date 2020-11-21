using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using oyasumi.Enums;

namespace oyasumi.Utilities
{
    public class Calculator
    {
        public static async Task<string> GetBeatmap(string md5, int id = 0)
        {
            var file = $"./data/beatmaps/{md5}.osu";

            if (File.Exists(file))
                return md5;
            else
            {
                using var httpClient = new HttpClient();
                var data = await httpClient.GetByteArrayAsync($"https://osu.ppy.sh/osu/{id}");

                md5 = Crypto.ComputeHash(data); // probably md5 got updated, so re-compute it.

                await File.WriteAllBytesAsync($"./data/beatmaps/{md5}.osu", data);
                return md5;
            }
        }

        public static async Task<double> CalculatePerformancePoints(Objects.Score score)
        {
            var beatmapMd5 = await GetBeatmap(score.FileChecksum, score.Beatmap.Id);

            var beatmap = new ProcessorWorkingBeatmap($"./data/beatmaps/{beatmapMd5}.osu");

            var psp = new ProcessorScoreDecoder(beatmap);
            var parsedScore = psp.Parse(score, $"./data/osr/{score.ReplayChecksum}.osr");

            var categoryAttribs = new Dictionary<string, double>();
            var pp = parsedScore.ScoreInfo.Ruleset
                .CreateInstance()
                .CreatePerformanceCalculator(beatmap, parsedScore.ScoreInfo)  
                .Calculate(categoryAttribs);

            return pp;
        }

        public static double CalculateAccuracy(Objects.Score score)
        {
            int totalHits; float accuracy = 0;

            switch (score.PlayMode)
            {
                case PlayMode.Osu:
                    totalHits = score.Count300 + score.Count100 + score.CountMiss;

                    if (totalHits > 0)
                        accuracy = (float)((
                            score.Count50 * 50.0 + score.Count100 * 100.0 + score.Count300 * 300.0) /
                            (totalHits * 300.0));

                    return accuracy;

                case PlayMode.Taiko:
                    totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss;
                    return totalHits > 0 ? (double)(score.Count100 * 150 + score.Count300 * 300) / (totalHits * 300) : 1;

                case PlayMode.CatchTheBeat:
                    totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss + score.CountKatu;
                    return totalHits > 0 ? (double)(score.Count50 + score.Count100 + score.Count300) / totalHits : 1;

                case PlayMode.OsuMania:
                    totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss + score.CountGeki + score.CountKatu;
                    return totalHits > 0 ? (double)(score.Count50 * 50 + score.Count100 * 100 + score.CountKatu * 200 + (score.Count300 + score.CountGeki) * 300) / (totalHits * 300) : 1;
                default:
                    return 0;
            }
        }
    }
}

