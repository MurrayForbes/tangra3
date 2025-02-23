﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tangra.Helpers;
using Tangra.Model.Config;
using Tangra.Model.Image;
using Tangra.PInvoke;
using Tangra.Video.AstroDigitalVideo.UI;

namespace Tangra.Video.AstroDigitalVideo
{
	public partial class frmAdvViewer : Form
	{
		private string m_FileName;
		private AdvFile m_AdvFile;

		private AdvImageData m_CurrentImageData;
		private Pixelmap m_CurrentPixelmap;
		private AdvStatusData m_CurrentStatusData;
	    private bool m_IsAavFile = false;

	    public frmAdvViewer(string fileName)
		{
			InitializeComponent();

			timerScrolling.Enabled = false;

			m_FileName = fileName;
			m_AdvFile = AdvFile.OpenFile(fileName);
		}

		private void frmAdvViewer_Load(object sender, EventArgs e)
		{
			if (m_AdvFile.IsCorrupted)
				MessageBox.Show(
					this,
					"This is not a V1 ADV/AAV file or it is corrupted. Try using [Tools] -> [ADV/AAV Tools] -> [Repair ADV/AAV File] to repair it.",
					"Tangra",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			else
				PresentFile();
		}

		private void PresentFile()
		{
			lblFileName.Text = m_FileName;
			lblNumberFrames.Text = m_AdvFile.NumberOfFrames.ToString();
		    m_IsAavFile = m_AdvFile.AdvFileTags["FSTF-TYPE"] == "AAV";
			
			var bindingList = new List<AdvTagValuePair>();
			foreach(string key in m_AdvFile.AdvFileTags.Keys)
			{
				bindingList.Add(new AdvTagValuePair() {Tag = key, Value = m_AdvFile.AdvFileTags[key]});
			}
			dgvFileTags.DataSource = bindingList;

			lvImageLayouts.Items.Clear();
			foreach(byte layoutId in m_AdvFile.ImageSection.ImageLayouts.Keys)
			{
				AdvImageLayout layout = m_AdvFile.ImageSection.ImageLayouts[layoutId];

				ListViewItem li = lvImageLayouts.Items.Add(layoutId.ToString());
				li.SubItems.Add(layout.BitsPerPixel.ToString());
				li.SubItems.Add(layout.IsDiffCorrLayout ? "yes" : "no");
				li.Tag = layout;
			}
			if (lvImageLayouts.Items.Count > 0)
				lvImageLayouts.Items[0].Selected = true;


			lblWidth.Text = m_AdvFile.ImageSection.Width.ToString();
			lblHeight.Text = m_AdvFile.ImageSection.Height.ToString();
			lblBpp.Text = m_AdvFile.ImageSection.BitsPerPixel.ToString();

			var imageTagsList = new List<AdvTagValuePair>();
			foreach(string key in m_AdvFile.ImageSection.ImageSerializationProperties.Keys)
			{
				imageTagsList.Add(new AdvTagValuePair() {Tag = key, Value = m_AdvFile.ImageSection.ImageSerializationProperties[key]});
			}
			dgvImageTags.DataSource = imageTagsList;

			lvStatusTags.Items.Clear();
			int idx = 0;
			foreach (AdvTagDefinition tagDef in m_AdvFile.StatusSection.TagDefinitions)
			{
				ListViewItem li = lvStatusTags.Items.Add(idx.ToString());
				li.SubItems.Add(tagDef.Name);
				li.SubItems.Add(tagDef.Type.ToString());
				li.Tag = tagDef;

				idx++;
			}

			sbFrames.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
			nudCropFirstFrame.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
			nudCropFirstFrame.Value = 0;
			nudCropLastFrame.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
			nudCropLastFrame.Value = nudCropLastFrame.Maximum;

            nudAviFirstFrame.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
            nudAviFirstFrame.Value = 0;
            nudAviLastFrame.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
			nudAviLastFrame.Value = nudAviFirstFrame.Maximum;

			nudCsvFirstFrame.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
            nudCsvFirstFrame.Value = 0;
			nudCsvLastFrame.Maximum = (int)m_AdvFile.NumberOfFrames - 1;
			nudCsvLastFrame.Value = nudCsvFirstFrame.Maximum;

			LoadFrame(0);

            gbxAdvSplit.Text = m_IsAavFile ? "AAV Crop" : "ADV Crop";
            gbxConvertToAVI.Text = m_IsAavFile ? "AAV to AVI" : "ADV to AVI";
			gbxConvertToCSV.Text = m_IsAavFile ? "AAV to CSV (No video frames exported)" : "ADV to CSV (No video frames exported)";
		    btnCropADV.Text = m_IsAavFile ? "Save AAV chunk as ..." : "Save ADV chunk as ...";

            cbxFrameRate.SelectedIndex = 0;
		    cbxAddedGamma.SelectedIndex = 0;

#if !WIN32
		    gbxConvertToAVI.Visible = false;
#endif
		}

		private void lvImageLayouts_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvImageLayouts.SelectedItems.Count == 1)
			{
				AdvImageLayout layout = (AdvImageLayout)lvImageLayouts.SelectedItems[0].Tag;

				var bindingList = new List<AdvTagValuePair>();
				foreach (string key in layout.ImageSerializationProperties.Keys)
				{
					bindingList.Add(new AdvTagValuePair() { Tag = key, Value = layout.ImageSerializationProperties[key] });
				}
				dgvLayoutTags.DataSource = bindingList;
			}
		}

