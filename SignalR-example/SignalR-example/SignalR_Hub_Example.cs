using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace SignalR_example.SignalR.hubs
{
    /// <summary>
    /// SignalR_Hub_Example
    /// --------------------
    /// Example of SignalR hub
    /// </summary>
    public class SignalR_Hub_Example : Hub
    {
        /// <summary>
        /// Send "real time" text to all the clients 
        /// </summary>
        /// <param name="text">Text that will be propagated</param>
        public void SendText(string text)
        {
            // Trigger "receiveText" in all clients with received text.
            Clients.All.receiveText(text);
        }
    }
}