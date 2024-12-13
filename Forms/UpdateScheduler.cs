using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Context;

using Microsoft.Win32.TaskScheduler;

using OSVersionExtension;

using SBS.Utilities;

using static SBS.Utilities.General;


namespace BlocklistManager;

public partial class UpdateScheduler : Form
{
    private List</*OSVersionExtension.OperatingSystem*/OSSchedulerVersion> _compatibleOperatingSystems = [];
    private string _remoteSiteIDs = "AllCurrent";
    private readonly string _applicationDirectory = Assembly.GetExecutingAssembly( ).Location[ 0..Assembly.GetExecutingAssembly( ).Location.LastIndexOf( '\\' ) ];
    private readonly string _applicationShortName = $"{Assembly.GetExecutingAssembly( ).GetName( ).Name}";
    private readonly string _assemblyFullName;
    private readonly string _activeUser;
    private readonly string _taskName = "Update Blocklist Firewall Entries";
    private Version _version = new ( 1, 0 );

    //public Microsoft.Win32.TaskScheduler.Task ScheduledTask { get; set; }

    public UpdateScheduler( )
    {
        InitializeComponent( );
        _assemblyFullName = $"{_applicationDirectory}\\{_applicationShortName}.exe";
        //ScheduledTask = Microsoft.Win32.TaskScheduler.Task;
        List<string> accounts = AdminUsers/*  UserAccounts */.Select( s => $"{s.DomainOrComputerName}\\{s.UserName}" ).ToList( );
        this.AccountsComboBox.DataSource = accounts;
        this.ApplicationName.Text = _applicationShortName;
        this.AuthorLabel.Text = $"Author:  {Environment.UserName}";
        this.LogFolder.Text = $"{_applicationDirectory}\\Log";
        this.Notes.Text = CurrentNotes;
        this.TaskNameLabel.Text = $"Scheduled Task Name:     {_taskName} (Version {_version})";
        this.ArgumentsText.Text = @$"/Sites:AllCurrent /LogPath:{this.LogFolder.Text}";

        _activeUser = accounts.FirstOrDefault( c => c == $"{Environment.UserDomainName}\\{Environment.UserName}" ) ?? $"{Environment.UserDomainName}\\{Environment.UserName}";
    }