		private void sbFrames_Scroll(object sender, ScrollEventArgs e)
		{
			timerScrolling.Enabled = false;
			timerScrolling.Enabled = true;
		}

		private void timerScrolling_Tick(object sender, EventArgs e)
		{
			timerScrolling.Enabled = false;
			LoadFrame(sbFrames.Value);

		}

		private void LoadFrame(int frameId)
		{
			Bitmap displayBitmap;

			m_CurrentPixelmap = m_AdvFile.GetFrameData(frameId, out m_CurrentImageData, out m_CurrentStatusData, out displayBitmap);

			picSmallImage.Image = displayBitmap;

			int numSatellites = 0;
			string gamma = "";
			string gain = "";
			string shutter = "";
			string offset = "";
			string systemTime = string.Empty;
            string almanacOffsetStr = "";
            string almanacStatusStr = "";

			lvFrameStatusData.Items.Clear();

			foreach(AdvTagDefinition statusTag in m_CurrentStatusData.TagValues.Keys)
			{
				
				string tagValue = m_CurrentStatusData.TagValues[statusTag];
				if ((statusTag.Name == "SystemTime" || statusTag.Name == "NTPStartTimestamp" || statusTag.Name == "NTPEndTimestamp" || statusTag.Name == "StartTimestampSecondary" || statusTag.Name == "EndTimestampSecondary") && 
                    !string.IsNullOrEmpty(tagValue))
				{
					systemTime = AdvFile.ADV_ZERO_DATE_REF.AddMilliseconds(long.Parse(tagValue)).ToString("dd-MMM-yyyy HH:mm:ss.fff");
					tagValue = systemTime;
				}
				else if (statusTag.Name == "SystemTimeFileTime" && !string.IsNullOrEmpty(tagValue))
				{
                    tagValue = new DateTime(long.Parse(tagValue)).ToString("dd-MMM-yyyy HH:mm:ss.ffffff");
				}
                else if (statusTag.Name == "OcrTime" && !string.IsNullOrEmpty(tagValue))
                {
                    tagValue = new DateTime(long.Parse(tagValue)).ToString("dd-MMM-yyyy HH:mm:ss.ffff");
                }

				if (statusTag.Name == "GPSTrackedSatellites" && !string.IsNullOrEmpty(tagValue))
					numSatellites = int.Parse(tagValue);
				else if (statusTag.Name == "Gamma" && !string.IsNullOrEmpty(tagValue))
				{
					gamma = string.Format("{0:0.000}", float.Parse(tagValue));
					tagValue = gamma;
				}
				else if (statusTag.Name == "Gain" && !string.IsNullOrEmpty(tagValue))
				{
					gain = string.Format("{0:0} dB", float.Parse(tagValue));
					tagValue = gain;
				}
				else if (statusTag.Name == "Shutter" && !string.IsNullOrEmpty(tagValue))
				{
					shutter = string.Format("{0:0.000} sec", float.Parse(tagValue));
					tagValue = shutter;
				}
				else if (statusTag.Name == "Offset" && !string.IsNullOrEmpty(tagValue))
				{
					offset = string.Format("{0:0.00} %", float.Parse(tagValue));
					tagValue = offset;
				}
				else if ((statusTag.Name == "VideoCameraFrameId" || statusTag.Name == "HardwareTimerFrameId") && !string.IsNullOrEmpty(tagValue))
				{
					tagValue = int.Parse(tagValue).ToString("#,###,###,###,###");
				}
				else if (statusTag.Name == "GPSAlmanacStatus" && !string.IsNullOrEmpty(tagValue))
				{
					int almanacStatus = int.Parse(tagValue);
					tagValue = AdvStatusValuesHelper.TranslateGpsAlmanacStatus(almanacStatus);
				    almanacStatusStr = tagValue;
				}
				else if (statusTag.Name == "GPSAlmanacOffset" && !string.IsNullOrEmpty(tagValue))
				{
					int almanacOffset = int.Parse(tagValue);
					if ((almanacOffset & 0x80) == 0x80)
						almanacOffset = (short) (almanacOffset + (0xFF << 8));

					tagValue = AdvStatusValuesHelper.TranslateGpsAlmanacOffset(1, almanacOffset, false);
                    almanacOffsetStr = tagValue;
				}
				else if (statusTag.Name == "GPSFixStatus" && !string.IsNullOrEmpty(tagValue))
				{
					int fixStatus = int.Parse(tagValue);
					tagValue = AdvStatusValuesHelper.TranslateGpsFixStatus(fixStatus);
				}

				if (!string.IsNullOrEmpty(tagValue) && (statusTag.Name == "UserCommand" || statusTag.Name == "SystemError" || statusTag.Name == "GPSFix"))
				{
					string[] tokens = tagValue.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < tokens.Length; i++)
					{
						ListViewItem li = lvFrameStatusData.Items.Add(string.Format("{0}[{1}]", statusTag.Name, i+1));
						li.SubItems.Add(tokens[i]);
						li.Tag = statusTag;	
					}
				}
				else
				{
					ListViewItem li = lvFrameStatusData.Items.Add(statusTag.Name);
					li.SubItems.Add(tagValue);
					li.Tag = statusTag;	
				} 
				
			}

