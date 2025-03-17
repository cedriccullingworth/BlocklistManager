using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using WindowsFirewallHelper;

namespace BlocklistManager;

/// <summary>
/// User interface to observe and manage automatic blacklist maintenance tasks
/// </summary>
public partial class MaintainUI : Form
{
    private const int MAX_BATCH_COLUMN_SIZE = 250;
    private const int MAX_NAME_SIZE = 300;
    private const string IDLE = "Idle";
    private ICollection<CandidateEntry> _candidateRules = []; // Do we really need this copy?
    private bool _processAll;
    private string _ruleName = "_Blocklist";
    private NormalPosition _normalPosition = new( 0, 0, 0, 0 );

    //    internal bool _startUp = true;

    /// <summary>
    /// Initialise
    /// </summary>
    public MaintainUI( )
    {
        InitializeComponent( );
        DeleteButton.Enabled = false;
        UpdateButton.Enabled = false;
        _processAll = false;
        FixBounds( );
    }

    private void FixBounds( )
    {
        // Adjust the form height and width to fit the working area of the screen
        if ( Height > /*Screen.PrimaryScreen.Bounds.Height - */Screen.PrimaryScreen!.WorkingArea.Height )
        {
            Height = /*Screen.PrimaryScreen.Bounds.Height - */Screen.PrimaryScreen.WorkingArea.Height;
        }

        if ( Width > /*Screen.PrimaryScreen.Bounds.Width - */Screen.PrimaryScreen.WorkingArea.Width )
        {
            Width = /*Screen.PrimaryScreen.Bounds.Width - */Screen.PrimaryScreen.WorkingArea.Width;
        }

        // Find out whether the taskbar is situated at the bottom, top, left or right of the screen
        if ( Screen.PrimaryScreen!.Bounds.Top == Screen.PrimaryScreen.WorkingArea.Top )
        {
            // Taskbar is at the top
            Top = Screen.PrimaryScreen.WorkingArea.Top;
        }
        else if ( Screen.PrimaryScreen.Bounds.Left == Screen.PrimaryScreen.WorkingArea.Left )
        {
            // Taskbar is at the left
            Left = Screen.PrimaryScreen.WorkingArea.Left;
        }
        else if ( Screen.PrimaryScreen.Bounds.Right == Screen.PrimaryScreen.WorkingArea.Right )
        {
            // Taskbar is at the right
            Left = 0;
        }
        else
        {
            // Taskbar is at the bottom
            Top = 0;
        }
    }

    private void MaintainUI_Load( object sender, EventArgs e )
    {
        RemoteSites.SelectionChanged -= RemoteSites_SelectionChanged!;
        Show( );
        _normalPosition = new NormalPosition( Top, Left, Height, Width );
        StatusMessage.Text = "Fetching download site details ...";
        Refresh( );
        RemoteSites.DataSource = new BlocklistData( ).ListDownloadSites( Maintain.ConnectedDevice!.ID, null, false );
        RemoteSites.AutoResizeColumns( DataGridViewAutoSizeColumnsMode.AllCells );
        RemoteSites.Columns[ "FilePaths" ]!.Visible = false;
        RemoteSites.Columns[ "FileType" ]!.Visible = false;
        RemoteSites.Columns[ "MinimumIntervalMinutes" ]!.Visible = false;
        RemoteSites.Refresh( );

        if ( RemoteSites.RowCount < 1 )
            Maintain.StatusMessage( "Load", "Blocklist data was downloaded too recently to update now.\r\nTry again after 30 minutes." );

        FirewallEntryName.Text = _ruleName == "_Blocklist" ? string.Empty : _ruleName;
        StatusBar.Left = FirewallRulesData.Left;
        StatusMessage.Width = FirewallRulesData.Width / 2;
        StatusProgress.Width = FirewallRulesData.Width / 2;

        //RemoteSites.Rows[ 0 ].Selected = false;
        //this.RemoteSites_SelectionChanged( sender, e );

        StatusMessage.Text = IDLE;
        StatusProgress.Value = 0;
        RemoteSites.SelectionChanged += RemoteSites_SelectionChanged!;
    }

    /// <summary>
    /// Delete existing firewall entries for the current site and create new ones
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ReplaceButton_Click( object sender, EventArgs e )
    {
        List<IFirewallRule> newRules = [];
        Cursor = Cursors.WaitCursor;

        if ( _processAll )
        {
            RemoteSites.SelectAll( );
        }

        List<RemoteSite> sitesList = SitesListFromGridView( );
        int counter = 0;

        foreach ( RemoteSite site in sitesList )
        {
            counter++;
            try
            {
                //ReplaceRulesForSite( site );
                FirewallEntryName.Text = site.Name + "_Blocklist";
                Maintain.ReplaceSiteRules( site, _candidateRules, ref newRules, this );
            }
            catch ( Exception ex )
            {
                Maintain.StatusMessage( "ReplaceButton", ex.Message );
                MessageBox.Show( StringUtilities.ExceptionMessage( "ReplaceButton", ex ) );
            }

            UpdateProgress( counter, sitesList.Count );
        }

        LoadFirewallRules( );
        string siteName = sitesList.Count == 1 ? sitesList.ToArray( )[ 0 ].Name : "all active sites";
        FirewallRulesLabel.Text = $"NEW firewall entries for the {siteName} blocklist";
        FirewallRulesData.ForeColor = Color.Green;
        FirewallRulesData.Refresh( );
        StatusMessage.Text = $"Idle";
        StatusProgress.Value = 0;
        UpdateButton.Enabled = false;
        Cursor = Cursors.Default;
    }

