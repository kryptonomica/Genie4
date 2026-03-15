using System;
using Avalonia.Threading;

namespace GenieClient.Avalonia.Services
{
    public static class UIDispatcher
    {
        public static void Post(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Post(action, DispatcherPriority.Normal);
        }
    }
}
