﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tangra.Controller;
using Tangra.Helpers;
using Tangra.ImageTools;
using Tangra.Model.Config;
using Tangra.Model.Context;
using Tangra.Model.Video;
using Tangra.Model.VideoOperations;
using Tangra.VideoOperations.LightCurves;

namespace Tangra.VideoOperations.ConvertVideoToFits
{
    public partial class ucConvertVideoToFits : UserControl
    {
        private ConvertVideoToFitsOperation m_Operation;
        private VideoController m_VideoController;

        private bool m_ShowingFields;
        private int m_CurrFrameNo;
        private int m_FirstTimeFrame;
        private int m_LastTimeFrame;
        private bool m_FirstTimeSet;
        private bool m_EnterOCRAttachDate;

        public ucConvertVideoToFits()
        {
            InitializeComponent();
        }

        public ucConvertVideoToFits(ConvertVideoToFitsOperation operation, VideoController videoController)
            : this ()
        {
            m_Operation = operation;
            m_VideoController = videoController;

            var videoFileFormat = m_VideoController.GetVideoFileFormat();

            nudFirstFrame.Minimum = videoController.VideoFirstFrame + (videoFileFormat == VideoFileFormat.AAV ? 1 : 0);
            nudFirstFrame.Maximum = videoController.VideoLastFrame - 1;
            nudFirstFrame.Value = nudFirstFrame.Minimum;

            nudLastFrame.Minimum = videoController.VideoFirstFrame + (videoFileFormat == VideoFileFormat.AAV ? 1 : 0);
            nudLastFrame.Maximum = videoController.VideoLastFrame - 1;
            nudLastFrame.Value = nudLastFrame.Maximum;

            cbxEveryFrame.SelectedIndex = 0;
        }

        internal void NextFrame(int frameNo, ConvertVideoToFitsState state)
        {
            m_CurrFrameNo = frameNo;

            if (state == ConvertVideoToFitsState.Configuring)
            {
                nudFirstFrame.SetNUDValue(frameNo);
            }
            else if (state == ConvertVideoToFitsState.Converting)
            {
                pbar.Value = Math.Max(Math.Min(frameNo, pbar.Maximum), pbar.Minimum);
                pbar.Update();
            }
        }

        private void rbROI_CheckedChanged(object sender, EventArgs e)
        {
            var roiSelector = m_VideoController.CurrentImageTool as RoiSelector;
            if (roiSelector != null)
                roiSelector.Enabled = rbROI.Checked;
        }


