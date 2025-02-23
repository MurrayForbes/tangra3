﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tangra.Helpers;
using Tangra.Model.Config;
using Tangra.Model.Image;
using Tangra.Model.Numerical;
using Tangra.PInvoke;
using Tangra.Video.AstroDigitalVideo;

namespace Tangra.VideoTools
{
	public partial class frmGenerateOccultationVideoModel : Form
	{
		internal class ModelConfig
		{
			public string FileName;
			public int TotalFrames;
			public int NoiseMean;
			public int NoiseStdDev;
			public int FlickeringStdDev;
			public double StandardStarMag;
			public int StandardStarIntensity;
			public double FWHM;
			public double StarMag2;
			public double StarMag3;
			public double StarMag4;
			public double StarMag5;
			public bool SimulateStar2;
			public bool SimulateStar3;
			public bool SimulateStar4;
			public bool SimulateStar5;
			public bool SimulatePassBy;
			public double PassByDist;
			public double PassByMag1; 
			public double PassByMag2;
			public string InfoLine1;
			public string InfoLine2;
			public string InfoLine3;
			public string InfoLine4;
			public double Gamma;
			public bool SimulateMovingBackground;
			public int PolyBgOrder;
			public int PolyBgFreq;
			public double PolyBgShift;
			public double PolyBgDepth;

			public int OccultedNumberOfFrames;
			public double MaxDistance;
		}

		private int m_PolyOrder = 0;
		private double m_PolyDepth = 0;

		private BackgroundModelGenerator m_BgModelGen;

		public frmGenerateOccultationVideoModel()
		{
			InitializeComponent();

			m_BgModelGen = new BackgroundModelGenerator(100, 100, 40);
		}

		private delegate void UpdateUIDelegate(int pbarId, int percent, bool show);

		private void UpdateUI(int pbarId, int percent, bool show)
		{
			pbar.Value = percent;

			if (show && !pbar.Visible)
			{
				pbar.Visible = true;
				pnlConfig.Enabled = false;
			}
			else if (!show && pbar.Visible)
			{
				pbar.Visible = false;
				pnlConfig.Enabled = true;
			}

			pbar.Update();

			Update();
			Application.DoEvents();
		}

		private void InvokeUpdateUI(int pbarId, int percentDone, bool show)
		{
			try
			{
				Invoke(new UpdateUIDelegate(UpdateUI), new object[] { pbarId, percentDone, show });
			}
			catch (InvalidOperationException)
			{ }
		}

		private void GenerateSimulatedVideo(object state)
		{
			InvokeUpdateUI(2, 0, true);

			try
			{
				ModelConfig modelConfig = (ModelConfig) state;

				TangraVideo.CloseAviFile();
				TangraVideo.StartNewAviFile(modelConfig.FileName, 300, 200, 8, 25, false);
				try
				{
					using (Bitmap bmp = new Bitmap(300, 200, PixelFormat.Format24bppRgb))
					{
						AddOnScreenText(bmp, modelConfig, "The simulated video stars from the next frame");
						Pixelmap pixmap = Pixelmap.ConstructFromBitmap(bmp, TangraConfig.ColourChannel.Red);
						TangraVideo.AddAviVideoFrame(pixmap, modelConfig.Gamma, null);
					}

					for (int i = 1; i <= modelConfig.TotalFrames; i++)
					{
						using (Pixelmap pixmap = GenerateFrame(i * 1.0 / modelConfig.TotalFrames, i, modelConfig))
						{
							TangraVideo.AddAviVideoFrame(pixmap, modelConfig.Gamma, null);
						}

						InvokeUpdateUI(2, (int)(100.0 * i / modelConfig.TotalFrames), true);
					}
				}
				finally
				{
					TangraVideo.CloseAviFile();
				}
			}
			finally
			{
				InvokeUpdateUI(2, 100, false);
			}
		}

		private static Font s_SmallFont = new Font(FontFamily.GenericSansSerif, 7);
		private const double FWHM_GAIN_PER_MAG = 0.15;

