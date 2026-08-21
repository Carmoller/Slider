using CommunityToolkit.Mvvm.Messaging.Messages;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider
{
    public class ShowSelectTileWindowMessage : RequestMessage<bool?>
    {
        public required ISelectTileViewModel ViewModel { get; set; }
    }
}
