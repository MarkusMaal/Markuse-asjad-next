namespace MasCommon;

/// <summary>
/// This class defines a desktop icon
/// </summary>
public class DesktopIcon
{
    /// <summary>
    /// Icon to use
    /// Only embedded resources with the TopIcon prefix can be used (see App.axaml)
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// The app to run
    /// If special: prefix is used, a command for this program is executed instead
    /// </summary>
    public string Executable { get; set; }
    
    /// <summary>
    /// Location on the desktop (X-coordinate)
    /// -1 means automatic positioning
    /// </summary>
    public int LocationX { get; set; }
    
    /// <summary>
    /// Location on the desktop (Y-coordinate)
    /// -1 means automatic positioning
    /// </summary>
    public int LocationY { get; set; }
}