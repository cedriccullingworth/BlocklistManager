using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using SBS.Utilities;

using WindowsFirewallHelper;

namespace BlocklistManager;

/* NOTES
 * Allow the user to:
 *  Edit the list (Add/remove IPs) // TODO
 */

/// <summary>
/// User interface to observe and manage automatic blacklist maintenance tasks
/// </summary>
public partial class MaintainUI : Form
{
    private const int MAX_BATCH_COLUMN_SIZE = 250;
    private const int MAX_NAME_SIZE = 300;
    private List<CandidateEntry> _candidateRules = [];
    // private RemoteSite? _activeSite;
    private bool _processAll;
    private string _ruleName = "_Blocklist";
    internal bool _startUp = true;

    /// <summary>
    /// Initialise
    /// </summary>
    public MaintainUI( )
    {
        InitializeComponent( );
        _processAll = false;
        FixBounds( );
    }

    private void FixBounds( )
    {
        if ( this.Height > Screen.PrimaryScreen!.Bounds.Height )
        {
            this.Height = Screen.PrimaryScreen.Bounds.Height;
            this.Top = 0;
        }

        if ( this.Width > Screen.PrimaryScreen.Bounds.Width )
        {
            this.Width = Screen.PrimaryScreen.Bounds.Width;
            this.Left = 0;
        }
    }

    private void MaintainUI_Load( object sender, EventArgs e )
    {
        //RemoteSite site = Maintain.ListDownloadSites( null, false ).First( f => f.ID == 16 );
        //translator.TranslateDataStream( site, testText );

        // string test = HttpHelper.ReadZipFileContents( "https://urlhaus.abuse.ch/downloads/json/" );
        Maintain.EnsureStartupDataExists( ); // WHY?

        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, false ).Where( w => w.Active ).ToList( ); // Maintain.ListDownloadSites( remoteSite, ShowAllCheckBox.Checked );
        this.RemoteSites.AutoResizeColumns( DataGridViewAutoSizeColumnsMode.AllCells );
        this.RemoteSites.Columns[ 0 ].Visible = false; // ID
        //if ( this.RemoteSites.SelectedRows.Count > 0 )
        //    remoteSite = (RemoteSite)this.RemoteSites.SelectedRows[ 0 ].DataBoundItem;

        if ( this.RemoteSites.RowCount < 1 )
            MessageBox.Show( "Blocklist data was downloaded too recently to update now.\r\nTry again after 30 minutes." );
        else
        {
            _ruleName = $"{RemoteSites.Rows[ 0 ].Cells[ 1 ].Value}_Blocklist";
            SiteMenuStrip.Items[ 0 ].Click += this.RemoteSite_Edit_Click!;
            SiteMenuStrip.Items[ 1 ].Click += this.RemoteSite_Add_Click!;
            this.RemoteSites.ContextMenuStrip = SiteMenuStrip;
        }

        this.FirewallEntryName.Text = _ruleName;
        _startUp = false;

