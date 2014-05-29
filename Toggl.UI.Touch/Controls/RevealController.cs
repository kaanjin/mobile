using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Cirrious.MvvmCross.Touch.Views;
using MonoTouch.CoreAnimation;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Linq;

namespace Toggle.UI.Touch.Controls
{
    public delegate void DefaultCompletionHandler(bool finished);

    public delegate void SequentialAnimationProgressHandler(NSValue fromValue, NSValue toValue, uint index);

    public delegate void AnimationCompletionHandler(bool finished);

    public enum RevealControllerType
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = (Left | Right)
    }

    public enum RevealControllerState
    {
        ShowsLeftViewControllerInPresentationMode = 1,
        ShowsLeftViewController = 2,
        ShowsFrontViewController = 3,
        ShowsRightViewController = 4,
        ShowsRightViewControllerInPresentationMode = 5
    }

    public enum RevealControllerAnimationType
    {
        Static
    }

    public class GestureRecognizerInteractionFlags
    {
        public PointF InitialTouchPoint { get; set; }
        public PointF PreviousTouchPoint { get; set; }
        public PointF CurrentTouchPoint { get; set; }

        public GestureRecognizerInteractionFlags()
        {
            InitialTouchPoint = new PointF(0, 0);
            PreviousTouchPoint = new PointF(0, 0);
            CurrentTouchPoint = new PointF(0, 0);
        }
    }

    public class RevealControllerFrontViewInteractionFlags
    {
        public GestureRecognizerInteractionFlags RecognizerFlags { get; set; }
        public PointF InitialFrontViewPosition { get; set; }
        public bool IsInteracting { get; set; }

        public RevealControllerFrontViewInteractionFlags()
        {
            RecognizerFlags = new GestureRecognizerInteractionFlags();
            InitialFrontViewPosition = new PointF(0, 0);
            IsInteracting = false;
        }
    }

    public class RevealController : MvxViewController
    {
        private const string RevealControllerFrontViewTranslationAnimationKey = @"frontViewTranslation";

        private static readonly TimeSpan DEFAULT_ANIMATION_DURATION_VALUE = new TimeSpan(0, 0, 0, 185);
        private const UIViewAnimationCurve DEFAULT_ANIMATION_CURVE_VALUE = UIViewAnimationCurve.Linear;
        private static readonly NSRange DEFAULT_LEFT_VIEW_WIDTH_RANGE = new NSRange(260, 40);
        private static readonly NSRange DefaultRightViewWidthRange = new NSRange(260, 40);
        private const bool DEFAULT_ALLOWS_OVERDRAW_VALUE = true;
        private const RevealControllerAnimationType DEFAULT_ANIMATION_TYPE_VALUE = RevealControllerAnimationType.Static;
        private const float DEFAULT_QUICK_SWIPE_TOGGLE_VELOCITY_VALUE = 800.0f;
        private const bool DEFAULT_DISABLES_FRONT_VIEW_INTERACTION_VALUE = true;
        private const bool DEFAULT_RECOGNIZES_PAN_ON_FRONT_VIEW_VALUE = true;
        private const bool DEFAULT_RECOGNIZES_RESET_TAP_ON_FRONT_VIEW_VALUE = true;
        private const bool DEFAULT_RECOGNIZES_RESET_TAP_ON_FRONT_VIEW_IN_PRESENTATION_MODE_VALUE = true;

        private RevealControllerFrontViewInteractionFlags _frontViewInteraction = new RevealControllerFrontViewInteractionFlags();
        RevealControllerFrontViewInteractionFlags FrontViewInteraction { get { return _frontViewInteraction; } }

        private TimeSpan _animationDuration;
        private UIViewAnimationCurve _animationCurve;
        private RevealControllerAnimationType _animationType;
        private float _quickSwipeVelocity;
        private bool _allowsOverdraw;
        private bool _disablesFrontViewInteraction;
        private bool _recognizesPanningOnFrontView;
        private bool _recognizesResetTapOnFrontView;
        private bool _recognizesResetTapOnFrontViewInPresentationMode;

        readonly NSString PKRevealControllerAnimationDurationKey = (NSString)@"animationDuration";
        readonly NSString PKRevealControllerAnimationCurveKey = (NSString)@"animationCurve";
        readonly NSString PKRevealControllerAnimationTypeKey = (NSString)@"animationType";
        readonly NSString PKRevealControllerAllowsOverdrawKey = (NSString)@"allowsOverdraw";
        readonly NSString PKRevealControllerQuickSwipeToggleVelocityKey = (NSString)@"quickSwipeVelocity";
        readonly NSString PKRevealControllerDisablesFrontViewInteractionKey = (NSString)@"disablesFrontViewInteraction";
        readonly NSString PKRevealControllerRecognizesPanningOnFrontViewKey = (NSString)@"recognizesPanningOnFrontView";
        readonly NSString PKRevealControllerRecognizesResetTapOnFrontViewKey = (NSString)@"recognizesResetTapOnFrontView";
        readonly NSString PKRevealControllerRecognizesResetTapOnFrontViewInPresentationModeKey = (NSString)@"recognizesResetTapOnFrontViewInPresentationMode";

        protected UIViewController _frontViewController;
        protected UIViewController _leftViewController;
        protected UIViewController _rightViewController;


        private UIGestureRecognizer _revealResetTapGestureRecognizer, _revealPanGestureRecognizer;

        private float LeftViewMinWidth { get { return LeftViewWidthRange.Location; } }
        private float RightViewMinWidth { get { return RightViewWidthRange.Location; } }
        private float LeftViewMaxWidth { get { return LeftViewWidthRange.Location + LeftViewWidthRange.Length; } }
        private float RightViewMaxWidth { get { return RightViewWidthRange.Location + RightViewWidthRange.Length; } }

        private RevealControllerView FrontView { get; set; }
        private RevealControllerView LeftView { get; set; }
        private RevealControllerView RightView { get; set; }

        public RevealLayerAnimator Animator { get; set; }

        public NSRange LeftViewWidthRange { get; set; }
        public NSRange RightViewWidthRange { get; set; }

        public TimeSpan AnimationDuration { get; set; }

        private RevealControllerState _state = RevealControllerState.ShowsFrontViewController;
        public RevealControllerState State { get { return _state; } set { _state = value; } }

        #region Constructors

        public RevealController()
            : base()
        {
            this.LoadDefaultValues();
        }

        public RevealController(UIViewController frontViewController, UIViewController leftViewController, UIViewController rightViewController)
            : this()
        {
            _frontViewController = frontViewController;
            _leftViewController = leftViewController;
            _rightViewController = rightViewController;
        }

        #endregion

        #region Show controllers

        private void ShowLeftViewController()
        {
            ShowViewController(_leftViewController);
        }

        private void ShowRightViewController()
        {
            ShowViewController(_rightViewController);
        }

        private void ShowFrontViewController()
        {
            ShowViewController(_frontViewController);
        }

        public void ShowViewController(UIViewController controller, bool animated = true, DefaultCompletionHandler completion = null)
        {
            RevealControllerState toState = RevealControllerState.ShowsFrontViewController;
            PointF toPoint = CenterPointForState(toState);

            if (controller == _leftViewController)
            {
                toState = RevealControllerState.ShowsLeftViewController;
                toPoint = CenterPointForState(toState);
            }
            else if (controller == _rightViewController)
            {
                toState = RevealControllerState.ShowsRightViewController;
                toPoint = CenterPointForState(toState);
            }

            if (animated)
            {
                AnimateToState(toState, completion);
            }
            else
            {
                FrontView.Layer.Position = toPoint;
            }
        }

        #endregion

        #region Set controllers

        public void SetFrontViewController(UIViewController frontViewController, bool focusAfterChange,
                                   DefaultCompletionHandler completion)
        {
            this.SetFrontViewController(frontViewController);

            if (focusAfterChange)
            {
                ShowViewController(frontViewController, true, completion);
            }
            else
            {
                UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
                {
                    if (completion != null)
                    {
                        completion(false);
                    }
                });
            }
        }

        public void SetFrontViewController(UIViewController frontViewController)
        {
            if (frontViewController != this._frontViewController)
            {
                if (this._frontViewController != null)
                {
                    this.RemoveViewController(this._frontViewController);
                }

                this._frontViewController = frontViewController;

                if (this._frontViewController != null)
                {
                    this.AddViewController(this._frontViewController, this.FrontView);
                }
            }
        }

        public void SetLeftViewController(UIViewController leftViewController)
        {
            if (leftViewController != this._leftViewController)
            {
                if (this._leftViewController != null)
                {
                    this.RemoveViewController(this._leftViewController);
                }
            }

            this._leftViewController = leftViewController;

            if (_leftViewController != null)
            {
                this.AddViewController(this._leftViewController, this.LeftView);
            }
        }

        public void SetRightViewController(UIViewController rightViewController)
        {
            if (rightViewController != this._rightViewController)
            {
                if (this._rightViewController != null)
                {
                    this.RemoveViewController(this._leftViewController);
                }
            }

            this._rightViewController = rightViewController;

            if (_leftViewController != null)
            {
                this.AddViewController(this._rightViewController, this.RightView);
            }
        }

        #endregion

        #region Presentation mode

        public void EnterPresentationModeForViewController(UIViewController controller, bool animated,
                                                   DefaultCompletionHandler completion)
        {
            if (controller != this._rightViewController || controller != this._leftViewController)
            {
                return;
            }

            RevealControllerState toState = RevealControllerState.ShowsLeftViewControllerInPresentationMode;

            if (controller == this._rightViewController)
            {
                toState = RevealControllerState.ShowsRightViewControllerInPresentationMode;
            }

            this.AnimateToState(toState, completion);
        }

        public void EnterPresentationModeAnimated(bool animated, DefaultCompletionHandler completion)
        {
            RevealControllerState toState = this.State;

            if (this.HasLeftViewController() && this.HasRightViewController())
            {
                if (this.State == RevealControllerState.ShowsFrontViewController)
                {
                    toState = RevealControllerState.ShowsLeftViewControllerInPresentationMode;
                }
                else if (this.State == RevealControllerState.ShowsRightViewController)
                {
                    toState = RevealControllerState.ShowsRightViewControllerInPresentationMode;
                }
                else
                {
                    UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
                    {
                        if (completion != null)
                        {
                            completion(false);
                        }
                    });
                }
            }
            else if (this.HasLeftViewController())
            {
                toState = RevealControllerState.ShowsLeftViewControllerInPresentationMode;
            }
            else if (this.HasRightViewController())
            {
                toState = RevealControllerState.ShowsRightViewControllerInPresentationMode;
            }

            this.AnimateToState(toState, completion);
        }

        public void ResignPresentationModeEntirely(bool entirely, bool animated, DefaultCompletionHandler completion)
        {
            RevealControllerState toState = RevealControllerState.ShowsFrontViewController;

            if (!entirely)
            {
                if (this.State == RevealControllerState.ShowsLeftViewControllerInPresentationMode)
                {
                    toState = RevealControllerState.ShowsLeftViewController;
                }
                else if (this.State == RevealControllerState.ShowsRightViewControllerInPresentationMode)
                {
                    toState = RevealControllerState.ShowsRightViewController;
                }
            }

            this.AnimateToState(toState, completion);
        }

        public bool IsPresentationModeActive()
        {
            return (this.State == RevealControllerState.ShowsLeftViewControllerInPresentationMode ||
                    this.State == RevealControllerState.ShowsRightViewControllerInPresentationMode);
        }


        #endregion

        public void SetMinimumWidth(float minWidth, float maxWidth, UIViewController forViewController)
        {
            if (forViewController == this._leftViewController)
            {
                this.LeftViewWidthRange = new NSRange((int)minWidth, (int)(maxWidth - minWidth));
            }
            else if (forViewController == this._rightViewController)
            {
                this.RightViewWidthRange = new NSRange((int)minWidth, (int)(maxWidth - minWidth));
            }
        }

        public void SetOptions(NSDictionary options)
        {
            if (options != null)
            {
                this.SetValuesForKeysWithDictionary(options);
            }
        }

        public NSDictionary Options()
        {
            NSString[] keys = new[] { PKRevealControllerAnimationDurationKey,
                                      PKRevealControllerAnimationCurveKey,
                                      PKRevealControllerAnimationTypeKey,
                                      PKRevealControllerAllowsOverdrawKey,
                                      PKRevealControllerQuickSwipeToggleVelocityKey,
                                      PKRevealControllerDisablesFrontViewInteractionKey,
                                      PKRevealControllerRecognizesPanningOnFrontViewKey,
                                      PKRevealControllerRecognizesResetTapOnFrontViewKey,
                                      PKRevealControllerRecognizesResetTapOnFrontViewInPresentationModeKey, };
            return this.GetDictionaryOfValuesFromKeys(keys);
        }

        public UIViewController FocusedController()
        {
            UIViewController controller = null;

            switch (State)
            {
                case RevealControllerState.ShowsFrontViewController:
                    controller = _frontViewController;
                    break;
                case RevealControllerState.ShowsLeftViewController:
                case RevealControllerState.ShowsLeftViewControllerInPresentationMode:
                    controller = _leftViewController;
                    break;
                case RevealControllerState.ShowsRightViewController:
                case RevealControllerState.ShowsRightViewControllerInPresentationMode:
                    controller = _rightViewController;
                    break;
                default:
                    break;
            }
            return controller;
        }



        public bool HasRightViewController()
        {
            return (this._rightViewController != null);
        }

        public bool HasLeftViewController()
        {
            return (this._leftViewController != null);
        }

        public void LoadDefaultValues()
        {
            _animationDuration = DEFAULT_ANIMATION_DURATION_VALUE;
            _animationCurve = DEFAULT_ANIMATION_CURVE_VALUE;
            _animationType = DEFAULT_ANIMATION_TYPE_VALUE;
            _quickSwipeVelocity = DEFAULT_QUICK_SWIPE_TOGGLE_VELOCITY_VALUE;
            _allowsOverdraw = DEFAULT_ALLOWS_OVERDRAW_VALUE;
            _disablesFrontViewInteraction = DEFAULT_DISABLES_FRONT_VIEW_INTERACTION_VALUE;
            _recognizesPanningOnFrontView = DEFAULT_RECOGNIZES_PAN_ON_FRONT_VIEW_VALUE;
            _recognizesResetTapOnFrontView = DEFAULT_RECOGNIZES_RESET_TAP_ON_FRONT_VIEW_VALUE;
            LeftViewWidthRange = DEFAULT_LEFT_VIEW_WIDTH_RANGE;
            RightViewWidthRange = DefaultRightViewWidthRange;
            _recognizesResetTapOnFrontViewInPresentationMode = DEFAULT_RECOGNIZES_RESET_TAP_ON_FRONT_VIEW_IN_PRESENTATION_MODE_VALUE;
        }

        public void SetupContainerViews()
        {
            this.RightView = new RevealControllerView(this.View.Bounds);
            this.LeftView = new RevealControllerView(this.View.Bounds);
            this.FrontView = new RevealControllerView(this.View.Bounds);

            this.RightView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.LeftView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.FrontView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            this.RightView.ViewController = this._rightViewController;
            this.LeftView.ViewController = this._leftViewController;
            this.FrontView.ViewController = this._frontViewController;

            this.FrontView.Shadow = true;

            this.LeftView.Hidden = true;
            this.RightView.Hidden = true;

            this.View.AddSubview(this.RightView);
            this.View.AddSubview(this.LeftView);
            this.View.AddSubview(this.FrontView);

            this.AddViewController(this._frontViewController, this.FrontView);
        }

        public void SetupGestureRecognizers()
        {
            this._revealPanGestureRecognizer = new UIPanGestureRecognizer(DidRecognizePanGesture);

            //this._revealPanGestureRecognizer.NumberOfTouches = 1; //todo: wtf?

            this._revealResetTapGestureRecognizer = new UITapGestureRecognizer(DidRecognizeTapGesture);

            this.UpdatePanGestureRecognizerPresence();
            this.UpdateTapGestureRecognizerPrecence();
        }

        public void SetRecognizesResetTapOnFrontView(bool recognizesResetTapOnFrontView)
        {
            if (recognizesResetTapOnFrontView != this._recognizesResetTapOnFrontView)
            {
                this._recognizesPanningOnFrontView = recognizesResetTapOnFrontView;
                this.UpdateTapGestureRecognizerPrecence();
            }
        }

        public void SetRecognizesPanningOnFrontView(bool recognizesPanningOnFrontView)
        {
            if (recognizesPanningOnFrontView != this._recognizesPanningOnFrontView)
            {
                this._recognizesPanningOnFrontView = recognizesPanningOnFrontView;
                this.UpdatePanGestureRecognizerPresence();
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.State = RevealControllerState.ShowsFrontViewController;

            this.SetupContainerViews();
            this.SetupGestureRecognizers();

            this.Animator = new RevealLayerAnimator(this.FrontView.Layer);
        }

        public override bool ShouldAutorotate()
        {
            if (this.FrontViewInteraction.IsInteracting)
            {
                return false;
            }
            else if (this.HasLeftViewController() && HasRightViewController())
            {
                return this._frontViewController.ShouldAutorotate() && this._leftViewController.ShouldAutorotate() &&
                       this._rightViewController.ShouldAutorotate();
            }
            else if (this.HasLeftViewController())
            {
                return this._frontViewController.ShouldAutorotate() && this._leftViewController.ShouldAutorotate();
            }
            else if (this.HasRightViewController())
            {
                return this._frontViewController.ShouldAutorotate() && this._rightViewController.ShouldAutorotate();
            }
            else
            {
                return this._frontViewController.ShouldAutorotate();
            }
        }

        public override UIStatusBarStyle PreferredStatusBarStyle()
        {
            UIViewController controller = null;

            switch (this.State)
            {
                case RevealControllerState.ShowsLeftViewControllerInPresentationMode:
                case RevealControllerState.ShowsLeftViewController:
                    controller = this._leftViewController;
                    break;
                case RevealControllerState.ShowsRightViewControllerInPresentationMode:
                case RevealControllerState.ShowsRightViewController:
                    controller = this._rightViewController;
                    break;
                case RevealControllerState.ShowsFrontViewController:
                    controller = this._frontViewController;
                    break;
                default:
                    break;
            }

            if (controller.RespondsToSelector(new Selector("PreferredStatusBarStyle")))
            {
                return controller.PreferredStatusBarStyle();
            }

            return UIStatusBarStyle.Default;
        }

        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
        {
            if (this.HasLeftViewController() && this.HasRightViewController())
            {
                return this._frontViewController.GetSupportedInterfaceOrientations() &
                       this._leftViewController.GetSupportedInterfaceOrientations() &
                       this._rightViewController.GetSupportedInterfaceOrientations();
            }
            else if (this.HasLeftViewController())
            {
                return this._frontViewController.GetSupportedInterfaceOrientations() &
                       this._leftViewController.GetSupportedInterfaceOrientations();
            }
            else if (this.HasRightViewController())
            {
                return this._frontViewController.GetSupportedInterfaceOrientations() &
                       this._rightViewController.GetSupportedInterfaceOrientations();
            }
            else
            {
                return this._frontViewController.GetSupportedInterfaceOrientations();
            }
        }

        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            if (this.FrontViewInteraction.IsInteracting)
            {
                return false;
            }
            else if (this.HasLeftViewController() && HasRightViewController())
            {
                return this._frontViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation) && this._leftViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation) &&
                       this._rightViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }
            else if (this.HasLeftViewController())
            {
                return this._frontViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation) && this._leftViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }
            else if (this.HasRightViewController())
            {
                return this._frontViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation) && this._rightViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }
            else
            {
                return this._frontViewController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }
        }

        public override void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            this.FrontView.UpdateShadowWithAnimationDuration(TimeSpan.FromSeconds(duration));
        }

        public bool AutomaticallyNotifiesObserversForKey(NSString key)
        {
            return (NSString)"state" != key;
        }

        public void DidRecognizePanGesture(UIPanGestureRecognizer recognizer)
        {
            switch (recognizer.State)
            {
                case UIGestureRecognizerState.Began:
                    this.HandlePanGestureBeganWithRecognizer(recognizer);
                    break;

                case UIGestureRecognizerState.Changed:
                    this.HandlePanGestureChangedWithRecognizer(recognizer);
                    break;

                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                case UIGestureRecognizerState.Failed:
                    this.HandlePanGestureEndedWithRecognizer(recognizer);
                    break;
                default:
                    this.HandlePanGestureEndedWithRecognizer(recognizer);
                    break;
            }
        }

        public void HandlePanGestureBeganWithRecognizer(UIPanGestureRecognizer recognizer)
        {
            this.Animator.StopAnimationForKey((NSString)RevealControllerFrontViewTranslationAnimationKey);

            this.FrontViewInteraction.RecognizerFlags.InitialTouchPoint = recognizer.TranslationInView(this.FrontView);
            this.FrontViewInteraction.RecognizerFlags.PreviousTouchPoint = this.FrontViewInteraction.RecognizerFlags.InitialTouchPoint;
            this.FrontViewInteraction.InitialFrontViewPosition = this.FrontView.Layer.Position;

            this.UpdateRearViewVisibility();
        }

        public void HandlePanGestureChangedWithRecognizer(UIPanGestureRecognizer recognizer)
        {
            this.FrontViewInteraction.RecognizerFlags.CurrentTouchPoint = recognizer.TranslationInView(this.FrontView);
            float newX = this.FrontViewInteraction.InitialFrontViewPosition.X + (this.FrontViewInteraction.RecognizerFlags.InitialTouchPoint.X + this.FrontViewInteraction.RecognizerFlags.CurrentTouchPoint.X);

            if (!this.HasLeftViewController() && newX >= this.CenterPointForState(RevealControllerState.ShowsFrontViewController).X)
            {
                newX = this.CenterPointForState(RevealControllerState.ShowsFrontViewController).X;
            }
            else if (!this.HasRightViewController() && newX <= this.CenterPointForState(RevealControllerState.ShowsFrontViewController).X)
            {
                newX = this.CenterPointForState(RevealControllerState.ShowsFrontViewController).X;
            }
            else
            {
                float dampenedLeft = this.DampenedValueForRealValue(newX - this.FrontView.Bounds.GetMidX(), this.LeftViewWidthRange) + this.FrontView.Bounds.GetMidX();
                float dampenedRight = this.DampenedValueForRealValue(newX - this.FrontView.Bounds.GetMidX(), this.RightViewWidthRange) + this.FrontView.Bounds.GetMidX();

                if (newX >= this.CenterPointForState(RevealControllerState.ShowsLeftViewControllerInPresentationMode).X &&
                    !(this.CenterPointForState(RevealControllerState.ShowsLeftViewControllerInPresentationMode).X >= dampenedLeft))
                {
                    newX = this.FrontView.Layer.Position.X;
                }
                else if (newX <= this.CenterPointForState(RevealControllerState.ShowsRightViewControllerInPresentationMode).X &&
                    !(this.CenterPointForState(RevealControllerState.ShowsRightViewControllerInPresentationMode).X <= dampenedRight))
                {
                    newX = this.FrontView.Layer.Position.X;
                }
                else if (newX >= this.CenterPointForState(RevealControllerState.ShowsLeftViewController).X)
                {
                    newX = dampenedLeft;
                }
                else if (newX <= this.CenterPointForState(RevealControllerState.ShowsRightViewController).X)
                {
                    newX = dampenedRight;
                }
            }

            this.FrontView.Layer.Position = new PointF(newX, this.FrontView.Layer.Position.Y);
            this.UpdateRearViewVisibility();

            this.FrontViewInteraction.RecognizerFlags.PreviousTouchPoint = this.FrontViewInteraction.RecognizerFlags.CurrentTouchPoint;
        }

        public void HandlePanGestureEndedWithRecognizer(UIPanGestureRecognizer recognizer)
        {
            this.FrontViewInteraction.RecognizerFlags.InitialTouchPoint = new PointF(0, 0);
            this.FrontViewInteraction.RecognizerFlags.PreviousTouchPoint = new PointF(0, 0);
            this.FrontViewInteraction.RecognizerFlags.CurrentTouchPoint = new PointF(0, 0);

            this.FrontViewInteraction.InitialFrontViewPosition = new PointF(0, 0);
            this.FrontViewInteraction.IsInteracting = false;

            if (this.ShouldMoveFrontViewLeftwardsForVelocity(recognizer.VelocityInView(this.View).X))
            {
                RevealControllerState toState = RevealControllerState.ShowsFrontViewController;

                if (this.State == RevealControllerState.ShowsRightViewController || this.State == RevealControllerState.ShowsRightViewControllerInPresentationMode)
                {
                    toState = this.State;
                }
                else if (this.State == RevealControllerState.ShowsFrontViewController && this.HasRightViewController())
                {
                    toState = RevealControllerState.ShowsRightViewController;
                }

                this.AnimateToState(toState, null);
            }
            else if (this.ShouldMoveFrontViewRightwardsForVelocity(recognizer.VelocityInView(this.FrontView).X))
            {
                RevealControllerState toState = RevealControllerState.ShowsFrontViewController;

                if (this.State == RevealControllerState.ShowsFrontViewController && this.HasLeftViewController())
                {
                    toState = RevealControllerState.ShowsLeftViewController;
                }
                else if (this.State == RevealControllerState.ShowsLeftViewController || this.State == RevealControllerState.ShowsLeftViewControllerInPresentationMode)
                {
                    toState = this.State;
                }

                this.AnimateToState(toState, null);
            }
            else
            {
                this.SnapFrontViewToAppropriateEdge();
            }
        }

        public float DampenedValueForRealValue(float realValue, NSRange absoluteRange)
        {
            bool isNegative = realValue < 0;

            realValue = Math.Abs(realValue);

            NSRange unitRange = new NSRange(0, 1);

            Func<float, NSRange, NSRange, float> linearMap = (x, from, to) =>
            {
                float a1 = from.Location;
                float b1 = from.Location + from.Length;

                float a2 = to.Location;
                float b2 = to.Location + to.Length;

                return ((x - a1) * (b2 - a2) / (b1 - a1)) + a2;
            };

            float unitValue = linearMap(realValue, absoluteRange, unitRange);

            Func<float, float> unitDampening = x => (float)((2.5 / Math.PI) * Math.Atan(x));

            float dampenedUnitValue = unitDampening(unitValue);

            float result = linearMap(dampenedUnitValue, unitRange, absoluteRange);

            if (isNegative)
            {
                result = -result;
            }

            return result;
        }

        //public void SetState(PKRevealControllerState state)
        //{
        //    if (state != this.State)
        //    {
        //        this.WillChangeValue("state");

        //        //todo: wtf?
        //        //        if (self.delegate &&
        //        //            [self.delegate conformsToProtocol:@protocol(PKRevealing)] &&
        //        //            [self.delegate respondsToSelector:@selector(revealController:willChangeToState:)])
        //        //        {
        //        //            [self.delegate revealController:self willChangeToState:state];
        //        //        }

        //        this.State = state;

        //        this.DidChangeValue("state");

        //        //        if (self.delegate &&
        //        //            [self.delegate conformsToProtocol:@protocol(PKRevealing)] &&
        //        //            [self.delegate respondsToSelector:@selector(revealController:didChangeToState:)])
        //        //        {
        //        //            [self.delegate revealController:self didChangeToState:state];
        //        //        }
        //    }
        //}

        public void UpdateRearViewVisibilityForFrontViewPosition(PointF position)
        {
            if (position.X > this.CenterPointForState(RevealControllerState.ShowsFrontViewController).X)
            {
                this.ShowLeftView();
            }
            else if (position.X < this.CenterPointForState(RevealControllerState.ShowsFrontViewController).X)
            {
                this.ShowRightView();
            }
            else
            {
                this.HideRearViews();
            }
        }

        public void UpdateRearViewVisibility()
        {
            if (this.IsLeftViewVisible)
            {
                if (this.LeftView.Hidden)
                {
                    this.ShowLeftView();
                }
            }
            else if (this.IsRightViewVisible)
            {
                if (this.RightView.Hidden)
                {
                    this.ShowRightView();
                }
            }
            else
            {
                if (!this.LeftView.Hidden || !this.LeftView.Hidden)
                {
                    this.HideRearViews();
                }
            }
        }

        public void HideRearViews()
        {
            this.RightView.Hidden = true;
            this.LeftView.Hidden = true;

            this.RemoveViewController(this._leftViewController);
            this.RemoveViewController(this._rightViewController);

            this.FrontView.SetUserInteractionForContainedViewEnabled(true);
        }

        public void ShowRightView()
        {
            this.RightView.Hidden = false;
            this.LeftView.Hidden = true;

            this.RemoveViewController(this._leftViewController);
            this.AddViewController(this._rightViewController, this.RightView);

            this.FrontView.SetUserInteractionForContainedViewEnabled(false);
        }

        public void ShowLeftView()
        {
            this.RightView.Hidden = true;
            this.LeftView.Hidden = false;

            this.RemoveViewController(this._rightViewController);
            this.AddViewController(this._leftViewController, this.LeftView);

            this.FrontView.SetUserInteractionForContainedViewEnabled(false);
        }

        public bool IsLeftViewVisible
        {
            get
            {
                CALayer layer = FrontView.Layer.PresentationLayer;
                return layer.Position.X > View.Bounds.GetMidX();
            }
        }

        public bool IsRightViewVisible
        {
            get
            {
                CALayer layer = this.FrontView.Layer.PresentationLayer;
                return layer.Position.X < this.View.Bounds.GetMidX();
            }
        }

        public void SnapFrontViewToAppropriateEdge()
        {
            float visibleWidth = 0.0f;

            RevealControllerState toState = RevealControllerState.ShowsFrontViewController;

            if (this.IsLeftViewVisible)
            {
                visibleWidth = this.FrontView.Layer.Position.X - this.LeftView.Layer.Position.X;

                if (visibleWidth > this.LeftViewMinWidth / 2.0f)
                {
                    toState = RevealControllerState.ShowsLeftViewController;
                }
            }
            else if (this.IsRightViewVisible)
            {
                visibleWidth = this.RightView.Layer.Position.X - this.FrontView.Layer.Position.X;

                if (visibleWidth > this.RightViewMinWidth / 2.0f)
                {
                    toState = RevealControllerState.ShowsRightViewController;
                }
            }

            this.AnimateToState(toState, null);
        }

        public bool ShouldMoveFrontViewRightwardsForVelocity(float velocity)
        {
            return velocity > 0 && velocity > this._quickSwipeVelocity;
        }

        public bool ShouldMoveFrontViewLeftwardsForVelocity(float velocity)
        {
            return velocity < 0 && Math.Abs(velocity) > this._quickSwipeVelocity;
        }

        public RevealControllerType Type
        {
            get
            {
                if (this.HasLeftViewController() && this.HasRightViewController())
                {
                    return RevealControllerType.Both;
                }
                else if (this.HasLeftViewController())
                {
                    return RevealControllerType.Left;
                }
                else if (this.HasRightViewController())
                {
                    return RevealControllerType.Right;
                }
                else
                {
                    return RevealControllerType.None;
                }
            }
        }

        public void DidRecognizeTapGesture(UITapGestureRecognizer recognizer)
        {
            if (this.State != RevealControllerState.ShowsFrontViewController)
            {
                this.AnimateToState(RevealControllerState.ShowsFrontViewController, null);
            }
        }

        public void AnimateToState(RevealControllerState toState, DefaultCompletionHandler completion)
        {
            UpdateRearViewVisibility();
            Animator.StopAnimationForKey((NSString)RevealControllerFrontViewTranslationAnimationKey);

            RevealSequentialAnimation animation = new RevealSequentialAnimation((NSString)"position",
                                                                                        this.KeyPositionsToState(toState),
                                                                                        this.AnimationDuration);

            animation.ProgressHandler = new SequentialAnimationProgressHandler((fromValue, toValue, index) =>
            {
                if (fromValue.PointFValue.X == CenterPointForState(RevealControllerState.ShowsFrontViewController).X)
                {
                    UpdateRearViewVisibilityForFrontViewPosition(toValue.PointFValue);
                }
                else
                {
                    UpdateRearViewVisibility();
                }
            });

            animation.CompletionHandler = new AnimationCompletionHandler(finished =>
            {
                if (finished)
                {
                    UpdateRearViewVisibility();
                }

                this.State = toState;

                UpdateTapGestureRecognizerPrecence();
                UpdatePanGestureRecognizerPresence();

                UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
                {
                    if (completion != null)
                    {
                        completion(finished);
                    }
                });
            });

            this.Animator.AddAnimation(animation, (NSString)RevealControllerFrontViewTranslationAnimationKey);
            this.Animator.StartAnimationForKey((NSString)RevealControllerFrontViewTranslationAnimationKey);
        }

        public void UpdateTapGestureRecognizerPrecence()
        {
            if ((this.State == RevealControllerState.ShowsRightViewControllerInPresentationMode ||
                this.State == RevealControllerState.ShowsLeftViewControllerInPresentationMode) &&
                this._recognizesResetTapOnFrontViewInPresentationMode)
            {
                if (!this.FrontView.GestureRecognizers.EmptyIfNull().Contains(this._revealResetTapGestureRecognizer))
                {
                    this.FrontView.AddGestureRecognizer(this._revealResetTapGestureRecognizer);
                }
            }
            else if ((this.State == RevealControllerState.ShowsLeftViewController ||
                      this.State == RevealControllerState.ShowsRightViewController) &&
                     this._recognizesResetTapOnFrontView)
            {
                if (!this.FrontView.GestureRecognizers.EmptyIfNull().Contains(this._revealResetTapGestureRecognizer))
                {
                    this.FrontView.AddGestureRecognizer(this._revealResetTapGestureRecognizer);
                }
            }
            else
            {
                if (this.FrontView.GestureRecognizers.EmptyIfNull().Contains(this._revealResetTapGestureRecognizer))
                {
                    this.FrontView.RemoveGestureRecognizer(this._revealResetTapGestureRecognizer);
                }
            }
        }

        public void UpdatePanGestureRecognizerPresence()
        {
            if (this._recognizesPanningOnFrontView)
            {
                if (!this.FrontView.GestureRecognizers.EmptyIfNull().Contains(this._revealPanGestureRecognizer))
                {
                    this.FrontView.AddGestureRecognizer(this._revealPanGestureRecognizer);
                }
            }
            else
            {
                if (this.FrontView.GestureRecognizers.EmptyIfNull().Contains(this._revealPanGestureRecognizer))
                {
                    this.FrontView.RemoveGestureRecognizer(this._revealPanGestureRecognizer);
                }
            }
        }

        public PointF[] KeyPositionsToState(RevealControllerState toState, bool viaFrontView)
        {
            if (viaFrontView)
            {
                return
                    new[] { this.CenterPointForState(RevealControllerState.ShowsFrontViewController), this.CenterPointForState(toState) };
            }
            else
            {
                return new[] { this.CenterPointForState(toState) };
            }
        }

        public PointF[] KeyPositionsToState(RevealControllerState toState)
        {
            IList<PointF> keyPositions = null;
            RevealControllerState fromState = this.State;

            if (fromState == toState)
            {
                keyPositions = this.KeyPositionsToState(toState, false);
            }
            else
            {
                if (fromState == RevealControllerState.ShowsLeftViewControllerInPresentationMode)
                {
                    if (toState == RevealControllerState.ShowsLeftViewController || toState == RevealControllerState.ShowsFrontViewController)
                    {
                        keyPositions = KeyPositionsToState(toState, false);
                    }
                    else
                    {
                        keyPositions = KeyPositionsToState(toState, true);
                    }
                }
                else if (fromState == RevealControllerState.ShowsLeftViewController)
                {
                    if (toState == RevealControllerState.ShowsLeftViewControllerInPresentationMode || toState == RevealControllerState.ShowsFrontViewController)
                    {
                        keyPositions = KeyPositionsToState(toState, false);
                    }
                    else
                    {
                        keyPositions = KeyPositionsToState(toState, true);
                    }
                }
                else if (fromState == RevealControllerState.ShowsFrontViewController)
                {
                    keyPositions = KeyPositionsToState(toState, false);
                }
                else if (fromState == RevealControllerState.ShowsRightViewController)
                {
                    if (toState == RevealControllerState.ShowsFrontViewController || toState == RevealControllerState.ShowsRightViewControllerInPresentationMode)
                    {
                        keyPositions = KeyPositionsToState(toState, false);
                    }
                    else
                    {
                        keyPositions = KeyPositionsToState(toState, true);
                    }
                }
                else if (fromState == RevealControllerState.ShowsRightViewControllerInPresentationMode)
                {
                    if (toState == RevealControllerState.ShowsRightViewController || toState == RevealControllerState.ShowsFrontViewController)
                    {
                        keyPositions = KeyPositionsToState(toState, false);
                    }
                    else
                    {
                        keyPositions = KeyPositionsToState(toState, true);
                    }
                }
            }

            return keyPositions.ToArray();
        }

        public PointF CenterPointForState(RevealControllerState state)
        {
            PointF center = new PointF(FrontView.Layer.Position.X, FrontView.Layer.Position.Y);

            switch (state)
            {
                case RevealControllerState.ShowsFrontViewController:
                    center.X = this.View.Bounds.GetMidX();
                    break;
                case RevealControllerState.ShowsLeftViewController:
                    center.X = this.View.Bounds.GetMidX() + LeftViewMinWidth;
                    break;
                case RevealControllerState.ShowsRightViewController:
                    center.X = this.View.Bounds.GetMidX() - RightViewMinWidth;
                    break;
                case RevealControllerState.ShowsLeftViewControllerInPresentationMode:
                    center.X = this.View.Bounds.GetMidX() + LeftViewMaxWidth;
                    break;
                case RevealControllerState.ShowsRightViewControllerInPresentationMode:
                    center.X = this.View.Bounds.GetMidX() - RightViewMaxWidth;
                    break;
            }

            return center;
        }

        public void AddViewController(UIViewController childController, UIView container)
        {
            if (childController != null && !this.ChildViewControllers.Contains(childController))
            {
                this.AddChildViewController(childController);
                childController.View.Frame = container.Bounds;
                childController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth |
                                                        UIViewAutoresizing.FlexibleHeight;
                childController.SetRevealController(this);
                container.AddSubview(childController.View);
                this.DidMoveToParentViewController(this);
            }
        }

        public void RemoveViewController(UIViewController childController)
        {
            if (childController != null && this.ChildViewControllers.Contains(childController))
            {
                childController.WillMoveToParentViewController(null);
                childController.View.RemoveFromSuperview();
                childController.RemoveFromParentViewController();
                childController.SetRevealController(null);
            }
        }
    }

    public class RevealControllerView : UIView
    {
        private const int SHADOW_TRANSITION_ANIMATION_IDENTIFIER = 1;

        private string _kShadowTransitionAnimationKey = "shadowTransitionAnimation";

        public RevealControllerView(RectangleF frame)
            : base(frame)
        {

        }

        public bool Shadow
        {
            set
            {
                if (value)
                {
                    this.Layer.MasksToBounds = false;
                    this.Layer.ShadowColor = UIColor.Black.CGColor;
                    this.Layer.ShadowOffset = new SizeF(0.0f, 0.0f);
                    this.Layer.ShadowOpacity = 0.5f;
                    this.Layer.ShadowRadius = 2.5f;
                    this.Layer.ShadowPath = UIBezierPath.FromRect(this.Bounds).CGPath;
                }
                else
                {
                    this.Layer.MasksToBounds = true;
                    this.Layer.ShadowColor = null;
                    this.Layer.ShadowOffset = SizeF.Empty;
                    this.Layer.ShadowOpacity = 0.0f;
                    this.Layer.ShadowRadius = 0.0f;
                    this.Layer.ShadowPath = null;
                }
            }
        }

        private UIViewController _viewController;

        public UIViewController ViewController
        {
            get { return _viewController; }
            set
            {
                if (value != null)
                {
                    _viewController = value;
                    _viewController.View.Frame = this.Bounds;
                    _viewController.View.AutoresizingMask = this.AutoresizingMask;
                }
            }
        }

        public void UpdateShadowWithAnimationDuration(TimeSpan timeSpan)
        {
            UIBezierPath existingShadowPath = UIBezierPath.FromPath(this.Layer.ShadowPath);
            this.Layer.ShadowPath = UIBezierPath.FromRect(this.Bounds).CGPath;

            if (existingShadowPath != null)
            {
                CABasicAnimation transition = new RevealAnimation().Animation("shadowPath");
                transition.From = NSObject.FromObject(existingShadowPath.CGPath);
                transition.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseInEaseOut);
                transition.Duration = timeSpan.TotalSeconds;
                transition.RemovedOnCompletion = false;
                transition.Delegate = new CAAnimationDelegate(this.Handle);
                transition.SetPkIdentidier(SHADOW_TRANSITION_ANIMATION_IDENTIFIER);

                this.Layer.AddAnimation(transition, _kShadowTransitionAnimationKey);
            }
        }

        public void SetUserInteractionForContainedViewEnabled(bool userInteractionEnabled)
        {
            this._viewController.View.UserInteractionEnabled = userInteractionEnabled;
        }

        public void AnimationDidStop(CAAnimation animation, bool flag)
        {
            if (flag && animation.ValueForKey((NSString)_kShadowTransitionAnimationKey) == NSObject.FromObject(SHADOW_TRANSITION_ANIMATION_IDENTIFIER))
            {
                base.SetNeedsLayout();
            }
        }
    }

    public class RevealAnimation : CABasicAnimation
    {
        public int Identifier { get; set; }
        public string Key { get { return string.Format("{0}lu{1}ld", this.GetHashCode(), this.Identifier); } }

        private CALayer _layer;
        private bool _animating;
        private Action _startHandler;
        private Action<bool> _completionHandler;


        public CABasicAnimation Animation(string path)
        {
            CABasicAnimation animation = CABasicAnimation.FromKeyPath(path);
            animation.Delegate = new CAAnimationDelegate(Handle);
            animation.AnimationStarted += new EventHandler((sender, args) => AnimationDidStart((CABasicAnimation)sender));
            animation.AnimationStopped += new EventHandler<CAAnimationStateEventArgs>((sender, args) => AnimationDidStop((CAAnimation)sender, args.Finished));
            return animation;
        }

        private string _path;

        public override string KeyPath
        {
            get
            {
                return this._path;
            }
            set
            {
                this._path = value;
            }
        }

        public void StartAnimationOnLayer(CALayer layer)
        {
            if (this._animating)
            {
                _layer = layer;
                _layer.AddAnimation(this, Key);
            }
        }

        public void StopAnimation()
        {
            _layer.RemoveAnimation(Key);
        }

        public void SetLayer(CALayer layer)
        {
            if (this._animating)
            {
                //PKLog(@"ERROR: Cannot mutate animation properties while animation is in progress.");
            }
            else
            {
                if (!_layer.Equals(layer))
                {
                    _layer = layer;
                }
            }
        }



        public void AnimationDidStart(CABasicAnimation animation)
        {
            this._animating = true;
            if (_startHandler != null)
                UIApplication.SharedApplication.BeginInvokeOnMainThread(() => _startHandler());
        }

        public void AnimationDidStop(CAAnimation animation, bool animationFinished)
        {
            this._animating = false;
            if (_completionHandler != null)
                UIApplication.SharedApplication.BeginInvokeOnMainThread(() => _completionHandler(animationFinished));
        }
    }

    public class RevealLayerAnimator : NSObject
    {
        public CALayer Layer { get; private set; }
        public IDictionary<string, RevealSequentialAnimation> Animations { get; private set; }

        public RevealLayerAnimator(CALayer layer)
        {
            Layer = layer;
            Animations = new Dictionary<string, RevealSequentialAnimation>();
        }

        public void AddAnimation(RevealSequentialAnimation animation, string forKey)
        {
            if (Animations.ContainsKey(forKey))
                Animations.Remove(forKey);
            Animations.Add(forKey, animation);
        }

        public void StartAnimationForKey(string key)
        {
            RevealSequentialAnimation animation = Animations[key];
            animation.StartAnimationOnLayer(this.Layer);
        }

        public void StopAnimationForKey(string key)
        {
            RevealSequentialAnimation animation;
            Animations.TryGetValue(key, out animation);
            if (animation != null)
                animation.StopAnimation();
        }

        public void stopAndRemoveAllAnimations()
        {
            foreach (string key in Animations.Keys)
            {
                StopAnimationForKey(key);
            }

            Animations.Clear();
        }
    }

    public class RevealSequentialAnimation : NSObject
    {
        public SequentialAnimationProgressHandler ProgressHandler { get; set; }
        public AnimationCompletionHandler CompletionHandler { get; set; }

        public IList<CABasicAnimation> Animations { get; private set; }
        public CALayer Layer { get; private set; }

        private string Key { get { return string.Format("{0}lu", this.GetHashCode()); } }

        private bool _isAnimating;

        public RevealSequentialAnimation(NSString keyPath, PointF[] values, TimeSpan duration)
            : this(keyPath, values, duration, null, null)
        {
            actions = new Dictionary<string, Action<CALayer, NSObject>>();
            actions.Add("position", (layer, o) =>
            {
                float x = ((PointF)o.ToObject(typeof(PointF))).X;
                float y = ((PointF)o.ToObject(typeof(PointF))).Y;
                layer.Position = new PointF(x, y);
            });
        }

        public RevealSequentialAnimation(NSString keyPath, PointF[] values, TimeSpan duration, SequentialAnimationProgressHandler progress, AnimationCompletionHandler completion)
        {
            Animations = AnimationsForKeyPath(keyPath, values, duration);
            CompletionHandler = completion;
            ProgressHandler = progress;
        }

        private IList<CABasicAnimation> AnimationsForKeyPath(NSString keyPath, PointF[] values, TimeSpan duration)
        {
            IList<CABasicAnimation> animations = new List<CABasicAnimation>(values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                CABasicAnimation animation = new RevealAnimation().Animation(keyPath);
                animation.From = this.Layer != null ? this.Layer.PresentationLayer.ValueForKeyPath(keyPath) : null;
                animation.To = NSObject.FromObject(values[i]);
                animation.Duration = duration.TotalSeconds / values.Length;
                animation.TimingFunction = this.TimingFunctionForAnimationAtIndex(i, values.Length);
                animation.SetPkIdentidier(i);
                animation.AnimationStarted += new EventHandler((sender, args) => AnimationDidStart(animation));
                animation.AnimationStopped += new EventHandler<CAAnimationStateEventArgs>((sender, args) => AnimationDidStop(animation, args.Finished));
                animations.Add(animation);
            }

            return animations.ToList();
        }

        private string KeyForAnimationAtIndex(int index)
        {
            return string.Format("{0}@{1}@", this.Key, Animations[index].GetHashCode());
        }

        public void StartAnimationOnLayer(CALayer layer)
        {
            if (!this._isAnimating && Animations.Count > 0)
            {
                this.Layer = layer;
                CABasicAnimation firstAnimation = Animations[0];
                firstAnimation.From = this.Layer.ValueForKeyPath((NSString)firstAnimation.KeyPath);
                //firstAnimation.To = NSObject.FromObject(new PointF(100, 0));
                //var t = NSValue.FromObject(firstAnimation.To);

                SetKey(firstAnimation.KeyPath, firstAnimation.To);
                this.Layer.AddAnimation(firstAnimation, this.KeyForAnimationAtIndex(0));
            }
        }

        private IDictionary<string, Action<CALayer, NSObject>> actions;

        public void SetKey(string key, NSObject obj)
        {
            actions[key](this.Layer, obj);
        }

        public void StopAnimation()
        {
            CALayer presentationLayer = this.Layer.PresentationLayer;

            foreach (var animationKey in this.Layer.AnimationKeys.EmptyIfNull())
            {
                CAAnimation animation = this.Layer.AnimationForKey(animationKey);

                if (animation is RevealAnimation && animationKey.StartsWith(this.Key))
                {
                    animation = this.Layer.AnimationForKey(animationKey);
                    this.Layer.SetValueForKeyPath(presentationLayer.ValueForKeyPath((NSString)((RevealAnimation)animation).KeyPath), (NSString)((RevealAnimation)animation).KeyPath);
                    this.Layer.RemoveAnimation(animationKey);
                }
            }
        }

        public CAMediaTimingFunction TimingFunctionForAnimationAtIndex(int index, int total)
        {
            CAMediaTimingFunction function = null;

            if (total == 1)
            {
                function = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseInEaseOut);
            }
            else if (total == 2)
            {
                if (index == 0)
                {
                    function = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseIn);
                }
                else
                {
                    function = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
                }
            }
            else
            {
                if (index == 0)
                {
                    function = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseIn);
                }
                else if ((index + 1) == total)
                {
                    function = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
                }
                else
                {
                    function = CAMediaTimingFunction.FromName(CAMediaTimingFunction.Linear);
                }
            }

            return function;
        }

        public void AnimationDidStart(CAAnimation anim)
        {
            this._isAnimating = true;

            CABasicAnimation animation = this.Animations[anim.GetPkIdentidier()];

            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                if (this.ProgressHandler != null)
                    this.ProgressHandler((NSValue)animation.From, ((NSValue)animation.To), (uint)animation.GetPkIdentidier());
            });
        }

        public void AnimationDidStop(CAAnimation anim, bool finished)
        {
            int currentIndex = anim.GetPkIdentidier();
            uint lastAnimationIndex = (uint)this.Animations.Count() - 1;

            if (finished && currentIndex < lastAnimationIndex)
            {
                int nextAnimationIndex = currentIndex + 1;

                NSString nextAnimationIndexString = (NSString)string.Format("{0}lu", nextAnimationIndex);
                CABasicAnimation nextAnimation = this.Animations[nextAnimationIndex];
                nextAnimation.From = this.Layer.PresentationLayer.ValueForKeyPath((NSString)nextAnimation.KeyPath);

                UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
                {
                    if (this.ProgressHandler != null)
                        this.ProgressHandler((NSValue)nextAnimation.From, ((NSValue)nextAnimation.To), (uint)nextAnimationIndex);
                });

                this.Layer.SetValueForKeyPath(nextAnimation.To, (NSString)nextAnimation.KeyPath);
                this.Layer.AddAnimation(nextAnimation, nextAnimationIndexString);
            }
            else
            {
                this._isAnimating = false;
                this.StopAnimation();

                UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
                {
                    if (this.CompletionHandler != null)
                        this.CompletionHandler(finished);
                });
            }
        }
    }

    public static class Ext
    {
        public static T[] EmptyIfNull<T>(this T[] array)
        {
            return array ?? new T[0];
        }

        private static readonly ConcurrentDictionary<string, int> Associations = new ConcurrentDictionary<string, int>();

        public static void SetPkIdentidier(this CAAnimation animation, int identifier)
        {
            Associations.TryAdd("key_" + animation.GetHashCode(), identifier);
        }

        public static int GetPkIdentidier(this CAAnimation animation)
        {
            int result;
            Associations.TryGetValue("key_" + animation.GetHashCode(), out result);
            return result;
        }

        private static readonly ConcurrentDictionary<string, RevealController> RevealControllers = new ConcurrentDictionary<string, RevealController>();

        public static void SetRevealController(this UIViewController controller, RevealController revealController)
        {
            RevealControllers.TryAdd("key_" + controller.GetHashCode(), revealController);
        }

        public static RevealController GetRevealController(this UIViewController controller)
        {
            RevealController revealController;
            RevealControllers.TryGetValue("key_" + controller.GetHashCode(), out revealController);
            return revealController;
        }

        public static Object ToObject(this NSObject nsO)
        {
            return nsO.ToObject(null);
        }

        //      public enum TypeCode
        //      {
        //          Empty,
        //          Object,
        //          DBNull,
        //          Boolean,
        //          Char,
        //          SByte,
        //          Byte,
        //          Int16,
        //          UInt16,
        //          Int32,
        //          UInt32,
        //          Int64,
        //          UInt64,
        //          Single,
        //          Double,
        //          Decimal,
        //          DateTime,
        //          String = 18
        //      }

        public static Object ToObject(this NSObject nsO, Type targetType)
        {
            if (nsO is NSString)
            {
                return nsO.ToString();
            }

            if (nsO is NSDate)
            {
                var nsDate = (NSDate)nsO;
                return DateTime.SpecifyKind(nsDate, DateTimeKind.Unspecified);
            }

            if (nsO is NSDecimalNumber)
            {
                return decimal.Parse(nsO.ToString(), CultureInfo.InvariantCulture);
            }

            if (nsO is NSNumber)
            {
                var x = (NSNumber)nsO;

                switch (Type.GetTypeCode(targetType))
                {
                    case TypeCode.Boolean:
                        return x.BoolValue;
                    case TypeCode.Char:
                        return Convert.ToChar(x.ByteValue);
                    case TypeCode.SByte:
                        return x.SByteValue;
                    case TypeCode.Byte:
                        return x.ByteValue;
                    case TypeCode.Int16:
                        return x.Int16Value;
                    case TypeCode.UInt16:
                        return x.UInt16Value;
                    case TypeCode.Int32:
                        return x.Int32Value;
                    case TypeCode.UInt32:
                        return x.UInt32Value;
                    case TypeCode.Int64:
                        return x.Int64Value;
                    case TypeCode.UInt64:
                        return x.UInt64Value;
                    case TypeCode.Single:
                        return x.FloatValue;
                    case TypeCode.Double:
                        return x.DoubleValue;
                }
            }

            if (nsO is NSValue)
            {
                var v = (NSValue)nsO;

                if (targetType == typeof(IntPtr))
                {
                    return v.PointerValue;
                }

                if (targetType == typeof(SizeF))
                {
                    return v.SizeFValue;
                }

                if (targetType == typeof(RectangleF))
                {
                    return v.RectangleFValue;
                }

                if (targetType == typeof(PointF))
                {
                    return v.PointFValue;
                }
            }

            return nsO;
        }
    }
}