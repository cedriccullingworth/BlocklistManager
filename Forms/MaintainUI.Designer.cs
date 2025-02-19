using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlocklistManager
{
    partial class MaintainUI
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent( )
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MaintainUI ) );
            this.label1 = new Label( );
            this.RemoteSites = new DataGridView( );
            this.label2 = new Label( );
            this.FirewallRulesData = new DataGridView( );
            this.label3 = new Label( );
            this.UpdateButton = new Button( );
            this.RemoteData = new DataGridView( );
            this.label4 = new Label( );
            this.saveFileDialog1 = new SaveFileDialog( );
            this.DeleteButton = new Button( );
            this.ScheduleButton = new Button( );
            this.StatusBar = new StatusStrip( );
            this.StatusMessage = new ToolStripStatusLabel( );
            this.StatusProgress = new ToolStripProgressBar( );
            this.FirewallEntryName = new Label( );
            this.ShowAllCheckBox = new CheckBox( );
            this.OptionsMenu = new MenuStrip( );
            this.ProcessAllButton = new Button( );
            ( (System.ComponentModel.ISupportInitialize)this.RemoteSites ).BeginInit( );
            ( (System.ComponentModel.ISupportInitialize)this.FirewallRulesData ).BeginInit( );
            ( (System.ComponentModel.ISupportInitialize)this.RemoteData ).BeginInit( );
            this.StatusBar.SuspendLayout( );
            this.SuspendLayout( );
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new Point( 13, 34 );
            this.label1.Name = "label1";
            this.label1.Size = new Size( 168, 15 );
            this.label1.TabIndex = 0;
            this.label1.Text = "Remote blocklist file locations:";
            // 
            // RemoteSites
            // 
            this.RemoteSites.AllowUserToOrderColumns = true;
            this.RemoteSites.Anchor =   AnchorStyles.Top  |  AnchorStyles.Left   |  AnchorStyles.Right ;
            this.RemoteSites.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            this.RemoteSites.BackgroundColor = SystemColors.Control;
            this.RemoteSites.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.RemoteSites.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.RemoteSites.Location = new Point( 13, 52 );
            this.RemoteSites.Name = "RemoteSites";
            this.RemoteSites.ReadOnly = true;
            this.RemoteSites.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.RemoteSites.Size = new Size( 1268, 140 );
            this.RemoteSites.TabIndex = 1;
            this.RemoteSites.SelectionChanged +=  this.RemoteSites_SelectionChanged ;
            this.RemoteSites.MouseDown +=  this.RemoteSites_MouseDown ;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new Point( 13, 195 );
            this.label2.Name = "label2";
            this.label2.Size = new Size( 193, 15 );
            this.label2.TabIndex = 2;
            this.label2.Text = "Name for Windows Firewall entries:";
            // 
            // FirewallRulesData
            // 
            this.FirewallRulesData.AllowUserToOrderColumns = true;
            this.FirewallRulesData.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
            this.FirewallRulesData.BackgroundColor = SystemColors.Control;
            this.FirewallRulesData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.FirewallRulesData.Location = new Point( 13, 444 );
            this.FirewallRulesData.Name = "FirewallRulesData";
            this.FirewallRulesData.Size = new Size( 1268, 200 );
            this.FirewallRulesData.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Anchor =  AnchorStyles.Bottom  |  AnchorStyles.Left ;
            this.label3.AutoSize = true;
            this.label3.Location = new Point( 13, 427 );
            this.label3.Name = "label3";
            this.label3.Size = new Size( 231, 15 );
            this.label3.TabIndex = 6;
            this.label3.Text = "Current firewall entries for the selected site";
            // 
            // UpdateButton
            // 
            this.UpdateButton.Anchor =  AnchorStyles.Bottom  |  AnchorStyles.Right ;
            this.UpdateButton.Location = new Point( 1154, 397 );
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new Size( 127, 42 );
            this.UpdateButton.TabIndex = 8;
            this.UpdateButton.Text = "&Replace this Site's Firewall Entries";
            this.UpdateButton.UseVisualStyleBackColor = true;
            this.UpdateButton.Click +=  this.ReplaceButton_Click ;
            // 
            // RemoteData
            // 
            this.RemoteData.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
            this.RemoteData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.RemoteData.BackgroundColor = SystemColors.Control;
            this.RemoteData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.RemoteData.Location = new Point( 13, 240 );
            this.RemoteData.Name = "RemoteData";
            this.RemoteData.Size = new Size( 1268, 151 );
            this.RemoteData.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new Point( 13, 223 );
            this.label4.Name = "label4";
            this.label4.Size = new Size( 244, 15 );
            this.label4.TabIndex = 10;
            this.label4.Text = "Rule details from downloaded blocklist file(s)";
            // 
            // DeleteButton
            // 
            this.DeleteButton.Anchor =  AnchorStyles.Bottom  |  AnchorStyles.Right ;
            this.DeleteButton.Location = new Point( 1010, 397 );
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new Size( 127, 42 );
            this.DeleteButton.TabIndex = 12;
            this.DeleteButton.Text = "&Delete this Site's Firewall Entries";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click +=  this.DeleteButton_Click ;
            // 
            // ScheduleButton
            // 
            this.ScheduleButton.Anchor =  AnchorStyles.Bottom  |  AnchorStyles.Right ;
            this.ScheduleButton.Location = new Point( 866, 397 );
            this.ScheduleButton.Name = "ScheduleButton";
            this.ScheduleButton.Size = new Size( 127, 42 );
            this.ScheduleButton.TabIndex = 13;
            this.ScheduleButton.Text = "&Schedule Automatic Processing";
            this.ScheduleButton.UseVisualStyleBackColor = true;
            this.ScheduleButton.Click +=  this.ScheduleButton_Click ;
            // 
            // StatusBar
            // 
            this.StatusBar.Anchor =  AnchorStyles.Bottom  |  AnchorStyles.Left ;
            this.StatusBar.Dock = DockStyle.None;
            this.StatusBar.Items.AddRange( new ToolStripItem[] { this.StatusMessage, this.StatusProgress } );
            this.StatusBar.Location = new Point( 13, 647 );
            this.StatusBar.Name = "StatusBar";
            this.StatusBar.Size = new Size( 631, 22 );
            this.StatusBar.TabIndex = 15;
            this.StatusBar.Text = "StatusBar";
            // 
            // StatusMessage
            // 
            this.StatusMessage.AutoSize = false;
            this.StatusMessage.Name = "StatusMessage";
            this.StatusMessage.Size = new Size( 512, 17 );
            this.StatusMessage.Text = "Idle";
            this.StatusMessage.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // StatusProgress
            // 
            this.StatusProgress.AutoSize = false;
            this.StatusProgress.Name = "StatusProgress";
            this.StatusProgress.Size = new Size( 100, 16 );
            // 
            // FirewallEntryName
            // 
            this.FirewallEntryName.AutoSize = true;
            this.FirewallEntryName.Location = new Point( 211, 195 );
            this.FirewallEntryName.Name = "FirewallEntryName";
            this.FirewallEntryName.Size = new Size( 90, 15 );
            this.FirewallEntryName.TabIndex = 16;
            this.FirewallEntryName.Text = "No site selected";
            // 
            // ShowAllCheckBox
            // 
            this.ShowAllCheckBox.AutoSize = true;
            this.ShowAllCheckBox.Location = new Point( 396, 33 );
            this.ShowAllCheckBox.Name = "ShowAllCheckBox";
            this.ShowAllCheckBox.Size = new Size( 156, 19 );
            this.ShowAllCheckBox.TabIndex = 17;
            this.ShowAllCheckBox.Text = "&Show All Download Sites";
            this.ShowAllCheckBox.UseVisualStyleBackColor = true;
            this.ShowAllCheckBox.CheckedChanged +=  this.ShowAllCheckBox_CheckedChanged ;
            // 
            // OptionsMenu
            // 
            this.OptionsMenu.Location = new Point( 0, 0 );
            this.OptionsMenu.Name = "OptionsMenu";
            this.OptionsMenu.Size = new Size( 1294, 24 );
            this.OptionsMenu.TabIndex = 18;
            this.OptionsMenu.Text = "&Remote DownloadSite Options";
            // 
            // ProcessAllButton
            // 
            this.ProcessAllButton.Anchor =  AnchorStyles.Bottom  |  AnchorStyles.Right ;
            this.ProcessAllButton.Location = new Point( 722, 397 );
            this.ProcessAllButton.Name = "ProcessAllButton";
            this.ProcessAllButton.Size = new Size( 127, 42 );
            this.ProcessAllButton.TabIndex = 19;
            this.ProcessAllButton.Text = "&Process All";
            this.ProcessAllButton.UseVisualStyleBackColor = true;
            this.ProcessAllButton.Click +=  this.ProcessAllButton_Click ;
            // 
            // MaintainUI
            // 
            this.AutoScaleDimensions = new SizeF( 7F, 15F );
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size( 1294, 669 );
            this.Controls.Add( this.ProcessAllButton );
            this.Controls.Add( this.ShowAllCheckBox );
            this.Controls.Add( this.FirewallEntryName );
            this.Controls.Add( this.StatusBar );
            this.Controls.Add( this.OptionsMenu );
            this.Controls.Add( this.ScheduleButton );
            this.Controls.Add( this.DeleteButton );
            this.Controls.Add( this.label4 );
            this.Controls.Add( this.RemoteData );
            this.Controls.Add( this.UpdateButton );
            this.Controls.Add( this.label3 );
            this.Controls.Add( this.FirewallRulesData );
            this.Controls.Add( this.label2 );
            this.Controls.Add( this.RemoteSites );
            this.Controls.Add( this.label1 );
            this.Icon = (Icon)resources.GetObject( "$this.Icon" );
            this.MainMenuStrip = this.OptionsMenu;
            this.Name = "MaintainUI";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Maintain Blocklists";
            this.Load +=  this.MaintainUI_Load ;
            ( (System.ComponentModel.ISupportInitialize)this.RemoteSites ).EndInit( );
            ( (System.ComponentModel.ISupportInitialize)this.FirewallRulesData ).EndInit( );
            ( (System.ComponentModel.ISupportInitialize)this.RemoteData ).EndInit( );
            this.StatusBar.ResumeLayout( false );
            this.StatusBar.PerformLayout( );
            this.ResumeLayout( false );
            this.PerformLayout( );
        }

        #endregion

        internal Label label1;
        internal DataGridView RemoteSites;
        internal Label label2;
        internal DataGridView FirewallRulesData;
        internal Label label3;
        internal Button UpdateButton;
        internal DataGridView RemoteData;
        internal Label label4;
        internal SaveFileDialog saveFileDialog1;
        internal Button DeleteButton;
        internal Button ScheduleButton;
        internal StatusStrip StatusBar;
        internal ToolStripStatusLabel StatusMessage;
        internal Label FirewallEntryName;
        internal CheckBox ShowAllCheckBox;
        private MenuStrip OptionsMenu;
        internal ToolStripProgressBar StatusProgress;
        internal Button ProcessAllButton;
    }
}
