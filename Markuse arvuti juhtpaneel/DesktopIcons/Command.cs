
using System.Text.Json.Serialization;

namespace DesktopIcons;

public class Command
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
[JsonSerializable(typeof(Command))]
[JsonSerializable(typeof(string))]        
internal partial class CommandSourceGenerationContext : JsonSerializerContext
{
}
