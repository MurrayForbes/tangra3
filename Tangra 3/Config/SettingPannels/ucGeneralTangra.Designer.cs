﻿using System.Windows.Forms;

namespace Tangra.Config.SettingPannels
{
	partial class ucGeneralTangra
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucGeneralTangra));
			this.label12 = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.cbShowProcessingSpeed = new System.Windows.Forms.CheckBox();
			this.cbxBetaUpdates = new System.Windows.Forms.CheckBox();
			this.cbxRunVideosAtFastest = new System.Windows.Forms.CheckBox();
			this.cbPerformanceQuality = new System.Windows.Forms.ComboBox();
			this.cbxOnOpenOperation = new System.Windows.Forms.ComboBox();
			this.cbxShowCursorPosition = new System.Windows.Forms.CheckBox();
			this.btnAssociateFiles = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.cbxSendStats = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(3, 3);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(149, 13);
			this.label12.TabIndex = 26;
			this.label12.Text = "When a new video is opened:";
			// 
			// label22
			// 
			this.label22.AutoSize = true;
			this.label22.Location = new System.Drawing.Point(255, 3);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(127, 13);
			this.label22.TabIndex = 36;
			this.label22.Text = "Optimize performance for:";
			// 
			// cbShowProcessingSpeed
			// 
			this.cbShowProcessingSpeed.Location = new System.Drawing.Point(6, 59);
			this.cbShowProcessingSpeed.Name = "cbShowProcessingSpeed";
			this.cbShowProcessingSpeed.Size = new System.Drawing.Size(214, 19);
			this.cbShowProcessingSpeed.TabIndex = 47;
			this.cbShowProcessingSpeed.Text = "Show processing speed in the statusbar";
			// 
			// cbxBetaUpdates
			// 
			this.cbxBetaUpdates.Location = new System.Drawing.Point(6, 128);
			this.cbxBetaUpdates.Name = "cbxBetaUpdates";
			this.cbxBetaUpdates.Size = new System.Drawing.Size(179, 19);
			this.cbxBetaUpdates.TabIndex = 48;
			this.cbxBetaUpdates.Text = "Accept beta version updates";
			// 
			// cbxRunVideosAtFastest
			// 
			this.cbxRunVideosAtFastest.Location = new System.Drawing.Point(258, 59);
			this.cbxRunVideosAtFastest.Name = "cbxRunVideosAtFastest";
			this.cbxRunVideosAtFastest.Size = new System.Drawing.Size(179, 19);
			this.cbxRunVideosAtFastest.TabIndex = 49;
			this.cbxRunVideosAtFastest.Text = "Play videos on fastest speed";
			// 
			// cbPerformanceQuality
			// 
			this.cbPerformanceQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbPerformanceQuality.Items.AddRange(new object[] {
            "Responsiveness",
            "Speed"});
			this.cbPerformanceQuality.Location = new System.Drawing.Point(258, 19);
			this.cbPerformanceQuality.Name = "cbPerformanceQuality";
			this.cbPerformanceQuality.Size = new System.Drawing.Size(177, 21);
			this.cbPerformanceQuality.TabIndex = 51;
			// 
			// cbxOnOpenOperation
			// 
			this.cbxOnOpenOperation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbxOnOpenOperation.Items.AddRange(new object[] {
            "Do nothing",
            "Reduce a light curve",
            "Run astrometry",
            "Reduce a spectra"});
			this.cbxOnOpenOperation.Location = new System.Drawing.Point(6, 19);
			this.cbxOnOpenOperation.Name = "cbxOnOpenOperation";
			this.cbxOnOpenOperation.Size = new System.Drawing.Size(193, 21);
			this.cbxOnOpenOperation.TabIndex = 52;
			// 
			// cbxShowCursorPosition
			// 
			this.cbxShowCursorPosition.Location = new System.Drawing.Point(6, 84);
			this.cbxShowCursorPosition.Name = "cbxShowCursorPosition";
			this.cbxShowCursorPosition.Size = new System.Drawing.Size(242, 19);
			this.cbxShowCursorPosition.TabIndex = 53;
			this.cbxShowCursorPosition.Text = "Show cursor coordinates in the statusbar";
			// 
			// btnAssociateFiles
			// 
			this.btnAssociateFiles.Location = new System.Drawing.Point(258, 124);
			this.btnAssociateFiles.Name = "btnAssociateFiles";
			this.btnAssociateFiles.Size = new System.Drawing.Size(177, 23);
			this.btnAssociateFiles.TabIndex = 54;
			this.btnAssociateFiles.Text = "Associate files with Tangra";
			this.toolTip1.SetToolTip(this.btnAssociateFiles, "Associate .lc, .aav and .adv files with Tangra ");
			this.btnAssociateFiles.UseVisualStyleBackColor = true;
			this.btnAssociateFiles.Click += new System.EventHandler(this.btnAssociateFiles_Click);
			// 
			// toolTip1
			// 
			this.toolTip1.AutoPopDelay = 15000;
			this.toolTip1.InitialDelay = 500;
			this.toolTip1.ReshowDelay = 100;
			// 
			// cbxSendStats
			// 
			this.cbxSendStats.Location = new System.Drawing.Point(6, 153);
			this.cbxSendStats.Name = "cbxSendStats";
			this.cbxSendStats.Size = new System.Drawing.Size(214, 19);
			this.cbxSendStats.TabIndex = 55;
			this.cbxSendStats.Text = "Send anonymous usage statistics";
			this.toolTip1.SetToolTip(this.cbxSendStats, resources.GetString("cbxSendStats.ToolTip"));
			// 
			// ucGeneralTangra
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add(this.cbxSendStats);
			this.Controls.Add(this.btnAssociateFiles);
			this.Controls.Add(this.cbxShowCursorPosition);
			this.Controls.Add(this.cbxOnOpenOperation);
			this.Controls.Add(this.cbPerformanceQuality);
			this.Controls.Add(this.cbxRunVideosAtFastest);
			this.Controls.Add(this.cbxBetaUpdates);
			this.Controls.Add(this.cbShowProcessingSpeed);
			this.Controls.Add(this.label22);
			this.Controls.Add(this.label12);
			this.Name = "ucGeneralTangra";
			this.Size = new System.Drawing.Size(502, 257);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.CheckBox cbShowProcessingSpeed;
		private System.Windows.Forms.CheckBox cbxBetaUpdates;
		private System.Windows.Forms.CheckBox cbxRunVideosAtFastest;
		private System.Windows.Forms.ComboBox cbPerformanceQuality;
		private System.Windows.Forms.ComboBox cbxOnOpenOperation;
		private CheckBox cbxShowCursorPosition;
		private Button btnAssociateFiles;
		private ToolTip toolTip1;
		private CheckBox cbxSendStats;
	}
}