    /// <summary>
    /// Read the existing firewall rules
    /// </summary>
    private void LoadFirewallRules( )
    {
        StatusMessage.Text = $"Reading firewall rules ...";
        string? siteName = null;
        if ( !_processAll && RemoteSites.SelectedRows.Count > 0 )
            siteName = ( (RemoteSite)RemoteSites.SelectedRows[ 0 ].DataBoundItem! ).Name;

        var rules = Maintain.FetchFirewallRulesFor( siteName );
        FirewallRulesData.DataSource = rules.Select( s => new { s.Name, s.RemoteAddressList, s.Action, s.Direction, Enabled = s.IsEnable, s.Profiles, Post = s.RemotePorts } )
                                            .ToList( );
        FirewallRulesData.Refresh( );
        SetFirewallRuleColumnWidths( );
    }

    private List<RemoteSite> SitesListFromGridView( )
    {
        return RemoteSites.SelectedRows
                          .Cast<DataGridViewRow>( )
                          .Select( s => s.DataBoundItem as RemoteSite )
                          .Where( w => w is not null )
                          .OrderBy( o => o!.Name ?? string.Empty )
                          .Select( s => s! )
                          .ToList( );
    }

    /// <summary>
    /// When a different site is selected, download and prepare blocklists from that site
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RemoteSites_SelectionChanged( object sender, EventArgs e )
    {
        Cursor = Cursors.WaitCursor;

        if ( !_processAll && RemoteSites.SelectedRows.Count == 1 )  // Multiselect is disabled
        {
            RemoteSite? selectedSite = RemoteSites.SelectedRows[ 0 ].DataBoundItem as RemoteSite;
            if ( $"{selectedSite!.Name}_Blocklist" != _ruleName )
            {
                DeleteButton.Enabled = true;
                RemoteData.DataSource = null;
                RemoteData.Refresh( );

                StatusMessage.Text = $"Downloading firewall rules ...";
                _ruleName = $"{selectedSite!.Name}_Blocklist";
                FirewallEntryName.Text = _ruleName;
                StatusMessage.Text = $"Downloading firewall rules for {selectedSite!.Name}";
                _candidateRules = Maintain.ProcessDownloads( [ selectedSite ], this, false, out int numberOfRules, out int ipAddressCount, out int allAddressCount );
                UpdateButton.Enabled = true;

                LoadFirewallRules( );
                FirewallRulesLabel.Text = $"EXISTING firewall entries for the {selectedSite.Name} blocklist (NOT UPDATED)";
                FirewallRulesData.ForeColor = Color.Gray;
                FirewallRulesData.Refresh( );
            }
        }
        else
        {
            DeleteButton.Enabled = false;
            UpdateButton.Enabled = false;
        }

        _processAll = false;
        if ( !StatusMessage.Text!.Contains( "failed" ) && !StatusMessage.Text.Contains( "error" ) )
        {
            StatusMessage.Text = $"Idle";
            StatusProgress.Value = 0;
        }

        Cursor = Cursors.Default;
    }

