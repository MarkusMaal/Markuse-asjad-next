using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MasCommon
{
    /// <summary>
    /// This class defines Markus' stuff integration configuration file
    /// </summary>
    public class CommonConfig
    {
        /// <summary>
        /// Whether we should automatically start quick notes app on integration software startup.
        /// </summary>
        public bool AutostartNotes { get; set; }

        /// <summary>
        /// Enables/disables scheduled tasks (legacy)
        /// </summary>
        public bool AllowScheduledTasks { get; set; }

        /// <summary>
        /// Show/hide Markus' stuff logo on integration software startup
        /// </summary>
        public bool ShowLogo { get; set; }

        /// <summary>
        /// The frequency at which integration software should poll for updates. 
        /// Higher value means slower responses to actions, but also lower CPU usage.
        /// Lower value means faster responses to actions, but way higher CPU usage.
        /// </summary>
        public int PollRate { get; set; }

        [JsonIgnore]
        private readonly JsonSerializerOptions _cnfSerializerOptions = new() { WriteIndented = true, TypeInfoResolver = MasConfigSourceGenerationContext.Default };

        /// <summary>
        /// Replaces current configuration
        /// </summary>
        /// <param name="mas_root">Root directory for Markus' stuff system. Usually %UserProfile\.mas.</param>
        public void Load(string mas_root)
        {
            var cnf = JsonSerializer.Deserialize<CommonConfig>(File.ReadAllText(mas_root + "/Config.json"), _cnfSerializerOptions);
            this.AutostartNotes = cnf.AutostartNotes;
            this.AllowScheduledTasks = cnf.AllowScheduledTasks;
            this.ShowLogo = cnf.ShowLogo;
            this.PollRate = cnf.PollRate;
        }

        /// <summary>
        /// Saves current configuration
        /// </summary>
        /// <param name="mas_root">Root directory for Markus' stuff system. Usually %UserProfile\.mas.</param>
        public void Save(string mas_root)
        {
            var jsonData = JsonSerializer.Serialize(this, this._cnfSerializerOptions);
            File.WriteAllText(mas_root + "/Config.json", jsonData);
        }
    }

    // Required for generating trimmed executables
    [JsonSerializable(typeof(CommonConfig))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    public partial class MasConfigSourceGenerationContext : JsonSerializerContext
    {
    }

}