        this.StatusBar.Left = this.FirewallRulesData.Left;
        this.StatusMessage.Width = this.FirewallRulesData.Width / 2;
        this.StatusProgress.Width = this.FirewallRulesData.Width / 2;
        // this.RemoteSites.ClearSelection( );
        this.StatusMessage.Text = "Idle";
    }

    private void RemoteSiteOptions_ItemClicked( object sender, ToolStripItemClickedEventArgs e )
    {
        switch ( e.ClickedItem!.Name )
        {
            case "RemoteSite_Add":
                {
                    using ( RemoteSiteForm add = new( ) )
                    {
                        add.ShowDialog( );
                    }

                    this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); //  Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
                    break;
                }
            case "RemoteSite_Edit": //// TODO
                {
                    using ( RemoteSiteForm edit = new( ) )
                    {
                        edit.DownloadSite = (RemoteSite)this.RemoteSites.SelectedRows[ 0 ].DataBoundItem;
                        edit.ShowDialog( );
                    }

                    this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); //  Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
                    break;
                }
            case "RemoteSite_Remove": //// TODO
                {
                    Maintain.DeleteRemoteSite( (RemoteSite)this.RemoteSites.SelectedRows[ 0 ].DataBoundItem );
                    this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); //  Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
                    break;
                }
        }
    }

    /// <summary>
    /// Changed 3 Nov 2024 to only delete entries which are being replaced; leave all others alone
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ReplaceButton_Click( object sender, EventArgs e )
    {
        List<IFirewallRule> newRules = [];
        this.Cursor = Cursors.WaitCursor;

        if ( _processAll )
        {
            this.RemoteSites.SelectAll( );
        }

        List<RemoteSite> sitesList = SitesListFromGridView( );

        foreach ( RemoteSite site in sitesList )
        {
            try
            {
                //ReplaceRulesForSite( site );
                this.FirewallEntryName.Text = site.Name + "_Blocklist";
                Maintain.ReplaceSiteRules( site, _candidateRules, ref newRules, this );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( StringUtilities.ExceptionMessage( "ReplaceButton", ex ) );
            }
        }

        this.StatusMessage.Text = $"Reading updated firewall rules ...";

        if ( _processAll )
            this.FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( );
        else if ( RemoteSites.SelectedRows.Count > 0 )
            this.FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( ( (RemoteSite)RemoteSites.SelectedRows[ 0 ].DataBoundItem ).Name );
        this.FirewallRulesData.Refresh( );
        this.SetFirewallRuleColumnWidths( );

        //        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, false ); // ShowAllCheckBox.Checked );
        //        SetFirewallRuleColumnWidths( );
        StatusMessage.Text = $"Idle";
        this.Cursor = Cursors.Default;
    }

    private List<RemoteSite> SitesListFromGridView( ) => this.RemoteSites
                                            .SelectedRows
                                            .Cast<DataGridViewRow>( )
                                            .Select( s => s.DataBoundItem as RemoteSite )
                                            .Where( w => w is not null )
                                            .OrderBy( o => o!.Name ?? string.Empty )
                                            .Select( s => s! )
                                            .ToList( );

    //private void ReplaceRulesForSite( RemoteSite site )
    //{
    //    _ruleName = $"{site.Name}_Blocklist";
    //    this.FirewallEntryName.Text = _ruleName;
    //    Maintain.ReplaceSiteRules( site, _candidateRules, this );
    //}

    private void RemoteSites_SelectionChanged( object sender, EventArgs e )
    {
        RemoteSite_Edit.Enabled = ( RemoteSites.SelectedRows.Count == 1 );
        this.Cursor = Cursors.WaitCursor;

        if ( !_startUp )
        {
            StatusMessage.Text = $"Downloading firewall rules ...";
            if ( !_processAll && RemoteSites.SelectedRows.Count == 1 )  // Multiselect is disabled
            {
                RemoteSite? selectedSite = RemoteSites.SelectedRows[ 0 ].DataBoundItem as RemoteSite;
                StatusMessage.Text = $"Downloading firewall rules for {selectedSite!.Name}";
                //LoadRules( false ); // _processAll );
                _candidateRules = Maintain.ProcessDownloads( [ selectedSite ], this, false );
            }
        }

        _processAll = false;
        StatusMessage.Text = $"Idle";
        this.Cursor = Cursors.Default;
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
    //        Maintain.RemoveInvalidAddresses( ref _candidateRules, out numberRemoved );

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
    //            Maintain.RemoveInvalidAddresses( ref _candidateRules, out numberRemoved );

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

    private void SetDownloadedRuleColumnWidths( )
    {
        if ( this.RemoteData.Columns[ "IPAddressBatch" ].Width > MAX_BATCH_COLUMN_SIZE )
        {
            this.RemoteData.Columns[ "IPAddressBatch" ].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            this.RemoteData.Columns[ "IPAddressBatch" ].Width = MAX_BATCH_COLUMN_SIZE;
        }
        else
            this.RemoteData.Columns[ "IPAddressBatch" ].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
    }

    internal void SetFirewallRuleColumnWidths( )
    {
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
        //        _ruleName = FirewallEntryName.Text;
        if ( _processAll )
        {
            _ruleName = "(All))";
            StatusMessage.Text = $"Removing firewall rules for all blocklists ...";
        }
        else
        {
            StatusMessage.Text = $"Removing firewall rules for {_ruleName} ...";
        }

        this.Cursor = Cursors.WaitCursor;
        // Delete the existing set
        Maintain.DeleteExistingFirewallRulesFor( _ruleName );
        if ( _processAll )
            StatusMessage.Text = $"Reading firewall rules for all blocklists ...";
        else
            StatusMessage.Text = $"Reading firewall rules for {FirewallEntryName.Text} ...";
        FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( _ruleName );
        SetFirewallRuleColumnWidths( );
        this.Cursor = Cursors.Default;
        StatusMessage.Text = "Idle";
    }

    private void ShowAllCheckBox_CheckedChanged( object sender, EventArgs e )
    {
        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
        _startUp = false;
    }

    private void ScheduleButton_Click( object sender, EventArgs e )
    {
        using UpdateScheduler scheduler = new( );
        scheduler.ShowDialog( this );
    }

    private void RemoteSites_Click( object sender, EventArgs e )
    {
        RemoteSites_SelectionChanged( sender, e );
    }

    private void RemoteSite_Add_Click( object sender, EventArgs e )
    {
        RemoteSiteForm form = new( );
        form.Show( this );
        this.RemoteSites.DataSource = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
    }

    private void RemoteSite_Edit_Click( object sender, EventArgs e )
    {
        if ( RemoteSites.SelectedRows.Count == 1 )
        {
            RemoteSiteForm form = new( )
            {
                DownloadSite = (RemoteSite)RemoteSites.SelectedRows[ 0 ].DataBoundItem
            };

            form.ShowDialog( );
        }
    }

    private void ProcessAllButton_Click( object sender, EventArgs e )
    {
        // ProcessAll also creates firewall entries
        _processAll = true;
        this.Cursor = Cursors.WaitCursor;
        //LoadRules( true );
        List<RemoteSite> allActiveSites = Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked ).ToList( ); // Maintain.ListDownloadSites( null, ShowAllCheckBox.Checked );
        Maintain.ProcessDownloads( allActiveSites, this, true );
        this.Cursor = Cursors.Default;
        _processAll = false;
    }

    private void RemoteSites_MouseDown( object sender, MouseEventArgs e )
    {
        if ( e.Button == MouseButtons.Right )
        {
            if ( this.RemoteSites.SelectedRows.Count > 0 )
            {
                SelectGridViewRowOnRightClick( sender, e );

                // DataGridViewRow selected = this.RemoteSites.SelectedRows[ 0 ];
                ////this.RemoteSites.Rows.Cast<DataGridViewRow>().FirstOrDefault( f => f.)
                System.Drawing.Point position = new( ) { X = e.X, Y = e.Y };
                SiteMenuStrip.Location = position;
                SiteMenuStrip.Top = position.Y;
                SiteMenuStrip.Left = position.X;
                SiteMenuStrip.Show( );
            }
        }
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