    /// <summary>
    /// Adjust all column widths in the FirewallRulesData datagridview
    /// </summary>
    internal void SetFirewallRuleColumnWidths( )
    {
        // Columns are now { s.Name, s.RemoteAddressList, s.Action, s.Direction, Enabled = s.IsEnable, s.Profiles, Post = s.RemotePorts }
        string[] hideColumns = [ "ApplicationName", "FriendlyName", "ServiceName" ];
        foreach ( DataGridViewColumn col in FirewallRulesData.Columns )
        {
            if ( col.Name == "RemoteAddressList" )
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Width = MAX_BATCH_COLUMN_SIZE;
            }
            else if ( col.Name == "Description" || col.Name == "FriendlyName" || col.Name == "ServiceName" )
            {
                col.Visible = false;
            }
            else if ( col.Name == "Name" && col.AutoSizeMode != DataGridViewAutoSizeColumnMode.None /* && col.Width > MAX_NAME_SIZE*/)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Width = MAX_NAME_SIZE;
            }
            else if ( hideColumns.Contains( col.Name ) )
            {
                col.Visible = false;
            }
            else
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.Visible = true;
            }
        }

        FirewallRulesData.Refresh( );
    }

    /// <summary>
    /// Delete Windows Firewall rules for the cuurent site
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DeleteButton_Click( object sender, EventArgs e )
    {
        string sitesForDeletion = _processAll ? "all download sites" : _ruleName;
        if ( MessageBox.Show( $"Delete all Windows Firewall rules matching {sitesForDeletion}?", "Confirm Deletion of Windows Firewall entries", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1 ) == DialogResult.OK )
        {
            DeleteButton.Enabled = false;

            if ( _processAll )
            {
                _ruleName = "(All))";
                StatusMessage.Text = $"Removing firewall rules for all blocklists ...";
            }
            else
            {
                StatusMessage.Text = $"Removing firewall rules for {_ruleName} ...";
            }

            Cursor = Cursors.WaitCursor;
            // Delete the existing set
            Maintain.DeleteExistingFirewallRulesFor( _ruleName );
            if ( _processAll )
                StatusMessage.Text = $"Reading firewall rules for all blocklists ...";
            else
                StatusMessage.Text = $"Reading firewall rules for {FirewallEntryName.Text} ...";
            FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( _ruleName )
                                                   .Select( s => new { s.Name, s.RemoteAddressList, s.Action, s.Direction, Enabled = s.IsEnable, s.Profiles, Post = s.RemotePorts } );
            SetFirewallRuleColumnWidths( );
        }

        Cursor = Cursors.Default;
        StatusMessage.Text = IDLE;
        StatusProgress.Value = 0;
    }

    /// <summary>
    /// Update the progress bar
    /// </summary>
    /// <param name="completed"></param>
    /// <param name="toComplete"></param>
    internal void UpdateProgress( int completed, int toComplete )
    {
        decimal portionCompleted = completed / (decimal)toComplete;
        StatusProgress.Value = Convert.ToInt32( portionCompleted * ( StatusProgress.Maximum - StatusProgress.Minimum ) );
        StatusBar.Refresh( );
    }

    /// <summary>
    /// Refresh the sites list when the 'Show All' checkbox is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowAllCheckBox_CheckedChanged( object sender, EventArgs e )
    {
        RemoteSites.SelectionChanged -= RemoteSites_SelectionChanged!;
        RemoteSites.DataSource = new BlocklistData( ).ListDownloadSites( Maintain.ConnectedDevice!.ID, null, ShowAllCheckBox.Checked );
        //.ToList( );
        RemoteSites.SelectionChanged += RemoteSites_SelectionChanged!;
    }

    /// <summary>
    /// Opens the form for scheduling runs of this app
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ScheduleButton_Click( object sender, EventArgs e )
    {
        using UpdateScheduler scheduler = new( );
        scheduler.ShowDialog( this );
    }

    /// <summary>
    /// Download and prepare all active blocklists, then replace existing firewall rules with them
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ProcessAllButton_Click( object sender, EventArgs e )
    {
        // ProcessAll also creates firewall entries
        _processAll = true;
        Cursor = Cursors.WaitCursor;
        //LoadRules( true );
        //ICollection<RemoteSite> allActiveSites = [ .. Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ) ]; // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
        ICollection<RemoteSite> allActiveSites = new BlocklistData( ).ListDownloadSites( Maintain.ConnectedDevice!.ID, null, ShowAllCheckBox.Checked );
        Maintain.ProcessDownloads( allActiveSites, this, true, out int numberOfRules, out int ipAddressCount, out int allAddressCount );
        Cursor = Cursors.Default;
        _processAll = false;
        StatusMessage.Text = IDLE;
        StatusProgress.Value = 0;
    }

    //private void SelectGridViewRowOnRightClick( object sender, MouseEventArgs e )
    //{
    //    DataGridView dgv = (DataGridView)sender;

    //    // Use HitTest to resolve the row under the cursor
    //    int rowIndex = dgv.HitTest( e.X, e.Y ).RowIndex;

    //    // If there was no DataGridViewRow under the cursor, return
    //    if ( rowIndex == -1 ) { return; }

    //    // Clear all other selections before making a new selection
    //    dgv.ClearSelection( );

    //    // Select the found DataGridViewRow
    //    dgv.Rows[ rowIndex ].Selected = true;
    //}

    /// <summary>
    /// Adjust the dorm position and size when resized using the form's ControlBox
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MaintainUI_Resize( object sender, EventArgs e )
    {
        if ( WindowState == FormWindowState.Maximized )
        {
            FixBounds( );
        }
        else
        {
            Top = _normalPosition.Top;
            Left = _normalPosition.Left;
            Height = _normalPosition.Height;
            Width = _normalPosition.Width;
            ResizeRedraw = true;
        }
    }

    // Holds the normal size and position of the form
    private sealed record NormalPosition( int Top, int Left, int Height, int Width );
}
