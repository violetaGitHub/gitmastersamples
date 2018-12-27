using System;
using System.Collections;
using System.Collections.Generic;

using Codice.Client.Common.Servers;
using Codice.CM.Common;
using Codice.I3;
using Codice.I3.Tree;
using PlasticGui.Help;

namespace Codice.CM.Client.Gui
{
    public class RepositoryViewControl : TypeBasedViewControl
    {
        private I3PushButton ListViewButton;
        private I3PushButton TreeViewButton;
        private I3Button CreateRepositoryButton;
        private Codice.I3.I3ComboBox ServerComboBox;
        private Codice.I3.I3Label ServerLabel;
        private System.ComponentModel.IContainer components = null;
        private bool mSaveLastUsedServer;

        private CreateRepositoryCommand mCreateRepositoryCommand;

        public RepositoryViewControl(ViewsGroup group, ViewInfo info)
            : base(group, info)
        {
            InitializeComponent();

            InitializeTheme();
//different change in main
            InitializeNodeBuilder();

            mCreateRepositoryCommand = new CreateRepositoryCommand();

            InitializeTargetServer(info as RegisteredRepositoriesViewInfo);

            ServerComboBox.KeyDown += ServerComboBox_KeyDown;
            ServerComboBox.SelectedIndexChanged += ServerComboBox_SelectedIndexChanged;

            CommandManager.AddCommand(CreateRepositoryButton, mCreateRepositoryCommand);
        }

        private RepositoryTreeListNodeBuilder mNodeBuilder;
        private ExpandedGUIServerObjectNodes mExpandedNodes = new ExpandedGUIServerObjectNodes();
        private GuiRepository mSelectedRepositoryToFocus;

        public override CaptionInfo GetViewCaption()
        {
            return new CaptionInfo(
               mViewInfo.GetCaption(), mViewInfo.GetViewImage());
        }

        public override void RefreshView()
        {
            SaveLastUsedServer(ServerComboBox.Text);

            ((RegisteredRepositoriesViewInfo)mViewInfo).SetTargetServer(
                ServerComboBox.Text);

            mCreateRepositoryCommand.SetServer(ServerComboBox.Text);

            base.RefreshView();
        }

