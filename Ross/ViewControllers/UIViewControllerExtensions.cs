using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    public static class UIViewControllerExtensions
    {
        public static StackController GetStackController (this UIViewController self)
        {
            var controller = self;

            while (controller != null) {
                var stack = controller as StackController;
                if (stack != null)
                    return stack;

                controller = controller.ParentViewController;
            }

            return null;
        }

        public static void PushViewController (this UIViewController self, UIViewController controller, bool animated)
        {
            var stack = self.GetStackController ();
            if (stack == null)
                throw new InvalidOperationException ("View controller not managed by StackController.");
            stack.PushViewController (controller, animated);
        }

        public static void PopViewController (this UIViewController self, bool animated)
        {
            var stack = self.GetStackController ();
            if (stack == null)
                throw new InvalidOperationException ("View controller not managed by StackController.");
            stack.PopViewController (animated);
        }

        public static void PopToViewController (this UIViewController self, UIViewController controller, bool animated)
        {
            var stack = self.GetStackController ();
            if (stack == null)
                throw new InvalidOperationException ("View controller not managed by StackController.");
            stack.PopToViewController (controller, animated);
        }

        public static void PopToRootViewController (this UIViewController self, bool animated)
        {
            var stack = self.GetStackController ();
            if (stack == null)
                throw new InvalidOperationException ("View controller not managed by StackController.");
            stack.PopViewController (animated);
        }
    }
}

