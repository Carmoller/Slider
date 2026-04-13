using Slider.Interfaces;
using Slider.ViewModels;
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

namespace Slider
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IUserAlert
    {
        private MainViewModel Vm { get { return (DataContext as MainViewModel)!; } }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Vm.CanvasSizeChanged(e);
        }

        public MessageBoxResult Alert(string message, string caption)
        {
            return MessageBox.Show(this, message, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}