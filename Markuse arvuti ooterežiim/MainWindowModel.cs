using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Markuse_arvuti_ooterežiim;

public class MainWindowModel
{
    public Thickness StartingPoint { get; set; }
    public Thickness ScreenDimensions {get;set;}
    public double Rotation { get; set; }
}