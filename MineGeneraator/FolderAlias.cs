using System.Text.Json;
using System.Text.Json.Serialization;

namespace MineGeneraator;

public class FolderAlias
{
    /// <summary>
    /// Real path of the directory (can be relative)
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// List of subdirectories
    /// </summary>
    public FolderAlias[] subdirs { get; set; }

    /// <summary>
    /// User-friendly name for directory (empty string = Name)
    /// </summary>
    public string alias { get; set; }

    [JsonIgnore]
    private readonly JsonSerializerOptions _cnfSerializerOptions = new() { WriteIndented = true, TypeInfoResolver = FolderAliasSourceGenerationContext.Default };

    /// <summary>
    /// Replaces current configuration
    /// </summary>
    /// <param name="filename">Full path to the .json file</param>
    public void Load(string filename)
    {
        var cnf = JsonSerializer.Deserialize<FolderAlias>(File.ReadAllText(filename), _cnfSerializerOptions);
        this.name = cnf.name;
        this.subdirs = cnf.subdirs;
        this.alias = cnf.alias;
    }
}

// Required for generating trimmed executables
[JsonSerializable(typeof(FolderAlias))]
[JsonSerializable(typeof(string))]
public partial class FolderAliasSourceGenerationContext : JsonSerializerContext;