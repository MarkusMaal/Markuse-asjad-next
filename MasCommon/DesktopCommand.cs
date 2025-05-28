using System.Text.Json.Serialization;

namespace MasCommon;

public class DesktopCommand
{
    /// <summary>
    /// Specify what kind of command you want to send to desktop app
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// Specific arguments for the command you send
    /// </summary>
    public string Arguments { get; set; }
}

// Required for generating trimmed executables
[JsonSerializable(typeof(DesktopCommand))]
[JsonSerializable(typeof(string))]
public partial class CommandSourceGenerationContext : JsonSerializerContext
{
}