			lblFrameId.Text = string.Format("Frame {0} of {1}", frameId, sbFrames.Maximum);
			lblFrameStart.Text = m_CurrentImageData.MidExposureUtc.AddMilliseconds(-0.5 * m_CurrentImageData.ExposureMilliseconds).ToString("dd MMM yyyy HH:mm:ss.fff");
			lblFrameEnd.Text = m_CurrentImageData.MidExposureUtc.AddMilliseconds(0.5 * m_CurrentImageData.ExposureMilliseconds).ToString("dd MMM yyyy HH:mm:ss.fff");

			lblFrameNumSatellites.Text = numSatellites.ToString();
			lblFrameGain.Text = gain;
			lblFrameGamma.Text = gamma;
			lblFrameOffset.Text = offset;
			lblFrameSystemTime.Text = systemTime;
            lblFrameAlmanacOffset.Text = almanacOffsetStr;
            lblFrameAlmanacStatus.Text = almanacStatusStr;

			double fps = 1000.0 / m_CurrentImageData.ExposureMilliseconds;
			lblFrameExposure.Text = string.Format("{0:0.0} ms ({1} {2})", m_CurrentImageData.ExposureMilliseconds, fps > 0 ? Math.Round(fps) : Math.Round(1 / fps), fps > 0 ? "fps" : "spf");

