using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BlocklistManager.Classes;

namespace BlocklistManager.Forms;

internal partial class TestOutputForm : Form
{
    private const int SAMPLE_SIZE = 100;
    private List<CandidateEntry> sample = [];
    //{
    //    get
    //    {
    //        return this.Data.Take( SAMPLE_SIZE ).ToList();
    //    }
    //}

    internal List<CandidateEntry> Data
    {
        get
        {
            return sample;
        }

        set
        {
            sample = value.Take( SAMPLE_SIZE ).ToList();
        }
    }

    internal TestOutputForm( )
    {
        InitializeComponent( );
        SampleDescription.Text = $"Only the first {SAMPLE_SIZE} entries are shown here";
        SampleDescription.Refresh( );
    }

    private void CloseButton_Click( object sender, EventArgs e )
    {
        this.Close( );
    }

    private void TestOutputForm_Load( object sender, EventArgs e )
    {
        this.Cursor = Cursors.WaitCursor;
        OutputList.DataSource = sample.Select( s => new { s.Name, s.IPAddress, s.IPAddressRange, s.Ports, s.Protocol } ).ToList();
        OutputList.AutoGenerateColumns = true;
        OutputList.Refresh( );
        this.Cursor = Cursors.Default;
    }
}
