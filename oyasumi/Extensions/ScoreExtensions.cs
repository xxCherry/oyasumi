using oyasumi.Enums;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Extensions
{
    public static class ScoreExtensions
    {
        private const string SUBMISSION_KEY = "osu!-scoreburgr---------{0}";

        public static async Task<Score> ToScore(this (string encScore, string iv, string osuVersion) self)
        {
            var scoreDecrypted = Crypto.DecryptString(
                self.encScore,
                Encoding.ASCII.GetBytes(string.Format(SUBMISSION_KEY, self.osuVersion)),
                self.iv
            );

            var split = scoreDecrypted.Split(':');

            var mods = (Mods)uint.Parse(split[13]);
            var isRelax = (mods & Mods.Relax) > 0;
            var isAutopilot = (mods & Mods.Relax2) > 0;

            var beatmap = await BeatmapManager.Get(split[0], new BeatmapTitle(), false);

            var osuVersionUnformatted = split[17];
            var osuVersion = osuVersionUnformatted.Trim();

            var flags = (osuVersionUnformatted.Length - osuVersion.Length) & ~4;

            return new Score
            {
                FileChecksum = split[0],
                Presence = PresenceManager.GetPresenceByName(split[1].TrimEnd()), // TrimEnd() because osu! adds extra space if user is supporter
                Count300 = int.Parse(split[3]),
                Count100 = int.Parse(split[4]),
                Count50 = int.Parse(split[5]),
                CountGeki = int.Parse(split[6]),
                CountKatu = int.Parse(split[7]),
                CountMiss = int.Parse(split[8]),
                TotalScore = int.Parse(split[9]),
                MaxCombo = int.Parse(split[10]),
                Perfect = bool.Parse(split[11]),
                Mods = mods,
                Relaxing = isRelax,
                Autopiloting = isAutopilot,
                Passed = bool.Parse(split[14]),
                PlayMode = (PlayMode)uint.Parse(split[15]),
                Date = DateTime.Now,
                Beatmap = beatmap.Item2,
                OsuVersion = int.Parse(osuVersion),
                Flags = (BadFlags)flags
            };
        }
    }
}
