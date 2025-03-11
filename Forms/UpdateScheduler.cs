using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Globalization;


//using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using Microsoft.Win32.TaskScheduler;

using OSVersionExtension;

namespace BlocklistManager;

/// <summary>
/// User interface to aid with creating a scheduled task to run this application
/// </summary>
public partial class UpdateScheduler : Form
{
    private List</*OSVersionExtension.OperatingSystem*/OSSchedulerVersion> _compatibleOperatingSystems = [];
    private string[] _remoteSiteIDs = [ "AllCurrent" ];
    [UnconditionalSuppressMessage( "SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "<Pending>" )]
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly( );
    private readonly string _applicationDirectory = assembly.Location[ 0..Assembly.GetExecutingAssembly( ).Location.LastIndexOf( '\\' ) ];
    private readonly string _applicationShortName = assembly/* $"{Assembly.GetExecutingAssembly( )*/.GetName( )!.Name!;
    private readonly string _assemblyFullName;
    private readonly string _activeUser;
    private readonly string _taskName = "Update Blocklist Firewall Entries";
    private Version _version = new Version( "1.0" );
    private readonly List<UserAccount> _adminUsers = [];

    //public Microsoft.Win32.TaskScheduler.Task ScheduledTask { get; set; }

    public UpdateScheduler( )
    {
        InitializeComponent( );

        _assemblyFullName = $"{_applicationDirectory}\\{_applicationShortName}.exe"; // or Assembly.GetExecutingAssembly().Location.Replace( ".dll", ".exe" )
        List<string> accounts = [];
        try
        {
            accounts = GetAdminUsers( ).Select( s => $"{s.DomainOrComputerName}\\{s.UserName}" ).ToList( );
            if ( accounts.Count < 1 )
                accounts.Add( $"{Environment.UserDomainName}\\{Environment.UserName}" );
        }
        catch ( Exception ex )
        {
            if ( accounts.Count < 1 )
            {
                GetAdminUsersAlternative( accounts );
            }

            if ( accounts.Count < 1 )
            {
                accounts.Add( $"{Environment.UserDomainName}\\{Environment.UserName}" );
                string message = StringUtilities.ExceptionMessage( "UpdateScheduler", ex );
                Logger.Log( "UpdateScheduler (Retrieving a list of local administrators failed):", message );
            }
        }

        AccountsComboBox.DataSource = accounts;
        ApplicationName.Text = _applicationShortName;
        AuthorLabel.Text = $"Author:  {Environment.UserName}";
        LogFolder.Text = Maintain.LogFileFullname[ 0..Maintain.LogFileFullname.LastIndexOf( '\\' ) ]; // $"{_applicationDirectory}\\Log";
        Notes.Text = CurrentNotes;
        ArgumentsText.Text = $"/Sites:AllCurrent /LogPath:\"{LogFolder.Text}\"";

        _activeUser = accounts.FirstOrDefault( c => c == $"{Environment.UserDomainName}\\{Environment.UserName}" ) ?? $"{Environment.UserDomainName}\\{Environment.UserName}";
    }

    private static void GetAdminUsersAlternative( List<string> accounts )
    {
        DirectoryEntry localMachine = new DirectoryEntry( "WinNT://" + Environment.MachineName );
        DirectoryEntry admGroup = localMachine.Children.Find( "administrators", "group" );
        object? members = admGroup.Invoke( "members", null );
        if ( members is not null )
        {
            foreach ( object groupMember in (IEnumerable)members )
            {
                DirectoryEntry member = new DirectoryEntry( groupMember );
                string[] pathParts = member.Path.Split( '/' );
                accounts.Add( pathParts[ 3 ] + "\\" + member.Name );
            }
        }
    }

    [RequiresUnreferencedCode( "Calls BlocklistManager.UpdateScheduler.LoadSitesComboBox()" )]
    [RequiresDynamicCode( "Calls BlocklistManager.UpdateScheduler.LoadSitesComboBox()" )]
    private void UpdateScheduler_Load( object sender, EventArgs e )
    {
        FrequencyComboBox.SelectedIndex = 1; // Daily
        RecurrenceComboBox.SelectedIndex = 0; // Every x number of <frequency>, default value 1 e.g. every 1 day
        LoadSitesComboBox( );
        SetAuthorLabel( );
        ArgumentsText.Text = $"/Sites:{string.Join( ';', _remoteSiteIDs )} /LogPath:\"{LogFolder.Text}\"";

        // UseExisting( );
        var existing = TaskService.Instance.GetTask( $"{_applicationShortName}\\{_taskName}" );
        if ( existing is not null )
        {
            StartDatePicker.Value = existing.NextRunTime.Date;
            StartTimePicker.Value = new DateTime( existing.NextRunTime.Year, existing.NextRunTime.Month, existing.NextRunTime.Day, existing.NextRunTime.Hour, existing.NextRunTime.Minute, existing.NextRunTime.Second );
            ExecAction action = (ExecAction)existing.Definition.Actions.First( );
            if ( action is not null )
            {
                ArgumentsText.Text = action.Arguments; //.Replace( "\"",  string.Empty );
                LogFolder.Text = $"{action.WorkingDirectory.Replace( "\"", string.Empty )}";
                string sitesPart = action.Arguments[ ( action.Arguments.IndexOf( "/sites:", StringComparison.CurrentCultureIgnoreCase ) + 7 ).. ];
                sitesPart = sitesPart[ ..sitesPart.IndexOf( ' ' ) ];
                string[] sites = sitesPart.Split( ';' );

                if ( sites.First( ).Equals( "allcurrent", StringComparison.OrdinalIgnoreCase ) )
                    SelectAllCheckBox.Checked = true;
                else
                {
                    foreach ( ListViewItem siteEntry in SitesList.Items )
                    {
                        siteEntry.Checked = sites.Any( c => c == siteEntry.Tag!.ToString( ) );
                    }
                }
            }

            Trigger trig = existing.Definition.Triggers.First( );
            FrequencyComboBox.SelectedItem = trig.TriggerType switch
            {
                TaskTriggerType.Weekly => FrequencyComboBox.Items[ 2 ],
                TaskTriggerType.Daily => FrequencyComboBox.Items[ 1 ],
                _ => FrequencyComboBox.Items[ 0 ],
            };

            CalculateTaskVersionNumber( existing! );
        }

        TaskNameLabel.Text = $"Scheduled Task Name:     {_taskName} (Task Version {_version})";
        Refresh( );
    }

