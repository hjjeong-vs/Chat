using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox; 

            if (e.Key == Key.Enter) {
                var viewModel = DataContext as MainViewModel;
                if (tb.Name == "tbName")
                {
                    viewModel?.UpdateNickname();
                }
                else if (tb.Name == "tbMessage")
                {
                    viewModel?.SendMessage();
                }
            }
        }
    }
}