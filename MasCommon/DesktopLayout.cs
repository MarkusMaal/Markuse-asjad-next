using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MasCommon;

/// <summary>
/// This class defines desktop layout
/// </summary>
public class DesktopLayout
{
    // show/hide stuff
    // use-case for hiding everything would be if you wanted to open the desktop with the built-in browser of this app (altough it's not very practical)
    
    /// <summary>
    /// Show/hide icons in the middle
    /// </summary>
    public bool ShowIcons { get; set; }
    
    /// <summary>
    /// Show/hide logo at the bottom left
    /// </summary>
    public bool ShowLogo { get; set; }
    
    /// <summary>
    /// Show/hide quick actions in the middle at the bottom of the screen for hiding, locking and resetting icons
    /// </summary>
    public bool ShowActions { get; set; }
    
    /// <summary>
    /// If true, prevents the icons from being moved
    /// </summary>
    public bool LockIcons { get; set; }

    /// <summary>
    /// If true, allow external apps to send signals (including the control panel)
    /// </summary>
    public bool AcceptCommands { get; set; }

    /// <summary>
    /// When auto-calculating icon positions, this is the grid width
    /// </summary>
    public int IconCountX { get; set; }
    
    /// <summary>
    /// When auto-calculating icon positions, this is the grid height
    /// </summary>
    public int IconCountY { get; set; }
    
    /// <summary>
    /// Size for each normal icon
    /// Special icons sizes are relative to this one
    /// </summary>
    public int IconSize { get; set; }
    
    /// <summary>
    /// Grid padding for each normal icon
    /// The padding for special icons are relative to this one
    /// </summary>
    public int IconPadding { get; set; } // padding, i.e. the space between icons

    /// <summary>
    /// Desktop folder to open when executing special:apps command
    /// </summary>
    public required string DesktopDir { get; set; }

    /// <summary>
    /// Definitions for icons located in the middle that can open programs
    /// </summary>
    public required DesktopIcon[] Children { get; set; }
    
    /// <summary>
    /// Definitions for icons located at the bottom center that can be used to toggle some settings quickly
    /// </summary>
    public required SpecialIcon[] SpecialIcons { get; set; }
    
    /// <summary>
    /// Definition of Markus' stuff logo located at the bottom left 
    /// </summary>
    public required SpecialIcon Logo { get; set; }
}

// Required for generating trimmed executables
[JsonSerializable(typeof(DesktopLayout))]
[JsonSerializable(typeof(string))]        
[JsonSerializable(typeof(int))]
public partial class DesktopLayoutSourceGenerationContext : JsonSerializerContext
{
}