        private float GetPsfMaxForMagnitude(double standardI, double standardMag, double mag)
        {
            return (float)((double)standardI / Math.Pow(10, (mag - standardMag) / 2.5));
        }

		private Pixelmap GenerateFrame(double percentDone, int frameNo, ModelConfig modelConfig)
		{
            float I1 = (float)modelConfig.StandardStarIntensity;
			float I2 = GetPsfMaxForMagnitude(modelConfig.StandardStarIntensity, modelConfig.StandardStarMag, modelConfig.StarMag2);
            float I3 = GetPsfMaxForMagnitude(modelConfig.StandardStarIntensity, modelConfig.StandardStarMag, modelConfig.StarMag3);
            float I4 = GetPsfMaxForMagnitude(modelConfig.StandardStarIntensity, modelConfig.StandardStarMag, modelConfig.StarMag4);
            float I5 = GetPsfMaxForMagnitude(modelConfig.StandardStarIntensity, modelConfig.StandardStarMag, modelConfig.StarMag5);
			int IPB1 = (int)Math.Round((double)modelConfig.StandardStarIntensity / Math.Pow(10, (modelConfig.PassByMag1 - modelConfig.StandardStarMag) / 2.5));
			int IPB2 = (int)Math.Round((double)modelConfig.StandardStarIntensity / Math.Pow(10, (modelConfig.PassByMag2 - modelConfig.StandardStarMag) / 2.5));
			float fwhm1 = (float) modelConfig.FWHM;
            // NOTE: Use the same FWHM to get accurate photometry
            //float fwhm2 = (float)(modelConfig.FWHM + (modelConfig.StandardStarMag - modelConfig.StarMag2) * FWHM_GAIN_PER_MAG);
            //float fwhm3 = (float)(modelConfig.FWHM + (modelConfig.StandardStarMag - modelConfig.StarMag3) * FWHM_GAIN_PER_MAG);
            //float fwhm4 = (float)(modelConfig.FWHM + (modelConfig.StandardStarMag - modelConfig.StarMag4) * FWHM_GAIN_PER_MAG);
            //float fwhm5 = (float)(modelConfig.FWHM + (modelConfig.StandardStarMag - modelConfig.StarMag5) * FWHM_GAIN_PER_MAG);
			float fwhm_pb1 = (float)(modelConfig.FWHM + (modelConfig.StandardStarMag - modelConfig.PassByMag1) * FWHM_GAIN_PER_MAG);
			float fwhm_pb2 = (float)(modelConfig.FWHM + (modelConfig.StandardStarMag - modelConfig.PassByMag2) * FWHM_GAIN_PER_MAG);

			if (modelConfig.FlickeringStdDev > 0)
			{
                I1 = (int)Math.Round(VideoModelUtils.Random(I1, modelConfig.FlickeringStdDev));
                I2 = (int)Math.Round(VideoModelUtils.Random(I2, modelConfig.FlickeringStdDev));
                I3 = (int)Math.Round(VideoModelUtils.Random(I3, modelConfig.FlickeringStdDev));
                I4 = (int)Math.Round(VideoModelUtils.Random(I4, modelConfig.FlickeringStdDev));
                I5 = (int)Math.Round(VideoModelUtils.Random(I5, modelConfig.FlickeringStdDev));
			}

			int[,] simulatedBackground = new int[300,200];
			for (int x = 0; x < 300; x++)
			for (int y = 0; y < 200; y++)
			{
				simulatedBackground[x, y] = 0;
			}

			using (Bitmap bmp = new Bitmap(300, 200, PixelFormat.Format24bppRgb))
			{
				
				if (modelConfig.SimulateMovingBackground)
				{
					simulatedBackground = m_BgModelGen.GenerateBackground(modelConfig.PolyBgOrder, modelConfig.PolyBgFreq, modelConfig.PolyBgShift, modelConfig.TotalFrames * percentDone, 110, 100, 35);
				}

                VideoModelUtils.GenerateNoise(bmp, simulatedBackground, modelConfig.NoiseMean, modelConfig.NoiseStdDev);

                VideoModelUtils.GenerateStar(bmp, 25, 160, (float)fwhm1, I1);
                if (modelConfig.SimulateStar2) VideoModelUtils.GenerateStar(bmp, 75, 160, (float)fwhm1, I2);
                if (modelConfig.SimulateStar3) VideoModelUtils.GenerateStar(bmp, 125, 160, (float)fwhm1, I3);
                if (modelConfig.SimulateStar4) VideoModelUtils.GenerateStar(bmp, 175, 160, (float)fwhm1, I4);
                if (modelConfig.SimulateStar5) VideoModelUtils.GenerateStar(bmp, 225, 160, (float)fwhm1, I5);

				if (modelConfig.SimulatePassBy)
				{
					double maxVerticaldistance = Math.Sqrt(modelConfig.MaxDistance * modelConfig.MaxDistance - modelConfig.PassByDist * modelConfig.PassByDist);
					bool isOcculted = false;
					if (modelConfig.OccultedNumberOfFrames > 0 && Math.Abs(modelConfig.PassByDist) < 0.2)
					{
						int firstOccFrame = (modelConfig.TotalFrames / 2) - modelConfig.OccultedNumberOfFrames;
						int lastOccFrame = (modelConfig.TotalFrames / 2) - 1;
						isOcculted = frameNo >= firstOccFrame && frameNo <= lastOccFrame;
					}
                    if (!isOcculted) VideoModelUtils.GenerateStar(bmp, 110, 100, fwhm_pb1, IPB1);

                    VideoModelUtils.GenerateStar(bmp, 110 + (float)modelConfig.PassByDist, (float)(100 - maxVerticaldistance + (2 * maxVerticaldistance) * percentDone), fwhm_pb2, IPB2);
				}

				AddOnScreenText(bmp, modelConfig);

				return Pixelmap.ConstructFromBitmap(bmp, TangraConfig.ColourChannel.Red);
			}
		}

