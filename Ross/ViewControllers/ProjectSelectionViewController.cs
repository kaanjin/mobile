﻿using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class ProjectSelectionViewController : UITableViewController
    {
        private const float CellSpacing = 4f;
        private readonly TimeEntryModel model;

        public ProjectSelectionViewController (TimeEntryModel model) : base (UITableViewStyle.Plain)
        {
            this.model = model;

            Title = "ProjectTitle".Tr ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;
            new Source (this).Attach ();
        }

        public Action ProjectSelected { get; set; }

        private void Finish (TaskModel task = null, ProjectModel project = null, WorkspaceModel workspace = null)
        {
            project = task != null ? task.Project : project;
            workspace = project != null ? project.Workspace : workspace;

            if (project != null || task != null || workspace != null) {
                model.Workspace = workspace;
                model.Project = project;
                model.Task = task;
            }

            var cb = ProjectSelected;
            if (cb != null) {
                cb ();
            } else {
                // Pop to previous view controller
                var vc = this.GetStackController ().ViewControllers;
                var i = Array.IndexOf (vc, this) - 1;
                if (i >= 0) {
                    this.PopToViewController (vc [i], true);
                }
            }
        }

        class Source : PlainDataViewSource<object>
        {
            private readonly static NSString WorkspaceHeaderId = new NSString ("SectionHeaderId");
            private readonly static NSString ProjectCellId = new NSString ("ProjectCellId");
            private readonly static NSString TaskCellId = new NSString ("TaskCellId");
            private readonly ProjectSelectionViewController controller;
            private readonly HashSet<Guid> expandedProjects = new HashSet<Guid> ();

            public Source (ProjectSelectionViewController controller)
                : base (controller.TableView, new ProjectAndTaskView ())
            {
                this.controller = controller;
            }

            public override void Attach ()
            {
                base.Attach ();

                controller.TableView.RegisterClassForCellReuse (typeof(WorkspaceHeaderCell), WorkspaceHeaderId);
                controller.TableView.RegisterClassForCellReuse (typeof(ProjectCell), ProjectCellId);
                controller.TableView.RegisterClassForCellReuse (typeof(TaskCell), TaskCellId);
                controller.TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            }

            private void ToggleTasksExpanded (Guid projectId)
            {
                SetTasksExpanded (projectId, !expandedProjects.Contains (projectId));
            }

            private void SetTasksExpanded (Guid projectId, bool expand)
            {
                if (expand && expandedProjects.Add (projectId)) {
                    Update ();
                } else if (!expand && expandedProjects.Remove (projectId)) {
                    Update ();
                }
            }

            public override float EstimatedHeight (UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
            {
                var row = GetRow (indexPath);
                if (row is ProjectAndTaskView.Workspace)
                    return 42f;
                if (row is TaskModel)
                    return 49f;
                return EstimatedHeight (tableView, indexPath);
            }

            public override float EstimatedHeightForHeader (UITableView tableView, int section)
            {
                return -1f;
            }

            public override float GetHeightForHeader (UITableView tableView, int section)
            {
                return EstimatedHeightForHeader (tableView, section);
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var row = GetRow (indexPath);

                var project = row as ProjectAndTaskView.Project;
                if (project != null) {
                    var cell = (ProjectCell)tableView.DequeueReusableCell (ProjectCellId, indexPath);
                    cell.Bind (project);
                    if (project.Model != null && project.Model.Id != null) {
                        var projectId = project.Model.Id.Value;
                        cell.ToggleTasks = () => ToggleTasksExpanded (projectId);
                    } else {
                        cell.ToggleTasks = null;
                    }
                    return cell;
                }

                var task = row as TaskModel;
                if (task != null) {
                    var cell = (TaskCell)tableView.DequeueReusableCell (TaskCellId, indexPath);
                    cell.Bind (task);

                    var rows = GetCachedRows (GetSection (indexPath.Section));
                    cell.IsFirst = indexPath.Row < 1 || !(rows [indexPath.Row - 1] is TaskModel);
                    cell.IsLast = indexPath.Row >= rows.Count || !(rows [indexPath.Row + 1] is TaskModel);
                    return cell;
                }

                var workspace = row as ProjectAndTaskView.Workspace;
                if (workspace != null) {
                    var cell = (WorkspaceHeaderCell)tableView.DequeueReusableCell (WorkspaceHeaderId, indexPath);
                    cell.Bind (workspace);
                    return cell;
                }

                throw new InvalidOperationException (String.Format ("Unknown row type {0}", row.GetType ()));
            }

            public override UIView GetViewForHeader (UITableView tableView, int section)
            {
                return new UIView ().Apply (Style.ProjectList.HeaderBackgroundView);
            }

            public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var m = GetRow (indexPath);

                if (m is TaskModel) {
                    controller.Finish ((TaskModel)m);
                } else if (m is ProjectAndTaskView.Project) {
                    var wrap = (ProjectAndTaskView.Project)m;
                    if (wrap.IsNoProject) {
                        controller.Finish (workspace: wrap.WorkspaceModel);
                    } else if (wrap.IsNewProject) {
                        var proj = wrap.Model;
                        // Show create project dialog instead
                        var next = new NewProjectViewController (proj.Workspace, proj.Color) {
                            ProjectCreated = (p) => controller.Finish (project: p),
                        };
                        controller.PushViewController (next, true);
                    } else {
                        controller.Finish (project: wrap.Model);
                    }
                } else if (m is ProjectAndTaskView.Workspace) {
                    var wrap = (ProjectAndTaskView.Workspace)m;
                    controller.Finish (workspace: wrap.Model);
                }

                tableView.DeselectRow (indexPath, true);
            }

            protected override IEnumerable<object> GetRows (string section)
            {
                foreach (var row in DataView.Data) {
                    var task = row as TaskModel;
                    if (task != null && (task.ProjectId == null || !expandedProjects.Contains (task.ProjectId.Value)))
                        continue;

                    yield return row;
                }
            }
        }

        private class ProjectCell : BindableTableViewCell<ProjectAndTaskView.Project>
        {
            private Subscription<ModelChangedMessage> subscriptionModelChanged;
            private UIView textContentView;
            private UILabel projectLabel;
            private UILabel clientLabel;
            private UIButton tasksButton;

            public ProjectCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                BackgroundView = new UIView ();

                ContentView.Add (textContentView = new UIView ());
                ContentView.Add (tasksButton = new UIButton ().Apply (Style.ProjectList.TasksButtons));
                textContentView.Add (projectLabel = new UILabel ().Apply (Style.ProjectList.ProjectLabel));
                textContentView.Add (clientLabel = new UILabel ().Apply (Style.ProjectList.ClientLabel));

                var maskLayer = new CAGradientLayer () {
                    AnchorPoint = PointF.Empty,
                    StartPoint = new PointF (0.0f, 0.0f),
                    EndPoint = new PointF (1.0f, 0.0f),
                    Colors = new [] {
                        UIColor.FromWhiteAlpha (1, 1).CGColor,
                        UIColor.FromWhiteAlpha (1, 1).CGColor,
                        UIColor.FromWhiteAlpha (1, 0).CGColor,
                    },
                    Locations = new [] {
                        NSNumber.FromFloat (0f),
                        NSNumber.FromFloat (0.9f),
                        NSNumber.FromFloat (1f),
                    },
                };
                textContentView.Layer.Mask = maskLayer;

                tasksButton.TouchUpInside += OnTasksButtonTouchUpInside;
            }

            private void OnTasksButtonTouchUpInside (object sender, EventArgs e)
            {
                var cb = ToggleTasks;
                if (cb != null) {
                    cb ();
                }
            }

            public Action ToggleTasks { get; set; }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new RectangleF (0, CellSpacing / 2, Frame.Width, Frame.Height - CellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                if (!tasksButton.Hidden) {
                    var virtualWidth = contentFrame.Height;
                    var buttonWidth = tasksButton.CurrentBackgroundImage.Size.Width;
                    var extraPadding = (virtualWidth - buttonWidth) / 2f;
                    tasksButton.Frame = new RectangleF (
                        contentFrame.Width - virtualWidth + extraPadding, extraPadding,
                        buttonWidth, buttonWidth);
                    contentFrame.Width -= virtualWidth;
                }

                contentFrame.X += 13f;
                contentFrame.Width -= 13f;
                textContentView.Frame = contentFrame;
                textContentView.Layer.Mask.Bounds = contentFrame;

                contentFrame = new RectangleF (PointF.Empty, contentFrame.Size);

                if (clientLabel.Hidden) {
                    // Only display single item, so make it fill the whole text frame
                    var bounds = GetBoundingRect (projectLabel);
                    projectLabel.Frame = new RectangleF (
                        x: 0,
                        y: (contentFrame.Height - bounds.Height + projectLabel.Font.Descender) / 2f,
                        width: contentFrame.Width,
                        height: bounds.Height
                    );
                } else {
                    // Carefully craft the layout
                    var bounds = GetBoundingRect (projectLabel);
                    projectLabel.Frame = new RectangleF (
                        x: 0,
                        y: (contentFrame.Height - bounds.Height + projectLabel.Font.Descender) / 2f,
                        width: bounds.Width,
                        height: bounds.Height
                    );

                    const float clientLeftMargin = 7.5f;
                    bounds = GetBoundingRect (clientLabel);
                    clientLabel.Frame = new RectangleF (
                        x: projectLabel.Frame.X + projectLabel.Frame.Width + clientLeftMargin,
                        y: (float)Math.Floor (projectLabel.Frame.Y + projectLabel.Font.Ascender - clientLabel.Font.Ascender),
                        width: bounds.Width,
                        height: bounds.Height
                    );
                }
            }

            private static RectangleF GetBoundingRect (UILabel view)
            {
                var attrs = new UIStringAttributes () {
                    Font = view.Font,
                };
                var rect = ((NSString)(view.Text ?? String.Empty)).GetBoundingRect (
                               new SizeF (Single.MaxValue, Single.MaxValue),
                               NSStringDrawingOptions.UsesLineFragmentOrigin,
                               attrs, null);
                rect.Height = (float)Math.Ceiling (rect.Height);
                return rect;
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    if (subscriptionModelChanged != null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        bus.Unsubscribe (subscriptionModelChanged);
                        subscriptionModelChanged = null;
                    }
                }

                base.Dispose (disposing);
            }

            public override void WillMoveToSuperview (UIView newsuper)
            {
                base.WillMoveToSuperview (newsuper);

                if (newsuper != null) {
                    if (subscriptionModelChanged == null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> ((msg) => {
                            if (Handle == IntPtr.Zero)
                                return;

                            OnModelChanged (msg);
                        });
                    }
                } else {
                    if (subscriptionModelChanged != null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        bus.Unsubscribe (subscriptionModelChanged);
                        subscriptionModelChanged = null;
                    }
                }
            }

            protected void OnModelChanged (ModelChangedMessage msg)
            {
                if (msg.Model != DataSource.Model)
                    return;

                if (msg.PropertyName == ProjectModel.PropertyName
                    || msg.PropertyName == ProjectModel.PropertyColor
                    || msg.PropertyName == ProjectModel.PropertyClientId)
                    Rebind ();
            }

            protected override void Rebind ()
            {
                UIColor projectColor;
                string projectName;
                string clientName = String.Empty;
                int taskCount = 0;

                if (DataSource.IsNoProject) {
                    projectColor = Color.Gray;
                    projectName = "ProjectNoProject".Tr ();
                    projectLabel.Apply (Style.ProjectList.NoProjectLabel);
                } else if (DataSource.IsNewProject) {
                    projectColor = Color.LightGray;
                    projectName = "ProjectNewProject".Tr ();
                    projectLabel.Apply (Style.ProjectList.NewProjectLabel);
                } else {
                    var project = DataSource.Model;
                    projectColor = UIColor.Clear.FromHex (project.GetHexColor ());

                    projectName = project.Name;
                    clientName = project.Client != null ? project.Client.Name : String.Empty;
                    taskCount = DataSource.Tasks.Count;
                    projectLabel.Apply (Style.ProjectList.ProjectLabel);
                }

                if (String.IsNullOrWhiteSpace (projectName)) {
                    projectName = "ProjectNoNameProject".Tr ();
                    clientName = String.Empty;
                }

                if (!String.IsNullOrWhiteSpace (projectName)) {
                    projectLabel.Text = projectName;
                    projectLabel.Hidden = false;

                    if (!String.IsNullOrEmpty (clientName)) {
                        clientLabel.Text = clientName;
                        clientLabel.Hidden = false;
                    } else {
                        clientLabel.Hidden = true;
                    }
                } else {
                    projectLabel.Hidden = true;
                    clientLabel.Hidden = true;
                }

                tasksButton.Hidden = taskCount < 1;
                if (!tasksButton.Hidden) {
                    tasksButton.SetTitle (taskCount.ToString (), UIControlState.Normal);
                    tasksButton.SetTitleColor (projectColor, UIControlState.Normal);
                }

                BackgroundView.BackgroundColor = projectColor;
            }
        }

        private class TaskCell : ModelTableViewCell<TaskModel>
        {
            private readonly UILabel nameLabel;
            private readonly UIView separatorView;
            private bool isFirst;
            private bool isLast;

            public TaskCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                ContentView.Add (nameLabel = new UILabel ().Apply (Style.ProjectList.TaskLabel));
                ContentView.Add (separatorView = new UIView ().Apply (Style.ProjectList.TaskSeparator));
                BackgroundView = new UIView ().Apply (Style.ProjectList.TaskBackground);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new RectangleF (0, 0, Frame.Width, Frame.Height);

                if (isFirst) {
                    contentFrame.Y += CellSpacing / 2;
                    contentFrame.Height -= CellSpacing / 2;
                }

                if (isLast) {
                    contentFrame.Height -= CellSpacing / 2;
                }

                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                // Add padding
                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                nameLabel.Frame = contentFrame;
                separatorView.Frame = new RectangleF (
                    contentFrame.X, contentFrame.Y + contentFrame.Height - 1f,
                    contentFrame.Width, 1f);
            }

            protected override void Rebind ()
            {
                var taskName = DataSource.Name;
                if (String.IsNullOrWhiteSpace (taskName))
                    taskName = "ProjectNoNameTask".Tr ();
                nameLabel.Text = taskName;
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (msg.Model != DataSource)
                    return;

                if (msg.PropertyName == TaskModel.PropertyName)
                    Rebind ();
            }

            public bool IsFirst {
                get { return isFirst; }
                set {
                    if (isFirst == value)
                        return;
                    isFirst = value;
                    SetNeedsLayout ();
                }
            }

            public bool IsLast {
                get { return isLast; }
                set {
                    if (isLast == value)
                        return;
                    isLast = value;
                    SetNeedsLayout ();

                    separatorView.Hidden = isLast;
                }
            }
        }

        private class WorkspaceHeaderCell : BindableTableViewCell<ProjectAndTaskView.Workspace>
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel nameLabel;

            public WorkspaceHeaderCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                nameLabel = new UILabel ().Apply (Style.ProjectList.HeaderLabel);
                ContentView.AddSubview (nameLabel);

                BackgroundView = new UIView ().Apply (Style.ProjectList.HeaderBackgroundView);
                UserInteractionEnabled = false;
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();
                var contentFrame = ContentView.Frame;

                nameLabel.Frame = new RectangleF (
                    x: HorizSpacing,
                    y: 0,
                    width: contentFrame.Width - 2 * HorizSpacing,
                    height: contentFrame.Height
                );
            }

            protected override void Rebind ()
            {
                nameLabel.Text = DataSource.Model.Name;
            }
        }
    }
}