        protected override void InitializeTheme()
        {
            base.InitializeTheme();

            ListViewButton.PushedBackColor =
                ThemeManager.Get().GetThemeColor(ButtonColor.PushedBackground);

            TreeViewButton.PushedBackColor =
                ThemeManager.Get().GetThemeColor(ButtonColor.PushedBackground);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                ListViewButton.Click -= new EventHandler(ListViewButton_Click);
                TreeViewButton.Click -= new EventHandler(TreeViewButton_Click);

                ServerComboBox.KeyDown -= this.ServerComboBox_KeyDown;
                ServerComboBox.SelectedIndexChanged -= this.ServerComboBox_SelectedIndexChanged;

                if (components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void BeforeItemFill(IList guiContents)
        {
            base.BeforeItemFill(guiContents);

            SetupNameDataProperty(mNodeBuilder.TreeMode);
        }

        protected override void AfterItemFill(IList guiContents)
        {
            mExpandedNodes.RestoreExpandedNodes(MasterTreeListView);
            KnownServers.AddRecent(ServerComboBox.Text);

            base.AfterItemFill(guiContents);

            ShowHelp(guiContents.Count);
        }

        protected virtual void ShowHelp(int itemsCount)
        {
            if (!mViewInfo.ShowHelp)
                return;

            PlasticGui.Help.ShowHelp.ForRepositories(
                mGuiHelp,
                itemsCount,
                ListFilterTextBox.Text,
                mHelpPanel,
                false);
        }

        protected override void ShowHelpFromHelpButton()
        {
            PlasticGui.Help.ShowHelp.ForRepositories(
                mGuiHelp,
                MasterTreeListView.Nodes.TotalCount,
                ListFilterTextBox.Text,
                mHelpPanel,
                true);
        }

        protected override void NotifyPossibleFrustration()
        {
            mGuiHelp.NotifyPossibleFrustration(
                PlasticGui.ViewType.RepositoriesView,
                ConditionKey.Refresh,
                DateTime.UtcNow);
        }

        protected override INodeBuilder GetNodeBuilder()
        {
            return mNodeBuilder;
        }

        private void InitializeNodeBuilder()
        {
            mNodeBuilder = new RepositoryTreeListNodeBuilder();

            TreeListMode mode = GetTreeListModeFromConfig();

            if (mode == TreeListMode.List)
            {
                SetListViewMode();
                return;
            }

            SetTreeViewMode();
        }

        private TreeListMode GetTreeListModeFromConfig()
        {
            int repositoryViewMode = GuiClientConfig.Get().Configuration.RepositoryViewMode;

            if (!Enum.IsDefined(typeof(TreeListMode), repositoryViewMode))
                return TreeListMode.List;

            return (TreeListMode)Enum.ToObject(typeof(TreeListMode), repositoryViewMode);
        }

        private void TreeViewButton_Click(object sender, EventArgs e)
        {
            SetTreeViewMode();
        }

        private void ListViewButton_Click(object sender, EventArgs e)
        {
            SetListViewMode();
        }

        private void SetTreeViewMode()
        {
            SetViewMode(TreeListMode.Tree);
        }

        private void SetListViewMode()
        {
            SetViewMode(TreeListMode.List);
        }

        private void InitializeTargetServer(
            RegisteredRepositoriesViewInfo registeredRepositoriesViewInfo)
        {
            if (registeredRepositoriesViewInfo == null)
                return;

            List<string> servers = KnownServers.Get();
            foreach (string server in servers)
                ServerComboBox.Items.Add(server);

            if (!string.IsNullOrEmpty(registeredRepositoriesViewInfo.CloudServer))
                ServerComboBox.Items.Add(registeredRepositoriesViewInfo.CloudServer);

            mSaveLastUsedServer = string.IsNullOrEmpty(registeredRepositoriesViewInfo.TargetServer);

            string targetServer = string.IsNullOrEmpty(registeredRepositoriesViewInfo.TargetServer) ?
                GuiClientConfig.Get().Configuration.LastUsedRepServer : 
                registeredRepositoriesViewInfo.TargetServer;

            SetDefaultServer(
                targetServer, registeredRepositoriesViewInfo,
                ServerComboBox, mCreateRepositoryCommand);
        }

        void ServerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshView();
        }

        void SetDefaultServer(string server, RegisteredRepositoriesViewInfo viewInfo,
            I3ComboBox serverComboBox, CreateRepositoryCommand cmd)
        {
            serverComboBox.Text = server;
            viewInfo.SetTargetServer(server);
            cmd.SetServer(server);

            if (string.IsNullOrEmpty(server) || serverComboBox.Items.Contains(server))
                return;

            serverComboBox.Items.Add(server);
        }

        void ServerComboBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyData != System.Windows.Forms.Keys.Return)
                return;

            RefreshView();
        }

        void SaveLastUsedServer(string server)
        {
            if (!mSaveLastUsedServer)
                return;

            GuiClientConfig.Get().Configuration.LastUsedRepServer = server;
            GuiClientConfig.Get().Save();
        }

        private void SetViewMode(TreeListMode mode)
        {
            mNodeBuilder.TreeMode = mode;

            TreeViewButton.Pushed = mode == TreeListMode.Tree;
            ListViewButton.Pushed = mode == TreeListMode.List;

            mSelectedRepositoryToFocus = GetSelectedRepository();

            FillViewNodes(null, mNodeBuilder.GenerateNodes(MasterTreeListView.Nodes));

            SaveTreeListViewMode(mode);

            SetupNameDataProperty(mode);

            FocusSelectedRepository(mSelectedRepositoryToFocus);

            Filter(ListFilterTextBox.Text);
        }

