using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Pidu_
{
    public partial class MessageEntry : Window
    {
        public MessageEntry()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.GetControl<TextBox>("MessageField").Focus();
        }

        private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                this.Close();
            }
        }
    }
}
