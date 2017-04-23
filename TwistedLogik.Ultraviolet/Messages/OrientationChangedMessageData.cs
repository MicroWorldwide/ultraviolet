﻿using Ultraviolet.Core.Messages;
using TwistedLogik.Ultraviolet.Platform; 

namespace TwistedLogik.Ultraviolet.Messages
{
    /// <summary>
    /// Represents the message data for an Orientation Changed message.
    /// </summary>
    public sealed class OrientationChangedMessageData : MessageData
    {
        /// <summary>
        /// Gets or sets the display which changed orientation.
        /// </summary>
        public IUltravioletDisplay Display
        {
            get;
            set;
        }
    }
}
