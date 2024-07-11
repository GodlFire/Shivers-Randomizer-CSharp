using System.Windows;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for Message.xaml
/// </summary>
public partial class Message : Window
{
    public Message(string message)
    {
        InitializeComponent();
        text_Message.Text = message;
    }

    public void ButtonOK_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
