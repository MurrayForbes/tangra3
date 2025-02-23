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
using Tangra.Config;
using Tangra.Config.SettingPannels;
using Tangra.Controller;
using Tangra.Helpers;
using Tangra.Model.Config;
using Tangra.OCR;
using Tangra.PInvoke;

namespace Tangra.Config.SettingPannels
{
	public partial class ucAnalogueVideo8bit : SettingsPannel
	{
        private VideoController m_VideoController;

	    public ucAnalogueVideo8bit()
	    {
            InitializeComponent();

            cbxRenderingEngineAttemptOrder.Items.Clear();

            string[] availableRenderingEngines = TangraVideo.EnumVideoEngines();
            cbxRenderingEngineAttemptOrder.Items.AddRange(availableRenderingEngines);
	    }

        public ucAnalogueVideo8bit(VideoController videoController)
            : this()
	    {
            m_VideoController = videoController;
	    }

		public override void LoadSettings()
		{
			nudSaturation8bit.SetNUDValue(TangraConfig.Settings.Photometry.Saturation.Saturation8Bit);

            cbxRenderingEngineAttemptOrder.SetCBXIndex(TangraConfig.Settings.Generic.AviRenderingEngineIndex);
			if (cbxRenderingEngineAttemptOrder.SelectedIndex == -1 && cbxRenderingEngineAttemptOrder.Items.Count > 0)
				cbxRenderingEngineAttemptOrder.SelectedIndex = 0;

			cbxColourChannel.SetCBXIndex((int)TangraConfig.Settings.Photometry.ColourChannel);

			cbxEnableOsdOcr.Checked = TangraConfig.Settings.Generic.OsdOcrEnabled;
			cbxOcrAskEveryTime.Checked = TangraConfig.Settings.Generic.OcrAskEveryTime;

            m_VideoController.LoadAvailableOcrEngines(cbxOcrEngine);

			pnlOsdOcr.Enabled = cbxEnableOsdOcr.Checked;
            nudMaxAutocorrectDigits.SetNUDValue(TangraConfig.Settings.Generic.OcrMaxNumberDigitsToAutoCorrect);
		}

		public override void SaveSettings()
		{
			TangraConfig.Settings.Photometry.Saturation.Saturation8Bit = (byte)nudSaturation8bit.Value;
            TangraConfig.Settings.Generic.AviRenderingEngineIndex = cbxRenderingEngineAttemptOrder.SelectedIndex;
			TangraConfig.Settings.Photometry.ColourChannel = (TangraConfig.ColourChannel)cbxColourChannel.SelectedIndex;
			TangraConfig.Settings.Generic.OsdOcrEnabled = cbxEnableOsdOcr.Checked;
			TangraConfig.Settings.Generic.OcrEngine = cbxOcrEngine.Text;
			TangraConfig.Settings.Generic.OcrAskEveryTime = cbxOcrAskEveryTime.Checked;
		    TangraConfig.Settings.Generic.OcrMaxNumberDigitsToAutoCorrect = (int)nudMaxAutocorrectDigits.Value;

			if (!TangraConfig.Settings.Generic.OcrInitialSetupCompleted &&
			    (TangraConfig.Settings.Generic.OsdOcrEnabled == false || TangraConfig.Settings.Generic.OcrAskEveryTime == true))
			{
				TangraConfig.Settings.Generic.OcrInitialSetupCompleted = true;                
			}
		}

		private void cbxEnableOsdOcr_CheckedChanged(object sender, EventArgs e)
		{
			pnlOsdOcr.Enabled = cbxEnableOsdOcr.Checked;
        }

	}
}
