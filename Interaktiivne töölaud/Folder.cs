using Avalonia.Media.Imaging;

namespace Interaktiivne_töölaud
{
    public class Folder
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required Bitmap? Image { get; set; }
    }
}
