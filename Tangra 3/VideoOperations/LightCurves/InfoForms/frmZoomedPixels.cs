﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tangra.Controller;
using Tangra.Model.Config;
using Tangra.Model.Context;
using Tangra.VideoOperations.LightCurves.Tracking;

namespace Tangra.VideoOperations.LightCurves.InfoForms
{
	public partial class frmZoomedPixels : Form
	{
		private static Font s_TitleFont = new Font(FontFamily.GenericSerif, 8);
		private static Font s_CoordsFont = new Font(FontFamily.GenericSerif, 6);

		private LightCurveContext m_Context;
		private LCFile m_LCFile;
		private LCMeasurement[] m_SelectedMeasurements;
		private TangraConfig.LightCurvesDisplaySettings m_DisplaySettings;
	    private frmLightCurve m_Parent;

		private uint m_AllObjectsPeak = 0;

        private PictureBox[] m_TargetBoxes;
		private Dictionary<int, string> m_ObjectTitles = new Dictionary<int, string>();
		private bool[] m_ObjectinGroup = new bool[4];
		private uint m_Saturation;

		private LightCurveController m_LightcurveController;

		internal frmZoomedPixels(LightCurveContext context, LCFile lcFile, TangraConfig.LightCurvesDisplaySettings displaySettings, LightCurveController lightcurveController)
		{
			InitializeComponent();

			m_Context = context;
			m_LCFile = lcFile;
			m_DisplaySettings = displaySettings;
			m_LightcurveController = lightcurveController;

            m_Saturation = TangraConfig.Settings.Photometry.Saturation.GetSaturationForBpp(context.BitPix, context.MaxPixelValue);

			picTarget1Pixels.Image = new Bitmap(picTarget1Pixels.Width, picTarget1Pixels.Height);
			picTarget2Pixels.Image = new Bitmap(picTarget2Pixels.Width, picTarget2Pixels.Height);
			picTarget3Pixels.Image = new Bitmap(picTarget3Pixels.Width, picTarget3Pixels.Height);
			picTarget4Pixels.Image = new Bitmap(picTarget4Pixels.Width, picTarget4Pixels.Height);

			m_AllObjectsPeak = 0;

			if (lcFile.Footer.ReductionContext.BitPix <= 8)
			{
				lblDisplayBandTitle.Text = "Displayed Band:";
				lblDisplayedBand.Text = lcFile.Footer.ReductionContext.ColourChannel.ToString();
			}
			else
			{
				lblDisplayBandTitle.Text = "Digital Video";
				lblDisplayedBand.Text = "";
			}

			m_TargetBoxes = new PictureBox[] { picTarget1Pixels, picTarget2Pixels, picTarget3Pixels, picTarget4Pixels };

            for (int i = 0; i < m_TargetBoxes.Length; i++)
		    {
                warningProvider.SetIconAlignment(m_TargetBoxes[i], ErrorIconAlignment.TopLeft);
                warningProvider.SetIconPadding(m_TargetBoxes[i], -17-16);

                infoProvider.SetIconAlignment(m_TargetBoxes[i], ErrorIconAlignment.TopLeft);
                infoProvider.SetIconPadding(m_TargetBoxes[i], -17);
		    }

			for (int i = 0; i < m_LCFile.Footer.TrackedObjects.Count; i++)
			{
				TrackedObjectConfig cfg = m_LCFile.Footer.TrackedObjects[i];

				m_ObjectinGroup[i] = cfg.GroupId >=0 && m_LCFile.Footer.TrackedObjects.Count(x => x.GroupId == cfg.GroupId) > 1;

				switch(cfg.TrackingType)
				{
					case TrackingType.OccultedStar:
						m_ObjectTitles.Add(i, "Occulted");
						break;

					case TrackingType.GuidingStar:
						m_ObjectTitles.Add(i, "Guiding");
						break;

					case TrackingType.ComparisonStar:
						m_ObjectTitles.Add(i, "No Guiding");
						break;
				}
			}
		}

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

            m_LCFile = null;
		    m_SelectedMeasurements = null;
		}

		internal void HandleNewSelectedFrame(LCMeasurement[] selectedMeasurements)
		{
			m_SelectedMeasurements = selectedMeasurements;

            for (int i = 0; i < m_TargetBoxes.Length; i++)
            {
                infoProvider.SetError(m_TargetBoxes[i], null);
                warningProvider.SetError(m_TargetBoxes[i], null);
            }

		    PlotMeasuredPixels();
		}

