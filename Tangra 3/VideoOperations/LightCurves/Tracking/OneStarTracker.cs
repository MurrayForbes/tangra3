﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tangra.Model.Astro;
using Tangra.Model.Config;
using Tangra.Model.Helpers;
using Tangra.Model.Image;
using Tangra.Model.Numerical;
using Tangra.Model.VideoOperations;
using Tangra.VideoOperations.LightCurves;

namespace Tangra.VideoOperations.LightCurves.Tracking
{
    internal class OneStarTracker : Tracker
    {
        private int m_RefiningFramesLeft = 0;
        private bool m_IsTrackedSuccessfully = false;
        private int m_FrameNo;
        private int m_RefiningNumFrames = 9;

        private LinearRegression m_LinearFitX = new LinearRegression();
        private LinearRegression m_LinearFitY = new LinearRegression();

        private List<ImagePixel> m_PreviousPositions = new List<ImagePixel>();
        private List<int> m_PreviousPositionFrameIds = new List<int>();

        public OneStarTracker(List<TrackedObjectConfig> measuringStars)
            : base(measuringStars)
        {
            LocateFirstObjects.Clear();
            LocateSecondObjects.Clear();

            // we want the occulted star to be in the locate fistr objects so the brightness fluctoation is determined 
            LocateFirstObjects.Add(OccultedStar);

            m_RefiningNumFrames = LightCurveReductionContext.Instance.IsDriftThrough ? 30 : 10;
        }

		public override bool InitializeNewTracking(IAstroImage astroImage)
		{
			if (base.InitializeNewTracking(astroImage))
			{
				m_RefiningFramesLeft = TangraConfig.Settings.Tracking.RefiningFrames;
				return true;
			}
			else
				return false;
		}

		public override void BeginMeasurements(IAstroImage astroImage)
		{
			base.BeginMeasurements(astroImage);

			// We consider the first frame of the measurements successfully tracked (after the refining is completed)
			m_IsTrackedSuccessfully = true;
		}

        public override void NextFrame(int frameNo, IAstroImage astroImage)
        {
            m_FrameNo = frameNo;
        	
            if (m_Refining)
            {
                // For refining use the base class
                base.NextFrame(frameNo, astroImage);    
            }
            else
            {
                // And for measuring use 
                TrackSingleStar(frameNo, astroImage);
            }

            if (m_RefiningFramesLeft > 0)
            {
				if (OccultedStar.IsLocated)
                    m_RefiningFramesLeft--;
            }
            else
            {
				m_IsTrackedSuccessfully = OccultedStar.IsLocated;
            }

			if (OccultedStar.IsLocated && !m_NotCertain)
                AddPreviousPosition();

			if (OccultedStar.IsLocated)
			{
				OccultedStar.LastKnownGoodPosition = new ImagePixel(OccultedStar.Center);
			    OccultedStar.LastKnownGoodPsfCertainty = OccultedStar.PSFFit != null ? OccultedStar.PSFFit.Certainty : 0;
			}
        }

        private bool m_NotCertain = false;

		private void GetExpectedXY(out float expectedX, out float expectedY)
		{
			if (LightCurveReductionContext.Instance.IsDriftThrough)
			{
				int firstFrameId = m_PreviousPositionFrameIds[0];
				expectedX = (float)m_LinearFitX.ComputeY(m_FrameNo - firstFrameId);
				expectedY = (float)m_LinearFitY.ComputeY(m_FrameNo - firstFrameId);
			}
			else
			{
				expectedX = (float)OccultedStar.LastKnownGoodPosition.XDouble;
				expectedY = (float)OccultedStar.LastKnownGoodPosition.YDouble;
			}
		}