		private void AddOnScreenText(Bitmap bmp, ModelConfig modelConfig, string instructions = null)
		{
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.DrawString(modelConfig.InfoLine1, s_SmallFont, Brushes.LightGray, 0, 5);
				g.DrawString(modelConfig.InfoLine2, s_SmallFont, Brushes.LightGray, 0, 17);
				if (modelConfig.InfoLine3 != null) g.DrawString(modelConfig.InfoLine3, s_SmallFont, Brushes.LightGray, 0, 29);
				if (modelConfig.InfoLine4 != null) g.DrawString(modelConfig.InfoLine4, s_SmallFont, Brushes.LightGray, 0, 41);
				if (instructions != null) g.DrawString(instructions, s_SmallFont, Brushes.LightGray, 0, 100);
				g.Save();
			}	
		}

		private void btnGenerateVideo_Click(object sender, EventArgs e)
		{
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				UsageStats.Instance.ModelVideosGenerated++;
				UsageStats.Instance.Save();

				var config = new ModelConfig()
				{
					FileName = saveFileDialog.FileName,
					TotalFrames = (int)nudTotalFrames.Value,
					NoiseMean = (int)nudNoiseMean.Value,
					NoiseStdDev = (int)nudNoiseStdDev.Value,
					FlickeringStdDev = (int)nudStarFlickering.Value,
					StandardStarIntensity = (int)nudStar1Intensity.Value - (int)nudNoiseMean.Value,
					StandardStarMag = (double)nudStar1Mag.Value,
					FWHM = (double)nudStar1FWHM.Value,
					StarMag2 = (double)nudStar2Mag.Value,
					StarMag3 = (double)nudStar3Mag.Value,
					StarMag4 = (double)nudStar4Mag.Value,
					StarMag5 = (double)nudStar5Mag.Value,
					SimulateStar2 = cbxStar2.Checked,
					SimulateStar3 = cbxStar3.Checked,
					SimulateStar4 = cbxStar4.Checked,
					SimulateStar5 = cbxStar5.Checked,
					SimulatePassBy = cbClosePassBySim.Checked,
					PassByDist = (double)nudPassByDist.Value,
					PassByMag1= (double)nudPassByMag1.Value,
					PassByMag2 = (double)nudPassByMag2.Value,
					Gamma = (double)nudGamma.Value,
					SimulateMovingBackground = cbxPolyBackground.Checked,
					PolyBgOrder = m_PolyOrder,
					PolyBgFreq = (int)nudPolyFreq.Value,
					PolyBgShift = (double)nudPolyShift.Value,
					PolyBgDepth = m_PolyDepth,
					OccultedNumberOfFrames = cbOccultationSimulation.Checked ? (int)nudNumOccFrames.Value : 0,
					MaxDistance = (double)nudMaxDist.Value
				};

				config.InfoLine1 = string.Format("Model Video Generated by Tangra v.{0}", ((AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)[0]).Version);
				config.InfoLine2 = string.Format("Noise: {0} +/- {1}, Flickering: {2}, FWHM: {3}, {4} = {5} mag", 
					config.NoiseMean, config.NoiseStdDev, config.FlickeringStdDev, config.FWHM.ToString("0.0"), config.StandardStarIntensity, config.StandardStarMag.ToString("0.00"));

				var modelConfigStr = new StringBuilder("Stars: ");
				if (config.SimulateStar2) modelConfigStr.AppendFormat("m2={0}; ", config.StarMag2.ToString("0.00"));
				if (config.SimulateStar3) modelConfigStr.AppendFormat("m3={0}; ", config.StarMag3.ToString("0.00"));
				if (config.SimulateStar4) modelConfigStr.AppendFormat("m4={0}; ", config.StarMag4.ToString("0.00"));
				if (config.SimulateStar5) modelConfigStr.AppendFormat("m5={0}; ", config.StarMag5.ToString("0.00"));
				config.InfoLine3 = modelConfigStr.ToString();

				modelConfigStr = new StringBuilder("Pass-By: ");
				if (config.SimulatePassBy) modelConfigStr.AppendFormat("{0}->{1}pix, m'={2}, m\"={3}, {4} occulted frames",
					config.MaxDistance.ToString("0.0"), config.PassByDist.ToString("0.0"), config.PassByMag1.ToString("0.00"), config.PassByMag2.ToString("0.00"), config.OccultedNumberOfFrames.ToString());
				config.InfoLine4 = modelConfigStr.ToString();

				ThreadPool.QueueUserWorkItem(new WaitCallback(GenerateSimulatedVideo), config);
			}
		}

		private void btnConfigureBackground_Click(object sender, EventArgs e)
		{
			var frmCfg = new frmGenerate3DPolyBackground(m_BgModelGen);
			if (frmCfg.ShowDialog(this) == DialogResult.OK)
			{
				m_PolyOrder = frmCfg.PolyOrder;
				m_PolyDepth = frmCfg.PolyDepth;

				m_BgModelGen = frmCfg.BackgroundModelGenerator;
			}
		}

		private void cbxPolyBackground_CheckedChanged(object sender, EventArgs e)
		{
			if (m_PolyOrder == 0)
			{
				// If this is the first time the box is checked, then generate some default values
				m_PolyOrder = 2;
				m_PolyDepth = 20;

				m_BgModelGen.GenerateBackgroundModelParameters(m_PolyOrder, m_PolyDepth);
			}
		}

		private void cbOccultationSimulation_CheckedChanged(object sender, EventArgs e)
		{
			if (cbOccultationSimulation.Checked)
			{
				nudPassByDist.Value = 0;
				nudPassByDist.Enabled = false;
			}
			else
			{
				nudPassByDist.Value = 3.5m;
				nudPassByDist.Enabled = true;
			}
		}
	}
}
