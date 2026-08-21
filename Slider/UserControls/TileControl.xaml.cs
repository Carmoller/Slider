using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.SliderEventArgs;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.X86;
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
                WeakEventManager<TileControlViewModel, TilePositionChangedEventArgs>.AddHandler(newVm, nameof(newVm.TilePositionChanged), OnTileMove);
            }
        }

        private void OnTileMove(object? sender, TilePositionChangedEventArgs e)
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

            if (e.NewRow != e.OldRow)
            {
                propertyName = "(Canvas.Top)";
                startPos = e.OldRow * Vm.TileSize;
                endPos = e.NewRow * Vm.TileSize;
                Vm.Y = (int)endPos;
            }
            else
            {
#if DEBUG
                if (e.NewColumn == e.OldColumn) throw new InvalidOperationException("Moving a tile where nothing has changed!!");
#endif
                propertyName = "(Canvas.Left)";
                startPos = e.OldColumn * Vm.TileSize;
                endPos = e.NewColumn * Vm.TileSize;
                Vm.X = (int)endPos;
            }

            if (Vm.Value == 0)
            {
                // We do not animate the blank, so since will have set X and Y, our job is done
                return;
            }
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
                FillBehavior = FillBehavior.HoldEnd
            };
            Storyboard.SetTarget(animation, presenter);
            Storyboard.SetTargetProperty(animation, new PropertyPath(propertyName));
            storyboard.Children.Add(animation);
            storyboard.Completed += (s, args) =>
            {
                storyboard.Stop();
                // Need to clear the animation local values to allow binding to retake control,
                // otherwise we would get tiles stacked on top of each other, but only if you 
                // hit "Generate", then clicked a few tiles, then hit "Solve" then "Autoplay" where it would fail after ~10 moves
                // While stil work perfecly if you just clicked through the same list of moves
                // or just did Generate -> Solve -> Autplay
                // Thanks a lot, WPF!! That didn't take forever to find :S
                if (e.NewRow != e.OldRow)
                    presenter.BeginAnimation(Canvas.TopProperty, null);
                else
                    presenter.BeginAnimation(Canvas.LeftProperty, null);
            };
            _currentStoryboard = storyboard;
            storyboard.Begin();
        }
    }
}