        protected virtual void TrackSingleStar(int frameNo, IAstroImage astroImage)
        {
            OccultedStar.NewFrame();
            m_NotCertain = false;
        	float expectedX;
        	float expectedY;

        	GetExpectedXY(out expectedX, out expectedY);

            uint[,] pixels = astroImage.GetPixelsArea((int)expectedX, (int)expectedY, 17);

            // There is only one object in the area, just do a wide fit followed by a fit with the selected matrix size
            PSFFit gaussian = new PSFFit((int)expectedX, (int)expectedY);
            gaussian.Fit(pixels, 17);

            IImagePixel firstCenter =
                gaussian.IsSolved
                    ? new ImagePixel((int)gaussian.XCenter, (int)gaussian.YCenter)
                    : astroImage.GetCentroid((int)expectedX, (int)expectedY, 17, m_MedianValue);

            if (firstCenter != null)
            {
                // Do a second fit 
                pixels = astroImage.GetPixelsArea(firstCenter.X, firstCenter.Y, 17);
                gaussian = new PSFFit(firstCenter.X, firstCenter.Y);
                gaussian.Fit(pixels, OccultedStar.PsfFitMatrixSize);
                if (gaussian.IsSolved)
                {
                    double signal = gaussian.IMax - gaussian.I0; if (signal < 0) signal = 0;
                    double brightnessFluctoation = signal > OccultedStar.RefinedOrLastSignalLevel
                                                       ? signal / OccultedStar.RefinedOrLastSignalLevel
                                                       : OccultedStar.RefinedOrLastSignalLevel / signal;

                    //double brightnessFluctoation = (trackedObject.RefinedOrLastSignalLevel - gaussian.IMax + gaussian.I0) / trackedObject.RefinedOrLastSignalLevel;
                    double fluckDiff = Math.Abs(brightnessFluctoation) / m_AllowedSignalFluctoation;

                    if (fluckDiff < 1 || (LightCurveReductionContext.Instance.HighFlickeringOrLargeStars && !LightCurveReductionContext.Instance.FullDisappearance && !LightCurveReductionContext.Instance.IsDriftThrough))
                    {
                        OccultedStar.PSFFit = gaussian;
                        OccultedStar.ThisFrameX = (float)gaussian.XCenter;
                        OccultedStar.ThisFrameY = (float)gaussian.YCenter;
                        OccultedStar.ThisSignalLevel = (float)(gaussian.IMax - gaussian.I0);
                        OccultedStar.ThisFrameCertainty = (float)gaussian.Certainty;
                        OccultedStar.SetIsLocated(true, NotMeasuredReasons.TrackedSuccessfully);
                    }
                    else
                    {
                        m_NotCertain = true;

						// This is the Occulted Star, so no brightness fluctoations can be used as excuses!
						FitObjectInLimitedArea(OccultedStar, astroImage, expectedX, expectedY);
						OccultedStar.SetIsLocated(
							LightCurveReductionContext.Instance.FullDisappearance || OccultedStar.PSFFit != null,
							LightCurveReductionContext.Instance.FullDisappearance || OccultedStar.PSFFit != null
								? NotMeasuredReasons.TrackedSuccessfully
								: NotMeasuredReasons.DistanceToleranceTooHighForNonFullDisappearingOccultedStar);

                        m_NotCertain = !OccultedStar.IsLocated;

                        if (m_PreviousPositions.Count > 1 && LightCurveReductionContext.Instance.FullDisappearance && LightCurveReductionContext.Instance.IsDriftThrough)
                        {
                            var prevSignals = m_PreviousPositions.Select(x => x.Brightness).ToList();
                            var prevSignalMedian = prevSignals.Median();
                            var prevSignalSigma = Math.Sqrt(prevSignals.Sum(x => (x - prevSignalMedian)*(x - prevSignalMedian)) / (m_PreviousPositions.Count - 1));
                            var bigBrightnessVariation = (Math.Abs(signal - prevSignalMedian) > prevSignalSigma);
                            m_NotCertain = m_NotCertain || bigBrightnessVariation;
                        }

                        if (m_NotCertain)
                        {
                            OccultedStar.ThisFrameX = expectedX;
                            OccultedStar.ThisFrameY = expectedY;
                            OccultedStar.ThisFrameCertainty = (float)gaussian.Certainty;
                        }
                    }
                }
                else
                {
                    OccultedStar.ThisFrameX = expectedX;
                    OccultedStar.ThisFrameY = expectedY;
                    OccultedStar.ThisFrameCertainty = (float)gaussian.Certainty;

                    Trace.WriteLine(string.Format("Frame {0}: Cannot confirm target {1} [SingleStar]. Cannot solve second PSF", m_FrameNo, OccultedStar.TargetNo));
                    OccultedStar.SetIsLocated(false, NotMeasuredReasons.PSFFittingFailed);
                }
            }
            else
            {
                OccultedStar.ThisFrameX = expectedX;
                OccultedStar.ThisFrameY = expectedY;
                OccultedStar.ThisFrameCertainty = (float)gaussian.Certainty;

                Trace.WriteLine(string.Format("Frame {0}: Cannot confirm target {1} [SingleStar]. Cannot solve first PSF", m_FrameNo, OccultedStar.TargetNo));
                OccultedStar.SetIsLocated(false, NotMeasuredReasons.PSFFittingFailed);
            }
        }

