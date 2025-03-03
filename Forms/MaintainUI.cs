using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using SBS.Utilities;

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
    private List<CandidateEntry> _candidateRules = [];
    // private RemoteSite? _activeSite;
    private bool _processAll;
    private string _ruleName = "_Blocklist";
    //    internal bool _startUp = true;

    /// <summary>
    /// Initialise
    /// </summary>
    public MaintainUI( )
    {
        InitializeComponent( );
        this.DeleteButton.Enabled = false;
        this.UpdateButton.Enabled = false;
        _processAll = false;
        FixBounds( );
    }

    private void FixBounds( )
    {
        if ( Height > Screen.PrimaryScreen!.Bounds.Height )
        {
            Height = Screen.PrimaryScreen.Bounds.Height;
            Top = 0;
        }

        if ( Width > Screen.PrimaryScreen.Bounds.Width )
        {
            Width = Screen.PrimaryScreen.Bounds.Width;
            Left = 0;
        }
    }

    private void MaintainUI_Load( object sender, EventArgs e )
    {
        this.RemoteSites.SelectionChanged -= RemoteSites_SelectionChanged;
        this.Show( );
        this.StatusMessage.Text = "Fetching download site details ...";
        this.Refresh( );
        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, false )
           .Where( w => w.Active )
           //            .Select( s => new { s.ID, s.Name, s.LastDownloaded, s.SiteUrl, s.FileUrls, FileType = s.FileType!.Name, s.Active } )
           .Select( s => new RemoteSite( )
           {
               ID = s.ID,
               Name = s.Name,
               LastDownloaded = s.LastDownloaded,
               SiteUrl = s.SiteUrl,
               FileUrls = s.FileUrls,
               FileTypeID = s.FileTypeID,
               FileType = s.FileType,
               Active = s.Active,
               MinimumIntervalMinutes = s.MinimumIntervalMinutes
           } )
           .ToList( ); // Maintain.ListDownloadSites( remoteSite, ShowAllCheckBox.Checked );
        this.RemoteSites.AutoResizeColumns( DataGridViewAutoSizeColumnsMode.AllCells );
        this.RemoteSites.Columns[ "ID" ]!.Visible = false; // ID
        this.RemoteSites.Columns[ "FilePaths" ]!.Visible = false; // ID
        this.RemoteSites.Columns[ "FileType" ]!.Visible = false; // ID
        this.RemoteSites.Columns[ "MinimumIntervalMinutes" ]!.Visible = false; // ID

        if ( this.RemoteSites.RowCount < 1 )
            Maintain.StatusMessage( "Load", "Blocklist data was downloaded too recently to update now.\r\nTry again after 30 minutes." );
        //else if ( _startUp && this.RemoteSites.SelectedRows.Count > 0 )
        //    foreach ( var row in this.RemoteSites.Rows.Cast<DataGridViewRow>( ).Where( w => w.Selected ) )
        //        row.Selected = false;
        //else
        //{
        //    _ruleName = $"{this.RemoteSites.Rows[ 0 ].Cells[ 1 ].Value}_Blocklist";
        //}

        this.FirewallEntryName.Text = _ruleName == "_Blocklist" ? string.Empty : _ruleName;
        //        _startUp = false;

        this.StatusBar.Left = FirewallRulesData.Left;
        this.StatusMessage.Width = FirewallRulesData.Width / 2;
        this.StatusProgress.Width = FirewallRulesData.Width / 2;

        this.RemoteSites.Rows[ 0 ].Selected = false;
        //this.RemoteSites_SelectionChanged( sender, e );

        this.StatusMessage.Text = IDLE;
        this.StatusProgress.Value = 0;
        this.RemoteSites.SelectionChanged += RemoteSites_SelectionChanged!;
    }

    /// <summary>
    /// Changed 3 Nov 2024 to only delete entries which are being replaced; leave all others alone
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

            this.UpdateProgress( counter, sitesList.Count );
        }

        LoadFirewallRules( );
        this.FirewallRulesData.ForeColor = Color.Green;
        this.FirewallRulesData.Refresh( );

        //        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, false ); // ShowAllCheckBox.Checked );
        //        SetFirewallRuleColumnWidths( );
        this.StatusMessage.Text = $"Idle";
        this.StatusProgress.Value = 0;
        this.UpdateButton.Enabled = false;
        this.Cursor = Cursors.Default;
    }

    private void LoadFirewallRules( )
    {
        StatusMessage.Text = $"Reading firewall rules ...";
        string? siteName = null;
        if ( !_processAll && RemoteSites.SelectedRows.Count > 0 )
            siteName = ( (RemoteSite)( RemoteSites.SelectedRows[ 0 ].DataBoundItem )! ).Name;

        var rules = Maintain.FetchFirewallRulesFor( siteName );
        //        if ( _processAll )
        FirewallRulesData.DataSource = rules.Select( s => new { s.Name, s.RemoteAddressList, s.Action, s.Direction, Enabled = s.IsEnable, s.Profiles, Post = s.RemotePorts } )
                                            .ToList( );
        //else if ( RemoteSites.SelectedRows.Count > 0 )
        //    FirewallRulesData.DataSource = rules.Select( s => new { s.Name, s.RemoteAddressList, s.Action, s.Direction, Enabled = s.IsEnable, s.Profiles, Post = s.RemotePorts } )
        //                                        .ToList( );

        this.FirewallRulesData.ForeColor = Color.Green;
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

    //private void ReplaceRulesForSite( RemoteSite site )
    //{
    //    _ruleName = $"{site.Name}_Blocklist";
    //    this.FirewallEntryName.Text = _ruleName;
    //    Maintain.ReplaceSiteRules( site, _candidateRules, this );
    //}

    private void RemoteSites_SelectionChanged( object sender, EventArgs e )
    {
        //    RemoteSite_Edit.Enabled = ( RemoteSites.SelectedRows.Count == 1 );
        //        _startUp = false;
        Cursor = Cursors.WaitCursor;

        //        if ( !_startUp )
        //        {
        this.RemoteData.DataSource = null;
        this.RemoteData.Refresh( );
        StatusMessage.Text = $"Downloading firewall rules ...";
        if ( !_processAll && RemoteSites.SelectedRows.Count == 1 )  // Multiselect is disabled
        {
            RemoteSite? selectedSite = RemoteSites.SelectedRows[ 0 ].DataBoundItem as RemoteSite;
            this.DeleteButton.Enabled = true;
            _ruleName = $"{selectedSite!.Name}_Blocklist";
            StatusMessage.Text = $"Downloading firewall rules for {selectedSite!.Name}";
            //LoadRules( false ); // _processAll );
            _candidateRules = Maintain.ProcessDownloads( [ selectedSite ], this, false, out int numberOfRules, out int ipAddressCount, out int allAddressCount );
            this.UpdateButton.Enabled = true;

            // Show existing firewall rules
            //                this.FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( selectedSite.Name );
            LoadFirewallRules( );
            this.FirewallRulesData.ForeColor = Color.Gray;
            this.FirewallRulesData.Refresh( );
        }
        else
        {
            this.DeleteButton.Enabled = false;
            this.UpdateButton.Enabled = false;
        }
        //        }
        //else if ( _startUp && this.RemoteSites.Rows.Count > 0 )
        //    this.RemoteSites.Rows[ 0 ].Selected = false;

        _processAll = false;
        if ( !this.StatusMessage.Text!.Contains( "failed" ) && !this.StatusMessage.Text.Contains( "error" ) )
        {
            this.StatusMessage.Text = $"Idle";
            this.StatusProgress.Value = 0;
        }

        Cursor = Cursors.Default;
    }

    //private void SetRemoteDataColumnWidths( )
    //{
    //    foreach ( DataGridViewColumn col in RemoteData.Columns )
    //    {
    //        if ( col.Name.StartsWith( "IPAddress" ) && col.Width > MAX_BATCH_COLUMN_SIZE )
    //        {
    //            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
    //            col.Width = MAX_BATCH_COLUMN_SIZE;
    //        }
    //        else if ( col.Name == "Name" && col.Width > MAX_NAME_SIZE )
    //        {
    //            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
    //            col.Width = MAX_NAME_SIZE;
    //        }
    //        else
    //        {
    //            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
    //        }
    //    }
    //}

    //internal void LoadRules( bool all )
    //{
    //    this.RemoteData.DataSource = null;
    //    this.RemoteData.Refresh( );
    //    this.Cursor = Cursors.WaitCursor;
    //    if ( all )
    //        RemoteSites.SelectAll( );

    //    List<RemoteSite> sitesList = SitesListFromGridView( );

    //    if ( all )
    //    {
    //        this.FirewallRulesData.DataSource = null;
    //        this.FirewallRulesData.Refresh( );
    //        foreach ( RemoteSite site in sitesList )
    //        {
    //            this.StatusMessage.Text = $"Downloading blocklist(s) from {site.Name} ...";
    //            this.Refresh( );
    //            _candidateRules.AddRange( Maintain.DownloadBlocklists( this, site ) );
    //        }

    //        this.StatusMessage.Text = "Removing private address ranges ...";
    //        Maintain.RemovePrivateAddressesRanges( ref _candidateRules, out int numberRemoved );

    //        this.StatusMessage.Text = "Removing duplicates ...";
    //        Maintain.RemoveDuplicates( ref _candidateRules, out numberRemoved );

    //        this.StatusMessage.Text = "Removing any invalid addresses ...";
    //        Maintain.RemoveInvalidIPAddresses( ref _candidateRules, out numberRemoved );

    //        StatusMessage.Text = $"Consolidating addresses into sets of {Maintain.MAX_FIREWALL_BATCH_SIZE} ...";
    //        Maintain.ConvertIPAddressesToIPAddressSets( ref _candidateRules, sitesList );

    //        this.RemoteData.DataSource = _candidateRules; // blocklistData; // Maintain.DownloadBlocklists( );
    //        this.RemoteData.Refresh( );
    //        this.ReplaceButton_Click( this, new EventArgs( ) );
    //    }

    //    if ( sitesList.Count == 1 )
    //    {
    //        RemoteSite? chosenSite = sitesList.First( ); // RemoteSites.SelectedRows[ 0 ].DataBoundItem as RemoteSite;
    //        bool load = ( _activeSite is null && chosenSite is not null )
    //                 || ( chosenSite is not null && _activeSite!.Name != chosenSite.Name );
    //        if ( load )
    //        {
    //            _activeSite = chosenSite;
    //            _ruleName = $"{_activeSite!.Name}_Blocklist";
    //            this.FirewallEntryName.Text = _ruleName;
    //            this.FirewallEntryName.Refresh( );

    //            StatusMessage.Text = $"Downloading blocklist(s) from {_activeSite!.Name} ...";
    //            _candidateRules = Maintain.DownloadBlocklists( this, _activeSite );

    //            this.StatusMessage.Text = "Removing private address ranges ...";
    //            Maintain.RemovePrivateAddressesRanges( ref _candidateRules, out int numberRemoved );

    //            this.StatusMessage.Text = "Removing duplicates ...";
    //            Maintain.RemoveDuplicates( ref _candidateRules, out numberRemoved );

    //            this.StatusMessage.Text = "Removing any invalid addresses ...";
    //            Maintain.RemoveInvalidIPAddresses( ref _candidateRules, out numberRemoved );

    //            StatusMessage.Text = $"Consolidating addresses into sets of {Maintain.MAX_FIREWALL_BATCH_SIZE} ...";
    //            Maintain.ConvertIPAddressesToIPAddressSets( ref _candidateRules, [ chosenSite ] );
    //            this.RemoteData.DataSource = _candidateRules.OrderBy( o => o.Sort0 )
    //                                                        .ThenBy( t => t.Sort1 )
    //                                                        .ThenBy( t => t.Sort2 )
    //                                                        .ThenBy( t => t.Sort3 )
    //                                                        .ToList( );

    //            SetDownloadedRuleColumnWidths( );
    //            StatusMessage.Text = $"Reading existing firewall rules for the {_activeSite.Name} blocklist(s) ...";
    //            FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( _ruleName );
    //        }
    //    }

    //    SetFirewallRuleColumnWidths( );
    //    this.Cursor = Cursors.Default;
    //    StatusMessage.Text = $"Idle";
    //}

    //private void SetDownloadedRuleColumnWidths( )
    //{
    //    if ( RemoteData.Columns[ "IPAddressBatch" ].Width > MAX_BATCH_COLUMN_SIZE )
    //    {
    //        RemoteData.Columns[ "IPAddressBatch" ].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
    //        RemoteData.Columns[ "IPAddressBatch" ].Width = MAX_BATCH_COLUMN_SIZE;
    //    }
    //    else
    //        RemoteData.Columns[ "IPAddressBatch" ].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
    //}

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

    private void DeleteButton_Click( object sender, EventArgs e )
    {
        string sitesForDeletion = _processAll ? "all download sites" : _ruleName;
        if ( MessageBox.Show( $"Delete all Windows Firewall rules matching {sitesForDeletion}?", "Confirm Deletion of Windows Firewall entries", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1 ) == DialogResult.OK )
        {
            this.DeleteButton.Enabled = false;

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
        this.StatusMessage.Text = IDLE;
        this.StatusProgress.Value = 0;
    }

    internal void UpdateProgress( int completed, int toComplete )
    {
        decimal portionCompleted = (decimal)completed / (decimal)toComplete;
        this.StatusProgress.Value = Convert.ToInt32( portionCompleted * (decimal)( StatusProgress.Maximum - StatusProgress.Minimum ) );
        this.StatusBar.Refresh( );
    }

    private void ShowAllCheckBox_CheckedChanged( object sender, EventArgs e )
    {
        this.RemoteSites.SelectionChanged -= RemoteSites_SelectionChanged;
        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked )
            //            .Select( s => new { s.ID, s.Name, s.LastDownloaded, s.SiteUrl, s.FileUrls, FileType = s.FileType!.Name, s.Active } )
            .ToList( ); // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
                        //        _startUp = false;
        this.RemoteSites.SelectionChanged += RemoteSites_SelectionChanged;
    }

    private void ScheduleButton_Click( object sender, EventArgs e )
    {
        using UpdateScheduler scheduler = new( );
        scheduler.ShowDialog( this );
    }

    //private void RemoteSites_Click( object sender, EventArgs e )
    //{
    //    RemoteSites_SelectionChanged( sender, e );
    //}

    //private void RemoteSite_Add_Click( object sender, EventArgs e )
    //{
    //    // RemoteSiteForm form = new( );
    //    form.Show( this );
    //    RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
    //}

    //private void RemoteSite_Edit_Click( object sender, EventArgs e )
    //{
    //    if ( RemoteSites.SelectedRows.Count == 1 )
    //    {
    //        // RemoteSiteForm form = new( )
    //        {
    //            DownloadSite = (RemoteSite)RemoteSites.SelectedRows[ 0 ].DataBoundItem
    //        };

    //        form.ShowDialog( );
    //    }
    //}

    private void ProcessAllButton_Click( object sender, EventArgs e )
    {
        // ProcessAll also creates firewall entries
        _processAll = true;
        Cursor = Cursors.WaitCursor;
        //LoadRules( true );
        List<RemoteSite> allActiveSites = [ .. Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ) ]; // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
        Maintain.ProcessDownloads( allActiveSites, this, true, out int numberOfRules, out int ipAddressCount, out int allAddressCount );
        Cursor = Cursors.Default;
        _processAll = false;
        this.StatusMessage.Text = IDLE;
        this.StatusProgress.Value = 0;
    }

    private void SelectGridViewRowOnRightClick( object sender, MouseEventArgs e )
    {
        DataGridView dgv = (DataGridView)sender;

        // Use HitTest to resolve the row under the cursor
        int rowIndex = dgv.HitTest( e.X, e.Y ).RowIndex;

        // If there was no DataGridViewRow under the cursor, return
        if ( rowIndex == -1 ) { return; }

        // Clear all other selections before making a new selection
        dgv.ClearSelection( );

        // Select the found DataGridViewRow
        dgv.Rows[ rowIndex ].Selected = true;
    }
}
