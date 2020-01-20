using System;

namespace UCNLDrivers
{
    public static class Utilities
    {       
        public static void Rise(this EventHandler handler, object sender, EventArgs e)
        {
            if (handler != null)
                handler(sender, e);
        }

        public static void Rise<TEventArgs>(this EventHandler<TEventArgs> handler,
            object sender, TEventArgs e) where TEventArgs : EventArgs
        {
            if (handler != null)
                handler(sender, e);
        }                    

        public static void BeginRise(this EventHandler handler, object sender, EventArgs e, AsyncCallback callback, object _object)
        {
            if (handler != null)
                handler.BeginInvoke(sender, e, callback, _object);
        }

        public static void BeginRise<TEventArgs>(this EventHandler<TEventArgs> handler,
            object sender, TEventArgs e, AsyncCallback callback, object _object)
        {
            if (handler != null)
                handler.BeginInvoke(sender, e, callback, _object);
        }


    }
}
