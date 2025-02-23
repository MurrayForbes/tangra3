﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Diagnostics;
using System.Windows.Forms;
using Tangra.Model.Helpers;
using Tangra.SDK;

namespace Tangra.Addins
{
	[Serializable]
	public class MarshalByRefLightCurveDataProvider : ILightCurveDataProvider
	{
		private ISingleMeasurement[] m_TargetMeasurements;
		private ISingleMeasurement[] m_Comp1Measurements;
		private ISingleMeasurement[] m_Comp2Measurements;
		private ISingleMeasurement[] m_Comp3Measurements;
		private int m_IntegrationRate;
		private int m_FirstIntegratingFrame;
        private ITangraDrawingSettings m_TangraDrawingSettings;


		private ILightCurveDataProvider m_DelegatedLocalProvider;

		internal MarshalByRefLightCurveDataProvider(ILightCurveDataProvider localProvider)
		{
			m_DelegatedLocalProvider = localProvider;

			FileName = localProvider.FileName;
			NumberOfMeasuredComparisonObjects = localProvider.NumberOfMeasuredComparisonObjects;
            CameraCorrectionsHaveBeenApplied = localProvider.CameraCorrectionsHaveBeenApplied;
		    HasEmbeddedTimeStamps = localProvider.HasEmbeddedTimeStamps;
            VideoCameraName = localProvider.VideoCameraName;
            VideoSystem = localProvider.VideoSystem;
            NumberIntegratedFrames = localProvider.NumberIntegratedFrames;
			MinFrameNumber = localProvider.MinFrameNumber;
			MaxFrameNumber = localProvider.MaxFrameNumber;
		    m_TangraDrawingSettings = localProvider.GetTangraDrawingSettings();

            CurrentlySelectedFrameNumber = localProvider.CurrentlySelectedFrameNumber;
            HasReliableTimeBase = localProvider.HasReliableTimeBase;
			m_TargetMeasurements = localProvider.GetTargetMeasurements();
			if (NumberOfMeasuredComparisonObjects > 0)
				m_Comp1Measurements = localProvider.GetComparisonObjectMeasurements(0);
			if (NumberOfMeasuredComparisonObjects > 1)
				m_Comp2Measurements = localProvider.GetComparisonObjectMeasurements(1);
			if (NumberOfMeasuredComparisonObjects > 2)
				m_Comp3Measurements = localProvider.GetComparisonObjectMeasurements(2);
			localProvider.GetIntegrationRateAndFirstFrame(out m_IntegrationRate, out m_FirstIntegratingFrame);
		}

        public void CurrentlySelectedFrameNumberChanged(int frameNo)
        {
            CurrentlySelectedFrameNumber = frameNo;
        }

		public string FileName { get; private set; }

		public int NumberOfMeasuredComparisonObjects { get; private set; }

        public bool CameraCorrectionsHaveBeenApplied { get; private set; }

        public bool HasEmbeddedTimeStamps { get; private set; }

		public string VideoCameraName { get; private set; }

        public string VideoSystem { get; private set; }

        public int NumberIntegratedFrames { get; private set; }

		public int MinFrameNumber { get; private set; }

		public int MaxFrameNumber { get; private set; }

        public int CurrentlySelectedFrameNumber { get; private set; }

        public bool HasReliableTimeBase { get; private set; }

		public ISingleMeasurement[] GetTargetMeasurements()
		{
			return m_TargetMeasurements;
		}

        public ITangraDrawingSettings GetTangraDrawingSettings()
        {
            return m_TangraDrawingSettings;
        }

		public ISingleMeasurement[] GetComparisonObjectMeasurements(int comparisonObjectId)
		{
			switch (comparisonObjectId)
			{
				case 0:
					return m_Comp1Measurements;
				case 1:
					return m_Comp2Measurements;
				case 2:
					return m_Comp3Measurements;
			}

			throw new IndexOutOfRangeException();
		}

		public void GetIntegrationRateAndFirstFrame(out int integrationRate, out int firstIntegratingFrame)
		{
			integrationRate = m_IntegrationRate;
			firstIntegratingFrame = m_FirstIntegratingFrame;
		}

        public void SetFoundOccultationEvent(int eventId, float dFrame, float rFrame, float dFrameErrorMinus, float dFrameErrorPlus, float rFrameErrorMinus, float rFrameErrorPlus, string dTime, string rTime, bool cameraDelaysKnownToAOTA, int framesIntegrated)
		{
			try
			{
                m_DelegatedLocalProvider.SetFoundOccultationEvent(eventId, dFrame, rFrame, dFrameErrorMinus, dFrameErrorPlus, rFrameErrorMinus, rFrameErrorPlus, dTime, rTime, cameraDelaysKnownToAOTA, framesIntegrated);
			}
			catch (Exception ex)
			{
			    Trace.WriteLine(ex.GetFullStackTrace());
                MessageBox.Show("Error reading data from AOTA: " + ex.Message);
			}
		}

		public void SetNoOccultationEvents()
		{
			try
			{
				m_DelegatedLocalProvider.SetNoOccultationEvents();
			}
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetFullStackTrace());
            }
		}

        public void SetTimeExtractionEngine(string engineAndVersion)
        {
            try
            {
                m_DelegatedLocalProvider.SetTimeExtractionEngine(engineAndVersion);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetFullStackTrace());
            }
        }


        public void FinishedLightCurveEventTimeExtraction()
        {
            try
            {
                m_DelegatedLocalProvider.FinishedLightCurveEventTimeExtraction();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetFullStackTrace());
            }
        }
    }
}
