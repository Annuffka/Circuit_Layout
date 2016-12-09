using System.Windows;

namespace Circuit_Layout
{
    /// <summary>
    /// Логика взаимодействия для AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Button_Click( object sender, RoutedEventArgs e )
        {
            this.Close();
        }
    }
}
