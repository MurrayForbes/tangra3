﻿namespace MagnitudeFitter
{
	partial class frmMain
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
		private void InitializeComponent()
		{
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.miFile = new System.Windows.Forms.ToolStripMenuItem();
            this.miOpenExports = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.miExit = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.miConfigureFit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.miConfigurePlot = new System.Windows.Forms.ToolStripMenuItem();
            this.miColourPlot = new System.Windows.Forms.ToolStripMenuItem();
            this.miFixedColourPlot = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miFile});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(638, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // miFile
            // 
            this.miFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miOpenExports,
            this.toolStripSeparator1,
            this.miConfigureFit,
            this.miConfigurePlot,
            this.toolStripMenuItem1,
            this.miExit});
            this.miFile.Name = "miFile";
            this.miFile.Size = new System.Drawing.Size(35, 20);
            this.miFile.Text = "&File";
            // 
            // miOpenExports
            // 
            this.miOpenExports.Name = "miOpenExports";
            this.miOpenExports.Size = new System.Drawing.Size(152, 22);
            this.miOpenExports.Text = "&Open Exports";
            this.miOpenExports.Click += new System.EventHandler(this.miOpenExports_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
            // 
            // miExit
            // 
            this.miExit.Name = "miExit";
            this.miExit.Size = new System.Drawing.Size(152, 22);
            this.miExit.Text = "E&xit";
            this.miExit.Click += new System.EventHandler(this.miExit_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Tangra Exports (*.csv)|*.csv|All Files (*.*)|*.*";
            // 
            // pictureBox
            // 
            this.pictureBox.Location = new System.Drawing.Point(0, 24);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(638, 524);
            this.pictureBox.TabIndex = 1;
            this.pictureBox.TabStop = false;
            // 
            // miConfigureFit
            // 
            this.miConfigureFit.Name = "miConfigureFit";
            this.miConfigureFit.Size = new System.Drawing.Size(152, 22);
            this.miConfigureFit.Text = "Configure Fit";
            this.miConfigureFit.Click += new System.EventHandler(this.miConfigureFit_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // miConfigurePlot
            // 
            this.miConfigurePlot.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miFixedColourPlot,
            this.miColourPlot});
            this.miConfigurePlot.Name = "miConfigurePlot";
            this.miConfigurePlot.Size = new System.Drawing.Size(152, 22);
            this.miConfigurePlot.Text = "Configure Plot";
            // 
            // miColourPlot
            // 
            this.miColourPlot.Checked = true;
            this.miColourPlot.CheckOnClick = true;
            this.miColourPlot.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miColourPlot.Name = "miColourPlot";
            this.miColourPlot.Size = new System.Drawing.Size(155, 22);
            this.miColourPlot.Text = "Colour Fit Plot";
            this.miColourPlot.CheckedChanged += new System.EventHandler(this.miColourPlot_CheckedChanged);
            // 
            // miFixedColourPlot
            // 
            this.miFixedColourPlot.CheckOnClick = true;
            this.miFixedColourPlot.Name = "miFixedColourPlot";
            this.miFixedColourPlot.Size = new System.Drawing.Size(155, 22);
            this.miFixedColourPlot.Text = "Fixed Colour Plot";
            this.miFixedColourPlot.CheckedChanged += new System.EventHandler(this.miColourPlot_CheckedChanged);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(638, 548);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "Magnitude Fitter";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem miFile;
		private System.Windows.Forms.ToolStripMenuItem miOpenExports;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem miExit;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.ToolStripMenuItem miConfigureFit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem miConfigurePlot;
        private System.Windows.Forms.ToolStripMenuItem miColourPlot;
        private System.Windows.Forms.ToolStripMenuItem miFixedColourPlot;
	}
}

