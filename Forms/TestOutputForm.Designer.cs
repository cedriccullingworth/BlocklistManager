namespace BlocklistManager.Forms
{
    partial class TestOutputForm
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
            this.OKButton = new System.Windows.Forms.Button( );
            this.OutputList = new System.Windows.Forms.DataGridView( );
            this.SampleDescription = new System.Windows.Forms.Label( );
            ( (System.ComponentModel.ISupportInitialize)this.OutputList ).BeginInit( );
            this.SuspendLayout( );
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point( 498, 399 );
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size( 59, 32 );
            this.OKButton.TabIndex = 0;
            this.OKButton.Text = "&Close";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click +=  this.CloseButton_Click ;
            // 
            // OutputList
            // 
            this.OutputList.AllowUserToAddRows = false;
            this.OutputList.AllowUserToDeleteRows = false;
            this.OutputList.AllowUserToOrderColumns = true;
            this.OutputList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.OutputList.BackgroundColor = System.Drawing.SystemColors.Control;
            this.OutputList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.OutputList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.OutputList.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.OutputList.Location = new System.Drawing.Point( 35, 41 );
            this.OutputList.Name = "OutputList";
            this.OutputList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.OutputList.Size = new System.Drawing.Size( 447, 390 );
            this.OutputList.TabIndex = 1;
            // 
            // SampleDescription
            // 
            this.SampleDescription.AutoSize = true;
            this.SampleDescription.Location = new System.Drawing.Point( 35, 22 );
            this.SampleDescription.Name = "SampleDescription";
            this.SampleDescription.Size = new System.Drawing.Size( 202, 15 );
            this.SampleDescription.TabIndex = 2;
            this.SampleDescription.Text = "Description of data sample goes here";
            // 
            // TestOutputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 7F, 15F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 579, 456 );
            this.Controls.Add( this.SampleDescription );
            this.Controls.Add( this.OutputList );
            this.Controls.Add( this.OKButton );
            this.Name = "TestOutputForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = " Preview Download Data Sample";
            this.Load +=  this.TestOutputForm_Load ;
            ( (System.ComponentModel.ISupportInitialize)this.OutputList ).EndInit( );
            this.ResumeLayout( false );
            this.PerformLayout( );
        }

        #endregion

        private System.Windows.Forms.Button OKButton;
        internal System.Windows.Forms.DataGridView OutputList;
        private System.Windows.Forms.Label SampleDescription;
    }
}