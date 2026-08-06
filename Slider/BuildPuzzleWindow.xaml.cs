using Slider.Interfaces;
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
    /// Interaction logic for BuildPuzzleWindow.xaml
    /// </summary>
    public partial class BuildPuzzleWindow : Window
    {
        private IBuildPuzzleViewModel Vm { get { return (DataContext as IBuildPuzzleViewModel)!; } }
        public BuildPuzzleWindow()
        {
            InitializeComponent();
        }
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void AvailableTiles_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Vm.AvailableSizeChanged(e);
        }

        private void BoardTiles_SizeChanged(Object sender, SizeChangedEventArgs e)
        {
            Vm.BoardSizeChanged(e);
        }
    }
}
