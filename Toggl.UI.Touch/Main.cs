using System;
using MonoTouch.UIKit;

namespace Toggle.UI.Touch
{
    public class Application
    {
        private static void Main(string[] args)
        {
            try
            {
                UIApplication.Main(args, null, "AppDelegate");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}