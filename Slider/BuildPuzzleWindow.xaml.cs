using Slider.Interfaces;
using Slider.SliderEventArgs;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for BuildPuzzleWindow.xaml
    /// </summary>
    public partial class BuildPuzzleWindow : Window
    {
        private IBuildPuzzleViewModel Vm { get { return (DataContext as IBuildPuzzleViewModel)!; } }
        public BuildPuzzleWindow()
        {
            DataContext = new BuildPuzzleViewModel();
            InitializeComponent();
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
            ISelectTileViewModel viewModel = Vm.CreateSelectTileViewModel();
            SelectTileWindow dialog = new(viewModel)
            {
                Owner = this
            };
            bool? result = dialog.ShowDialog();
            if (result != null && result.Value == true)
            {
                e.Tile.Value = viewModel.SelectedValue;
                if (e.SelectionMethod == BoardSelectionMethod.Keyboard)
                {
                    MyBoard.ExecuteMoveNext();
                }
            }
        }
    }
}
