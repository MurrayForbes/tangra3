﻿namespace Tangra.Config.SettingPannels
{
	partial class ucPhotometry
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
            this.groupControl2 = new System.Windows.Forms.GroupBox();
            this.cbxPsfQuadrature = new System.Windows.Forms.ComboBox();
            this.cbxPsfFittingMethod = new System.Windows.Forms.ComboBox();
            this.pnlSeeingSettings = new System.Windows.Forms.Panel();
            this.label13 = new System.Windows.Forms.Label();
            this.pnlUserSeeing = new System.Windows.Forms.Panel();
            this.nudUserSpecifiedFWHM = new System.Windows.Forms.NumericUpDown();
            this.label14 = new System.Windows.Forms.Label();
            this.rgSeeing = new System.Windows.Forms.GroupBox();
            this.rbSeeingUser = new System.Windows.Forms.RadioButton();
            this.rbSeeingAuto = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupControl1 = new System.Windows.Forms.GroupBox();
            this.nudSubPixelSquare = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nudPhotoAperture = new System.Windows.Forms.NumericUpDown();
            this.cbxPhotoSignalApertureType = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.nudInnerAnnulusInApertures = new System.Windows.Forms.NumericUpDown();
            this.nudMinimumAnnulusPixels = new System.Windows.Forms.NumericUpDown();
            this.cbxBackgroundMethod = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.nudSNFrameWindow = new System.Windows.Forms.NumericUpDown();
            this.label25 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rb3DThirdOrder = new System.Windows.Forms.RadioButton();
            this.rb3DFirstOrder = new System.Windows.Forms.RadioButton();
            this.rb3DSecondOrder = new System.Windows.Forms.RadioButton();
            this.nudStackedViewFrames = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.groupControl2.SuspendLayout();
            this.pnlSeeingSettings.SuspendLayout();
            this.pnlUserSeeing.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUserSpecifiedFWHM)).BeginInit();
            this.rgSeeing.SuspendLayout();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSubPixelSquare)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPhotoAperture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudInnerAnnulusInApertures)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinimumAnnulusPixels)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSNFrameWindow)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStackedViewFrames)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl2
            // 
            this.groupControl2.Controls.Add(this.cbxPsfQuadrature);
            this.groupControl2.Controls.Add(this.cbxPsfFittingMethod);
            this.groupControl2.Controls.Add(this.pnlSeeingSettings);
            this.groupControl2.Controls.Add(this.label6);
            this.groupControl2.Controls.Add(this.label3);
            this.groupControl2.Location = new System.Drawing.Point(3, 119);
            this.groupControl2.Name = "groupControl2";
            this.groupControl2.Size = new System.Drawing.Size(233, 193);
            this.groupControl2.TabIndex = 36;
            this.groupControl2.TabStop = false;
            this.groupControl2.Text = "PSF-Fitting Photometry";
            // 
            // cbxPsfQuadrature
            // 
            this.cbxPsfQuadrature.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxPsfQuadrature.Items.AddRange(new object[] {
            "Numerical Quadrature in Aperture",
            "Full Analytical Quadrature"});
            this.cbxPsfQuadrature.Location = new System.Drawing.Point(16, 42);
            this.cbxPsfQuadrature.Name = "cbxPsfQuadrature";
            this.cbxPsfQuadrature.Size = new System.Drawing.Size(185, 21);
            this.cbxPsfQuadrature.TabIndex = 39;
            // 
            // cbxPsfFittingMethod
            // 
            this.cbxPsfFittingMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxPsfFittingMethod.Items.AddRange(new object[] {
            "Direct Non-Linear Fit",
            "Linear Fit of Averaged Model"});
            this.cbxPsfFittingMethod.Location = new System.Drawing.Point(16, 85);
            this.cbxPsfFittingMethod.Name = "cbxPsfFittingMethod";
            this.cbxPsfFittingMethod.Size = new System.Drawing.Size(185, 21);
            this.cbxPsfFittingMethod.TabIndex = 38;
            this.cbxPsfFittingMethod.SelectedIndexChanged += new System.EventHandler(this.cbxPsfFittingMethod_SelectedIndexChanged);
            // 
            // pnlSeeingSettings
            // 
            this.pnlSeeingSettings.Controls.Add(this.label13);
            this.pnlSeeingSettings.Controls.Add(this.pnlUserSeeing);
            this.pnlSeeingSettings.Controls.Add(this.rgSeeing);
            this.pnlSeeingSettings.Location = new System.Drawing.Point(8, 117);
            this.pnlSeeingSettings.Name = "pnlSeeingSettings";
            this.pnlSeeingSettings.Size = new System.Drawing.Size(203, 69);
            this.pnlSeeingSettings.TabIndex = 37;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(5, 3);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(119, 13);
            this.label13.TabIndex = 33;
            this.label13.Text = "Averged Model FWHM:";
            // 
            // pnlUserSeeing
            // 
            this.pnlUserSeeing.Controls.Add(this.nudUserSpecifiedFWHM);
            this.pnlUserSeeing.Controls.Add(this.label14);
            this.pnlUserSeeing.Enabled = false;
            this.pnlUserSeeing.Location = new System.Drawing.Point(121, 41);
            this.pnlUserSeeing.Name = "pnlUserSeeing";
            this.pnlUserSeeing.Size = new System.Drawing.Size(75, 21);
            this.pnlUserSeeing.TabIndex = 36;
            // 
            // nudUserSpecifiedFWHM
            // 
            this.nudUserSpecifiedFWHM.DecimalPlaces = 1;
            this.nudUserSpecifiedFWHM.Location = new System.Drawing.Point(1, 1);
            this.nudUserSpecifiedFWHM.Name = "nudUserSpecifiedFWHM";
            this.nudUserSpecifiedFWHM.Size = new System.Drawing.Size(46, 20);
            this.nudUserSpecifiedFWHM.TabIndex = 37;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(53, 5);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(18, 13);
            this.label14.TabIndex = 30;
            this.label14.Text = "px";
            // 
            // rgSeeing
            // 
            this.rgSeeing.Controls.Add(this.rbSeeingUser);
            this.rgSeeing.Controls.Add(this.rbSeeingAuto);
            this.rgSeeing.Location = new System.Drawing.Point(10, 14);
            this.rgSeeing.Name = "rgSeeing";
            this.rgSeeing.Size = new System.Drawing.Size(190, 54);
            this.rgSeeing.TabIndex = 38;
            this.rgSeeing.TabStop = false;
            // 
            // rbSeeingUser
            // 
            this.rbSeeingUser.AutoSize = true;
            this.rbSeeingUser.Location = new System.Drawing.Point(7, 30);
            this.rbSeeingUser.Name = "rbSeeingUser";
            this.rbSeeingUser.Size = new System.Drawing.Size(94, 17);
            this.rbSeeingUser.TabIndex = 40;
            this.rbSeeingUser.Text = "User Specified";
            this.rbSeeingUser.UseVisualStyleBackColor = true;
            this.rbSeeingUser.CheckedChanged += new System.EventHandler(this.rbSeeingUser_CheckedChanged);
            // 
            // rbSeeingAuto
            // 
            this.rbSeeingAuto.AutoSize = true;
            this.rbSeeingAuto.Checked = true;
            this.rbSeeingAuto.Location = new System.Drawing.Point(7, 12);
            this.rbSeeingAuto.Name = "rbSeeingAuto";
            this.rbSeeingAuto.Size = new System.Drawing.Size(47, 17);
            this.rbSeeingAuto.TabIndex = 0;
            this.rbSeeingAuto.TabStop = true;
            this.rbSeeingAuto.Text = "Auto";
            this.rbSeeingAuto.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 26);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(86, 13);
            this.label6.TabIndex = 31;
            this.label6.Text = "PSF Quadrature:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 13);
            this.label3.TabIndex = 29;
            this.label3.Text = "Fitting Method:";
            // 
            // groupControl1
            // 
            this.groupControl1.Controls.Add(this.nudSubPixelSquare);
            this.groupControl1.Controls.Add(this.label2);
            this.groupControl1.Controls.Add(this.nudPhotoAperture);
            this.groupControl1.Controls.Add(this.cbxPhotoSignalApertureType);
            this.groupControl1.Controls.Add(this.label17);
            this.groupControl1.Location = new System.Drawing.Point(3, 3);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(233, 110);
            this.groupControl1.TabIndex = 35;
            this.groupControl1.TabStop = false;
            this.groupControl1.Text = "Aperture Photometry";
            // 
            // nudSubPixelSquare
            // 
            this.nudSubPixelSquare.Location = new System.Drawing.Point(109, 77);
            this.nudSubPixelSquare.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nudSubPixelSquare.Name = "nudSubPixelSquare";
            this.nudSubPixelSquare.Size = new System.Drawing.Size(40, 20);
            this.nudSubPixelSquare.TabIndex = 38;
            this.nudSubPixelSquare.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 37;
            this.label2.Text = "Sub-pixel square:";
            // 
            // nudPhotoAperture
            // 
            this.nudPhotoAperture.DecimalPlaces = 1;
            this.nudPhotoAperture.Location = new System.Drawing.Point(54, 45);
            this.nudPhotoAperture.Name = "nudPhotoAperture";
            this.nudPhotoAperture.Size = new System.Drawing.Size(47, 20);
            this.nudPhotoAperture.TabIndex = 36;
            // 
            // cbxPhotoSignalApertureType
            // 
            this.cbxPhotoSignalApertureType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxPhotoSignalApertureType.Items.AddRange(new object[] {
            "FHWM",
            "Pixels"});
            this.cbxPhotoSignalApertureType.Location = new System.Drawing.Point(107, 45);
            this.cbxPhotoSignalApertureType.Name = "cbxPhotoSignalApertureType";
            this.cbxPhotoSignalApertureType.Size = new System.Drawing.Size(76, 21);
            this.cbxPhotoSignalApertureType.TabIndex = 33;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(19, 26);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(119, 13);
            this.label17.TabIndex = 12;
            this.label17.Text = "Default Signal Aperture:";
            // 
            // nudInnerAnnulusInApertures
            // 
            this.nudInnerAnnulusInApertures.DecimalPlaces = 2;
            this.nudInnerAnnulusInApertures.Location = new System.Drawing.Point(51, 106);
            this.nudInnerAnnulusInApertures.Name = "nudInnerAnnulusInApertures";
            this.nudInnerAnnulusInApertures.Size = new System.Drawing.Size(47, 20);
            this.nudInnerAnnulusInApertures.TabIndex = 35;
            // 
            // nudMinimumAnnulusPixels
            // 
            this.nudMinimumAnnulusPixels.Location = new System.Drawing.Point(163, 143);
            this.nudMinimumAnnulusPixels.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudMinimumAnnulusPixels.Name = "nudMinimumAnnulusPixels";
            this.nudMinimumAnnulusPixels.Size = new System.Drawing.Size(50, 20);
            this.nudMinimumAnnulusPixels.TabIndex = 34;
            // 
            // cbxBackgroundMethod
            // 
            this.cbxBackgroundMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxBackgroundMethod.Items.AddRange(new object[] {
            "Average Background",
            "Background Mode",
            "3D Polynomial Fit",
            "PSF-Fitting Background",
            "Median Background"});
            this.cbxBackgroundMethod.Location = new System.Drawing.Point(22, 45);
            this.cbxBackgroundMethod.Name = "cbxBackgroundMethod";
            this.cbxBackgroundMethod.Size = new System.Drawing.Size(191, 21);
            this.cbxBackgroundMethod.TabIndex = 32;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(13, 146);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(150, 13);
            this.label9.TabIndex = 30;
            this.label9.Text = "Pixels in Background Annulus:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(107, 109);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(81, 13);
            this.label8.TabIndex = 29;
            this.label8.Text = "signal apertures";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 84);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(184, 13);
            this.label7.TabIndex = 28;
            this.label7.Text = "Inner Radius of Background Annulus:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 13);
            this.label1.TabIndex = 25;
            this.label1.Text = "Default Background Method:";
            // 
            // nudSNFrameWindow
            // 
            this.nudSNFrameWindow.Location = new System.Drawing.Point(180, 318);
            this.nudSNFrameWindow.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudSNFrameWindow.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudSNFrameWindow.Name = "nudSNFrameWindow";
            this.nudSNFrameWindow.Size = new System.Drawing.Size(47, 20);
            this.nudSNFrameWindow.TabIndex = 58;
            this.nudSNFrameWindow.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(8, 321);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(169, 13);
            this.label25.TabIndex = 57;
            this.label25.Text = "Num Frames for S/N Computation:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.cbxBackgroundMethod);
            this.groupBox1.Controls.Add(this.nudInnerAnnulusInApertures);
            this.groupBox1.Controls.Add(this.nudMinimumAnnulusPixels);
            this.groupBox1.Location = new System.Drawing.Point(242, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(230, 306);
            this.groupBox1.TabIndex = 59;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Background Measurements";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rb3DThirdOrder);
            this.groupBox2.Controls.Add(this.rb3DFirstOrder);
            this.groupBox2.Controls.Add(this.rb3DSecondOrder);
            this.groupBox2.Location = new System.Drawing.Point(19, 185);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(194, 88);
            this.groupBox2.TabIndex = 41;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "3D-Polynomial Fitting";
            // 
            // rb3DThirdOrder
            // 
            this.rb3DThirdOrder.AutoSize = true;
            this.rb3DThirdOrder.Location = new System.Drawing.Point(23, 64);
            this.rb3DThirdOrder.Name = "rb3DThirdOrder";
            this.rb3DThirdOrder.Size = new System.Drawing.Size(70, 17);
            this.rb3DThirdOrder.TabIndex = 40;
            this.rb3DThirdOrder.TabStop = true;
            this.rb3DThirdOrder.Text = "3-rd order";
            this.rb3DThirdOrder.UseVisualStyleBackColor = true;
            // 
            // rb3DFirstOrder
            // 
            this.rb3DFirstOrder.AutoSize = true;
            this.rb3DFirstOrder.Location = new System.Drawing.Point(23, 20);
            this.rb3DFirstOrder.Name = "rb3DFirstOrder";
            this.rb3DFirstOrder.Size = new System.Drawing.Size(69, 17);
            this.rb3DFirstOrder.TabIndex = 38;
            this.rb3DFirstOrder.TabStop = true;
            this.rb3DFirstOrder.Text = "1-st order";
            this.rb3DFirstOrder.UseVisualStyleBackColor = true;
            // 
            // rb3DSecondOrder
            // 
            this.rb3DSecondOrder.AutoSize = true;
            this.rb3DSecondOrder.Location = new System.Drawing.Point(23, 42);
            this.rb3DSecondOrder.Name = "rb3DSecondOrder";
            this.rb3DSecondOrder.Size = new System.Drawing.Size(73, 17);
            this.rb3DSecondOrder.TabIndex = 39;
            this.rb3DSecondOrder.TabStop = true;
            this.rb3DSecondOrder.Text = "2-nd order";
            this.rb3DSecondOrder.UseVisualStyleBackColor = true;
            // 
            // nudStackedViewFrames
            // 
            this.nudStackedViewFrames.Location = new System.Drawing.Point(401, 318);
            this.nudStackedViewFrames.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.nudStackedViewFrames.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nudStackedViewFrames.Name = "nudStackedViewFrames";
            this.nudStackedViewFrames.Size = new System.Drawing.Size(44, 20);
            this.nudStackedViewFrames.TabIndex = 61;
            this.nudStackedViewFrames.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(255, 320);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(145, 13);
            this.label4.TabIndex = 60;
            this.label4.Text = "Stacked Image View Frames:";
            // 
            // ucPhotometry
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.nudStackedViewFrames);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.nudSNFrameWindow);
            this.Controls.Add(this.label25);
            this.Controls.Add(this.groupControl2);
            this.Controls.Add(this.groupControl1);
            this.Name = "ucPhotometry";
            this.Size = new System.Drawing.Size(1052, 349);
            this.groupControl2.ResumeLayout(false);
            this.groupControl2.PerformLayout();
            this.pnlSeeingSettings.ResumeLayout(false);
            this.pnlSeeingSettings.PerformLayout();
            this.pnlUserSeeing.ResumeLayout(false);
            this.pnlUserSeeing.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUserSpecifiedFWHM)).EndInit();
            this.rgSeeing.ResumeLayout(false);
            this.rgSeeing.PerformLayout();
            this.groupControl1.ResumeLayout(false);
            this.groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSubPixelSquare)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPhotoAperture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudInnerAnnulusInApertures)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinimumAnnulusPixels)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSNFrameWindow)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStackedViewFrames)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel pnlSeeingSettings;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Panel pnlUserSeeing;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.GroupBox groupControl1;
		private System.Windows.Forms.GroupBox groupControl2;
		private System.Windows.Forms.ComboBox cbxBackgroundMethod;
		private System.Windows.Forms.ComboBox cbxPhotoSignalApertureType;
		private System.Windows.Forms.ComboBox cbxPsfFittingMethod;
		private System.Windows.Forms.NumericUpDown nudMinimumAnnulusPixels;
		private System.Windows.Forms.NumericUpDown nudInnerAnnulusInApertures;
		private System.Windows.Forms.NumericUpDown nudPhotoAperture;
		private System.Windows.Forms.NumericUpDown nudUserSpecifiedFWHM;
		private System.Windows.Forms.GroupBox rgSeeing;
		private System.Windows.Forms.ComboBox cbxPsfQuadrature;
		private System.Windows.Forms.RadioButton rbSeeingAuto;
        private System.Windows.Forms.RadioButton rbSeeingUser;
		private System.Windows.Forms.NumericUpDown nudSNFrameWindow;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.NumericUpDown nudSubPixelSquare;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.RadioButton rb3DThirdOrder;
		private System.Windows.Forms.RadioButton rb3DFirstOrder;
		private System.Windows.Forms.RadioButton rb3DSecondOrder;
        private System.Windows.Forms.NumericUpDown nudStackedViewFrames;
        private System.Windows.Forms.Label label4;
	}
}