        private void FocusSelectedRepository(GuiRepository selectedRepository)
        {
            if (selectedRepository == null)
                return;

            TreeListNode node = GetNodeBySpec(
                MasterTreeListView.Nodes, selectedRepository.FullObjectSpec.ToLower());

            if (node == null)
                return;

            mExpandedNodes.ExpandNodes(node.Parent);

            MasterTreeListView.FocusNode(node);
        }

        private GuiRepository GetSelectedRepository()
        {
            if (MasterTreeListView.SelectedNodes == null ||
                MasterTreeListView.SelectedNodes.Count == 0)
            {
                return null;
            }

            return (GuiRepository)((TreeListNode)(MasterTreeListView.SelectedNodes[0])).Tag;
        }

        private void SetupNameDataProperty(TreeListMode mode)
        {
            MasterTreeListView.Columns[0].DataPropertyName =
                GetDataPropertyName(mode);
        }

        private string GetDataPropertyName(TreeListMode treeListMode)
        {
            return treeListMode == TreeListMode.Tree ?
                ViewColumnFactory.REPOSITORY_SHORT_PROPERTY_NAME :
                ViewColumnFactory.REPOSITORY_LONG_PROPERTY_NAME;
        }

        private void SaveTreeListViewMode(TreeListMode treeListMode)
        {
            GuiClientConfig.Get().Configuration.RepositoryViewMode = (int)treeListMode;
            GuiClientConfig.Get().Save();
        }

