using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Catch;
using oyasumi.Enums;
using osu.Game.Rulesets.Mania;
using osu.Game.IO;
using osu.Game.Beatmaps.Formats;
using osu.Game.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Replays;
using osu.Game.Scoring.Legacy;

namespace oyasumi.Utilities
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
}
