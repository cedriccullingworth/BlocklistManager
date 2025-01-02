using System;
using System.Linq;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using SBS.Utilities;

namespace BlocklistManager;

public partial class RemoteSiteForm : Form
{
    public RemoteSite DownloadSite { get; set; } = new( ) { Name = "", FileUrls = string.Empty, Active = false, FileTypeID = 1, FileType = Maintain.FILETYPES.First( f => f.ID == 1 ) };

    public RemoteSiteForm( )
    {
        InitializeComponent( );
    }

    private void CancelButton_Click( object sender, EventArgs e )
    {
        this.Close( );
        this.Dispose( );
    }

    private void OKButton_Click( object sender, EventArgs e )
    {
        if ( SiteUrlIsValid( ) )
        {
            RemoteSite site = new( )
            {
                Name = NameText.Text,
                SiteUrl = SiteUrlText.Text,
                FileUrls = String.Join( $",", FileUrlsArrayText.Lines ),
                // FileType = (FileType)FileTypeComboBox.SelectedItem!,
                FileTypeID = ( (FileType)FileTypeComboBox.SelectedItem! ).ID,
                Active = ActiveCheckBox.Checked,
                MinimumIntervalMinutes = 0,
            };

            if ( Maintain.ValidRemoteSite( site ) )
            {
                if ( Maintain.AddRemoteSite( site ) is not null )
                {
                    MessageBox.Show( $"{site.Name} was saved successfully, ID {site.ID}" );
                    this.Close( );
                    this.Dispose( );
                }
            }
        }
    }

    private void RemoteSiteForm_Load( object sender, EventArgs e )
    {
        this.OKButton.Enabled = false;
        this.TestButton.Enabled = false;
        this.ActiveCheckBox.Checked = true;

        if ( this.DownloadSite != null )
        {
            this.NameText.Text = this.DownloadSite.Name;
            this.SiteUrlText.Text = this.DownloadSite.SiteUrl;
            this.FileUrlsArrayText.Text = this.DownloadSite.FilePaths[ 0 ];
            for ( int i = 1; i < this.DownloadSite.FilePaths.Count; i++ )
            {
                this.FileUrlsArrayText.Text += Environment.NewLine + this.DownloadSite.FilePaths[ i ];
            }

            this.ActiveCheckBox.Checked = this.DownloadSite.Active;
        }

        this.FileTypeComboBox.DisplayMember = "Description";
        this.FileTypeComboBox.DataSource = Maintain.FILETYPES;
        this.FileTypeComboBox.SelectedItem = this.FileTypeComboBox.Items.Cast<FileType>( ).First( f => f.ID == 1 );
    }

    private void TestButton_Click( object sender, EventArgs e )
    {
        try
        {
            this.OKButton.Enabled = false;
            if ( SiteUrlIsValid( ) )
            {
                this.Cursor = Cursors.WaitCursor;
                var data = Maintain.DownloadBlocklists( null, [ DownloadSite ] );
                this.Cursor = Cursors.Default;

                if ( data is not null && data.Count > 0 )
                {
                    MessageBox.Show( "Data was collected successfully" );
                    this.OKButton.Enabled = true;
                }
            }
        }
        catch ( Exception ex )
        {
            MessageBox.Show( $"Download test failed\r\n{StringUtilities.ExceptionMessage( "Test", ex )}" );
        }
    }

    private bool SiteUrlIsValid( )
    {
        try
        {
            bool siteUrlIsValid = Maintain.UrlHostExists( this.SiteUrlText.Text );
            if ( !siteUrlIsValid )
            {
                this.OKButton.Enabled = false;
                MessageBox.Show( $"The download site's URL ('{this.SiteUrlText.Text}') is invalid\r\nPlease correct this and try again." );
            }

            return siteUrlIsValid;
        }
        catch
        {
            this.OKButton.Enabled = false;
            MessageBox.Show( $"The download site's URL ('{this.SiteUrlText.Text}') is invalid\r\nPlease correct this and try again." );
            return false;
        }
    }

    private void NameText_Leave( object sender, EventArgs e )
    {
        DownloadSite.Name = this.NameText.Text;
    }

    private void SiteUrlText_Leave( object sender, EventArgs e )
    {
        DownloadSite.SiteUrl = this.SiteUrlText.Text;
    }

    private void FileUrlsArrayText_Leave( object sender, EventArgs e )
    {
        TestButton.Enabled = false;
        try
        {
            DownloadSite.FileUrls = string.Join( ',', this.FileUrlsArrayText.Text.Split( ',' ) );
            if ( DownloadSite.FileUrls.Length > 0 )
                TestButton.Enabled = true;
        }
        catch
        {
            MessageBox.Show( "Multiple URLS should be separated by commas" );
        }
    }

    private void FileTypeComboBox_Leave( object sender, EventArgs e )
    {
        if ( FileTypeComboBox.SelectedItem is null )
            MessageBox.Show( "Select a file type" );
        else
            DownloadSite.FileTypeID = ( (FileType)this.FileTypeComboBox.SelectedItem ).ID;
    }

    //private void AddFileUrlButton_Click(object sender, EventArgs e)
    //{
    //    if (FileUrlsArrayText.Text != string.Empty && !FileUrlsArrayText.Multiline)
    //        FileUrlsArrayText.Multiline = true;

    //    if (FileUrlsArrayText.Lines[FileUrlsArrayText.Lines.Length - 1].TrimEnd() != string.Empty)
    //        FileUrlsArrayText.Lines.Append($";{Environment.NewLine}");

    //    FileUrlsArrayText.Lines[FileUrlsArrayText.Lines.Length - 1] = string.Empty;
    //}
}
