using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    /// <summary>
    /// Base navigation controller reimplements the UINavigationController for better NavigationBar customizability
    /// (height and transition animations).
    /// </summary>
    public class StackController : UIViewController
    {
        private readonly List<UIViewController> viewControllers = new List<UIViewController> ();
        private TransitionContext transitionContext;
        private UIPercentDrivenInteractiveTransition interactiveTransition;

        public StackController ()
        {
        }

        public StackController (UIViewController rootViewController)
        {
            SetViewControllers (new[] { rootViewController }, false);
        }

        public StackController (IntPtr handle) : base (handle)
        {
        }

        public override void LoadView ()
        {
            View = new UIView ();

            UIScreenEdgePanGestureRecognizer panGesture = null;
            panGesture = new UIScreenEdgePanGestureRecognizer (() => {
                var progress = panGesture.TranslationInView (View).X / View.Bounds.Width * 1.0;
                progress = Math.Min (1, Math.Max (0, progress));

                switch (panGesture.State) {
                case UIGestureRecognizerState.Began:
                    interactiveTransition = new UIPercentDrivenInteractiveTransition ();
                    PopViewController (true);
                    break;
                case UIGestureRecognizerState.Changed:
                    // Update the interactive transition's progress
                    interactiveTransition.UpdateInteractiveTransition ((float)progress);
                    break;
                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                    if (progress > 0.5) {
                        interactiveTransition.FinishInteractiveTransition ();
                    } else {
                        interactiveTransition.CancelInteractiveTransition ();
                    }

                    interactiveTransition = null;
                    break;
                }
            }) {
                Edges = UIRectEdge.Left,
            };
            View.AddGestureRecognizer (panGesture);
        }

        public void SetNavigationBarHidden (bool hidden, bool animated)
        {
            // TODO
        }

        public void SetViewControllers (UIViewController[] controllers, bool animated)
        {
            // Check that there aren't any other animations running currently
            if (transitionContext != null)
                return;

            var fromController = TopViewController;
            var toController = controllers.Length > 0 ? controllers [controllers.Length - 1] : null;

            if (fromController == toController)
                return;

            // Make sure all of the view controllers in between are hidden.
            foreach (var vc in viewControllers) {
                if (vc != fromController) {
                    vc.View.RemoveFromSuperview ();
                }
            }

            if (fromController != null && toController != null) {
                View.InsertSubviewAbove (toController.View, fromController.View);
            } else if (toController != null) {
                View.AddSubview (toController.View);
            }
            if (toController != null) {
                AddChildViewController (toController);
            }
            if (fromController != null) {
                fromController.WillMoveToParentViewController (null);
            }

            transitionContext = new TransitionContext (this) {
                FromController = fromController,
                ToController = toController,
                Operation = UINavigationControllerOperation.Pop,
                Callback = (success) => {
                    transitionContext = null;

                    if (!success) {
                        if (toController != null) {
                            toController.WillMoveToParentViewController (null);
                            toController.View.RemoveFromSuperview ();
                            toController.RemoveFromParentViewController ();
                        }
                        return;
                    }

                    // Clear old controllers
                    foreach (var controller in viewControllers) {
                        if (controller != fromController) {
                            controller.WillMoveToParentViewController (null);
                        }
                        controller.View.RemoveFromSuperview ();
                        controller.RemoveFromParentViewController ();
                    }
                    viewControllers.Clear ();

                    // Add new ones:
                    foreach (var controller in controllers) {
                        viewControllers.Add (controller);

                        if (controller != toController) {
                            AddChildViewController (controller);
                        }
                        controller.DidMoveToParentViewController (this);
                    }
                },
            };

            if (animated) {
                transitionContext.StartTransition ();
            } else {
                if (toController != null) {
                    toController.View.Frame = transitionContext.GetFinalFrameForViewController (toController);
                }
                transitionContext.Callback (true);
            }
        }

        public UIViewController[] ViewControllers {
            get { return viewControllers.ToArray (); }
            set { SetViewControllers (value, false); }
        }

        public void PushViewController (UIViewController controller, bool animated)
        {
            // Check that there aren't any other animations running currently
            if (transitionContext != null)
                return;
            if (viewControllers.Contains (controller))
                throw new ArgumentException ("This controller has already been pushed to the stack.", "controller");

            var fromController = TopViewController;
            var toController = controller;
            if (fromController == toController)
                return;

            // Make sure all of the view controllers in between are hidden.
            View.InsertSubviewAbove (toController.View, fromController.View);
            AddChildViewController (toController);

            transitionContext = new TransitionContext (this) {
                FromController = fromController,
                ToController = toController,
                Operation = UINavigationControllerOperation.Push,
                Callback = (success) => {
                    transitionContext = null;

                    if (!success) {
                        toController.WillMoveToParentViewController (null);
                        toController.View.RemoveFromSuperview ();
                        toController.RemoveFromParentViewController ();
                    } else {
                        fromController.View.RemoveFromSuperview ();
                        viewControllers.Add (toController);
                        toController.DidMoveToParentViewController (this);
                    }
                },
            };

            if (animated) {
                transitionContext.StartTransition ();
            } else {
                toController.View.Frame = transitionContext.GetFinalFrameForViewController (toController);
                transitionContext.Callback (true);
            }
        }

        public void PopViewController (bool animated)
        {
            // Check that there aren't any other animations running currently
            if (transitionContext != null)
                return;

            if (viewControllers.Count < 2)
                return;

            var fromController = viewControllers [viewControllers.Count - 1];
            var toController = viewControllers [viewControllers.Count - 2];
            if (fromController == toController)
                return;

            // Make sure all of the view controllers in between are hidden.
            for (var i = viewControllers.Count - 2; i >= 0; i--) {
                var vc = viewControllers [i];

                vc.View.RemoveFromSuperview ();

                if (vc == toController)
                    break;
            }

            View.InsertSubviewBelow (toController.View, fromController.View);
            fromController.WillMoveToParentViewController (null);

            transitionContext = new TransitionContext (this) {
                FromController = fromController,
                ToController = toController,
                Operation = UINavigationControllerOperation.Pop,
                Callback = (success) => {
                    transitionContext = null;

                    if (!success)
                        return;

                    // Clear popped view controllers
                    while (viewControllers.Count > 1) {
                        var controller = viewControllers [viewControllers.Count - 1];
                        if (controller == toController)
                            break;

                        if (controller != fromController) {
                            controller.WillMoveToParentViewController (null);
                        } else {
                            controller.View.RemoveFromSuperview ();
                        }
                        controller.RemoveFromParentViewController ();

                        viewControllers.RemoveAt (viewControllers.Count - 1);
                    }
                },
            };

            if (animated) {
                transitionContext.StartTransition ();
            } else {
                toController.View.Frame = transitionContext.GetFinalFrameForViewController (toController);
                transitionContext.Callback (true);
            }
        }

        public void PopToViewController (UIViewController controller, bool animated)
        {
            // Check that there aren't any other animations running currently
            if (transitionContext != null)
                return;

            if (!viewControllers.Contains (controller))
                throw new ArgumentException ("Specified container is not present in the stack.", "controller");

            var fromController = TopViewController;
            var toController = controller;
            if (fromController == toController)
                return;

            // Make sure all of the view controllers in between are hidden.
            for (var i = viewControllers.Count - 2; i >= 0; i--) {
                var vc = viewControllers [i];

                vc.View.RemoveFromSuperview ();

                if (vc == toController)
                    break;
            }

            View.InsertSubviewBelow (toController.View, fromController.View);
            fromController.WillMoveToParentViewController (null);

            transitionContext = new TransitionContext (this) {
                FromController = fromController,
                ToController = toController,
                Operation = UINavigationControllerOperation.Pop,
                Callback = (success) => {
                    transitionContext = null;

                    if (!success)
                        return;

                    // Clear popped view controllers
                    while (viewControllers.Count > 1) {
                        controller = viewControllers [viewControllers.Count - 1];
                        if (controller == toController)
                            break;

                        if (controller != fromController) {
                            controller.WillMoveToParentViewController (null);
                        } else {
                            controller.View.RemoveFromSuperview ();
                        }
                        controller.RemoveFromParentViewController ();

                        viewControllers.RemoveAt (viewControllers.Count - 1);
                    }
                },
            };

            if (animated) {
                transitionContext.StartTransition ();
            } else {
                toController.View.Frame = transitionContext.GetFinalFrameForViewController (toController);
                transitionContext.Callback (true);
            }
        }

        public void PopToRootViewController (bool animated)
        {
            // Check that there aren't any other animations running currently
            if (transitionContext != null)
                return;

            var fromController = TopViewController;
            var toController = viewControllers [0];
            if (fromController == toController)
                return;

            // Make sure all of the view controllers in between are hidden.
            foreach (var vc in viewControllers) {
                if (vc != fromController) {
                    vc.View.RemoveFromSuperview ();
                }
            }

            View.InsertSubviewBelow (toController.View, fromController.View);
            fromController.WillMoveToParentViewController (null);

            transitionContext = new TransitionContext (this) {
                FromController = fromController,
                ToController = toController,
                Operation = UINavigationControllerOperation.Pop,
                Callback = (success) => {
                    transitionContext = null;

                    if (!success)
                        return;

                    // Clear popped view controllers
                    while (viewControllers.Count > 1) {
                        var controller = viewControllers [viewControllers.Count - 1];
                        if (controller == toController)
                            break;

                        if (controller != fromController) {
                            controller.WillMoveToParentViewController (null);
                        } else {
                            controller.View.RemoveFromSuperview ();
                        }
                        controller.RemoveFromParentViewController ();

                        viewControllers.RemoveAt (viewControllers.Count - 1);
                    }
                },
            };

            if (animated) {
                transitionContext.StartTransition ();
            } else {
                toController.View.Frame = transitionContext.GetFinalFrameForViewController (toController);
                transitionContext.Callback (true);
            }
        }

        protected virtual UIViewControllerAnimatedTransitioning GetAnimationController (
            UINavigationControllerOperation operation, UIViewController fromController, UIViewController toController)
        {
            return new DefaultAnimator ();
        }

        protected virtual UIViewControllerInteractiveTransitioning GetInteractionController (
            UIViewControllerAnimatedTransitioning animationController)
        {
            if (interactiveTransition != null)
                return new WrapperInteractor (interactiveTransition);
            return null;
        }

        public UIViewController TopViewController {
            get {
                if (viewControllers.Count > 0)
                    return viewControllers [viewControllers.Count - 1];
                return null;
            }
        }

        public UIView NavigationBar {
            get { throw new NotImplementedException (); }
        }

        private class TransitionContext : UIViewControllerContextTransitioning
        {
            private readonly StackController stackController;
            private UIViewControllerAnimatedTransitioning animationController;
            private bool isInteractive;
            private bool transitionWasCancelled;

            public TransitionContext (StackController stackController)
            {
                this.stackController = stackController;
            }

            public void StartTransition ()
            {
                animationController = stackController.GetAnimationController (UINavigationControllerOperation.Pop, FromController, ToController);
                var interactionController = stackController.GetInteractionController (animationController);
                isInteractive = interactionController != null;

                if (interactionController != null) {
                    interactionController.StartInteractiveTransition (this);
                } else {
                    animationController.AnimateTransition (this);
                }
            }

            public UIViewController FromController { get; set; }

            public UIViewController ToController { get; set; }

            public UINavigationControllerOperation Operation { get; set; }

            public Action<bool> Callback { get; set; }

            public override void CancelInteractiveTransition ()
            {
                transitionWasCancelled = true;

                // TODO: ?
            }

            public override void CompleteTransition (bool didComplete)
            {
                if (Callback != null) {
                    Callback (didComplete);
                }

                if (animationController != null) {
                    animationController.AnimationEnded (didComplete);
                }
            }

            public override void FinishInteractiveTransition ()
            {
                // TODO: ?
            }

            public override RectangleF GetFinalFrameForViewController (UIViewController vc)
            {
                if (vc == FromController) {
                    if (Operation == UINavigationControllerOperation.Pop)
                        return AboveFrame;
                    if (Operation == UINavigationControllerOperation.Push)
                        return BelowFrame;
                } else if (vc == ToController) {
                    return CenterFrame;
                }
                return RectangleF.Empty;
            }

            public override RectangleF GetInitialFrameForViewController (UIViewController vc)
            {
                if (vc == FromController) {
                    return CenterFrame;
                } else if (vc == ToController) {
                    if (Operation == UINavigationControllerOperation.Pop)
                        return BelowFrame;
                    if (Operation == UINavigationControllerOperation.Push)
                        return AboveFrame;
                }
                return RectangleF.Empty;
            }

            private RectangleF BelowFrame {
                get {
                    var frame = CenterFrame;
                    frame.X -= frame.Width / 3;
                    return frame;
                }
            }

            private RectangleF CenterFrame {
                get { return ContainerView.Frame; }
            }

            private RectangleF AboveFrame {
                get {
                    var frame = CenterFrame;
                    frame.X += frame.Width;
                    return frame;
                }
            }

            public override UIViewController GetViewControllerForKey (NSString uiTransitionKey)
            {
                if (uiTransitionKey.Compare (UITransitionContext.FromViewControllerKey) == NSComparisonResult.Same) {
                    return FromController;
                }
                if (uiTransitionKey.Compare (UITransitionContext.ToViewControllerKey) == NSComparisonResult.Same) {
                    return ToController;
                }
                return null;
            }

            public override void UpdateInteractiveTransition (float percentComplete)
            {
                // TODO: Update navigation bar animation progress
            }

            public override UIView ContainerView {
                get { return stackController.View; }
            }

            public override bool IsAnimated {
                get { return true; }
            }

            public override bool IsInteractive {
                get { return isInteractive; }
            }

            public override UIModalPresentationStyle PresentationStyle {
                get { return default(UIModalPresentationStyle); }
            }

            public override bool TransitionWasCancelled {
                get { return transitionWasCancelled; }
            }
        }

        private class DefaultAnimator : UIViewControllerAnimatedTransitioning
        {
            public override void AnimateTransition (IUIViewControllerContextTransitioning transitionContext)
            {
                var fromController = transitionContext.GetViewControllerForKey (UITransitionContext.FromViewControllerKey);
                var toController = transitionContext.GetViewControllerForKey (UITransitionContext.ToViewControllerKey);

                var containerView = transitionContext.ContainerView;

                if (toController != null) {
                    toController.View.Frame = transitionContext.GetInitialFrameForViewController (toController);
                }
                if (fromController != null) {
                    fromController.View.Frame = transitionContext.GetInitialFrameForViewController (fromController);
                }

                UIView.Animate (
                    TransitionDuration (transitionContext),
                    delegate {
                        if (toController != null) {
                            toController.View.Frame = transitionContext.GetFinalFrameForViewController (toController);
                        }
                        if (fromController != null) {
                            fromController.View.Frame = transitionContext.GetFinalFrameForViewController (fromController);
                        }
                    },
                    delegate {
                        transitionContext.CompleteTransition (!transitionContext.TransitionWasCancelled);
                    }
                );
            }

            public override void AnimationEnded (bool transitionCompleted)
            {
            }

            public override double TransitionDuration (IUIViewControllerContextTransitioning transitionContext)
            {
                return 0.3;
            }
        }

        private class WrapperInteractor : UIViewControllerInteractiveTransitioning
        {
            private readonly UIPercentDrivenInteractiveTransition transition;

            public WrapperInteractor (UIPercentDrivenInteractiveTransition transition)
            {
                this.transition = transition;
            }

            public override void StartInteractiveTransition (IUIViewControllerContextTransitioning transitionContext)
            {
                transition.StartInteractiveTransition (transitionContext);
            }
        }
    }
}
