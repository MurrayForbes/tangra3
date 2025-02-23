﻿namespace Tangra.View.CustomRenderers.AavTimeAnalyser
{
    partial class frmAavStatusChannelOnlyView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAavStatusChannelOnlyView));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabOverview = new System.Windows.Forms.TabPage();
            this.pbLoadData = new System.Windows.Forms.ProgressBar();
            this.tbxAnalysisDetails = new System.Windows.Forms.TextBox();
            this.tabGraphs = new System.Windows.Forms.TabPage();
            this.pbGraph = new System.Windows.Forms.PictureBox();
            this.pnlGraph = new System.Windows.Forms.Panel();
            this.pnlTimeMedianConfig = new System.Windows.Forms.Panel();
            this.cbMeinbergData = new System.Windows.Forms.CheckBox();
            this.nudDensityThreshold = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nudMedianInterval = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.pnlTimeBucketsConfig = new System.Windows.Forms.Panel();
            this.btnExportBuckets = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.nudDeltaBucketInterval = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.pnlTimeDeltaConfig = new System.Windows.Forms.Panel();
            this.rbSystemTime = new System.Windows.Forms.RadioButton();
            this.rbSystemTimeAsFileTime = new System.Windows.Forms.RadioButton();
            this.cbxNtpTime = new System.Windows.Forms.CheckBox();
            this.cbxNtpError = new System.Windows.Forms.CheckBox();
            this.cbxGraphType = new System.Windows.Forms.ComboBox();
            this.tabOCRErrors = new System.Windows.Forms.TabPage();
            this.pbOcrErrorFrame = new System.Windows.Forms.PictureBox();
            this.pnlOcrErrorControl = new System.Windows.Forms.Panel();
            this.lblOcrText = new System.Windows.Forms.Label();
            this.nudOcrErrorFrame = new System.Windows.Forms.NumericUpDown();
            this.resizeUpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.graphsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miSubset = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.miNoConnections = new System.Windows.Forms.ToolStripMenuItem();
            this.miLineConnections = new System.Windows.Forms.ToolStripMenuItem();
            this.gridlinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miCompleteGridlines = new System.Windows.Forms.ToolStripMenuItem();
            this.miTickGridlines = new System.Windows.Forms.ToolStripMenuItem();
            this.miDimentions = new System.Windows.Forms.ToolStripMenuItem();
            this.mi640 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi800 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi960 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1024 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1280 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1400 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1440 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1600 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1632x600 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1632 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1856 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi1920 = new System.Windows.Forms.ToolStripMenuItem();
            this.mi2048 = new System.Windows.Forms.ToolStripMenuItem();
            this.miPublicationMode = new System.Windows.Forms.ToolStripMenuItem();
            this.miCopyToClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.miExport = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportAll = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportTimeDeltaOnly = new System.Windows.Forms.ToolStripMenuItem();
            this.miData = new System.Windows.Forms.ToolStripMenuItem();
            this.miAddMoreData = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnLoadFromFile = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabOverview.SuspendLayout();
            this.tabGraphs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbGraph)).BeginInit();
            this.pnlGraph.SuspendLayout();
            this.pnlTimeMedianConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDensityThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMedianInterval)).BeginInit();
            this.pnlTimeBucketsConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDeltaBucketInterval)).BeginInit();
            this.pnlTimeDeltaConfig.SuspendLayout();
            this.tabOCRErrors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOcrErrorFrame)).BeginInit();
            this.pnlOcrErrorControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOcrErrorFrame)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabOverview);
            this.tabControl.Controls.Add(this.tabGraphs);
            this.tabControl.Controls.Add(this.tabOCRErrors);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 24);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(815, 550);
            this.tabControl.TabIndex = 0;
            // 
            // tabOverview
            // 
            this.tabOverview.Controls.Add(this.pbLoadData);
            this.tabOverview.Controls.Add(this.tbxAnalysisDetails);
            this.tabOverview.Location = new System.Drawing.Point(4, 22);
            this.tabOverview.Name = "tabOverview";
            this.tabOverview.Padding = new System.Windows.Forms.Padding(3);
            this.tabOverview.Size = new System.Drawing.Size(807, 524);
            this.tabOverview.TabIndex = 0;
            this.tabOverview.Text = "Overview";
            this.tabOverview.UseVisualStyleBackColor = true;
            // 
            // pbLoadData
            // 
            this.pbLoadData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbLoadData.Location = new System.Drawing.Point(17, 17);
            this.pbLoadData.Name = "pbLoadData";
            this.pbLoadData.Size = new System.Drawing.Size(773, 23);
            this.pbLoadData.TabIndex = 0;
            // 
            // tbxAnalysisDetails
            // 
            this.tbxAnalysisDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxAnalysisDetails.BackColor = System.Drawing.SystemColors.Info;
            this.tbxAnalysisDetails.Location = new System.Drawing.Point(17, 17);
            this.tbxAnalysisDetails.Multiline = true;
            this.tbxAnalysisDetails.Name = "tbxAnalysisDetails";
            this.tbxAnalysisDetails.ReadOnly = true;
            this.tbxAnalysisDetails.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbxAnalysisDetails.Size = new System.Drawing.Size(773, 485);
            this.tbxAnalysisDetails.TabIndex = 1;
            this.tbxAnalysisDetails.Visible = false;
            // 
            // tabGraphs
            // 
            this.tabGraphs.Controls.Add(this.pbGraph);
            this.tabGraphs.Controls.Add(this.pnlGraph);
            this.tabGraphs.Location = new System.Drawing.Point(4, 22);
            this.tabGraphs.Name = "tabGraphs";
            this.tabGraphs.Padding = new System.Windows.Forms.Padding(3);
            this.tabGraphs.Size = new System.Drawing.Size(807, 524);
            this.tabGraphs.TabIndex = 1;
            this.tabGraphs.Text = "Graphs";
            this.tabGraphs.UseVisualStyleBackColor = true;
            // 
            // pbGraph
            // 
            this.pbGraph.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbGraph.Location = new System.Drawing.Point(3, 37);
            this.pbGraph.Name = "pbGraph";
            this.pbGraph.Size = new System.Drawing.Size(801, 484);
            this.pbGraph.TabIndex = 0;
            this.pbGraph.TabStop = false;
            // 
            // pnlGraph
            // 
            this.pnlGraph.Controls.Add(this.pnlTimeBucketsConfig);
            this.pnlGraph.Controls.Add(this.pnlTimeDeltaConfig);
            this.pnlGraph.Controls.Add(this.cbxGraphType);
            this.pnlGraph.Controls.Add(this.pnlTimeMedianConfig);
            this.pnlGraph.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGraph.Location = new System.Drawing.Point(3, 3);
            this.pnlGraph.Name = "pnlGraph";
            this.pnlGraph.Size = new System.Drawing.Size(801, 34);
            this.pnlGraph.TabIndex = 0;
            // 
            // pnlTimeMedianConfig
            // 
            this.pnlTimeMedianConfig.Controls.Add(this.cbMeinbergData);
            this.pnlTimeMedianConfig.Controls.Add(this.nudDensityThreshold);
            this.pnlTimeMedianConfig.Controls.Add(this.label3);
            this.pnlTimeMedianConfig.Controls.Add(this.label2);
            this.pnlTimeMedianConfig.Controls.Add(this.nudMedianInterval);
            this.pnlTimeMedianConfig.Controls.Add(this.label1);
            this.pnlTimeMedianConfig.Location = new System.Drawing.Point(171, 0);
            this.pnlTimeMedianConfig.Name = "pnlTimeMedianConfig";
            this.pnlTimeMedianConfig.Size = new System.Drawing.Size(625, 33);
            this.pnlTimeMedianConfig.TabIndex = 2;
            // 
            // cbMeinbergData
            // 
            this.cbMeinbergData.AutoSize = true;
            this.cbMeinbergData.Checked = true;
            this.cbMeinbergData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbMeinbergData.Location = new System.Drawing.Point(385, 9);
            this.cbMeinbergData.Name = "cbMeinbergData";
            this.cbMeinbergData.Size = new System.Drawing.Size(96, 17);
            this.cbMeinbergData.TabIndex = 4;
            this.cbMeinbergData.Text = "Meinberg Data";
            this.cbMeinbergData.UseVisualStyleBackColor = true;
            this.cbMeinbergData.CheckedChanged += new System.EventHandler(this.cbMeinbergData_CheckedChanged);
            // 
            // nudDensityThreshold
            // 
            this.nudDensityThreshold.Location = new System.Drawing.Point(304, 7);
            this.nudDensityThreshold.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudDensityThreshold.Name = "nudDensityThreshold";
            this.nudDensityThreshold.Size = new System.Drawing.Size(41, 20);
            this.nudDensityThreshold.TabIndex = 3;
            this.nudDensityThreshold.ValueChanged += new System.EventHandler(this.TimeDeltasTimeSourceChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(206, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Density Theshold:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(150, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "min";
            // 
            // nudMedianInterval
            // 
            this.nudMedianInterval.Location = new System.Drawing.Point(103, 7);
            this.nudMedianInterval.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudMedianInterval.Name = "nudMedianInterval";
            this.nudMedianInterval.Size = new System.Drawing.Size(41, 20);
            this.nudMedianInterval.TabIndex = 1;
            this.nudMedianInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudMedianInterval.ValueChanged += new System.EventHandler(this.TimeDeltasTimeSourceChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Median Interval:";
            // 
            // pnlTimeBucketsConfig
            // 
            this.pnlTimeBucketsConfig.Controls.Add(this.btnLoadFromFile);
            this.pnlTimeBucketsConfig.Controls.Add(this.btnExportBuckets);
            this.pnlTimeBucketsConfig.Controls.Add(this.label4);
            this.pnlTimeBucketsConfig.Controls.Add(this.nudDeltaBucketInterval);
            this.pnlTimeBucketsConfig.Controls.Add(this.label5);
            this.pnlTimeBucketsConfig.Location = new System.Drawing.Point(171, 0);
            this.pnlTimeBucketsConfig.Name = "pnlTimeBucketsConfig";
            this.pnlTimeBucketsConfig.Size = new System.Drawing.Size(615, 33);
            this.pnlTimeBucketsConfig.TabIndex = 3;
            // 
            // btnExportBuckets
            // 
            this.btnExportBuckets.Location = new System.Drawing.Point(179, 5);
            this.btnExportBuckets.Name = "btnExportBuckets";
            this.btnExportBuckets.Size = new System.Drawing.Size(75, 23);
            this.btnExportBuckets.TabIndex = 6;
            this.btnExportBuckets.Text = "Export";
            this.btnExportBuckets.UseVisualStyleBackColor = true;
            this.btnExportBuckets.Click += new System.EventHandler(this.btnExportBuckets_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(151, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "ms";
            // 
            // nudDeltaBucketInterval
            // 
            this.nudDeltaBucketInterval.Location = new System.Drawing.Point(104, 8);
            this.nudDeltaBucketInterval.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDeltaBucketInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDeltaBucketInterval.Name = "nudDeltaBucketInterval";
            this.nudDeltaBucketInterval.Size = new System.Drawing.Size(41, 20);
            this.nudDeltaBucketInterval.TabIndex = 4;
            this.nudDeltaBucketInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDeltaBucketInterval.ValueChanged += new System.EventHandler(this.TimeDeltasTimeSourceChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 11);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(82, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Bucket Interval:";
            // 
            // pnlTimeDeltaConfig
            // 
            this.pnlTimeDeltaConfig.Controls.Add(this.rbSystemTime);
            this.pnlTimeDeltaConfig.Controls.Add(this.rbSystemTimeAsFileTime);
            this.pnlTimeDeltaConfig.Controls.Add(this.cbxNtpTime);
            this.pnlTimeDeltaConfig.Controls.Add(this.cbxNtpError);
            this.pnlTimeDeltaConfig.Location = new System.Drawing.Point(171, 0);
            this.pnlTimeDeltaConfig.Name = "pnlTimeDeltaConfig";
            this.pnlTimeDeltaConfig.Size = new System.Drawing.Size(606, 33);
            this.pnlTimeDeltaConfig.TabIndex = 1;
            // 
            // rbSystemTime
            // 
            this.rbSystemTime.AutoSize = true;
            this.rbSystemTime.Location = new System.Drawing.Point(177, 8);
            this.rbSystemTime.Name = "rbSystemTime";
            this.rbSystemTime.Size = new System.Drawing.Size(82, 17);
            this.rbSystemTime.TabIndex = 3;
            this.rbSystemTime.Text = "SystemTime";
            this.rbSystemTime.UseVisualStyleBackColor = true;
            // 
            // rbSystemTimeAsFileTime
            // 
            this.rbSystemTimeAsFileTime.AutoSize = true;
            this.rbSystemTimeAsFileTime.Checked = true;
            this.rbSystemTimeAsFileTime.Location = new System.Drawing.Point(3, 8);
            this.rbSystemTimeAsFileTime.Name = "rbSystemTimeAsFileTime";
            this.rbSystemTimeAsFileTime.Size = new System.Drawing.Size(168, 17);
            this.rbSystemTimeAsFileTime.TabIndex = 2;
            this.rbSystemTimeAsFileTime.TabStop = true;
            this.rbSystemTimeAsFileTime.Text = "SystemTimePreciseAsFileTime";
            this.rbSystemTimeAsFileTime.UseVisualStyleBackColor = true;
            this.rbSystemTimeAsFileTime.CheckedChanged += new System.EventHandler(this.TimeDeltasTimeSourceChanged);
            // 
            // cbxNtpTime
            // 
            this.cbxNtpTime.AutoSize = true;
            this.cbxNtpTime.Checked = true;
            this.cbxNtpTime.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxNtpTime.Location = new System.Drawing.Point(338, 9);
            this.cbxNtpTime.Name = "cbxNtpTime";
            this.cbxNtpTime.Size = new System.Drawing.Size(158, 17);
            this.cbxNtpTime.TabIndex = 1;
            this.cbxNtpTime.Text = "OccuRec\'s Reference Time";
            this.cbxNtpTime.UseVisualStyleBackColor = true;
            this.cbxNtpTime.CheckedChanged += new System.EventHandler(this.TimeDeltasTimeSourceChanged);
            // 
            // cbxNtpError
            // 
            this.cbxNtpError.AutoSize = true;
            this.cbxNtpError.Checked = true;
            this.cbxNtpError.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxNtpError.Location = new System.Drawing.Point(512, 9);
            this.cbxNtpError.Name = "cbxNtpError";
            this.cbxNtpError.Size = new System.Drawing.Size(73, 17);
            this.cbxNtpError.TabIndex = 0;
            this.cbxNtpError.Text = "NTP Error";
            this.cbxNtpError.UseVisualStyleBackColor = true;
            this.cbxNtpError.CheckedChanged += new System.EventHandler(this.TimeDeltasTimeSourceChanged);
            // 
            // cbxGraphType
            // 
            this.cbxGraphType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxGraphType.FormattingEnabled = true;
            this.cbxGraphType.Items.AddRange(new object[] {
            "Time Deltas",
            "System Utilisation",
            "NTP Updates",
            "NTP Updates & Unapplied",
            "Zoomed Time Deltas",
            "Delta Bucket Intervals (%)"});
            this.cbxGraphType.Location = new System.Drawing.Point(6, 7);
            this.cbxGraphType.Name = "cbxGraphType";
            this.cbxGraphType.Size = new System.Drawing.Size(159, 21);
            this.cbxGraphType.TabIndex = 0;
            this.cbxGraphType.SelectedIndexChanged += new System.EventHandler(this.cbxGraphType_SelectedIndexChanged);
            // 
            // tabOCRErrors
            // 
            this.tabOCRErrors.Controls.Add(this.pbOcrErrorFrame);
            this.tabOCRErrors.Controls.Add(this.pnlOcrErrorControl);
            this.tabOCRErrors.Location = new System.Drawing.Point(4, 22);
            this.tabOCRErrors.Name = "tabOCRErrors";
            this.tabOCRErrors.Size = new System.Drawing.Size(807, 524);
            this.tabOCRErrors.TabIndex = 2;
            this.tabOCRErrors.Text = "OCR Errors";
            this.tabOCRErrors.UseVisualStyleBackColor = true;
            // 
            // pbOcrErrorFrame
            // 
            this.pbOcrErrorFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbOcrErrorFrame.Location = new System.Drawing.Point(0, 41);
            this.pbOcrErrorFrame.Name = "pbOcrErrorFrame";
            this.pbOcrErrorFrame.Size = new System.Drawing.Size(807, 483);
            this.pbOcrErrorFrame.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pbOcrErrorFrame.TabIndex = 1;
            this.pbOcrErrorFrame.TabStop = false;
            // 
            // pnlOcrErrorControl
            // 
            this.pnlOcrErrorControl.Controls.Add(this.lblOcrText);
            this.pnlOcrErrorControl.Controls.Add(this.nudOcrErrorFrame);
            this.pnlOcrErrorControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlOcrErrorControl.Location = new System.Drawing.Point(0, 0);
            this.pnlOcrErrorControl.Name = "pnlOcrErrorControl";
            this.pnlOcrErrorControl.Size = new System.Drawing.Size(807, 41);
            this.pnlOcrErrorControl.TabIndex = 0;
            // 
            // lblOcrText
            // 
            this.lblOcrText.AutoSize = true;
            this.lblOcrText.Location = new System.Drawing.Point(114, 17);
            this.lblOcrText.Name = "lblOcrText";
            this.lblOcrText.Size = new System.Drawing.Size(0, 13);
            this.lblOcrText.TabIndex = 1;
            // 
            // nudOcrErrorFrame
            // 
            this.nudOcrErrorFrame.Location = new System.Drawing.Point(10, 11);
            this.nudOcrErrorFrame.Name = "nudOcrErrorFrame";
            this.nudOcrErrorFrame.Size = new System.Drawing.Size(70, 20);
            this.nudOcrErrorFrame.TabIndex = 0;
            this.nudOcrErrorFrame.ValueChanged += new System.EventHandler(this.nudOcrErrorFrame_ValueChanged);
            // 
            // resizeUpdateTimer
            // 
            this.resizeUpdateTimer.Interval = 250;
            this.resizeUpdateTimer.Tick += new System.EventHandler(this.resizeUpdateTimer_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.graphsToolStripMenuItem,
            this.miExport,
            this.miData});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(815, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // graphsToolStripMenuItem
            // 
            this.graphsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miSubset,
            this.toolStripMenuItem1,
            this.gridlinesToolStripMenuItem,
            this.miDimentions,
            this.miPublicationMode,
            this.miCopyToClipboard});
            this.graphsToolStripMenuItem.Name = "graphsToolStripMenuItem";
            this.graphsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.graphsToolStripMenuItem.Text = "&Graphs";
            // 
            // miSubset
            // 
            this.miSubset.Name = "miSubset";
            this.miSubset.Size = new System.Drawing.Size(191, 22);
            this.miSubset.Text = "&Subset";
            this.miSubset.Click += new System.EventHandler(this.miSubset_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miNoConnections,
            this.miLineConnections});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(191, 22);
            this.toolStripMenuItem1.Text = "DataPoint Connection";
            // 
            // miNoConnections
            // 
            this.miNoConnections.Checked = true;
            this.miNoConnections.CheckOnClick = true;
            this.miNoConnections.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miNoConnections.Name = "miNoConnections";
            this.miNoConnections.Size = new System.Drawing.Size(103, 22);
            this.miNoConnections.Text = "None";
            this.miNoConnections.Click += new System.EventHandler(this.DataPointConnectionStyleChanged);
            // 
            // miLineConnections
            // 
            this.miLineConnections.CheckOnClick = true;
            this.miLineConnections.Name = "miLineConnections";
            this.miLineConnections.Size = new System.Drawing.Size(103, 22);
            this.miLineConnections.Text = "Lines";
            this.miLineConnections.Click += new System.EventHandler(this.DataPointConnectionStyleChanged);
            // 
            // gridlinesToolStripMenuItem
            // 
            this.gridlinesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miCompleteGridlines,
            this.miTickGridlines});
            this.gridlinesToolStripMenuItem.Name = "gridlinesToolStripMenuItem";
            this.gridlinesToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.gridlinesToolStripMenuItem.Text = "&Gridlines";
            // 
            // miCompleteGridlines
            // 
            this.miCompleteGridlines.Checked = true;
            this.miCompleteGridlines.CheckOnClick = true;
            this.miCompleteGridlines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miCompleteGridlines.Name = "miCompleteGridlines";
            this.miCompleteGridlines.Size = new System.Drawing.Size(126, 22);
            this.miCompleteGridlines.Text = "&Complete";
            this.miCompleteGridlines.Click += new System.EventHandler(this.GridlinesStyleChanged);
            // 
            // miTickGridlines
            // 
            this.miTickGridlines.CheckOnClick = true;
            this.miTickGridlines.Name = "miTickGridlines";
            this.miTickGridlines.Size = new System.Drawing.Size(126, 22);
            this.miTickGridlines.Text = "&Ticks";
            this.miTickGridlines.Click += new System.EventHandler(this.GridlinesStyleChanged);
            // 
            // miDimentions
            // 
            this.miDimentions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mi640,
            this.mi800,
            this.mi960,
            this.mi1024,
            this.mi1280,
            this.mi1400,
            this.mi1440,
            this.mi1600,
            this.mi1632x600,
            this.mi1632,
            this.mi1856,
            this.mi1920,
            this.mi2048});
            this.miDimentions.Name = "miDimentions";
            this.miDimentions.Size = new System.Drawing.Size(191, 22);
            this.miDimentions.Text = "Dimentions";
            this.miDimentions.DropDownOpening += new System.EventHandler(this.miDimentions_DropDownOpening);
            // 
            // mi640
            // 
            this.mi640.Name = "mi640";
            this.mi640.Size = new System.Drawing.Size(130, 22);
            this.mi640.Text = "640×480";
            this.mi640.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi800
            // 
            this.mi800.Name = "mi800";
            this.mi800.Size = new System.Drawing.Size(130, 22);
            this.mi800.Text = "800×600";
            this.mi800.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi960
            // 
            this.mi960.Name = "mi960";
            this.mi960.Size = new System.Drawing.Size(130, 22);
            this.mi960.Text = "960×720";
            this.mi960.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1024
            // 
            this.mi1024.Name = "mi1024";
            this.mi1024.Size = new System.Drawing.Size(130, 22);
            this.mi1024.Text = "1024×768";
            this.mi1024.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1280
            // 
            this.mi1280.Name = "mi1280";
            this.mi1280.Size = new System.Drawing.Size(130, 22);
            this.mi1280.Text = "1280×960";
            this.mi1280.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1400
            // 
            this.mi1400.Name = "mi1400";
            this.mi1400.Size = new System.Drawing.Size(130, 22);
            this.mi1400.Text = "1400×1050";
            this.mi1400.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1440
            // 
            this.mi1440.Name = "mi1440";
            this.mi1440.Size = new System.Drawing.Size(130, 22);
            this.mi1440.Text = "1440×1080";
            this.mi1440.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1600
            // 
            this.mi1600.Name = "mi1600";
            this.mi1600.Size = new System.Drawing.Size(130, 22);
            this.mi1600.Text = "1600×1200";
            this.mi1600.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1632x600
            // 
            this.mi1632x600.Name = "mi1632x600";
            this.mi1632x600.Size = new System.Drawing.Size(130, 22);
            this.mi1632x600.Text = "1632×600";
            this.mi1632x600.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1632
            // 
            this.mi1632.Name = "mi1632";
            this.mi1632.Size = new System.Drawing.Size(130, 22);
            this.mi1632.Text = "1632×768";
            this.mi1632.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1856
            // 
            this.mi1856.Name = "mi1856";
            this.mi1856.Size = new System.Drawing.Size(130, 22);
            this.mi1856.Text = "1856×1392";
            this.mi1856.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi1920
            // 
            this.mi1920.Name = "mi1920";
            this.mi1920.Size = new System.Drawing.Size(130, 22);
            this.mi1920.Text = "1920×1440";
            this.mi1920.Click += new System.EventHandler(this.SetFormSize);
            // 
            // mi2048
            // 
            this.mi2048.Name = "mi2048";
            this.mi2048.Size = new System.Drawing.Size(130, 22);
            this.mi2048.Tag = "";
            this.mi2048.Text = "2048×1536";
            this.mi2048.Click += new System.EventHandler(this.SetFormSize);
            // 
            // miPublicationMode
            // 
            this.miPublicationMode.CheckOnClick = true;
            this.miPublicationMode.Name = "miPublicationMode";
            this.miPublicationMode.Size = new System.Drawing.Size(191, 22);
            this.miPublicationMode.Text = "Publication Mode";
            this.miPublicationMode.CheckedChanged += new System.EventHandler(this.miPublicationMode_CheckedChanged);
            // 
            // miCopyToClipboard
            // 
            this.miCopyToClipboard.Name = "miCopyToClipboard";
            this.miCopyToClipboard.Size = new System.Drawing.Size(191, 22);
            this.miCopyToClipboard.Text = "&Copy to Clipboard";
            this.miCopyToClipboard.Click += new System.EventHandler(this.miCopyToClipboard_Click);
            // 
            // miExport
            // 
            this.miExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miExportAll,
            this.miExportTimeDeltaOnly});
            this.miExport.Name = "miExport";
            this.miExport.Size = new System.Drawing.Size(53, 20);
            this.miExport.Text = "&Export";
            // 
            // miExportAll
            // 
            this.miExportAll.Name = "miExportAll";
            this.miExportAll.Size = new System.Drawing.Size(163, 22);
            this.miExportAll.Text = "&All Data";
            this.miExportAll.Click += new System.EventHandler(this.miExportAll_Click);
            // 
            // miExportTimeDeltaOnly
            // 
            this.miExportTimeDeltaOnly.Name = "miExportTimeDeltaOnly";
            this.miExportTimeDeltaOnly.Size = new System.Drawing.Size(163, 22);
            this.miExportTimeDeltaOnly.Text = "&Time Deltas Only";
            this.miExportTimeDeltaOnly.Click += new System.EventHandler(this.miExportTimeDeltaOnly_Click);
            // 
            // miData
            // 
            this.miData.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miAddMoreData});
            this.miData.Name = "miData";
            this.miData.Size = new System.Drawing.Size(43, 20);
            this.miData.Text = "&Data";
            // 
            // miAddMoreData
            // 
            this.miAddMoreData.Name = "miAddMoreData";
            this.miAddMoreData.Size = new System.Drawing.Size(171, 22);
            this.miAddMoreData.Text = "&Add another file ...";
            this.miAddMoreData.Click += new System.EventHandler(this.miAddMoreData_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "cvs";
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "aav";
            this.openFileDialog.Filter = "AAV Files (*.aav)|*.aav";
            // 
            // btnLoadFromFile
            // 
            this.btnLoadFromFile.Location = new System.Drawing.Point(270, 6);
            this.btnLoadFromFile.Name = "btnLoadFromFile";
            this.btnLoadFromFile.Size = new System.Drawing.Size(109, 23);
            this.btnLoadFromFile.TabIndex = 7;
            this.btnLoadFromFile.Text = "Load Data from File";
            this.btnLoadFromFile.UseVisualStyleBackColor = true;
            this.btnLoadFromFile.Click += new System.EventHandler(this.btnLoadFromFile_Click);
            // 
            // frmAavStatusChannelOnlyView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(815, 574);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAavStatusChannelOnlyView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.frmAavStatusChannelOnlyView_Load);
            this.ResizeEnd += new System.EventHandler(this.frmAavStatusChannelOnlyView_ResizeEnd);
            this.tabControl.ResumeLayout(false);
            this.tabOverview.ResumeLayout(false);
            this.tabOverview.PerformLayout();
            this.tabGraphs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbGraph)).EndInit();
            this.pnlGraph.ResumeLayout(false);
            this.pnlTimeMedianConfig.ResumeLayout(false);
            this.pnlTimeMedianConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDensityThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMedianInterval)).EndInit();
            this.pnlTimeBucketsConfig.ResumeLayout(false);
            this.pnlTimeBucketsConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDeltaBucketInterval)).EndInit();
            this.pnlTimeDeltaConfig.ResumeLayout(false);
            this.pnlTimeDeltaConfig.PerformLayout();
            this.tabOCRErrors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbOcrErrorFrame)).EndInit();
            this.pnlOcrErrorControl.ResumeLayout(false);
            this.pnlOcrErrorControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOcrErrorFrame)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabOverview;
        private System.Windows.Forms.TabPage tabGraphs;
        private System.Windows.Forms.TabPage tabOCRErrors;
        private System.Windows.Forms.ProgressBar pbLoadData;
        private System.Windows.Forms.Panel pnlGraph;
        private System.Windows.Forms.PictureBox pbGraph;
        private System.Windows.Forms.Timer resizeUpdateTimer;
        private System.Windows.Forms.ComboBox cbxGraphType;
        private System.Windows.Forms.Panel pnlTimeDeltaConfig;
        private System.Windows.Forms.RadioButton rbSystemTime;
        private System.Windows.Forms.RadioButton rbSystemTimeAsFileTime;
        private System.Windows.Forms.CheckBox cbxNtpTime;
        private System.Windows.Forms.CheckBox cbxNtpError;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem graphsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gridlinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem miCompleteGridlines;
        private System.Windows.Forms.ToolStripMenuItem miTickGridlines;
        private System.Windows.Forms.Panel pnlOcrErrorControl;
        private System.Windows.Forms.Label lblOcrText;
        private System.Windows.Forms.NumericUpDown nudOcrErrorFrame;
        private System.Windows.Forms.PictureBox pbOcrErrorFrame;
        private System.Windows.Forms.ToolStripMenuItem miExport;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem miSubset;
        private System.Windows.Forms.TextBox tbxAnalysisDetails;
        private System.Windows.Forms.Panel pnlTimeMedianConfig;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudMedianInterval;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudDensityThreshold;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel pnlTimeBucketsConfig;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nudDeltaBucketInterval;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem miNoConnections;
        private System.Windows.Forms.ToolStripMenuItem miLineConnections;
        private System.Windows.Forms.ToolStripMenuItem miDimentions;
        private System.Windows.Forms.ToolStripMenuItem mi640;
        private System.Windows.Forms.ToolStripMenuItem mi800;
        private System.Windows.Forms.ToolStripMenuItem mi960;
        private System.Windows.Forms.ToolStripMenuItem mi1024;
        private System.Windows.Forms.ToolStripMenuItem mi1280;
        private System.Windows.Forms.ToolStripMenuItem mi1400;
        private System.Windows.Forms.ToolStripMenuItem mi1440;
        private System.Windows.Forms.ToolStripMenuItem mi1600;
        private System.Windows.Forms.ToolStripMenuItem mi1856;
        private System.Windows.Forms.ToolStripMenuItem mi1920;
        private System.Windows.Forms.ToolStripMenuItem mi2048;
        private System.Windows.Forms.Button btnExportBuckets;
        private System.Windows.Forms.ToolStripMenuItem miData;
        private System.Windows.Forms.ToolStripMenuItem miAddMoreData;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolStripMenuItem miPublicationMode;
        private System.Windows.Forms.ToolStripMenuItem miCopyToClipboard;
        private System.Windows.Forms.ToolStripMenuItem mi1632;
        private System.Windows.Forms.ToolStripMenuItem mi1632x600;
        private System.Windows.Forms.CheckBox cbMeinbergData;
        private System.Windows.Forms.ToolStripMenuItem miExportAll;
        private System.Windows.Forms.ToolStripMenuItem miExportTimeDeltaOnly;
        private System.Windows.Forms.Button btnLoadFromFile;
    }
}