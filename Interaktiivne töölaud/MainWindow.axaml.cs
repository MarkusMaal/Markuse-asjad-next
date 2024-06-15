using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Interaktiivne_töölaud
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Enter_PointerReleased(object? sender, RoutedEventArgs e)
        {
            Inside inside = new();
            inside.Show();
            Close();
        }

        private void Playground_Click(object? sender, RoutedEventArgs e)
        {
            Playground pg = new();
            pg.Show();
            Close();
        }
    }
}