    private void SetAuthorLabel( )
    {
        if ( _activeUser is not null && ( (List<string>)AccountsComboBox.DataSource! ).Any( a => a == Environment.UserDomainName + '\\' + Environment.UserName ) )
        {
            AccountsComboBox.SelectedItem = _activeUser;
            AuthorLabel.Text = $"Author:  {_activeUser}";
        }
        else
            AuthorLabel.Text = $"Author:  {Environment.UserDomainName}\\{Environment.UserName}";
    }

    private void CalculateTaskVersionNumber( Task existing )
    {
        _version = existing.Definition.RegistrationInfo.Version;
        if ( _version is null )
            _version = new Version( 1, 0 );
        else
        {
            if ( _version.Minor >= 9 )
                _version = new Version( _version.Major + 1, 0 );
            else
                _version = new Version( _version.Major, _version.Minor + 1 );
        }
    }

    [RequiresUnreferencedCode( "Calls BlocklistManager.Classes.BlocklistData.ListDownloadSites(Int32, RemoteSite, Boolean)" )]
    [RequiresDynamicCode( "Calls BlocklistManager.Classes.BlocklistData.ListDownloadSites(Int32, RemoteSite, Boolean)" )]
    private void LoadSitesComboBox( )
    {
        if ( Maintain.ConnectedDevice is null )
        {
            MessageBox.Show( "Unable to determine your network address.  Please connect a device and try again." );
            return;
        }

        List<RemoteSite> sites = new BlocklistData( ).ListDownloadSites( Maintain.ConnectedDevice!.ID, null );
        SitesList.View = View.List;
        foreach ( var site in sites )
        {
            SitesList.Items.Add( new ListViewItem( site.Name ) { Checked = true, Tag = site.ID } );
        }

        _remoteSiteIDs = [ "AllCurrent" ];
        //_remoteSiteIDs = sites.OrderBy( o => o.ID )
        //                      .Select( s => s.ID.As<string>( ) )
        //                      .ToArray( );
        SelectAllCheckBox.Checked = true;
        SelectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged!;
        SitesList.ItemChecked += SitesList_ItemChecked!;
    }

