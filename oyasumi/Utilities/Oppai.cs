using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.Objects;

namespace oyasumi.Utilities
{
    public class OppaiProvider : IDisposable
    {
        private IntPtr _handle;

        public static async Task<byte[]> GetBeatmap(string md5, int id = 0)
        {
            var file = $"./data/beatmaps/{md5}.osu";

            if (File.Exists(file))
            {
                using var fileData = File.OpenRead($"./data/beatmaps/{md5}.osu");

                await using var ms = new MemoryStream();
                await fileData.CopyToAsync(ms);
                return ms.ToArray();
            }
            else
            {
                using var httpClient = new HttpClient();
                var data = await httpClient.GetByteArrayAsync($"https://osu.ppy.sh/osu/{id}");

                md5 = Crypto.ComputeHash(data); // probably md5 got updated, so re-compute it.

                await File.WriteAllBytesAsync($"./data/beatmaps/{md5}.osu", data);
                return data;
            }
        }

        private static void CopyResultsFromHandle(IntPtr handle, ref PerformanceData pp)
        {
            pp.Total = ezpp_pp(handle);
            pp.Aim = ezpp_aim_pp(handle);
            pp.Speed = ezpp_speed_pp(handle);
            pp.Acc = ezpp_acc_pp(handle);
            pp.Accuracy = ezpp_accuracy_percent(handle) / 100.0f;
        }

        public OppaiProvider() => _handle = ezpp_new();

        private PerformanceData _ppResult;

        public async Task<PerformanceData?> CalculatePerformancePoints(Score score)
        {
            var beatmapData = await GetBeatmap(score.FileChecksum, score.Beatmap.Id);

            ezpp_set_mods(_handle, (int)score.Mods);
            ezpp_set_mode_override(_handle, (int)score.PlayMode);
            ezpp_set_accuracy(_handle, score.Count100, score.Count50);
            ezpp_set_nmiss(_handle, score.CountMiss);
            ezpp_set_combo(_handle, score.MaxCombo);

            if (ezpp_data(_handle, beatmapData, beatmapData.Length) < 0)
                return null;

            CopyResultsFromHandle(_handle, ref _ppResult);

            return _ppResult;
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            ezpp_free(_handle);

            _handle = IntPtr.Zero;
        }

        public struct PerformanceData
        {
            public float Total, Aim, Speed, Acc, Accuracy;
        };

        #region oppai P/Invoke
        [DllImport(@"lib/oppai.dll")] public static extern IntPtr ezpp_new();
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_free(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern int ezpp_data(IntPtr handle, byte[] data, int data_size);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_base_cs(IntPtr handle, float cs);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_base_od(IntPtr handle, float cs);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_base_ar(IntPtr handle, float cs);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_base_hp(IntPtr handle, float cs);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_mods(IntPtr handle, int mods);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_accuracy(IntPtr handle, int n100, int n50);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_nmiss(IntPtr handle, int mods);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_combo(IntPtr handle, int combo);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_mode_override(IntPtr handle, int mode);
        [DllImport(@"lib/oppai.dll")] public static extern void ezpp_set_end(IntPtr ez, int end);
        [DllImport(@"lib/oppai.dll")] public static extern float ezpp_pp(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern float ezpp_aim_pp(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern float ezpp_speed_pp(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern float ezpp_acc_pp(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern float ezpp_accuracy_percent(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern int ezpp_combo(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern int ezpp_max_combo(IntPtr handle);
        [DllImport(@"lib/oppai.dll")] public static extern float ezpp_stars(IntPtr handle);
        #endregion

        #region oppai-ng function

        private static double round_oppai(double x)
        {
            return Math.Floor((x) + 0.5);
        }

        public static float CalculateAccuracy(Score score)
        {
            int totalHits = score.Count300 + score.Count100 + score.Count50 + score.CountMiss;
            float acc = 1.0f;

            if (totalHits > 0)
            {
                acc = (float)((
                    score.Count50 * 50.0 + score.Count100 * 100.0 + score.Count300 * 300.0) /
                    (totalHits * 300.0));
            }

            return acc;
        }

        /*public static void RoundAccuracy(double accPercent, int nobjects,
            int misses, out int n300, out int n100, out int n50)
        {
            misses = Math.Min(nobjects, misses);
            var max300 = nobjects - misses;
            var maxacc = CalculateAccuracy(max300, 0, 0, misses) * 100.0;
            accPercent = Math.Max(0.0, Math.Min(maxacc, accPercent));

            n50 = 0;

            // just some black magic maths from wolfram alpha
            n100 = (int)round_oppai(-3.0 * ((accPercent * 0.01 - 1.0) *
            nobjects + misses) * 0.5);
            if (n100 > nobjects - misses)
            {
                //acc lower than all 100s, use 50s 
                n100 = 0;
                n50 = (int)round_oppai(-6.0 * ((accPercent * 0.01 - 1.0) * nobjects + misses) * 0.2);

                n50 = Math.Min(max300, n50);
            }
            else
            {
                n100 = Math.Min(max300, n100);
            }

            n300 = nobjects - n100 - n50 - misses;
        } */

        /*public static double taiko_acc_calc(int n300, int n150, int nmiss)
        {
            int totalHits = n300 + n150 + nmiss;
            double acc = 0;

            if (totalHits > 0)
            {
                acc = (n150 * 150.0 + n300 * 300.0) / (totalHits * 300.0);
            }

            return acc;
        }

        public static void taiko_acc_round(double accPercent, int nobjects, int nmisses, out int n300, out int n150)
        {
            nmisses = Math.Min(nobjects, nmisses);
            var max300 = nobjects - nmisses;
            var maxacc = acc_calc(max300, 0, 0, nmisses) * 100.0;
            accPercent = Math.Max(0.0, Math.Min(maxacc, accPercent));

            // just some black magic maths from wolfram alpha 
            n150 = (int)
                round_oppai(-2.0 * ((accPercent * 0.01 - 1.0) *
                    nobjects + nmisses));

            n150 = Math.Min(max300, n150);
            n300 = nobjects - n150 - nmisses;
        } */

        #endregion
    }
}
