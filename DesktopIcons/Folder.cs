using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DesktopIcons
{
    public class Folder
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        
        public required string? Selected { get; set; }
        public required Bitmap? Image { get; set; }
    }
}
