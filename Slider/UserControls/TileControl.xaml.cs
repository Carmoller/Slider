using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.SliderEventArgs;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Slider.UserControls
{
    /// <summary>
    /// Interaction logic for TileControl.xaml
    /// </summary>
    public partial class TileControl : UserControl
    {
        private Storyboard? _currentStoryboard;
        
        private TileControlViewModel Vm { get { return (DataContext as TileControlViewModel)!; } }
        public TileControl()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T)
                    return (T)parent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TileControlViewModel newVm)
            {
                // No unsubscription logic needed with a WeakEventManager
                WeakEventManager<TileControlViewModel, TileMoveEventArgs>.AddHandler(newVm, nameof(newVm.TileMove), OnTileMove);
            }
        }

        private void OnTileMove(object? sender, SliderEventArgs.TileMoveEventArgs e)
        {
            if (!Vm.CanMove)
                return;
            // Get the parent ContentPresenter
            ContentPresenter? presenter = FindParent<ContentPresenter>(this);
            if (presenter == null)
                return;

            string propertyName = string.Empty;
            double startPos = 0;
            double endPos = 0;
            if (e.Direction == AllowedMove.Left || e.Direction == AllowedMove.Right)
            {
                startPos = Canvas.GetLeft(presenter);
                propertyName = "(Canvas.Left)";
                endPos = startPos + (e.Direction == AllowedMove.Left ? -Vm.TileSize : Vm.TileSize);
                Vm.X = (int)endPos;
            }
            else if (e.Direction == AllowedMove.Up || e.Direction == AllowedMove.Down)
            {
                startPos = Canvas.GetTop(presenter);
                propertyName = "(Canvas.Top)";
                endPos = startPos + (e.Direction == AllowedMove.Up ? -Vm.TileSize : Vm.TileSize);
                Vm.Y = (int)endPos;
            }

            double currentLeft = Canvas.GetLeft(presenter);
            double currentTop = Canvas.GetTop(presenter);
            if (double.IsNaN(currentLeft)) currentLeft = 0;

            // Stop any existing animation to prevent accumulation
            if (_currentStoryboard != null)
            {
                _currentStoryboard.Stop();
                _currentStoryboard = null;
            }

            Storyboard storyboard = new();
            DoubleAnimation animation = new()
            {
                From = startPos,
                To = endPos,
                Duration = new Duration(TimeSpan.FromMilliseconds(Vm.AnimationDelay)),
                FillBehavior = FillBehavior.Stop
            };
            Storyboard.SetTarget(animation, presenter);
            Storyboard.SetTargetProperty(animation, new PropertyPath(propertyName));
            storyboard.Children.Add(animation);
            storyboard.Completed += (s, args) =>
            {
                storyboard.Stop();
                //storyboard = null;
            };
            _currentStoryboard = storyboard;
            storyboard.Begin();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                int a = 1;
            }
        }
    }
}
