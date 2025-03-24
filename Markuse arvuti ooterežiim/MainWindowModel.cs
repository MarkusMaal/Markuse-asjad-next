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
    
    public Thickness StartingPointG { get; set; }
    public Thickness ScreenDimensionsG {get;set;}
    public double RotationG { get; set; }
    
    public Thickness StartingPointB { get; set; }
    public Thickness ScreenDimensionsB {get;set;}
    public double RotationB { get; set; }
    
    public Thickness StartingPointY { get; set; }
    public Thickness ScreenDimensionsY {get;set;}
    public double RotationY { get; set; }
}