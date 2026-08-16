using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Slider.Commands
{
    public static class BoardCommands
    {
        public static readonly RoutedUICommand MoveNext = new RoutedUICommand(
            "Move Next",
            nameof(MoveNext),
            typeof(BoardCommands));
        public static readonly RoutedUICommand MovePrevious = new RoutedUICommand(
            "Move Previous",
            nameof(MovePrevious),
            typeof(BoardCommands));
        public static readonly RoutedUICommand MoveUp= new RoutedUICommand(
            "Move Up",
            nameof(MoveUp),
            typeof(BoardCommands));
        public static readonly RoutedUICommand MoveDown = new RoutedUICommand(
            "Move Down",
            nameof(MoveDown),
            typeof(BoardCommands));
        public static readonly RoutedUICommand KeyboardSelect = new RoutedUICommand(
            "Keyboard Select",
            nameof(KeyboardSelect),
            typeof(BoardCommands));
    }
}
