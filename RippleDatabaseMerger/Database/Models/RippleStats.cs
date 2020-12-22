using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Build.Framework;

namespace RippleDatabaseMerger.Database.Models
{
    // lazy to make it look cool
   [Table("users_stats")]
    public class RippleStats
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        
        [Required] public string username { get; set; }
        [Required] public string? userpage_content { get; set; }
        [Required] public string? country { get; set; }
        [Required] public int play_style { get; set; } 
        [Required] public int favourite_mode { get; set; }
        [Required] public string? custom_badge_icon { get; set; }
        [Required] public string? custom_badge_name { get; set; }
        [Required] public bool show_custom_badge { get; set; }
        [Required] public int level_std { get; set; }
        [Required] public int level_taiko { get; set; }
        [Required] public int level_ctb { get; set; }
        [Required] public int level_mania { get; set; }
        
        [Required] public long ranked_score_std { get; set; }
        [Required] public long ranked_score_taiko { get; set; }
        [Required] public long ranked_score_ctb { get; set; }
        [Required] public long ranked_score_mania { get; set; }
        
        [Required] public long total_score_std { get; set; }
        [Required] public long total_score_taiko { get; set; }
        [Required] public long total_score_ctb { get; set; }
        [Required] public long total_score_mania { get; set; }

        [Required] public int total_hits_std { get; set; }
        [Required] public int total_hits_taiko { get; set; }
        [Required] public int total_hits_ctb { get; set; }
        [Required] public int total_hits_mania { get; set; }
        
        [Required] public int replays_watched_std { get; set; }
        [Required] public int replays_watched_taiko { get; set; }
        [Required] public int replays_watched_ctb { get; set; }
        [Required] public int replays_watched_mania { get; set; }

        [Required] public int playcount_std { get; set; }
        [Required] public int playcount_taiko { get; set; }
        [Required] public int playcount_ctb { get; set; }
        [Required] public int playcount_mania { get; set; }

        [Required] public float avg_accuracy_std { get; set; }
        [Required] public float avg_accuracy_taiko { get; set; }
        [Required] public float avg_accuracy_ctb { get; set; }
        [Required] public float avg_accuracy_mania { get; set; }
        
        [Required] public int pp_std { get; set; }
        [Required] public int pp_taiko { get; set; }
        [Required] public int pp_ctb { get; set; }
        [Required] public int pp_mania { get; set; }
    }
    
    [Table("rx_stats")]
    public class RippleRelaxStats
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [Required] public string? username { get; set; }
        [Required] public string? country { get; set; }
        
        [Required] public int favourite_mode { get; set; }
        
        [Required] public int level_std { get; set; }
        [Required] public int level_taiko { get; set; }
        [Required] public int level_ctb { get; set; }
        [Required] public int level_mania { get; set; }
        
        [Required] public long ranked_score_std { get; set; }
        [Required] public long ranked_score_taiko { get; set; }
        [Required] public long ranked_score_ctb { get; set; }
        [Required] public long ranked_score_mania { get; set; }
        
        [Required] public long total_score_std { get; set; }
        [Required] public long total_score_taiko { get; set; }
        [Required] public long total_score_ctb { get; set; }
        [Required] public long total_score_mania { get; set; }

        [Required] public int total_hits_std { get; set; }
        [Required] public int total_hits_taiko { get; set; }
        [Required] public int total_hits_ctb { get; set; }
        [Required] public int total_hits_mania { get; set; }
        
        [Required] public int replays_watched_std { get; set; }
        [Required] public int replays_watched_taiko { get; set; }
        [Required] public int replays_watched_ctb { get; set; }
        [Required] public int replays_watched_mania { get; set; }
        
        [Required] public int playcount_std { get; set; }
        [Required] public int playcount_taiko { get; set; }
        [Required] public int playcount_ctb { get; set; }
        [Required] public int playcount_mania { get; set; }
        
        [Required] public float avg_accuracy_std { get; set; }
        [Required] public float avg_accuracy_taiko { get; set; }
        [Required] public float avg_accuracy_ctb { get; set; }
        [Required] public float avg_accuracy_mania { get; set; }
        
        [Required] public int pp_std { get; set; }
        [Required] public int pp_taiko { get; set; }
        [Required] public int pp_ctb { get; set; }
        [Required] public int pp_mania { get; set; }
    }
}