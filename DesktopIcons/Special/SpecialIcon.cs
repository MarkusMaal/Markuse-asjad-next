using System.Text.Json.Serialization;

namespace DesktopIcons;

/// <summary>
/// This class defines a special icon, which is different from a regular icon, as it may have 2 icon states and is independent of regular icons
/// </summary>
public class SpecialIcon
{
    // dummy constructor
    [JsonConstructor]
    public SpecialIcon(string iconA, string iconB, string executable, int locationX, int locationY)
    {
        IconA = iconA;
        IconB = iconB;
        Executable = executable;
        LocationX = locationX;
        LocationX = locationY;
    }
    
    public SpecialIcon() {
        LocationY = -1;
        LocationX = -1;}
    
    /// <summary>
    /// State A of the icon.
    /// </summary>
    public string IconA { get; set; }
    
    /// <summary>
    /// State B of the icon.
    /// </summary>
    public string IconB { get; set; }
    
    
    /// <summary>
    /// The app to run.
    /// If special: prefix is used, a command for this program is executed instead
    /// </summary>
    public string Executable { get; set; }
    
    /// <summary>
    /// Location on the desktop (X-coordinate).
    /// -1 means automatic positioning
    /// </summary>
    public int LocationX { get; set; }
    
    /// <summary>
    /// Location on the desktop (Y-coordinate).
    /// -1 means automatic positioning
    /// </summary>
    public int LocationY { get; set; }
}