        private void AddPreviousPosition()
        {
            while (m_PreviousPositions.Count > m_RefiningNumFrames)
            {
                m_PreviousPositions.RemoveAt(0);
                m_PreviousPositionFrameIds.RemoveAt(0);
            }

            if (OccultedStar.IsLocated)
            {
                m_PreviousPositions.Add(new ImagePixel(OccultedStar.Center));
                m_PreviousPositionFrameIds.Add(m_FrameNo);
            }

            if (m_RefiningFramesLeft <= 0)
            {
                DoLinearFit();
            }
        }

        private void DoLinearFit()
        {
            m_LinearFitX.Reset();
            m_LinearFitY.Reset();

            // Look for linear movement:
            // x = a + i * b
            // y = a + i * b
            int firstFrameId = m_PreviousPositionFrameIds[0];
            for (int i = 0; i < m_PreviousPositions.Count; i++)
            {
                int deltaFrames = m_PreviousPositionFrameIds[i] - firstFrameId;
                m_LinearFitX.AddDataPoint(deltaFrames, m_PreviousPositions[i].XDouble);
                m_LinearFitY.AddDataPoint(deltaFrames, m_PreviousPositions[i].YDouble);
            }

            m_LinearFitX.Solve();
            m_LinearFitY.Solve();
        }

        public override bool IsTrackedSuccessfully
        {
            get
            {
                return m_IsTrackedSuccessfully;
            }
        }

        public override float RefiningPercentageWorkLeft
        {
            get
            {
                return m_RefiningFramesLeft * 1.0f / TangraConfig.Settings.Tracking.RefiningFrames;
            }
        }

        public override int RefiningFramesRemaining
        {
            get { return m_RefiningFramesLeft; }
        }

        public override void DoManualFrameCorrection(int targetId, int deltaX, int deltaY)
        {
            int firstFrameId = m_PreviousPositionFrameIds[0];
            float expectedX = (float)m_LinearFitX.ComputeY(m_FrameNo - firstFrameId);
            float expectedY = (float)m_LinearFitY.ComputeY(m_FrameNo - firstFrameId);

            m_PreviousPositions.Clear();
            m_PreviousPositionFrameIds.Clear();

            ImagePixel newCenter = new ImagePixel(expectedX + deltaX, expectedY + deltaY);
            m_PreviousPositions.Add(newCenter);
            m_PreviousPositionFrameIds.Add(m_FrameNo);

            m_PreviousPositions.Add(newCenter);
            m_PreviousPositionFrameIds.Add(m_FrameNo - 1);

            m_PreviousPositions.Add(newCenter);
            m_PreviousPositionFrameIds.Add(m_FrameNo - 2);

            DoLinearFit();
        }
    }
}
