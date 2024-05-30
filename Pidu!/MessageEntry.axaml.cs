using Avalonia.Controls;

namespace Pidu_
{
    public partial class MessageEntry : Window
    {
        public MessageEntry()
        {
            InitializeComponent();
        }

        private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
