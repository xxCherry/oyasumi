using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using oyasumi.Database.Models;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Catch;
using oyasumi.Enums;
using osu.Game.Rulesets.Mania;
using osu.Game.IO;
using osu.Game.Rulesets.Replays;
using osu.Game.Beatmaps.Formats;
using osu.Game.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Mods;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Replays;
using SharpCompress.Compressors.LZMA;
using osu.Game.Replays.Legacy;
using osu.Game.Scoring.Legacy;
using osu.Game.Screens.Play.Break;
using osu.Game.Rulesets.Mania.Beatmaps;

namespace oyasumi.Utilities
{
    public class Calculator
    {
        public static class LegacyHelper
        {
            public static Ruleset Convert(PlayMode mode)
            {
                return mode switch
                {
                    PlayMode.Osu => (Ruleset)new OsuRuleset(),
                    PlayMode.Taiko => new CatchRuleset(),
                    PlayMode.CatchTheBeat => new TaikoRuleset(),
                    PlayMode.OsuMania => new ManiaRuleset(),
                    _ => new OsuRuleset(),
                };
            }
        }

        public class ProcessorWorkingBeatmap : WorkingBeatmap
        {
            private readonly Beatmap _beatmap;

            public ProcessorWorkingBeatmap(string file, int? beatmapId = null)
                : this(ReadFromFile(file), beatmapId)
            {
            }

            private ProcessorWorkingBeatmap(Beatmap beatmap, int? beatmapId = null)
                : base(beatmap.BeatmapInfo, null)
            {
                _beatmap = beatmap;

                beatmap.BeatmapInfo.Ruleset = (PlayMode)beatmap.BeatmapInfo.RulesetID switch
                {
                    PlayMode.Osu => new OsuRuleset().RulesetInfo,
                    PlayMode.Taiko => new CatchRuleset().RulesetInfo,
                    PlayMode.CatchTheBeat => new TaikoRuleset().RulesetInfo,
                    PlayMode.OsuMania => new ManiaRuleset().RulesetInfo,
                    _ => new OsuRuleset().RulesetInfo,
                };

                if (beatmapId.HasValue)
                    beatmap.BeatmapInfo.OnlineBeatmapID = beatmapId;
            }

            private static Beatmap ReadFromFile(string filename)
            {
                using var stream = File.OpenRead(filename);
                using var streamReader = new LineBufferedReader(stream);

                return Decoder.GetDecoder<Beatmap>(streamReader).Decode(streamReader);
            }

            protected override IBeatmap GetBeatmap() => _beatmap;
            protected override Texture GetBackground() => null;
            protected override Track GetBeatmapTrack() => null;
        }

        public class ProcessorScoreDecoder : LegacyScoreDecoder
        {
            private readonly WorkingBeatmap _beatmap;
            private Ruleset _ruleset;

            public ProcessorScoreDecoder(WorkingBeatmap beatmap) => _beatmap = beatmap;

            public Score Parse(Objects.Score oScore, string replayPath = null)
            {
                using var rawReplay = replayPath == null
                    ? File.OpenRead(".data/osr/" + oScore.ReplayChecksum)
                    : File.OpenRead(replayPath);

                var properties = new byte[5];
                if (rawReplay.Read(properties, 0, 5) != 5)
                    throw new IOException("input .lzma is too short");

                long outSize = 0;

                for (var i = 0; i < 8; i++)
                {
                    var v = rawReplay.ReadByte();
                    if (v < 0)
                        throw new IOException("Can't Read 1");

                    outSize |= (long)(byte)v << (8 * i);
                }

                var compressedSize = rawReplay.Length - rawReplay.Position;

                _ruleset = LegacyHelper.Convert(oScore.PlayMode);

                var mods = LegacyHelper.Convert(oScore.PlayMode).ConvertFromLegacyMods((LegacyMods)oScore.Mods).ToArray();

                var score = new Score
                {
                    ScoreInfo = new ScoreInfo
                    {
                        Accuracy = oScore.Accuracy,
                        Beatmap = _beatmap.BeatmapInfo,
                        Combo = oScore.MaxCombo,
                        MaxCombo = oScore.MaxCombo,
                        User = new osu.Game.Users.User { Username = oScore.User.Username },
                        RulesetID = (int)oScore.PlayMode,
                        Date = oScore.Date,
                        Files = null,
                        Hash = null,
                        Mods = mods,
                        Ruleset = _ruleset.RulesetInfo,
                        Passed = true,
                        TotalScore = oScore.TotalScore,
                        Statistics = new Dictionary<HitResult, int>
                        {
                            [HitResult.Perfect] = oScore.Count300,
                            [HitResult.Good] = oScore.Count100,
                            [HitResult.Great] = oScore.CountGeki,
                            [HitResult.Meh] = oScore.Count50,
                            [HitResult.Miss] = oScore.CountMiss,
                            [HitResult.Ok] = oScore.CountKatu,
                            [HitResult.None] = 0,
                        },
                    },
                    Replay = new Replay(),
                };

                CalculateAccuracy(score.ScoreInfo);

                return score;
            }

            protected override Ruleset GetRuleset(int rulesetId) => LegacyHelper.Convert((PlayMode)rulesetId);

            protected override WorkingBeatmap GetBeatmap(string md5Hash) => _beatmap;
        }

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