			lblFrameLayout.Text = string.Format("#{0} - {1}", m_CurrentImageData.LayoutId, m_CurrentImageData.ByteMode);
			lblDataBlockSize.Text = string.Format("{0} bytes", m_CurrentImageData.DataBlocksBytesCount.ToString("#,###,###,###"));
		}

		private void btnCropADV_Click(object sender, EventArgs e)
		{
			if (nudCropLastFrame.Value < nudCropFirstFrame.Value)
			{
				MessageBox.Show(
					this, 
					"The last frame cannot be 'before' the first frame.", 
					"Tangra", 
					MessageBoxButtons.OK, 
					MessageBoxIcon.Error);

				nudCropLastFrame.Focus();
				return;
			}

			if (Path.GetExtension(m_FileName).Equals(".adv", StringComparison.InvariantCultureIgnoreCase))
				saveFileDialog.Filter = "ADV Files (*.adv)|*.adv";
			else if (Path.GetExtension(m_FileName).Equals(".aav", StringComparison.InvariantCultureIgnoreCase))
				saveFileDialog.Filter = "AAV Files (*.aav)|*.aav";
			else
				saveFileDialog.Filter = "All Files (*.*)|*.*";

			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				UsageStats.Instance.FSTFCropInvoked++;
				UsageStats.Instance.Save();

				ThreadPool.QueueUserWorkItem(new WaitCallback(CropFileWorker), new Tuple<string, int, int>(saveFileDialog.FileName, (int)nudCropFirstFrame.Value, (int)nudCropLastFrame.Value));
			}
		}

		private void CropFileWorker(object state)
		{
			Tuple<string, int, int> cropFileCfg = (Tuple<string, int, int>) state;

			InvokeUpdateUI(1, 0, true);

			try
			{
				m_AdvFile.CropAdvFile(
					cropFileCfg.Item1,
					cropFileCfg.Item2,
					cropFileCfg.Item3,
					delegate(int percentDone, int framesFound)
						{
                            InvokeUpdateUI(1, percentDone, true);
						});
			}
			finally
			{
                InvokeUpdateUI(1, 100, false);
			}
		}

#if WIN32
        private void SaveAsAviFileWorker(object state)
        {
            Tuple<string, int, int, bool, double, double, AdvToAviConverter> cropFileCfg = (Tuple<string, int, int, bool, double, double, AdvToAviConverter>)state;

            InvokeUpdateUI(2, 0, true);

            try
            {
                m_AdvFile.SaveAsAviFile(
                    cropFileCfg.Item1,
                    cropFileCfg.Item2,
                    cropFileCfg.Item3,
                    cropFileCfg.Item7,
                    cropFileCfg.Item4,
                    cropFileCfg.Item5,
                    cropFileCfg.Item6,
                    delegate(int percentDone, int framesFound)
                    {
                        InvokeUpdateUI(2, percentDone, true);
                    });
            }
            finally
            {
                InvokeUpdateUI(2, 100, false);
            }
        }
