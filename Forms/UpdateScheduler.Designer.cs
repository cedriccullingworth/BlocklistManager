using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlocklistManager;

partial class UpdateScheduler
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing )
    {
        if ( disposing && ( components != null ) )
        {
            components.Dispose( );
        }
        base.Dispose( disposing );
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent( )
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( UpdateScheduler ) );
        this.TaskNameLabel = new Label( );
        this.label2 = new Label( );
        this.FrequencyComboBox = new ComboBox( );
        this.label3 = new Label( );
        this.StartDatePicker = new DateTimePicker( );
        this.StartTimePicker = new DateTimePicker( );
        this.label4 = new Label( );
        this.label5 = new Label( );
        this.RecurrenceComboBox = new ComboBox( );
        this.RecurrenceLabel = new Label( );
        this.label6 = new Label( );
        this.label7 = new Label( );
        this.ArgumentsText = new TextBox( );
        this.label8 = new Label( );
        this.OKButton = new Button( );
        this.CancelButton = new Button( );
        this.SitesList = new ListView( );
        this.label9 = new Label( );
        this.LogFolderDialog = new FolderBrowserDialog( );
        this.LogFolder = new TextBox( );
        this.SelectAllCheckBox = new CheckBox( );
        this.BrowseFoldersButton = new Button( );
        this.Notes = new TextBox( );
        this.label10 = new Label( );
        this.AccountsComboBox = new ComboBox( );
        this.AuthorLabel = new Label( );
        this.ApplicationName = new TextBox( );
        this.label1 = new Label( );
        this.SuspendLayout( );
        // 
        // TaskNameLabel
        // 
        this.TaskNameLabel.AutoSize = true;
        this.TaskNameLabel.Location = new Point( 26, 16 );
        this.TaskNameLabel.Name = "TaskNameLabel";
        this.TaskNameLabel.Size = new Size( 274, 15 );
        this.TaskNameLabel.TabIndex = 0;
        this.TaskNameLabel.Text = "Scheduled Task Name:     Update Firewall Blocklists";
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new Point( 26, 45 );
        this.label2.Name = "label2";
        this.label2.Size = new Size( 124, 15 );
        this.label2.TabIndex = 1;
        this.label2.Text = "Frequency of updates:";
        // 
        // FrequencyComboBox
        // 
        this.FrequencyComboBox.FormattingEnabled = true;
        this.FrequencyComboBox.Items.AddRange( new object[] { "Hourly", "Daily", "Weekly" } );
        this.FrequencyComboBox.Location = new Point( 163, 41 );
        this.FrequencyComboBox.Name = "FrequencyComboBox";
        this.FrequencyComboBox.Size = new Size( 203, 23 );
        this.FrequencyComboBox.TabIndex = 2;
        // 
        // label3
        // 
        this.label3.AutoSize = true;
        this.label3.Location = new Point( 26, 74 );
        this.label3.Name = "label3";
        this.label3.Size = new Size( 68, 15 );
        this.label3.TabIndex = 3;
        this.label3.Text = "Starting on:";
        // 
        // StartDatePicker
        // 
        this.StartDatePicker.Location = new Point( 163, 70 );
        this.StartDatePicker.Name = "StartDatePicker";
        this.StartDatePicker.Size = new Size( 134, 23 );
        this.StartDatePicker.TabIndex = 4;
        // 
        // StartTimePicker
        // 
        this.StartTimePicker.Format = DateTimePickerFormat.Time;
        this.StartTimePicker.Location = new Point( 329, 70 );
        this.StartTimePicker.MinDate = new DateTime( 2024, 12, 4, 12, 23, 44, 344 );
        this.StartTimePicker.Name = "StartTimePicker";
        this.StartTimePicker.ShowUpDown = true;
        this.StartTimePicker.Size = new Size( 81, 23 );
        this.StartTimePicker.TabIndex = 5;
        // 
        // label4
        // 
        this.label4.AutoSize = true;
        this.label4.Location = new Point( 303, 74 );
        this.label4.Name = "label4";
        this.label4.Size = new Size( 20, 15 );
        this.label4.TabIndex = 6;
        this.label4.Text = "at:";
        // 
        // label5
        // 
        this.label5.AutoSize = true;
        this.label5.Location = new Point( 26, 103 );
        this.label5.Name = "label5";
        this.label5.Size = new Size( 71, 15 );
        this.label5.TabIndex = 7;
        this.label5.Text = "Recur every:";
        // 
        // RecurrenceComboBox
        // 
        this.RecurrenceComboBox.FormattingEnabled = true;
        this.RecurrenceComboBox.Items.AddRange( new object[] { "1", "2", "3", "4", "5", "6", "7" } );
        this.RecurrenceComboBox.Location = new Point( 100, 99 );
        this.RecurrenceComboBox.Name = "RecurrenceComboBox";
        this.RecurrenceComboBox.Size = new Size( 48, 23 );
        this.RecurrenceComboBox.TabIndex = 8;
        // 
        // RecurrenceLabel
        // 
        this.RecurrenceLabel.AutoSize = true;
        this.RecurrenceLabel.Location = new Point( 163, 103 );
        this.RecurrenceLabel.Name = "RecurrenceLabel";
        this.RecurrenceLabel.Size = new Size( 39, 15 );
        this.RecurrenceLabel.TabIndex = 9;
        this.RecurrenceLabel.Text = "day(s)";
        // 
        // label6
        // 
        this.label6.AutoSize = true;
        this.label6.Location = new Point( 26, 360 );
        this.label6.Name = "label6";
        this.label6.Size = new Size( 104, 15 );
        this.label6.TabIndex = 10;
        this.label6.Text = "Application name:";
        // 
        // label7
        // 
        this.label7.AutoSize = true;
        this.label7.Location = new Point( 26, 408 );
        this.label7.Name = "label7";
        this.label7.Size = new Size( 69, 15 );
        this.label7.TabIndex = 11;
        this.label7.Text = "Arguments:";
        // 
        // ArgumentsText
        // 
        this.ArgumentsText.Location = new Point( 163, 404 );
        this.ArgumentsText.Name = "ArgumentsText";
        this.ArgumentsText.Size = new Size( 614, 23 );
        this.ArgumentsText.TabIndex = 12;
        this.ArgumentsText.Text = "/Sites:AllCurrent /LogPath:C:\\Temp";
        // 
        // label8
        // 
        this.label8.AutoSize = true;
        this.label8.Location = new Point( 26, 132 );
        this.label8.Name = "label8";
        this.label8.Size = new Size( 117, 15 );
        this.label8.TabIndex = 13;
        this.label8.Text = "Sites to update from:";
        // 
        // OKButton
        // 
        this.OKButton.Location = new Point( 700, 548 );
        this.OKButton.Name = "OKButton";
        this.OKButton.Size = new Size( 75, 23 );
        this.OKButton.TabIndex = 16;
        this.OKButton.Text = "O&K";
        this.OKButton.UseVisualStyleBackColor = true;
        this.OKButton.Click +=  this.OKButton_Click ;
        // 
        // CancelButton
        // 
        this.CancelButton.Location = new Point( 700, 492 );
        this.CancelButton.Name = "CancelButton";
        this.CancelButton.Size = new Size( 75, 23 );
        this.CancelButton.TabIndex = 17;
        this.CancelButton.Text = "&Cancel";
        this.CancelButton.UseVisualStyleBackColor = true;
        this.CancelButton.Click +=  this.CancelButton_Click ;
        // 
        // SitesList
        // 
        this.SitesList.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
        this.SitesList.CheckBoxes = true;
        this.SitesList.Location = new Point( 26, 151 );
        this.SitesList.Name = "SitesList";
        this.SitesList.Size = new Size( 747, 177 );
        this.SitesList.TabIndex = 18;
        this.SitesList.UseCompatibleStateImageBehavior = false;
        // 
        // label9
        // 
        this.label9.AutoSize = true;
        this.label9.Location = new Point( 26, 384 );
        this.label9.Name = "label9";
        this.label9.Size = new Size( 100, 15 );
        this.label9.TabIndex = 19;
        this.label9.Text = "Folder for log file:";
        // 
        // LogFolder
        // 
        this.LogFolder.Location = new Point( 163, 380 );
        this.LogFolder.Name = "LogFolder";
        this.LogFolder.Size = new Size( 577, 23 );
        this.LogFolder.TabIndex = 20;
        this.LogFolder.Leave +=  this.LogFolder_Leave ;
        // 
        // SelectAllCheckBox
        // 
        this.SelectAllCheckBox.AutoSize = true;
        this.SelectAllCheckBox.Location = new Point( 189, 130 );
        this.SelectAllCheckBox.Name = "SelectAllCheckBox";
        this.SelectAllCheckBox.Size = new Size( 74, 19 );
        this.SelectAllCheckBox.TabIndex = 21;
        this.SelectAllCheckBox.Text = "Select &All";
        this.SelectAllCheckBox.UseVisualStyleBackColor = true;
        // 
        // BrowseFoldersButton
        // 
        this.BrowseFoldersButton.Location = new Point( 734, 378 );
        this.BrowseFoldersButton.Name = "BrowseFoldersButton";
        this.BrowseFoldersButton.Size = new Size( 37, 26 );
        this.BrowseFoldersButton.TabIndex = 22;
        this.BrowseFoldersButton.Text = "...";
        this.BrowseFoldersButton.UseVisualStyleBackColor = true;
        this.BrowseFoldersButton.Click +=  this.BrowseFoldersButton_Click ;
        // 
        // Notes
        // 
        this.Notes.BackColor = SystemColors.Control;
        this.Notes.BorderStyle = BorderStyle.None;
        this.Notes.Location = new Point( 26, 431 );
        this.Notes.Multiline = true;
        this.Notes.Name = "Notes";
        this.Notes.Size = new Size( 656, 136 );
        this.Notes.TabIndex = 23;
        this.Notes.Text = resources.GetString( "Notes.Text" );
        // 
        // label10
        // 
        this.label10.AutoSize = true;
        this.label10.Location = new Point( 26, 336 );
        this.label10.Name = "label10";
        this.label10.Size = new Size( 111, 15 );
        this.label10.TabIndex = 24;
        this.label10.Text = "Run under account:";
        // 
        // AccountsComboBox
        // 
        this.AccountsComboBox.BackColor = SystemColors.Control;
        this.AccountsComboBox.FlatStyle = FlatStyle.Flat;
        this.AccountsComboBox.FormattingEnabled = true;
        this.AccountsComboBox.Location = new Point( 163, 332 );
        this.AccountsComboBox.Name = "AccountsComboBox";
        this.AccountsComboBox.Size = new Size( 201, 23 );
        this.AccountsComboBox.TabIndex = 25;
        // 
        // AuthorLabel
        // 
        this.AuthorLabel.AutoSize = true;
        this.AuthorLabel.Location = new Point( 469, 45 );
        this.AuthorLabel.Name = "AuthorLabel";
        this.AuthorLabel.Size = new Size( 47, 15 );
        this.AuthorLabel.TabIndex = 26;
        this.AuthorLabel.Text = "Author:";
        // 
        // ApplicationName
        // 
        this.ApplicationName.BackColor = SystemColors.Control;
        this.ApplicationName.BorderStyle = BorderStyle.None;
        this.ApplicationName.Location = new Point( 163, 359 );
        this.ApplicationName.Name = "ApplicationName";
        this.ApplicationName.Size = new Size( 229, 16 );
        this.ApplicationName.TabIndex = 27;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new Point( 370, 336 );
        this.label1.Name = "label1";
        this.label1.Size = new Size( 169, 15 );
        this.label1.TabIndex = 28;
        this.label1.Text = "(Only administrators are listed)";
        // 
        // UpdateScheduler
        // 
        this.AcceptButton = this.OKButton;
        this.AutoScaleDimensions = new SizeF( 7F, 15F );
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size( 800, 591 );
        this.Controls.Add( this.label1 );
        this.Controls.Add( this.ApplicationName );
        this.Controls.Add( this.AuthorLabel );
        this.Controls.Add( this.AccountsComboBox );
        this.Controls.Add( this.label10 );
        this.Controls.Add( this.Notes );
        this.Controls.Add( this.BrowseFoldersButton );
        this.Controls.Add( this.SelectAllCheckBox );
        this.Controls.Add( this.LogFolder );
        this.Controls.Add( this.label9 );
        this.Controls.Add( this.SitesList );
        this.Controls.Add( this.CancelButton );
        this.Controls.Add( this.OKButton );
        this.Controls.Add( this.label8 );
        this.Controls.Add( this.ArgumentsText );
        this.Controls.Add( this.label7 );
        this.Controls.Add( this.label6 );
        this.Controls.Add( this.RecurrenceLabel );
        this.Controls.Add( this.RecurrenceComboBox );
        this.Controls.Add( this.label5 );
        this.Controls.Add( this.label4 );
        this.Controls.Add( this.StartTimePicker );
        this.Controls.Add( this.StartDatePicker );
        this.Controls.Add( this.label3 );
        this.Controls.Add( this.FrequencyComboBox );
        this.Controls.Add( this.label2 );
        this.Controls.Add( this.TaskNameLabel );
        this.Name = "UpdateScheduler";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Schedule Automatic Updates";
        this.Load +=  this.UpdateScheduler_Load ;
        this.ResumeLayout( false );
        this.PerformLayout( );
    }

    #endregion

    private Label TaskNameLabel;
    private Label label2;
    private ComboBox FrequencyComboBox;
    private Label label3;
    private DateTimePicker StartDatePicker;
    private DateTimePicker StartTimePicker;
    private Label label4;
    private Label label5;
    private ComboBox RecurrenceComboBox;
    private Label RecurrenceLabel;
    private Label label6;
    private Label label7;
    private TextBox ArgumentsText;
    private Label label8;
    private Button OKButton;
    private new Button CancelButton;
    private ListView SitesList;
    private Label label9;
    private FolderBrowserDialog LogFolderDialog;
    private TextBox LogFolder;
    private CheckBox SelectAllCheckBox;
    private Button BrowseFoldersButton;
    private TextBox Notes;
    private Label label10;
    private ComboBox AccountsComboBox;
    private Label AuthorLabel;
    private TextBox ApplicationName;
    private Label label1;
}