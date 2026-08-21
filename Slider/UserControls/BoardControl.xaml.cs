using Slider.Interfaces;
using Slider.SliderEventArgs;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

namespace Slider.UserControls
{
    /// <summary>
    /// Interaction logic for BoardControl.xaml
    /// </summary>
    public partial class BoardControl : UserControl
    {
        public BoardViewModel Vm { get { return (ItemsControlBoard.DataContext as BoardViewModel)!; } }

        #region SelectionChanged Routed Event
        public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
            name: "SelectionChanged",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(BoardSelectionChangedEventHandler),
            ownerType: typeof(BoardControl));
        public event BoardSelectionChangedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }
        #endregion
        #region GridSize Dependency Property
        public int GridSize
        {
            get { return (int)GetValue(GridSizeProperty); }
            set { SetValue(GridSizeProperty, value); }
        }

        public static readonly DependencyProperty GridSizeProperty =
            DependencyProperty.Register(nameof(GridSize), typeof(int), typeof(BoardControl),
                new FrameworkPropertyMetadata(
                    0, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnGridSizeChanged));
        private static void OnGridSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null && e.NewValue is int newGridSize)
            {
                control.Vm.GridSize = newGridSize;
            }
        }
        #endregion
        #region CanSelect Dependency Property
        public bool CanSelect
        {
            get { return (bool)GetValue(CanSelectProperty); }
            set { SetValue(CanSelectProperty, value); }
        }

        public static readonly DependencyProperty CanSelectProperty =
            DependencyProperty.Register(nameof(CanSelect), typeof(bool), 
                typeof(BoardControl), new PropertyMetadata(false, OnCanSelectChanged));
        private static void OnCanSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null && e.NewValue is bool newCanSelect)
            {
                control.Vm.CanSelect = newCanSelect;
            }
        }
        #endregion
        #region CanGray Dependency Property
        public bool CanGray
        {
            get { return (bool)GetValue(CanGrayProperty); }
            set { SetValue(CanGrayProperty, value); }
        }

        public static readonly DependencyProperty CanGrayProperty =
            DependencyProperty.Register(nameof(CanGray), typeof(bool),
                typeof(BoardControl), new PropertyMetadata(false, OnCanGrayChanged));
        private static void OnCanGrayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null && e.NewValue is bool newCanGray)
            {
                control.Vm.CanGray = newCanGray;
            }
        }
        #endregion
        #region CanMove Dependency Property
        public bool CanMove
        {
            get { return (bool)GetValue(CanMoveProperty); }
            set { SetValue(CanMoveProperty, value); }
        }

        public static readonly DependencyProperty CanMoveProperty =
            DependencyProperty.Register(nameof(CanMove), typeof(bool),
                typeof(BoardControl), new PropertyMetadata(false, OnCanMoveChanged));
        private static void OnCanMoveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null && e.NewValue is bool newCanMove)
            {
                control.Vm.CanMove = newCanMove;
            }
        }
        #endregion
        #region SelectedItem Dependency Property
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
            nameof(SelectedItem), typeof(ITileControlViewModel), typeof(BoardControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));
        public ITileControlViewModel? SelectedItem
        {
            get { return (ITileControlViewModel)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null)
            {
                if (control.Vm.Selected != (ITileControlViewModel)e.NewValue)
                {
                    control.Vm.Selected = (ITileControlViewModel)e.NewValue;
                }
            }
        }
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BoardViewModel.Selected))
            {
                if (SelectedItem != Vm.Selected)
                {
                    SelectedItem = Vm.Selected;
                }
            }
        }
        #endregion
        #region ItemsSource Dependency Property
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
            nameof(ItemsSource), typeof(ObservableCollection<ITileControlViewModel>), typeof(BoardControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnItemsSourceChanged));
        public ObservableCollection<ITileControlViewModel> ItemsSource
        {
            get { return (ObservableCollection<ITileControlViewModel>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null)
            {
                if (control.Vm.ItemsSource != (ObservableCollection<ITileControlViewModel>)e.NewValue)
                {
                    control.Vm.ItemsSource = (ObservableCollection<ITileControlViewModel>)e.NewValue;
                }
            }
        }

        #endregion
        #region AnimationDelay Dependency Property
        public int AnimationDelay
        {
            get { return (int)GetValue(AnimationDelayProperty); }
            set { SetValue(AnimationDelayProperty, value); }
        }

        public static readonly DependencyProperty AnimationDelayProperty =
            DependencyProperty.Register(nameof(AnimationDelay), typeof(int), typeof(BoardControl),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnAnimationDelayChanged));
        private static void OnAnimationDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BoardControl control && control.DataContext != null && e.NewValue is int newAnimationDelay)
            {
                control.Vm.AnimationDelay = newAnimationDelay;
            }
        }
        #endregion


        public BoardControl() : this(new BoardViewModel())
        {
            InitializeComponent();
        }
        public BoardControl(BoardViewModel viewModel)
        {
            InitializeComponent();
            ItemsControlBoard.DataContext = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Tiles_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Vm.SizeChanged(e);
        }

        public void ExecuteMoveNext()
        {
            if (CanSelect)
                Vm.MoveNext();
        }
        public void ExecuteMovePrevious()
        {
            if (CanSelect)
                Vm.MovePrevious();
        }
        public void ExecuteMoveUp()
        {
            if (CanSelect)
                Vm.MoveUp();
        }
        public void ExecuteMoveDown()
        {
            if (CanSelect)
                Vm.MoveDown();
        }

        public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }

        public TileControl? FindTile(Point mousePosition)
        {
            DependencyObject? hitControl = null;
            VisualTreeHelper.HitTest(this, null,
                new HitTestResultCallback(result =>
                {
                    hitControl = result.VisualHit;
                    return HitTestResultBehavior.Stop; // Stop at the topmost control
                }),
                new PointHitTestParameters(mousePosition));
            if (hitControl != null)
            {
                TileControl? parentUserControl = FindParent<TileControl>(hitControl);
                return parentUserControl;
            }
            return null;
        }
        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(this);
            TileControl? tile = FindTile(mousePos);
            if (tile == null)
                return;
            if (tile.DataContext is ITileControlViewModel tileVm)
            {
                if (CanSelect)
                    Vm.SetSelection(tileVm);
                RaiseEvent(new BoardSelectionChangedEventArgs(SelectionChangedEvent, this) { SelectionMethod = BoardSelectionMethod.Mouse, Tile = tileVm});
                e.Handled = true;
            }
        }

        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            if (!CanSelect)
                return;
            Point mousePos = Mouse.GetPosition(this);
            TileControl? tile = FindTile(mousePos);
            if (tile == null)
            {
                Vm.ClearBorderHighlight();
                return;
            }
            if (tile.DataContext is ITileControlViewModel tileVm)
                Vm.SetBorderHighlight(tileVm);
        }

        public void KeyBoardSelect()
        {
            if (!CanSelect || Vm.Selected == null)
                return;
            RaiseEvent(new BoardSelectionChangedEventArgs(SelectionChangedEvent, this) { SelectionMethod = BoardSelectionMethod.Keyboard, Tile = Vm.Selected });
        }
    }
}