		private void PlotMeasuredPixels()
		{
			if (!TangraContext.Current.CanProcessLightCurvePixels)
				return;

			var targetBoxes = new PictureBox[] { picTarget1Pixels, picTarget2Pixels, picTarget3Pixels, picTarget4Pixels };

			if (m_AllObjectsPeak == 0)
			{
				if (m_SelectedMeasurements != null)
				{
					for (int i = 0; i < m_SelectedMeasurements.Length; i++)
					{
						LCMeasurement reading = m_SelectedMeasurements[i];
						if (!LCMeasurement.IsEmpty(reading) &&
							reading.TargetNo >= 0 &&
							reading.TargetNo <= 3)
						{
							uint[,] pixelsToDraw = GetPixelData(reading.PixelData);
							for (int x = 0; x < 35; x++)
								for (int y = 0; y < 35; y++)
								{
									uint pix = pixelsToDraw[x, y];

									if (m_AllObjectsPeak < pix)
										m_AllObjectsPeak = pix;
								}
						}
					}
				}				
			}

            for (int i = 0; i < m_LCFile.Header.ObjectCount; i++)
            {
                LCMeasurement reading = m_SelectedMeasurements != null ? m_SelectedMeasurements[i] : LCMeasurement.Empty;
                if (!LCMeasurement.IsEmpty(reading) &&
                    reading.TargetNo >= 0 &&
                    reading.TargetNo <= 3)
                {
					PlotSingleTargetPixels(targetBoxes[reading.TargetNo], reading.TargetNo, reading, m_AllObjectsPeak);

					if (cbxDrawApertures.Checked)
					{
						string message = m_SelectedMeasurements[i].GetFlagsExplained().Trim();
						bool isWarning = false;
						if (message != null)
						{
							if (message.Contains("W:"))
							{
								isWarning = true;
								message = message.Replace("W:", "");
								message = message.Replace("I:", "");
							}
							else
							{
								message = message.Replace("I:", "");
							}
						}
						if (isWarning)
							warningProvider.SetError(targetBoxes[reading.TargetNo], message);
						else
							infoProvider.SetError(targetBoxes[reading.TargetNo], message);						
					}
					else
					{
						warningProvider.SetError(targetBoxes[reading.TargetNo], null);
						infoProvider.SetError(targetBoxes[reading.TargetNo], null);
					}
                }
                else
                {
                    Bitmap bmp = targetBoxes[i].Image as Bitmap;
                    if (bmp != null)
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.Clear(m_DisplaySettings.BackgroundColor);
                            g.Save();
                        }                        
                    }
                    targetBoxes[i].Refresh();
                }
            }                
		}

        private void PlotSingleTargetPixels(PictureBox pictureBox, int targetNo, LCMeasurement reading, uint allObjectsPeak)
		{
			int MAGN = 4;

			Bitmap image = new Bitmap(35 * MAGN, 35 * MAGN, PixelFormat.Format24bppRgb);

			int pixelsCenterX = (int)Math.Round(reading.X0);
			int pixelsCenterY = (int)Math.Round(reading.Y0);

			uint[,] pixelsToDraw = GetPixelData(reading.PixelData);
			

			for (int x = 0; x < 35; x++)
				for (int y = 0; y < 35; y++)
				{

					uint pixelValue = pixelsToDraw[x, y];
					Color pixelcolor;

					if (pixelValue < m_Saturation)
					{
						uint modifiedValue = (uint)Math.Min(m_Context.MaxPixelValue, (pixelValue + Math.Round((m_Context.MaxPixelValue - pixelValue) * (pixelValue * 1.0 / allObjectsPeak) * tbIntensity.Value / 100.0)));
						byte byteValue = (byte)Math.Round((modifiedValue * 255.0 / m_Context.MaxPixelValue));
						pixelcolor = Color.FromArgb(byteValue, byteValue, byteValue);
					}
					else
						pixelcolor = TangraConfig.Settings.Color.Saturation;

					// TODO: THIS IS SOOO SLOW !!!
					for (int i = 0; i < MAGN; i++)
					{
						for (int j = 0; j < MAGN; j++)
						{
							image.SetPixel(MAGN * x + i, MAGN * y + j, pixelcolor);
						}
					}
				}

			m_LightcurveController.ApplyDisplayModeAdjustments(image);

			pictureBox.Image = image;

			using (Graphics g = Graphics.FromImage(pictureBox.Image))
			{
				if (cbxDrawApertures.Checked)
				{
					float radius = m_Context.ReProcessApertures[reading.TargetNo];

					Pen pen = reading.IsSuccessfulReading
						? m_DisplaySettings.TargetPens[reading.TargetNo]
						: Pens.Gray;

					bool isInGroup = m_ObjectinGroup[reading.TargetNo];
					bool appertureInGroup = isInGroup && m_Context.SignalMethod == TangraConfig.PhotometryReductionMethod.AperturePhotometry;

					float ax = appertureInGroup && reading.ApertureX > 0 ? reading.ApertureX : 17.5f;
					float ay = appertureInGroup && reading.ApertureY > 0 ? reading.ApertureY : 17.5f;
					float ap = appertureInGroup && reading.ApertureSize > 0 ? reading.ApertureSize : radius;

					g.DrawEllipse(
						pen,
						(float)(MAGN * (reading.X0 - pixelsCenterX + ax - ap)),
						(float)(MAGN * (reading.Y0 - pixelsCenterY + ay - ap)),
						2 * ap * MAGN,
						2 * ap * MAGN);

					if (m_LightcurveController.Context.BackgroundMethod != TangraConfig.BackgroundMethod.Background3DPolynomial &&
					    m_LightcurveController.Context.BackgroundMethod != TangraConfig.BackgroundMethod.PSFBackground)
					{
						Pen bgPen = reading.IsSuccessfulReading
							? m_DisplaySettings.TargetBackgroundPens[reading.TargetNo]
							: new Pen(Color.FromArgb(64, Color.Gray.R, Color.Gray.G, Color.Gray.B));

						// If Average Background is used, then plot the background annulus
						float innerRadius = ap * TangraConfig.Settings.Photometry.AnnulusInnerRadius;
						float outerRadius = (float)Math.Sqrt(TangraConfig.Settings.Photometry.AnnulusMinPixels / Math.PI + innerRadius * innerRadius);

						g.DrawEllipse(
							bgPen,
							(float)(MAGN * (reading.X0 - pixelsCenterX + ax - innerRadius)),
							(float)(MAGN * (reading.Y0 - pixelsCenterY + ay - innerRadius)),
							2 * innerRadius * MAGN,
							2 * innerRadius * MAGN);

						g.DrawEllipse(
							bgPen,
							(float)(MAGN * (reading.X0 - pixelsCenterX + ax - outerRadius)),
							(float)(MAGN * (reading.Y0 - pixelsCenterY + ay - outerRadius)),
							2 * outerRadius * MAGN,
							2 * outerRadius * MAGN);
					}

					string title = m_ObjectTitles[reading.TargetNo];
					SizeF fntSize = g.MeasureString(title, s_TitleFont);
					g.DrawString(
						title, s_TitleFont, m_DisplaySettings.TargetBrushes[reading.TargetNo],
						pictureBox.Width - fntSize.Width - 5,
						pictureBox.Height - fntSize.Height - 5);
					
					if (isInGroup)
						g.DrawString("Grouped", s_TitleFont, m_DisplaySettings.TargetBrushes[reading.TargetNo], 2, pictureBox.Height - fntSize.Height - 5);

					string coords = string.Format("{0}, {1}", reading.X0.ToString("0.0"), reading.Y0.ToString("0.0"));
					fntSize = g.MeasureString(coords, s_CoordsFont);

					g.DrawString(
						coords, s_CoordsFont, m_DisplaySettings.TargetBrushes[reading.TargetNo], pictureBox.Width - fntSize.Width - 2, 2);
				}

				g.Save();
			}

			pictureBox.Refresh();
		}

        private void cbxDrawApertures_CheckedChanged(object sender, EventArgs e)
        {
            PlotMeasuredPixels();
        }

        private void frmZoomedPixels_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                if (m_Parent != null) m_Parent.HideZoomedAreas();
            }
        }

        private void frmZoomedPixels_Load(object sender, EventArgs e)
        {
            m_Parent = this.Owner as frmLightCurve;
        }

        private void tbIntensity_ValueChanged(object sender, EventArgs e)
        {
            PlotMeasuredPixels();
        }


		private uint[,] GetPixelData(uint[,] sourcePixels)
		{
			return sourcePixels;
		}

		private void rbPreProcessedData_CheckedChanged(object sender, EventArgs e)
		{
			PlotMeasuredPixels();
		}
	}
}
