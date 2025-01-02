using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlocklistManager;

partial class RemoteSiteForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
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
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent( )
    {
        this.label1 = new Label( );
        this.label2 = new Label( );
        this.label3 = new Label( );
        this.NameText = new TextBox( );
        this.SiteUrlText = new TextBox( );
        this.FileUrlsArrayText = new TextBox( );
        this.OKButton = new Button( );
        this.CancelButton = new Button( );
        this.label4 = new Label( );
        this.FileTypeComboBox = new ComboBox( );
        this.TestButton = new Button( );
        this.ActiveCheckBox = new CheckBox( );
        this.SuspendLayout( );
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new Point( 23, 19 );
        this.label1.Name = "label1";
        this.label1.Size = new Size( 138, 15 );
        this.label1.TabIndex = 0;
        this.label1.Text = "Name of the remote site:";
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new Point( 21, 52 );
        this.label2.Name = "label2";
        this.label2.Size = new Size( 124, 15 );
        this.label2.TabIndex = 1;
        this.label2.Text = "The remote site's URL:";
        // 
        // label3
        // 
        this.label3.AutoSize = true;
        this.label3.Location = new Point( 22, 90 );
        this.label3.Name = "label3";
        this.label3.Size = new Size( 200, 15 );
        this.label3.TabIndex = 2;
        this.label3.Text = "The full URL(s) of the blocklist file(s):";
        // 
        // NameText
        // 
        this.NameText.Location = new Point( 238, 21 );
        this.NameText.MaxLength = 50;
        this.NameText.Name = "NameText";
        this.NameText.Size = new Size( 251, 23 );
        this.NameText.TabIndex = 3;
        this.NameText.Leave +=  this.NameText_Leave ;
        // 
        // SiteUrlText
        // 
        this.SiteUrlText.Location = new Point( 238, 52 );
        this.SiteUrlText.MaxLength = 255;
        this.SiteUrlText.Name = "SiteUrlText";
        this.SiteUrlText.Size = new Size( 412, 23 );
        this.SiteUrlText.TabIndex = 4;
        this.SiteUrlText.Leave +=  this.SiteUrlText_Leave ;
        // 
        // FileUrlsArrayText
        // 
        this.FileUrlsArrayText.Location = new Point( 237, 91 );
        this.FileUrlsArrayText.Multiline = true;
        this.FileUrlsArrayText.Name = "FileUrlsArrayText";
        this.FileUrlsArrayText.Size = new Size( 740, 46 );
        this.FileUrlsArrayText.TabIndex = 5;
        this.FileUrlsArrayText.Leave +=  this.FileUrlsArrayText_Leave ;
        // 
        // OKButton
        // 
        this.OKButton.DialogResult = DialogResult.OK;
        this.OKButton.Location = new Point( 1061, 127 );
        this.OKButton.Name = "OKButton";
        this.OKButton.Size = new Size( 59, 32 );
        this.OKButton.TabIndex = 6;
        this.OKButton.Text = "O&K";
        this.OKButton.UseVisualStyleBackColor = true;
        this.OKButton.Click +=  this.OKButton_Click ;
        // 
        // CancelButton
        // 
        this.CancelButton.Location = new Point( 1061, 162 );
        this.CancelButton.Name = "CancelButton";
        this.CancelButton.Size = new Size( 61, 34 );
        this.CancelButton.TabIndex = 7;
        this.CancelButton.Text = "&Cancel";
        this.CancelButton.UseVisualStyleBackColor = true;
        this.CancelButton.Click +=  this.CancelButton_Click ;
        // 
        // label4
        // 
        this.label4.AutoSize = true;
        this.label4.Location = new Point( 22, 150 );
        this.label4.Name = "label4";
        this.label4.Size = new Size( 90, 15 );
        this.label4.TabIndex = 8;
        this.label4.Text = "Download type:";
        // 
        // FileTypeComboBox
        // 
        this.FileTypeComboBox.FormattingEnabled = true;
        this.FileTypeComboBox.Items.AddRange( new object[] { "Single column text (default)", "Delimited text", "HTML", "JSON", "XML" } );
        this.FileTypeComboBox.Location = new Point( 238, 146 );
        this.FileTypeComboBox.Name = "FileTypeComboBox";
        this.FileTypeComboBox.Size = new Size( 260, 23 );
        this.FileTypeComboBox.TabIndex = 9;
        this.FileTypeComboBox.Leave +=  this.FileTypeComboBox_Leave ;
        // 
        // TestButton
        // 
        this.TestButton.DialogResult = DialogResult.OK;
        this.TestButton.Location = new Point( 972, 90 );
        this.TestButton.Name = "TestButton";
        this.TestButton.Size = new Size( 59, 47 );
        this.TestButton.TabIndex = 10;
        this.TestButton.Text = "&Validate";
        this.TestButton.UseVisualStyleBackColor = true;
        this.TestButton.Click +=  this.TestButton_Click ;
        // 
        // ActiveCheckBox
        // 
        this.ActiveCheckBox.AutoSize = true;
        this.ActiveCheckBox.CheckAlign = ContentAlignment.MiddleRight;
        this.ActiveCheckBox.Location = new Point( 23, 177 );
        this.ActiveCheckBox.Name = "ActiveCheckBox";
        this.ActiveCheckBox.Size = new Size( 230, 19 );
        this.ActiveCheckBox.TabIndex = 11;
        this.ActiveCheckBox.Text = "&Active                                                         ";
        this.ActiveCheckBox.UseVisualStyleBackColor = true;
        // 
        // RemoteSiteForm
        // 
        this.AutoScaleDimensions = new SizeF( 7F, 15F );
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size( 1146, 208 );
        this.Controls.Add( this.ActiveCheckBox );
        this.Controls.Add( this.TestButton );
        this.Controls.Add( this.FileTypeComboBox );
        this.Controls.Add( this.label4 );
        this.Controls.Add( this.CancelButton );
        this.Controls.Add( this.OKButton );
        this.Controls.Add( this.FileUrlsArrayText );
        this.Controls.Add( this.SiteUrlText );
        this.Controls.Add( this.NameText );
        this.Controls.Add( this.label3 );
        this.Controls.Add( this.label2 );
        this.Controls.Add( this.label1 );
        this.Name = "RemoteSiteForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "RemoteSiteForm";
        this.Load +=  this.RemoteSiteForm_Load ;
        this.ResumeLayout( false );
        this.PerformLayout( );
    }

    #endregion

    private Label label1;
    private Label label2;
    private Label label3;
    private TextBox NameText;
    private TextBox SiteUrlText;
    private TextBox FileUrlsArrayText;
    private Button OKButton;
    private new Button CancelButton;
    private Label label4;
    private ComboBox FileTypeComboBox;
    private Button TestButton;
    private CheckBox ActiveCheckBox;
}