    private void CancelButton_Click( object sender, EventArgs e )
    {
        Close( );
    }

    private void SitesList_ItemChecked( object sender, ItemCheckedEventArgs e )
    {
        UpdateSiteIDsList( );
        //if ( SitesList.Items
        //             .Cast<ListViewItem>( )
        //             .Count( w => w.Checked ) == SitesList.Items.Count )
        //{
        //    _remoteSiteIDs = [ "AllCurrent" ];
        //}
        //else
        //{
        //    _remoteSiteIDs = SitesList.Items
        //                              .Cast<ListViewItem>( )
        //                              .Where( w => w.Checked )
        //                              .OrderBy( o => Convert.ToInt32( o.Tag, CultureInfo.InvariantCulture ) )
        //                              .Select( s => Convert.ToString( s.Tag, CultureInfo.InvariantCulture )! )
        //                              .ToArray( );
        //}
    }

    private bool AllSitesChecked( )
    {
        return SitesList.Items.Cast<ListViewItem>( ).Count( c => c.Checked ) == SitesList.Items.Count;
    }

    private void OKButton_Click( object sender, EventArgs e )
    {
        if ( !Directory.Exists( LogFolder.Text.Replace( "\"", string.Empty ) ) )
            Directory.CreateDirectory( LogFolder.Text );

        // Register the task in the BlocklistManager folder of the local machine
        TaskDefinition? taskDefinition = CreateTaskDefinition( );
        try
        {
            var folder = TaskService.Instance.GetFolder( _applicationShortName );
            folder ??= TaskService.Instance.RootFolder.CreateFolder( _applicationShortName );
            folder.RegisterTaskDefinition( _taskName, taskDefinition, TaskCreation.CreateOrUpdate, System.Security.Principal.WindowsIdentity.GetCurrent( ).Name );
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "UpdateScheduler", ex.Message );
            MessageBox.Show( $"Unable to create the scheduled task.\r\n{ex.Message}" );
        }