        internal void ExportFinished()
        {
            pbar.Value = pbar.Maximum;
            pbar.Update();

            btnCancel.Enabled = false;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (nudFirstFrame.Value >= nudLastFrame.Value)
            {
                m_VideoController.ShowMessageBox(
                    "There must be at least two frames to export.", 
                    "Tangra", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                nudFirstFrame.Focus();
                return;
            }

            bool hasDatePart;
            if (!m_Operation.HasEmbeddedTimeStamps(out hasDatePart))
                PrepareToEnterStartTime();
            else if (!hasDatePart)
                PrepareToEnterStartDate();
            else
                StartExport(UsedTimeBase.EmbeddedTimeStamp);
        }

        private static int[] INDEX_TO_VAL_MAP = new int[] { 1, 2, 4, 8, 16, 32, 64 };

        private void StartExport(UsedTimeBase timeBase)
        {
            var roiSelector = m_VideoController.CurrentImageTool as RoiSelector;
            Rectangle rect = (rbROI.Checked && roiSelector != null) ? roiSelector.SelectedROI : new Rectangle(0, 0, TangraContext.Current.FrameWidth, TangraContext.Current.FrameHeight);

            if (rect.Width < 1 || rect.Height < 1)
            {
                m_VideoController.ShowMessageBox("Error in ROI size.", "Tangra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedPath;

            if (m_VideoController.ShowBrowseFolderDialog("Select Output Directory", TangraConfig.Settings.LastUsed.FitsExportLastFolderLocation, out selectedPath) == DialogResult.OK)
            {
                btnExport.Enabled = false;
                if (roiSelector != null) roiSelector.Enabled = false;
                gbxFormat.Enabled = false;
                gbxFrameSize.Enabled = false;
                gbxSection.Enabled = false;
                pbar.Minimum = (int)nudFirstFrame.Value;
                pbar.Maximum = (int)nudLastFrame.Value;
                pbar.Visible = true;

                pnlEnterTimes.Visible = false;

                TangraConfig.Settings.LastUsed.FitsExportLastFolderLocation = selectedPath;
                TangraConfig.Settings.Save();

                m_Operation.StartExport(
                    selectedPath,
                    rbCube.Checked,
                    (int)nudFirstFrame.Value,
                    (int)nudLastFrame.Value,
                    INDEX_TO_VAL_MAP[cbxEveryFrame.SelectedIndex],
                    rect,
                    timeBase);
            }            
        }

        private void PrepareToEnterStartTime()
        {
            ucUtcTime.DateTimeUtc = m_VideoController.GetBestGuessDateTimeForCurrentFrame();

            pnlEnterTimes.Visible = true;
            lblTimesHeader.Text = "Enter the UTC time of the first exported frame:";
            btnNextTime.Text = "Next >>";
            m_FirstTimeSet = false;

            ucUtcTime.FocusHourControl();

            // Split the video in fields for interlaced video
            m_ShowingFields = m_VideoController.IsPlainAviVideo || m_VideoController.IsAstroAnalogueVideo;

            m_VideoController.MoveToFrame((int)nudFirstFrame.Value);
            m_VideoController.ToggleShowFieldsMode(m_ShowingFields);
            UpdateShowingFieldControls();

            m_FirstTimeFrame = -1;
            m_LastTimeFrame = -1;
        }

        private void PrepareToEnterStartDate()
        {
            ucUtcTime.DateTimeUtc = m_VideoController.GetBestGuessDateTimeForCurrentFrame();

            pnlEnterTimes.Visible = true;
            lblTimesHeader.Text = "Enter the UTC date of the first exported frame:";
            btnNextTime.Text = "Start Export";
            m_EnterOCRAttachDate = true;

            ucUtcTime.FocusDateControl();

            // Split the video in fields for interlaced video
            m_ShowingFields = false;
            m_VideoController.ToggleShowFieldsMode(m_ShowingFields);
            UpdateShowingFieldControls();
        }

        private void UpdateShowingFieldControls()
        {
            if (m_ShowingFields)
                btnShowFields.Text = "Show Frames";
            else
                btnShowFields.Text = "Show Fields";

            btn1FrMinus.Enabled = m_FirstTimeFrame == -1
                                      ? TangraContext.Current.FirstFrame < m_CurrFrameNo
                                      : m_FirstTimeFrame < m_CurrFrameNo;

            btn1FrPlus.Enabled = m_LastTimeFrame == -1
                                     ? TangraContext.Current.LastFrame > m_CurrFrameNo
                                     : m_LastTimeFrame > m_CurrFrameNo;
        }

        private void btn1FrMinus_Click(object sender, EventArgs e)
        {
            m_VideoController.StepBackward();
            UpdateShowingFieldControls();

            if (m_ShowingFields) m_VideoController.ToggleShowFieldsMode(true);
        }

        private void btn1FrPlus_Click(object sender, EventArgs e)
        {
            m_VideoController.StepForward();
            UpdateShowingFieldControls();

            if (m_ShowingFields) m_VideoController.ToggleShowFieldsMode(true);
        }

        private void btnShowFields_Click(object sender, EventArgs e)
        {
            m_VideoController.ToggleShowFieldsMode(!m_ShowingFields);
            m_ShowingFields = !m_ShowingFields;

            UpdateShowingFieldControls();
        }

		private bool IsDuplicatedFrame(int frameId)
		{
			var avoider = new DuplicateFrameAvoider((VideoController)m_VideoController, frameId);
			return avoider.IsDuplicatedFrame();
		}

		private void ShowDuplicatedFrameMessage()
		{
			MessageBox.Show(
				"The current frame appears to be duplicate of the previous or the next frame. Because of this the timestamp on this frame " +
				"may not correspond to the actual time for this frame number. Please move to and enter the timestamp of a different video frame.",
				"Problem found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
		}

        private bool SanityCheckOCRDate()
        {
            if (ucUtcTime.DateTimeUtc.Date == DateTime.Now.Date)
            {
                if (m_VideoController.ShowMessageBox(
                    "The date component is also exported into the FITS header. Please ensure that the selected date it correct. Press OK to continue or Cancel to go back and change the date.",
                    "Tangra",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                {
                    return false;
                }
            }

            return true;
        }

        private void btnNextTime_Click(object sender, EventArgs e)
        {
            if (m_EnterOCRAttachDate)
            {
                if (!SanityCheckOCRDate())
                {
                    return;
                }
                DateTime dateToAttachToOCR = ucUtcTime.DateTimeUtc.Date;
                m_Operation.SetAttachDateToOCR(dateToAttachToOCR);
                StartExport(UsedTimeBase.EmbeddedTimeStamp);

                
            }
            else if (!m_FirstTimeSet)
			{
				if (IsDuplicatedFrame(m_CurrFrameNo))
				{
					ShowDuplicatedFrameMessage();
					return;
				}

                if (!SanityCheckOCRDate())
                {
                    return;
                }

				m_FirstTimeFrame = m_CurrFrameNo;
				m_FirstTimeSet = true;

				lblTimesHeader.Text = "Enter the UTC time of the last exported frame:";
				btnNextTime.Text = "Start Export";
				ucUtcTime.EnterTimeAtTheSameDate();
                m_Operation.SetStartTime(ucUtcTime.DateTimeUtc, m_FirstTimeFrame);

                m_VideoController.MoveToFrame((int)nudLastFrame.Value);
			}
			else
			{
				if (IsDuplicatedFrame(m_CurrFrameNo))
				{
					ShowDuplicatedFrameMessage();
					return;
				}

				m_LastTimeFrame = m_CurrFrameNo;
                m_Operation.SetEndTime(ucUtcTime.DateTimeUtc, m_LastTimeFrame);

                DialogResult checkResult = m_Operation.EnteredTimeIntervalLooksOkay();

                switch (checkResult)
                {
                    case DialogResult.OK:
                        StartExport(UsedTimeBase.UserEnterred);
                        break;

                    case DialogResult.Retry:
                        PrepareToEnterStartTime();
                        return;

                    case DialogResult.Abort:
                        m_VideoController.CloseOpenedVideoFile();
                        return;
                }
			}

			UpdateShowingFieldControls();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            m_Operation.CancelExport();
        }
    }
}
