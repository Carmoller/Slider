using CommunityToolkit.Mvvm.Messaging;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.SliderEventArgs;
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
            RegisterShowSelectTileWindowMessage();
        }

        private void RegisterShowSelectTileWindowMessage()
        {
            // Register to handle showing the Select Tile window
            WeakReferenceMessenger.Default.Register<MainWindow, ShowSelectTileWindowMessage>(this, (recipient, message) =>
            {
                SelectTileWindow window = new SelectTileWindow(message.ViewModel)
                {
                    Owner = recipient,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                // Display the window and pass the result back to the sender
                bool? result = window.ShowDialog();
                message.Reply(result);
            });
        }

        public MessageBoxResult Alert(string message, string caption)
        {
            return MessageBox.Show(this, message, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void MoveNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MyBoard.ExecuteMoveNext();
        }
        private void MovePrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MyBoard.ExecuteMovePrevious();
        }
        private void MoveUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MyBoard.ExecuteMoveUp();
        }
        private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MyBoard.ExecuteMoveDown();
        }

        private void KeyboardSelect_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MyBoard.KeyBoardSelect();
        }

        private void MyBoard_SelectionChanged(object sender, BoardSelectionChangedEventArgs e)
        {
            bool handled = Vm.TileSelected(e.Tile, e.SelectionMethod);
        }
    }
}