#endif
		private void ExportToCSVWorker(object state)
		{
			Tuple<string, int, int> cropFileCfg = (Tuple<string, int, int>)state;

			InvokeUpdateUI(3, 0, true);

			try
			{
				m_AdvFile.ExportStatusSectionToCSV(
					cropFileCfg.Item1,
					cropFileCfg.Item2,
					cropFileCfg.Item3,
					delegate(int percentDone, int framesFound)
					{
						InvokeUpdateUI(3, percentDone, true);
					});
			}
			finally
			{
				InvokeUpdateUI(3, 100, false);
			}			
		}

        private delegate void UpdateUIDelegate(int pbarId, int percent, bool show);

		private void UpdateUI(int pbarId, int percent, bool show)
		{
			ProgressBar pbar;
			if (pbarId == 1) pbar = pbar1;
			else if (pbarId == 2) pbar = pbar2;
			else if (pbarId == 3) pbar = pbar3;
			else pbar = pbar1;
            
            pbar.Value = percent;

            if (show && !pbar.Visible)
			{
                pbar.Visible = true;
				pnlCropChooseFrames.Enabled = false;
                pnlToAviConfig.Enabled = false;
                pnlToCSVConfig.Enabled = false;
			}
            else if (!show && pbar.Visible)
			{
                pbar.Visible = false;
				pnlCropChooseFrames.Enabled = true;
                pnlToAviConfig.Enabled = true;
                pnlToCSVConfig.Enabled = true;
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

        private void btnSaveAsAVI_Click(object sender, EventArgs e)
        {
#if WIN32
            if (nudAviLastFrame.Value < nudAviFirstFrame.Value)
            {
                MessageBox.Show(
                    this,
                    "The last frame cannot be 'before' the first frame.",
                    "Tangra",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                nudAviLastFrame.Focus();
                return;
            }

            if (cbxSpecifyCodec.Checked)
            {
                MessageBox.Show(
                    this,
                    "Please note that some of the codecs on the system may not be working correctly with this conversion to AVI by Tangra.",
                    "Tangra",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            if (MessageBox.Show(
                this,
                "Please note that the AVI export is doing resampling of the original video which will typically cause frames to duplicated and/or dropped.\r\n\r\nThis export function is meant to be used for video streaming (i.e. sharing the video for viewing on the Internet) and should not be used to convert the video to another format for measuring in another software. If you want to measure the video in another software either measure it directly as ADV/AAV file (if supported) or export it to a FITS file sequence from the main file menu and measure the FITS images.\r\n\r\nDo you wish to continue?",
                "Tangra",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (saveAviFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                double msPerFrame = cbxFrameRate.SelectedIndex == 0 ? 40 : 33.37;
                double addedGamma = 1;
                if (cbxAddedGamma.SelectedIndex == 1)
                    addedGamma = 0.45;
                else if (cbxAddedGamma.SelectedIndex == 2)
                    addedGamma = 0.35;
                else if (cbxAddedGamma.SelectedIndex == 3)
                    addedGamma = 1 / 0.45;
                else if (cbxAddedGamma.SelectedIndex == 4)
                    addedGamma = 1 / 0.35;

				UsageStats.Instance.FSTSToAVIInvoked++;
				UsageStats.Instance.Save();

                ThreadPool.QueueUserWorkItem(
                    new WaitCallback(SaveAsAviFileWorker),
                    new Tuple<string, int, int, bool, double, double, AdvToAviConverter>(
                        saveAviFileDialog.FileName, 
                        (int)nudAviFirstFrame.Value, 
                        (int)nudAviLastFrame.Value,
                        cbxSpecifyCodec.Checked,
                        msPerFrame,
                        addedGamma,
                        AdvToAviConverter.VideoForWindowsAviSaver));
            }
#endif
        }

		private void btnSaveAsCSV_Click(object sender, EventArgs e)
		{
            if (nudCsvLastFrame.Value < nudCsvFirstFrame.Value)
			{
				MessageBox.Show(
					this,
					"The last frame cannot be 'before' the first frame.",
					"Tangra",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);

                nudCsvLastFrame.Focus();
				return;
			}

			saveFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
			saveFileDialog.FileName = Path.ChangeExtension(m_FileName, "csv");

			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				UsageStats.Instance.FSTSToCSVInvoked++;
				UsageStats.Instance.Save();

                ThreadPool.QueueUserWorkItem(new WaitCallback(ExportToCSVWorker), new Tuple<string, int, int>(saveFileDialog.FileName, (int)nudCsvFirstFrame.Value, (int)nudCsvLastFrame.Value));
			}
		}
	}	
}
