using Slider.Interfaces;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Slider
{
    /// <summary>
    /// Interaction logic for SelectTileWindow.xaml
    /// </summary>
    public partial class SelectTileWindow : Window
    {
        private ISelectTileViewModel Vm { get { return (DataContext as ISelectTileViewModel)!; } }
        public SelectTileWindow(ISelectTileViewModel vm) 
        {
            InitializeComponent();
            DataContext = vm;
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

        private void MyBoard_BoardSelectionChanged(object sender, SliderEventArgs.BoardSelectionChangedEventArgs e)
        {
            DialogResult = true;
            Vm.SelectedValue = e.Tile.Value;
            Close();
        }
    }
}