        #region WinForms designer code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RepositoryViewControl));
            this.ListViewButton = new I3PushButton();
            this.TreeViewButton = new I3PushButton();
            this.CreateRepositoryButton = new I3Button();
            this.ServerComboBox = new Codice.I3.I3ComboBox();
            this.ServerLabel = new I3.I3Label();
            this.FilterPanel.SuspendLayout();
            this.ClientPanel.SuspendLayout();
            this.TopPanel.SuspendLayout();
            this.ToolbarPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MasterTreeListView
            // 
            resources.ApplyResources(this.MasterTreeListView, "MasterTreeListView");
            // 
            // FilterPanel
            // 
            resources.ApplyResources(this.FilterPanel, "FilterPanel");
            // 
            // ExportViewButton
            // 
            resources.ApplyResources(this.ExportViewButton, "ExportViewButton");
            // 
            // RefreshButton
            // 
            resources.ApplyResources(this.RefreshButton, "RefreshButton");
            // 
            // ClientPanel
            // 
            resources.ApplyResources(this.ClientPanel, "ClientPanel");
            // 
            // overlayAnimation1
            // 
            resources.ApplyResources(this.overlayAnimation1, "overlayAnimation1");
            // 
            // TopPanel
            // 
            resources.ApplyResources(this.TopPanel, "TopPanel");
            // 
            // ToolbarPanel
            // 
            this.ToolbarPanel.Controls.Add(this.ListViewButton);
            this.ToolbarPanel.Controls.Add(this.TreeViewButton);
            this.ToolbarPanel.Controls.Add(this.ServerComboBox);
            this.ToolbarPanel.Controls.Add(this.ServerLabel);
            this.ToolbarPanel.Controls.Add(this.CreateRepositoryButton);
            resources.ApplyResources(this.ToolbarPanel, "ToolbarPanel");
            this.ToolbarPanel.Controls.SetChildIndex(this.DescriptionButton, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.ExportViewButton, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.RefreshButton, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.TreeViewButton, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.ListViewButton, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.CreateRepositoryButton, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.ServerLabel, 0);
            this.ToolbarPanel.Controls.SetChildIndex(this.ServerComboBox, 0);
            // 
            // DescriptionButton
            // 
            resources.ApplyResources(this.DescriptionButton, "DescriptionButton");
            // 
            // ListViewButton
            // 
            this.ListViewButton.ActiveColor = System.Drawing.Color.Empty;
            this.ListViewButton.BackColor = System.Drawing.Color.Transparent;
            this.ListViewButton.ButtonStyle = Codice.I3.I3ButtonStyle.Plain;
            this.ListViewButton.ForeColor = System.Drawing.Color.White;
            resources.ApplyResources(this.ListViewButton, "ListViewButton");
            this.ListViewButton.Name = "ListViewButton";
            this.ListViewButton.Pushed = false;
            this.ListViewButton.PushedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.ListViewButton.PushedForeColor = System.Drawing.Color.Black;
            this.ListViewButton.State = Codice.I3.I3PushButtonState.Normal;
            this.ListViewButton.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
            this.ListViewButton.UseVisualStyleBackColor = false;
            this.ListViewButton.Click += new System.EventHandler(this.ListViewButton_Click);
            // 
            // TreeViewButton
            // 
            this.TreeViewButton.ActiveColor = System.Drawing.Color.Empty;
            this.TreeViewButton.BackColor = System.Drawing.Color.Transparent;
            this.TreeViewButton.ButtonStyle = Codice.I3.I3ButtonStyle.Plain;
            this.TreeViewButton.ForeColor = System.Drawing.Color.White;
            resources.ApplyResources(this.TreeViewButton, "TreeViewButton");
            this.TreeViewButton.Name = "TreeViewButton";
            this.TreeViewButton.Pushed = false;
            this.TreeViewButton.PushedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.TreeViewButton.PushedForeColor = System.Drawing.Color.Black;
            this.TreeViewButton.State = Codice.I3.I3PushButtonState.Normal;
            this.TreeViewButton.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
            this.TreeViewButton.UseVisualStyleBackColor = false;
            this.TreeViewButton.Click += new System.EventHandler(this.TreeViewButton_Click);
            // 
            // CreateRepositoryButton
            // 
            this.CreateRepositoryButton.ActiveColor = System.Drawing.Color.Empty;
            this.CreateRepositoryButton.BackColor = System.Drawing.Color.Transparent;
            this.CreateRepositoryButton.ButtonStyle = Codice.I3.I3ButtonStyle.Plain;
            this.CreateRepositoryButton.ForeColor = System.Drawing.Color.White;
            resources.ApplyResources(this.CreateRepositoryButton, "CreateRepositoryButton");
            this.CreateRepositoryButton.Name = "CreateRepositoryButton";
            this.CreateRepositoryButton.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
            this.CreateRepositoryButton.UseVisualStyleBackColor = false;
            // 
            // ServerLabel
            //
            this.ServerLabel.AutoWidth = false;
            this.ServerLabel.BackColor = System.Drawing.Color.Transparent;
            this.ServerLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.ServerLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.ServerLabel.Location = new System.Drawing.Point(225, 3);
            this.ServerLabel.Name = "ServerLabel";
            this.ServerLabel.Size = new System.Drawing.Size(56, 24);
            this.ServerLabel.TabIndex = 0;
            this.ServerLabel.Text = Localization.GetString("REPOSITORY_SERVER_TEXTBOX_LABEL");
            this.ServerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ServerLabel.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            // 
            // ServerTextBox
            // 
            this.ServerComboBox.BackColor = System.Drawing.Color.LightGray;
            this.ServerComboBox.ForeColor = System.Drawing.Color.Black;
            this.ServerComboBox.Location = new System.Drawing.Point(282, 6);
            this.ServerComboBox.Size = new System.Drawing.Size(145, 20);
            this.ServerComboBox.TabIndex = 4;
            this.ServerComboBox.Name = "ServerComboBox";
            // 
            // RepositoryViewControl
            // 
            this.Name = "RepositoryViewControl";
            resources.ApplyResources(this, "$this");
            this.FilterPanel.ResumeLayout(false);
            this.FilterPanel.PerformLayout();
            this.ClientPanel.ResumeLayout(false);
            this.TopPanel.ResumeLayout(false);
            this.ToolbarPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

    }
}


