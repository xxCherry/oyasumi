using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Build.Framework;

namespace gulagDatabaseMerger.Database.Models
{
    // lazy to make it look cool
   [Table("stats")]
    public class GulagStats
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required] public int tscore_vn_std { get; set; }
        [Required] public int tscore_vn_taiko { get; set; }
        [Required] public int tscore_vn_catch { get; set; }
        [Required] public int tscore_vn_mania { get; set; }
        [Required] public int tscore_rx_std { get; set; }
        [Required] public int tscore_rx_taiko { get; set; }
        [Required] public int tscore_rx_catch { get; set; }
        [Required] public int tscore_ap_std { get; set; }
        [Required] public int rscore_vn_std { get; set; }
        [Required] public int rscore_vn_taiko { get; set; }
        [Required] public int rscore_vn_catch { get; set; }
        [Required] public int rscore_vn_mania { get; set; }
        [Required] public int rscore_rx_std { get; set; }
        [Required] public int rscore_rx_taiko { get; set; }
        [Required] public int rscore_rx_catch { get; set; }
        [Required] public int rscore_ap_std { get; set; }
        [Required] public int pp_vn_taiko { get; set; }
        [Required] public int pp_vn_catch { get; set; }
        [Required] public int pp_vn_mania { get; set; }
        [Required] public int pp_rx_std { get; set; }
        [Required] public int pp_rx_taiko { get; set; }
        [Required] public int pp_rx_catch { get; set; }
        [Required] public int pp_ap_std { get; set; }
        [Required] public int plays_vn_std { get; set; }
        [Required] public int plays_vn_taiko { get; set; }
        [Required] public int plays_vn_catch { get; set; }
        [Required] public int plays_vn_mania { get; set; }
        [Required] public int plays_rx_std { get; set; }
        [Required] public int plays_rx_taiko { get; set; }
        [Required] public int plays_rx_catch { get; set; }
        [Required] public int plays_ap_std { get; set; }
        [Required] public int playtime_vn_std { get; set; }
        [Required] public int playtime_vn_taiko { get; set; }
        [Required] public int playtime_vn_catch { get; set; }
        [Required] public int playtime_vn_mania { get; set; }
        [Required] public int playtime_rx_std { get; set; }
        [Required] public int playtime_rx_taiko { get; set; }
        [Required] public int playtime_rx_catch { get; set; }
        [Required] public int playtime_ap_std { get; set; }
        [Required] public float acc_vn_std { get; set; }
        [Required] public float acc_vn_taiko { get; set; }
        [Required] public float acc_vn_catch { get; set; }
        [Required] public float acc_vn_mania { get; set; }
        [Required] public float acc_rx_std { get; set; }
        [Required] public float acc_rx_taiko { get; set; }
        [Required] public float acc_rx_catch { get; set; }
        [Required] public float acc_ap_std { get; set; }
        [Required] public int maxcombo_vn_std { get; set; }
        [Required] public int maxcombo_vn_taiko { get; set; }
        [Required] public int maxcombo_vn_catch { get; set; }
        [Required] public int maxcombo_vn_mania { get; set; }
        [Required] public int maxcombo_rx_std { get; set; }
        [Required] public int maxcombo_rx_taiko { get; set; }
        [Required] public int maxcombo_rx_catch { get; set; }
        [Required] public int maxcombo_ap_std { get; set; }
    }
}