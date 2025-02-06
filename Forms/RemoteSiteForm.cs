using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using SBS.Utilities;

namespace BlocklistManager;

public partial class RemoteSiteForm : Form
{
    [DesignerSerializationVisibility( DesignerSerializationVisibility.Visible )]
    public RemoteSite DownloadSite { get; set; } = new( ) { Name = "", FileUrls = string.Empty, Active = false, FileTypeID = 1, FileType = Maintain.FILETYPES.First( f => f.ID == 1 ) };

    public RemoteSiteForm( )
    {
        InitializeComponent( );
    }

    private void CancelButton_Click( object sender, EventArgs e )
    {
        Close( );
        Dispose( );
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
                    Close( );
                    Dispose( );
                }
            }
        }
    }

    private void RemoteSiteForm_Load( object sender, EventArgs e )
    {
        OKButton.Enabled = false;
        TestButton.Enabled = false;
        ActiveCheckBox.Checked = true;

        if ( DownloadSite != null )
        {
            NameText.Text = DownloadSite.Name;
            SiteUrlText.Text = DownloadSite.SiteUrl;
            FileUrlsArrayText.Text = DownloadSite.FilePaths[ 0 ];
            for ( int i = 1; i < DownloadSite.FilePaths.Count; i++ )
            {
                FileUrlsArrayText.Text += Environment.NewLine + DownloadSite.FilePaths[ i ];
            }

            ActiveCheckBox.Checked = DownloadSite.Active;
        }

        FileTypeComboBox.DisplayMember = "Description";
        FileTypeComboBox.DataSource = Maintain.FILETYPES;
        FileTypeComboBox.SelectedItem = FileTypeComboBox.Items.Cast<FileType>( ).First( f => f.ID == 1 );
    }

    private void TestButton_Click( object sender, EventArgs e )
    {
        try
        {
            OKButton.Enabled = false;
            if ( SiteUrlIsValid( ) )
            {
                Cursor = Cursors.WaitCursor;
                var data = Maintain.DownloadBlocklists( null, [ DownloadSite ] );
                Cursor = Cursors.Default;

                if ( data is not null && data.Count > 0 )
                {
                    MessageBox.Show( "Data was collected successfully" );
                    OKButton.Enabled = true;
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
            bool siteUrlIsValid = Maintain.UrlHostExists( SiteUrlText.Text );
            if ( !siteUrlIsValid )
            {
                OKButton.Enabled = false;
                MessageBox.Show( $"The download site's URL ('{SiteUrlText.Text}') is invalid\r\nPlease correct this and try again." );
            }

            return siteUrlIsValid;
        }
        catch
        {
            OKButton.Enabled = false;
            MessageBox.Show( $"The download site's URL ('{SiteUrlText.Text}') is invalid\r\nPlease correct this and try again." );
            return false;
        }
    }

    private void NameText_Leave( object sender, EventArgs e )
    {
        DownloadSite.Name = NameText.Text;
    }

    private void SiteUrlText_Leave( object sender, EventArgs e )
    {
        DownloadSite.SiteUrl = SiteUrlText.Text;
    }

    private void FileUrlsArrayText_Leave( object sender, EventArgs e )
    {
        TestButton.Enabled = false;
        try
        {
            DownloadSite.FileUrls = string.Join( ',', FileUrlsArrayText.Text.Split( ',' ) );
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
            DownloadSite.FileTypeID = ( (FileType)FileTypeComboBox.SelectedItem ).ID;
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