    private void UpdateScheduler_Load( object sender, EventArgs e )
    {
        this.FrequencyComboBox.SelectedIndex = 1; // Daily
        this.RecurrenceComboBox.SelectedIndex = 0; // Every x number of <frequency>, default value 1 e.g. every 1 day
        this.LoadSitesComboBox( );

        if ( _activeUser is not null && ((List<string>)this.AccountsComboBox.DataSource! ).Any( a => a == Environment.UserDomainName + '\\' + Environment.UserName ) )
        {
            this.AccountsComboBox.SelectedItem = _activeUser;
            this.AuthorLabel.Text = $"Author:  {_activeUser}";
        }
        else
            this.AuthorLabel.Text = $"Author:  {Environment.UserDomainName}\\{Environment.UserName}";

        ArgumentsText.Text = $"/Sites:{_remoteSiteIDs} /LogPath:{this.LogFolder.Text}";

        // UseExisting( );
        var existing = TaskService.Instance.GetTask( $"{_applicationShortName}\\{_taskName}" );
        if ( existing is not null )
        {
            StartDatePicker.Value = existing.NextRunTime.Date;
            StartTimePicker.Value = new DateTime( existing.NextRunTime.Year, existing.NextRunTime.Month, existing.NextRunTime.Day, existing.NextRunTime.Hour, existing.NextRunTime.Minute, existing.NextRunTime.Second );
            ExecAction action = (ExecAction)existing.Definition.Actions.First();
            if ( action is not null )
            {
                ArgumentsText.Text = action.Arguments;
                LogFolder.Text = action.WorkingDirectory;
                string sitesPart = action.Arguments[ ( action.Arguments.IndexOf( "/sites:", StringComparison.CurrentCultureIgnoreCase ) + 7 ).. ];
                sitesPart = sitesPart[ ..sitesPart.IndexOf( ' ' ) ];
                string[] sites = sitesPart.Split( ';' );

                if ( sites.First( ).Equals( "allcurrent", StringComparison.CurrentCultureIgnoreCase ) )
                    this.SelectAllCheckBox.Checked = true;
                else
                {
                    foreach ( ListViewItem siteEntry in this.SitesList.Items )
                    {
                        siteEntry.Checked = sites.Any( c => c == siteEntry.Tag!.ToString( ) );
                    }
                }
            }

            Trigger trig = existing.Definition.Triggers.First( );
            this.FrequencyComboBox.SelectedItem = trig.TriggerType switch
            {
                TaskTriggerType.Weekly => this.FrequencyComboBox.Items[ 2 ],
                TaskTriggerType.Daily => this.FrequencyComboBox.Items[ 1 ],
                _ => this.FrequencyComboBox.Items[ 0 ],
            };
            
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

        this.TaskNameLabel.Text = $"Scheduled Task Name:     {_taskName} (Version {_version})";
        this.Refresh( );
    }

    private void LoadSitesComboBox( )
    {
        using BlocklistDbContext context = new ( );
        var sites = context.ListRemoteSites( null )
                                         .OrderBy( o => o.Name )
                                         .ToList( );

        IList<ListViewItem> items = sites.Select( s => new ListViewItem( ) { Text = s.Name, Checked = true, Tag = s.ID } )
                                         .ToList( );
        this.SitesList.View = View.List;
        foreach ( var site in sites )
            this.SitesList.Items.Add( new ListViewItem( site.Name ) { Checked = true, Tag = site.ID } );

        this.SelectAllCheckBox.Checked = true;
        this.SelectAllCheckBox.CheckedChanged += this.SelectAllCheckBox_CheckedChanged!;
        this.SitesList.ItemChecked += this.SitesList_ItemChecked!;
    }

    private void CancelButton_Click( object sender, EventArgs e )
    {
        this.Close( );
    }

    private void SitesList_ItemChecked( object sender, ItemCheckedEventArgs e )
    {
        this.SelectAllCheckBox.Checked = AllSitesChecked( );
    }

    private bool AllSitesChecked( ) => this.SitesList.Items.Cast<ListViewItem>( ).Count( c => c.Checked ) == SitesList.Items.Count;

    private void OKButton_Click( object sender, EventArgs e )
    {
        if ( !Directory.Exists( this.LogFolder.Text ) )
            Directory.CreateDirectory( this.LogFolder.Text );

        // This works
        // TaskService.Instance.AddTask( "Test", DefineTrigger( ), new ExecAction( $"{_applicationDirectory}\\{_applicationShortName}.exe", ArgumentsText.Text, LogFolder.Text ));

        var existingTask = TaskService.Instance.FindTask( _applicationShortName );
        //TaskPrincipal principal = new System.Security.Principal.WindowsIdentity( _activeUser );
        
        //if ( existingTask is not null )
        //    TaskService.Instance.RootFolder.DeleteTask( _taskName, false );

        TaskDefinition? taskDefinition = this.CreateTaskDefinition( );

        // Register the task in the scheduler root folder of the local machine
        try
        {
            var folder = TaskService.Instance.GetFolder( _applicationShortName );
            if ( folder is null )
                folder = TaskService.Instance.RootFolder.CreateFolder( _applicationShortName );
            folder.RegisterTaskDefinition( _taskName, taskDefinition, TaskCreation.CreateOrUpdate, System.Security.Principal.WindowsIdentity.GetCurrent( ).Name );
        }
        catch ( Exception ex )
        {
            MessageBox.Show( $"Unable to create the scheduled task.\r\n{ex.Message}" );
        }

        this.Close( );
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
            definition.Actions.Add( $"{_assemblyFullName}", ArgumentsText.Text, LogFolder.Text );
            definition.CanUseUnifiedSchedulingEngine( );
            return definition;
        }
        catch ( Exception ex )
        {
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
        if ( this.FrequencyComboBox.SelectedItem is null )
            return trigger;

        TimeSpan maxDuration = trigger.EndBoundary - trigger.StartBoundary;
        RepetitionPattern repetition = new( TimeSpan.FromDays( 1 ), maxDuration, true );

        switch ( this.FrequencyComboBox.SelectedItem )
        {
            case "Hourly":
                {
                    repetition = new ( TimeSpan.FromHours( 1 /*Convert.ToInt32( this.RecurrenceComboBox.SelectedItem )*/ ), maxDuration, true );
                    repetition.Duration = TimeSpan.FromHours( 1 );
                    break;
                }
            case "Weekly":
                {
                    repetition = new ( TimeSpan.FromDays( 7 /*Convert.ToInt32( this.RecurrenceComboBox.SelectedItem )*/ ), maxDuration, true );
                    repetition.Duration = TimeSpan.FromDays( 7 );
                    break;
                }
            default: // Daily
                {
                    repetition = new ( TimeSpan.FromDays( 1 /*Convert.ToInt32( this.RecurrenceComboBox.SelectedItem )*/ ), maxDuration, true );
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
        if ( this.FrequencyComboBox.SelectedItem!.ToString( ) == "Hourly" )
        {
            trigger = new TimeTrigger( )
            {
                StartBoundary = DetermineStartTime( ),
                Enabled = true,
                EndBoundary = DateTime.MaxValue,
                ExecutionTimeLimit = TimeSpan.FromHours( 2 ),
            };
        }
        else if ( this.FrequencyComboBox.SelectedItem!.ToString( ) == "Weekly" )
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

    private DateTime DetermineStartTime( ) => new 
        (
            StartDatePicker.Value.Date.Year,
            StartDatePicker.Value.Date.Month,
            StartDatePicker.Value.Date.Day,
            StartTimePicker.Value.Hour,
            StartTimePicker.Value.Minute,
            StartTimePicker.Value.Second
        );

    private void DefineRegistrationInfo( TaskDefinition definition )
    {
        definition.RegistrationInfo.Author = _activeUser;
        definition.RegistrationInfo.Version = _version;
        definition.RegistrationInfo.Date = DateTime.Now;
        definition.RegistrationInfo.Description = this.TaskNameLabel.Text[ ( this.TaskNameLabel.Text.IndexOf( ':' ) + 1 ).. ].Trim( );
        definition.RegistrationInfo.Source = Assembly.GetExecutingAssembly( ).GetName( ).Name;
    }

    private void SelectAllCheckBox_CheckedChanged( object sender, EventArgs e )
    {
        if ( this.SelectAllCheckBox.Checked )
        {
            foreach ( ListViewItem item in this.SitesList.Items.Cast<ListViewItem>( ).Where( w => w.Checked != this.SelectAllCheckBox.Checked ) )
            {
                item.Checked = true; // this.SelectAllCheckBox.Checked;
            }
        }

        if ( this.SitesList.Items
                     .Cast<ListViewItem>( )
                     .Count( w => w.Checked ) == this.SitesList.Items.Count )
        {
            _remoteSiteIDs = "AllCurrent";
        }
        else
        {
            _remoteSiteIDs = string.Join( ';', SitesList.Items
                                                        .Cast<ListViewItem>( )
                                                        .Where( w => w.Checked )
                                                        .Select( s => s.Tag )
                                                );
        }

        ArgumentsText.Text = $"/Sites:{_remoteSiteIDs} /LogPath:{this.LogFolder.Text}";
        ArgumentsText.Refresh( );
    }

    private void BrowseFoldersButton_Click( object sender, EventArgs e )
    {
        string appLogPath = $"{Assembly.GetExecutingAssembly( ).Location}\\Log";
        using FolderBrowserDialog dialog = new ( )
        {
            SelectedPath = appLogPath,
            Description = "Select a directory for log files",
            InitialDirectory = appLogPath,
            RootFolder = Environment.SpecialFolder.MyComputer,
            ShowNewFolderButton = true,
        };

        dialog.ShowDialog( this );
        this.LogFolder.Text = dialog.SelectedPath;
        ArgumentsText.Text = $"/Sites:{_remoteSiteIDs} /LogPath:{this.LogFolder.Text}";
        ArgumentsText.Refresh( );
    }

    private void LogFolder_Leave( object sender, EventArgs e )
    {
        ArgumentsText.Text = $"/Sites:{_remoteSiteIDs} /LogPath:{this.LogFolder.Text}";
    }

    private string CurrentNotes
    {
        get
        {
            if ( !CompatibleOSVersion )
            {
                this.OKButton.Enabled = false;
                return $"This application cannot schedule updates under this operating system ({OSVersion.GetOperatingSystem( )})";
            }
            else
                //return $"NOTES:\r\n    The updates task will only run when the computer is logged in;\r\n    The task can only run on computers running Windows;\r\n    The task will be run as Administrator if possible;\r\n    The task will on run if the computer is on AC power and will stop if the computer switches to battery;\r\n    The task can be run on demand in the Windows Task Scheduler;\r\n    The task will be terminated if it runs for longer than 2 hours;\r\n    The task will not start if another instance is already running;\r\n    The task will run under the creating user's account.\r\n\r\n";
                return $"NOTES:\r\n  The updates task will only run when the computer is logged in;\r\n  The task can only run on computers running Windows 7 or later versions;\r\n  The task will be run as Administrator if possible;\r\n  The task will only run when the computer is on AC power and will stop if the computer switches to battery;\r\n  The task can be run on demand in the Windows Task Scheduler;\r\n  The task will be terminated if it runs for longer than 2 hours;\r\n  The task will not start if another instance is already running;\r\n  The task will run under the creating user's account.\r\n";
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
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsVista, CompatibleSchedulerVersion = TaskCompatibility.V2 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2008, CompatibleSchedulerVersion = TaskCompatibility.V2 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2008R2, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows7, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2012, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows8, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows81, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2012R2, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2016, CompatibleSchedulerVersion = TaskCompatibility.V2_1 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2019, CompatibleSchedulerVersion = TaskCompatibility.V2_2 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows10, CompatibleSchedulerVersion = TaskCompatibility.V2_3 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.Windows11, CompatibleSchedulerVersion = TaskCompatibility.V2_3 } );
                _compatibleOperatingSystems.Add( new ( ) { OperatingSystem = OSVersionExtension.OperatingSystem.WindowsServer2022, CompatibleSchedulerVersion = TaskCompatibility.V2_3 } );
            }

            return _compatibleOperatingSystems.Any( c => c.OperatingSystem == OSVersion.GetOperatingSystem( ) );
        }
    }
}

internal class OSSchedulerVersion
{
    internal OSVersionExtension.OperatingSystem OperatingSystem { get; set; }

    internal TaskCompatibility CompatibleSchedulerVersion { get; set; } = TaskCompatibility.V2;
}