        Close( );
    }

    private TaskDefinition? CreateTaskDefinition( )
    {
        try
        {
            using TaskService service = new( );
            TaskDefinition definition = service.NewTask( );
            //definition.Principal.UserId = _activeUser;
            definition.Principal.LogonType = TaskLogonType.ServiceAccount;
            definition.Principal.UserId = System.Security.Principal.WindowsIdentity.GetCurrent( ).Name;
            definition.Principal.RunLevel = TaskRunLevel.Highest;
            DefineRegistrationInfo( definition );
            definition.Triggers.Add( DefineTrigger( ) );
            DefineSettings( ref definition );
            definition.Actions.Add( $"\"{_assemblyFullName}\"", ArgumentsText.Text, $"{_applicationDirectory}" );
            definition.CanUseUnifiedSchedulingEngine( );
            return definition;
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "CreateTaskDefinition", ex.Message );
            MessageBox.Show( StringUtilities.ExceptionMessage( "CreateTaskDefinition", ex ) );
            return null;
        }
    }

    private void DefineSettings( ref TaskDefinition definition )
    {
        var operatingSystemMatch = _compatibleOperatingSystems.FirstOrDefault( f => f.OperatingSystem == OSVersion.GetOperatingSystem( ) );
        if ( operatingSystemMatch is null )
            definition.Settings.Compatibility = TaskCompatibility.V2; // V2 should cover everything since Vista
        else
            definition.Settings.Compatibility = operatingSystemMatch.CompatibleSchedulerVersion; // TODO: Try to improve this 

        definition.Settings.AllowDemandStart = true;
        definition.Settings.AllowHardTerminate = true;
        definition.Settings.DisallowStartIfOnBatteries = true;
        definition.Settings.Enabled = true;
        definition.Settings.ExecutionTimeLimit = TimeSpan.FromHours( 2 );
        definition.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
        definition.Settings.RestartCount = 2;
        // definition.Settings.RestartInterval = TimeSpan.FromSeconds( 30 ); // Throws a weird exception
        // definition.Settings.RunOnlyIfLoggedOn = true; // Compatibility issue but seems to default to this anyway
        definition.Settings.RunOnlyIfNetworkAvailable = true;
        definition.Settings.StartWhenAvailable = true;
        definition.Settings.StopIfGoingOnBatteries = true;
        definition.Settings.WakeToRun = false;
    }

    private Trigger DefineTrigger( )
    {
        Trigger trigger = DefineTriggerBase( );
        if ( FrequencyComboBox.SelectedItem is null )
            return trigger;

        TimeSpan maxDuration = trigger.EndBoundary - trigger.StartBoundary;
        RepetitionPattern repetition; // = new( TimeSpan.FromDays( 1 ), maxDuration, true );

        switch ( FrequencyComboBox.SelectedItem )
        {
            case "Hourly":
                {
                    repetition = new( TimeSpan.FromHours( 1 /*Convert.ToInt32( this.RecurrenceComboBox.SelectedItem )*/ ), maxDuration, true );
                    repetition.Duration = TimeSpan.FromHours( 1 );
                    break;
                }
            case "Weekly":
                {
                    repetition = new( TimeSpan.FromDays( 7 /*Convert.ToInt32( this.RecurrenceComboBox.SelectedItem )*/ ), maxDuration, true );
                    repetition.Duration = TimeSpan.FromDays( 7 );
                    break;
                }
            default: // Daily
                {
                    repetition = new( TimeSpan.FromDays( 1 /*Convert.ToInt32( this.RecurrenceComboBox.SelectedItem )*/ ), maxDuration, true );
                    repetition.Duration = TimeSpan.FromDays( 1 );
                    break;
                }
        }

        trigger.Repetition = repetition; // new RepetitionPattern( TimeSpan.FromDays( 1 ), maxDuration, true ) ;
        return trigger;
    }

    private Trigger DefineTriggerBase( )
    {
        Trigger trigger;
        if ( FrequencyComboBox.SelectedItem!.ToString( ) == "Hourly" )
        {
            trigger = new TimeTrigger( )
            {
                StartBoundary = DetermineStartTime( ),
                Enabled = true,
                EndBoundary = DateTime.MaxValue,
                ExecutionTimeLimit = TimeSpan.FromHours( 2 ),
            };
        }
        else if ( FrequencyComboBox.SelectedItem!.ToString( ) == "Weekly" )
        {
            trigger = new WeeklyTrigger
            {
                StartBoundary = DetermineStartTime( ),
                Enabled = true,
                EndBoundary = DateTime.MaxValue,
                ExecutionTimeLimit = TimeSpan.FromHours( 2 ),
            };
        }
        else // if ( this.FrequencyComboBox.SelectedItem!.ToString( ) == "Daily" )
        {
            trigger = new DailyTrigger( )
            {
                StartBoundary = DetermineStartTime( ),
                Enabled = true,
                EndBoundary = DateTime.MaxValue,
                ExecutionTimeLimit = TimeSpan.FromHours( 2 ),
            };
        }

        return trigger;
    }

    private DateTime DetermineStartTime( )
    {
        return new
        (
            StartDatePicker.Value.Date.Year,
            StartDatePicker.Value.Date.Month,
            StartDatePicker.Value.Date.Day,
            StartTimePicker.Value.Hour,
            StartTimePicker.Value.Minute,
            StartTimePicker.Value.Second
        );
    }

    private void DefineRegistrationInfo( TaskDefinition definition )
    {
        definition.RegistrationInfo.Author = _activeUser;
        definition.RegistrationInfo.Version = _version;
        definition.RegistrationInfo.Date = DateTime.Now;
        definition.RegistrationInfo.Description = TaskNameLabel.Text[ ( TaskNameLabel.Text.IndexOf( ':' ) + 1 ).. ].Trim( );
        definition.RegistrationInfo.Source = Assembly.GetExecutingAssembly( ).GetName( ).Name;
    }

    private void SelectAllCheckBox_CheckedChanged( object sender, EventArgs e )
    {

        SelectAllCheckBox.CheckedChanged -= SelectAllCheckBox_CheckedChanged!;
        SelectAllCheckBox.Checked = !SelectAllCheckBox.Checked;
        SelectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged!;
        if ( SelectAllCheckBox.Checked )
        {
            foreach ( ListViewItem item in SitesList.Items.Cast<ListViewItem>( ).Where( w => w.Checked != SelectAllCheckBox.Checked ) )
            {
                item.Checked = true; // this.SelectAllCheckBox.Checked;
            }

            _remoteSiteIDs = [ "AllCurrent" ];
        }

        //UpdateSiteIDsList( );
        //if ( SitesList.Items
        //             .Cast<ListViewItem>( )
        //             .Count( w => w.Checked ) == SitesList.Items.Count )
        //{
        //    _remoteSiteIDs = [ "AllCurrent" ];
        //}
        //else
        //{
        //    _remoteSiteIDs = SitesList.Items
        //                              .Cast<ListViewItem>( )
        //                              .Where( w => w.Checked )
        //                              .Select( s => Convert.ToString( s.Tag, CultureInfo.InvariantCulture )! )
        //                              .ToArray( );
        //}

        //ArgumentsText.Text = $"/Sites:{string.Join( ';', _remoteSiteIDs )} /LogPath:\"{LogFolder.Text}\"";
        //ArgumentsText.Refresh( );
    }

    private void UpdateSiteIDsList( )
    {
        if ( SitesList.Items
                     .Cast<ListViewItem>( )
                     .Count( w => w.Checked ) == SitesList.Items.Count )
        {
            _remoteSiteIDs = [ "AllCurrent" ];
        }
        else
        {
            _remoteSiteIDs = SitesList.Items
                                      .Cast<ListViewItem>( )
                                      .Where( w => w.Checked )
                                      .OrderBy( o => Convert.ToInt32( o.Tag, CultureInfo.InvariantCulture ) )
                                      .Select( s => Convert.ToString( s.Tag, CultureInfo.InvariantCulture )! )
                                      .ToArray( );
        }

        SelectAllCheckBox.Checked = AllSitesChecked( );
        ArgumentsText.Text = $"/Sites:{string.Join( ';', _remoteSiteIDs )} /LogPath:\"{LogFolder.Text}\"";
        ArgumentsText.Refresh( );
    }

    [RequiresAssemblyFiles( "Calls System.Reflection.Assembly.Location" )]
    private void BrowseFoldersButton_Click( object sender, EventArgs e )
    {
        string appLogPath = $"{Assembly.GetExecutingAssembly( ).Location}\\Log";
        using FolderBrowserDialog dialog = new( )
        {
            SelectedPath = appLogPath,
            Description = "Select a directory for log files",
            InitialDirectory = appLogPath,
            RootFolder = Environment.SpecialFolder.MyComputer,
            ShowNewFolderButton = true,
        };

        dialog.ShowDialog( this );
        LogFolder.Text = $"{dialog.SelectedPath}";
        ArgumentsText.Text = $"/Sites:{string.Join( ';', _remoteSiteIDs )} /LogPath:\"{LogFolder.Text}\"";
        ArgumentsText.Refresh( );
    }

    private void LogFolder_Leave( object sender, EventArgs e )
    {
        ArgumentsText.Text = $"/Sites:{string.Join( ';', _remoteSiteIDs )} /LogPath:\"{LogFolder.Text}\"";
    }

    private void FrequencyComboBox_SelectedIndexChanged( object sender, EventArgs e )
    {
        RecurrenceLabel.Text = FrequencyComboBox.SelectedItem!.ToString( ) switch
        {
            "Hourly" => "hour(s)",
            "Weekly" => "week(s)",
            _ => "day(s)",
        };

        RecurrenceComboBox.Items.Clear( );
        RecurrenceComboBox.Items.AddRange( FrequencyComboBox.SelectedItem!.ToString( ) switch
        {
            "Hourly" => [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 ],
            "Weekly" => [ 1, 2, 3, 4, 5 ],
            _ => [ 1, 2, 3, 4, 5, 6 ],
        } );
    }

    private string CurrentNotes
    {
        get
        {
            if ( !CompatibleOSVersion )
            {
                OKButton.Enabled = false;
                return $"This application cannot schedule updates under this operating system ({OSVersion.GetOperatingSystem( )})";
            }
            else
                //return $"NOTES:\r\n    The updates task will only run when the computer is logged in;\r\n    The task can only run on computers running Windows;\r\n    The task will be run as Administrator if possible;\r\n    The task will on run if the computer is on AC power and will stop if the computer switches to battery;\r\n    The task can be run on demand in the Windows Task Scheduler;\r\n    The task will be terminated if it runs for longer than 2 hours;\r\n    The task will not start if another instance is already running;\r\n    The task will run under the creating user's account.\r\n\r\n";
                return $"NOTES:\r\n  The updates task will only run when the computer is logged in;\r\n  The task can only run on computers running Windows 7 or later versions;\r\n  The task will be run as Administrator if possible;\r\n  The task will only run when the computer is on AC power and will stop if the computer switches to battery;\r\n  The task can be run on demand in the Windows Task Scheduler;\r\n  The task will be terminated if it runs for longer than 2 hours;\r\n  The task will not start if another instance is already running;\r\n  The task will be created/updated under the creating user's account ({Environment.UserDomainName}\\{Environment.UserName}).\r\n";
        }
    }

    /// <summary>
    /// Try to find a list with better version matching ( I pretty much guessed these )
    /// </summary>
    private bool CompatibleOSVersion
    {
        get
        {
            if ( _compatibleOperatingSystems.Count < 1 )
            {
                _compatibleOperatingSystems = [];
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsVista, CompatibleSchedulerVersion = TaskCompatibility.V2 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2008, CompatibleSchedulerVersion = TaskCompatibility.V2 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2008R2, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows7, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2012, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows8, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows81, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2012R2, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2016, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2019, CompatibleSchedulerVersion = TaskCompatibility.V2_2 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows10, CompatibleSchedulerVersion = TaskCompatibility.V2_3 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows11, CompatibleSchedulerVersion = TaskCompatibility.V2_3 } );
                _compatibleOperatingSystems.Add( new( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2022, CompatibleSchedulerVersion = TaskCompatibility.V2_3 } );
            }

            return _compatibleOperatingSystems.Any( c => c.OperatingSystem == OSVersion.GetOperatingSystem( ) );
        }
    }

    /// <summary>
    /// Fetch a list of active local administrators using PowerShell
    /// </summary>
    public static List<UserAccount> GetAdminUsers( )
    {
        try
        {
            List<UserAccount> users = [];
            using ( PrincipalContext ctx = new PrincipalContext( ContextType.Machine ) )
            {
                if ( ctx is null )
                {
                    Logger.Log( "GetAdminUsers", "Principal FAILED." );
                    return users;
                }

                using ( GroupPrincipal grpHost = new GroupPrincipal( ctx ) )
                {
                    if ( grpHost is null )
                    {
                        Logger.Log( "GetAdminUsers", "Group Principal FAILED." );
                        return users;
                    }

                    var grp = GroupPrincipal.FindByIdentity( ctx, IdentityType.Name, "Administrators" );
                    if ( grp is not null )
                    {
                        List<AuthenticablePrincipal> allAdmins = grp.GetMembers( recursive: true )
                                                                    .Cast<AuthenticablePrincipal>( )
                                                                    .Where( w => w.Enabled != false )
                                                                    .Where( static w => w.AccountExpirationDate is null || w.AccountExpirationDate > DateTime.Today )
                                                                    .ToList( );

                        foreach ( AuthenticablePrincipal p in allAdmins )
                        {
                            users.Add( new( ctx.ConnectedServer, p.Name ) );
                        }
                    }

                    return users;
                }
            }
        }
        catch ( Exception ex ) // It's not getting here
        {
            Logger.Log( "GetAdminUsers", ex.Message );
            if ( ex.InnerException is not null )
            {
                Logger.Log( "GetAdminUsers", ex.InnerException.Message );
                if ( ex.InnerException.InnerException is not null )
                {
                    Logger.Log( "GetAdminUsers", ex.InnerException.InnerException.Message );
                }
            }
            Logger.Log( "AdminUsers", ex.StackTrace! );
            return [];
        }
    }
}

internal sealed class OSSchedulerVersion
{
    internal OSVersionExtension.OperatingSystem OperatingSystem { get; set; }

    internal TaskCompatibility CompatibleSchedulerVersion { get; set; } = TaskCompatibility.V2;
}

public record UserAccount( string DomainOrComputerName, string UserName );
