using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Markuse_arvuti_ooterežiim;

public class ScreensaverOptions
{
    /// <summary>
    /// Defines animation easing types
    /// </summary>
    public enum Easing {
        CircularEaseIn,
        CircularEaseInOut,
        CircularEaseOut,
        CubicEaseIn,
        CubicEaseInOut,
        CubicEaseOut,
        ExponentialEaseIn,
        ExponentialEaseInOut,
        ExponentialEaseOut,
        LinearEasing,
        QuadraticEaseIn,
        QuadraticEaseInOut,
        QuadraticEaseOut,
        QuarticEaseIn,
        QuarticEaseInOut,
        QuarticEaseOut,
        QuinticEaseIn,
        QuinticEaseInOut,
        QuinticEaseOut, //+
        SineEaseIn,
        SineEaseInOut,
        SineEaseOut
    }
    /// <summary>
    /// Determines whether to display an image as the background
    /// </summary>
    public bool DisplayBackground { get; set; }
    
    /// <summary>
    /// Determines whether to display a custom animated graphic instead of the default Markus' stuff logo
    /// </summary>
    public bool CustomImage { get; set; }
    
    /// <summary>
    /// Path to the custom animated graphic
    /// </summary>
    public string ImagePath { get; set; }
    
    /// <summary>
    /// Path to the background image
    /// </summary>
    public string BackgroundPath { get; set; }
    
    /// <summary>
    /// Interval for each bounce (default: 3 seconds)
    /// </summary>
    public double AnimationInterval { get; set; }
    
    /// <summary>
    /// Type of easing to use for the animation
    /// </summary>
    public Easing AnimationEasing { get; set; }
    
    /// <summary>
    /// Width of the icon displayed on the screensaver
    /// </summary>
    public int ImageWidth { get; set; }

    [JsonIgnore]
    private readonly JsonSerializerOptions _cnfSerializerOptions = new() { WriteIndented = true, TypeInfoResolver = ScreenSaverSourceGenerationContext.Default };

    /// <summary>
    /// Replaces current configuration
    /// </summary>
    /// <param name="mas_root">Root directory for Markus' stuff system. Usually %UserProfile\.mas.</param>
    public void Load(string mas_root)
    {
        ScreensaverOptions? cnf = JsonSerializer.Deserialize<ScreensaverOptions>(File.ReadAllText(mas_root + "/ScreensaverConfig.json"), _cnfSerializerOptions);
        this.DisplayBackground = cnf.DisplayBackground;
        this.CustomImage = cnf.CustomImage;
        this.ImagePath = cnf.ImagePath;
        this.BackgroundPath = cnf.BackgroundPath;
        this.AnimationInterval = cnf.AnimationInterval;
        this.AnimationEasing = cnf.AnimationEasing;
        this.ImageWidth = cnf.ImageWidth;
    }

    /// <summary>
    /// Saves current configuration
    /// </summary>
    /// <param name="mas_root">Root directory for Markus' stuff system. Usually %UserProfile\.mas.</param>
    public void Save(string mas_root)
    {
        var jsonData = JsonSerializer.Serialize(this, this._cnfSerializerOptions);
        File.WriteAllText(mas_root + "/ScreensaverConfig.json", jsonData);
    }
}
// Required for generating trimmed executables
[JsonSerializable(typeof(ScreensaverOptions))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
internal partial class ScreenSaverSourceGenerationContext : JsonSerializerContext
{
}
