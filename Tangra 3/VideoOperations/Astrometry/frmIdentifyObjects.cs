﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tangra.Astrometry;
using Tangra.AstroServices;
using Tangra.Config;
using Tangra.Controller;
using Tangra.Model.Config;

namespace Tangra.VideoOperations.Astrometry
{
	public partial class frmIdentifyObjects : Form
	{
		private IAstrometricFit m_Fit;
		private double m_FovArcMin;
		private DateTime m_UtcTime;
		private double m_MagLimit;
		private string m_ObsCode;

	    private string m_ObjectDesignation;
        private double m_Lambda;
        private double m_Phi;

		internal List<MPCheckEntry> IdentifiedObjects;
	    internal MPEph2.MPEphEntry Position;

        private frmIdentifyObjects()
        {
            InitializeComponent();
        }

		internal frmIdentifyObjects(IAstrometricFit fit, VideoController videoController, double fovArcMin, DateTime utcTime, double magLimit, string obsCode)
            : this()
		{
			m_Fit = fit;
            m_FovArcMin = fovArcMin;
			m_UtcTime = utcTime;
			m_MagLimit = magLimit;
			m_ObsCode = obsCode;

			pnlProgress.Visible = false;
			pnlEnterTime.Visible = true;

		    if (m_UtcTime != DateTime.MinValue && m_UtcTime.Year != 1)
		    {
                dtTime.Value = m_UtcTime;
                dtDate.Value = m_UtcTime;
		    }
		    else
		    {
                DateTime? timeStamp = videoController.GetCurrentFrameTime();
                if (timeStamp != null && timeStamp != DateTime.MinValue && timeStamp.Value.Year != 1 /* Has a day component */)
                {
                    dtTime.Value = timeStamp.Value;
                    dtDate.Value = timeStamp.Value;
                }
                else
                {
                    DateTime dt = TangraConfig.Settings.LastUsed.LastIdentifyObjectsDate;
                    if (dt == DateTime.MinValue) dt = DateTime.Now.ToUniversalTime();
                    dtTime.Value = dt;
                    dtDate.Value = dt;
                }
            }
		}

        internal frmIdentifyObjects(string objectDesignation, DateTime utcTime, string obsCode)
            : this()
        {
            m_UtcTime = utcTime;
            m_ObsCode = obsCode;
            m_ObjectDesignation = objectDesignation;
        }

        internal frmIdentifyObjects(string objectDesignation, DateTime utcTime, double lambda, double phi)
            : this()
        {
            m_UtcTime = utcTime;
            m_Lambda = lambda;
            m_Phi = phi;
            m_ObjectDesignation = objectDesignation;
        }

	    private void frmIdentifyObjects_Load(object sender, EventArgs e)
		{
			if (m_UtcTime != DateTime.MinValue)
				GetData();
		}


		private void btnSearch_Click(object sender, EventArgs e)
		{
			Text = "Identifying Objects (takes time)...";

			m_UtcTime = new DateTime(
				dtDate.Value.Year, dtDate.Value.Month, dtDate.Value.Day,
				dtTime.Value.Hour, dtTime.Value.Minute, dtTime.Value.Second);

			TangraConfig.Settings.LastUsed.LastIdentifyObjectsDate = m_UtcTime;
			TangraConfig.Settings.Save();

			GetData();
		}

		private void GetData()
		{
			pnlProgress.Visible = true;
			pnlEnterTime.Visible = false;

			pbar.MarqueeAnimationSpeed = 55;
			pbar.Style = ProgressBarStyle.Marquee;

			ThreadPool.QueueUserWorkItem(new WaitCallback(CheckBgThread));
		}

		private void CheckBgThread(object state)
		{
			try
			{
                if (m_ObjectDesignation != null)
                {
                    if (!string.IsNullOrEmpty(m_ObsCode))
                         Position = MPEph2.GetCoordinatesForSingleDate(m_ObsCode, m_ObjectDesignation, m_UtcTime);
                    else if (!double.IsNaN(m_Lambda) && !double.IsNaN(m_Phi))
                        Position = MPEph2.GetCoordinatesForSingleDate(m_Lambda, m_Phi, m_ObjectDesignation, m_UtcTime);
                }
                else
                {
                    var obsCode = m_ObsCode;
                    if (string.IsNullOrWhiteSpace(obsCode)) obsCode = TangraConfig.Settings.Astrometry.MPCObservatoryCode;
                    if (string.IsNullOrWhiteSpace(obsCode)) obsCode = "500";
                    IdentifiedObjects = MPCheck.CheckRegion(m_UtcTime, m_Fit.RA0Deg, m_Fit.DE0Deg, m_FovArcMin, m_MagLimit, obsCode);

                    
                    // MPCheck is a low positional precision service. Check each object position with the more accurate MPEph2 service
                    foreach (var entry in IdentifiedObjects)
                    {
                        MPEph2.MPEphEntry position = null;
                        if (!string.IsNullOrEmpty(m_ObsCode))
                            position = MPEph2.GetCoordinatesForSingleDate(m_ObsCode, entry.ObjectName, m_UtcTime);
                        else if (!double.IsNaN(m_Lambda) && !double.IsNaN(m_Phi))
                            position = MPEph2.GetCoordinatesForSingleDate(m_Lambda, m_Phi, entry.ObjectName, m_UtcTime);

                        if (position != null)
                        {
                            entry.RAHours = position.RAHours;
                            entry.DEDeg = position.DEDeg;
                            entry.Mag = position.Mag;                            
                        }
                    }
                }

				Invoke(new MethodInvoker(Done));
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.ToString());
				Invoke(new WaitCallback(Error), ex.Message);
			}
		}

		private void Done()
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void Error(object error)
		{
			string errorMessage = (string)error;
			MessageBox.Show("There was an error accessing the MPC web services:\r\n\r\n" + errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
