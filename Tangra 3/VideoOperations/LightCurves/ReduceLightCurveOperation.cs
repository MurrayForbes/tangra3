﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tangra.Controller;
using Tangra.Helpers;
using Tangra.Model.Astro;
using Tangra.Model.Config;
using Tangra.Model.Context;
using Tangra.Model.Helpers;
using Tangra.Model.Image;
using Tangra.Model.ImageTools;
using Tangra.Model.Video;
using Tangra.Model.VideoOperations;
using Tangra.Video;
using Tangra.VideoOperations.LightCurves;
using Tangra.Config;
using Tangra.ImageTools;
using Tangra.PInvoke;
using Tangra.VideoOperations.LightCurves.Helpers;
using Tangra.VideoOperations.LightCurves.Measurements;
using Tangra.VideoOperations.LightCurves.MutualEvents;
using Tangra.VideoOperations.LightCurves.Tracking;
using Tangra.Resources;
using Tangra.Video.SER;
using TangraCore = Tangra.OCR.TangraCore;


namespace Tangra.VideoOperations.LightCurves
{
    public enum UsedTimeBase
    {
        NoTimesBaseAvailable,
        UserEnterred,
        EmbeddedTimeStamp
    }

    public class ReduceLightCurveOperation : VideoOperationBase, IVideoOperation
    {
        private VideoController m_VideoController;
        internal AstroImage m_AstroImage;

        private ucLightCurves m_ControlPanel = null;
        private AstroImage m_StackedAstroImage;

        private object m_SyncRoot = new object();

        private LCStateMachine m_StateMachine;
		private ITracker m_Tracker;
        private AveragedFrame m_AveragedFrame;

        private MeasurementsHelper m_Measurer;
        private GroupMeasurer m_GroupMeasurer;

        private int m_ProcessedFrames = 0;
        private int m_UnsuccessfulFrames = 0;
	    private int m_PartiallySuccessfulFrames = 0;
        private Stopwatch m_StopWatch = new Stopwatch();

	    private uint[] m_LastTotalReading = new uint[4];
	    private uint[] m_LastTotalBackground = new uint[4];
	    private float[] m_LastApertureX = new float[4];
		private float[] m_LastApertureY = new float[4];
		private float[] m_LastApertureSize = new float[4];
	    private DateTime[] m_LastOCRedTimeStamp = new DateTime[4];

        internal int m_CurrFrameNo = -1;
        internal string m_CurrFileName = null;
        private int m_FirstMeasuredFrame;
        internal uint m_MinFrame;
        internal uint m_MaxFrame;
        internal uint m_TotalFrames;
        internal int m_MeasurementInterval;
        internal float m_AverageFWHM;

        private DateTime m_OCRedTimeStamp;

	    private string m_InstumentalDelaySelectedCamera;
	    private Dictionary<int, float> m_InstumentalDelaySelectedConfig;
        private bool m_TimingCorrectionsRequired;
        private double? m_AcquisitionDelayMs;
        private double? m_ReferenceTimeToUtcOffsetMs;

        private string m_CameraName;
        private int m_AavFrameIntegration;
	    private string m_AavNativeVideoFormat;
		private bool m_DebugMode;
	    private int m_AavNtpTimestampError = -1;
	    private float m_AavNtpFitOneSigmaError = float.NaN;
        private int m_AavStackedFrameRate;
        private int m_FitsDynamicFromValue;
        private int m_FitsDynamicToValue;

        private bool m_StackedSelection;
        private AstroImage m_StackedSelectionFrame;

        private LCState m_BackedUpSelectMeasuringStarsState = null;

        private MeasuringZoomImageType MeasuringZoomImageType = MeasuringZoomImageType.Stripe;
	    private LightCurveController m_LightCurveController;

	    private LCFile m_LoadedLcFile;

		private int m_NumberFramesWithBadTracking;

	    private TangraConfig.LightCurvesDisplaySettings m_DisplaySettings;

		private Panel m_ControlPanelHolder;

		public ReduceLightCurveOperation()
		{
			Debug.Assert(false, "This constructor should not be called.");
		}

        internal ReduceLightCurveOperation(LightCurveController lightCurveController, bool debugMode)
		{
			m_LightCurveController = lightCurveController;
			m_DebugMode = debugMode;

            m_StackedSelection = TangraConfig.Settings.Photometry.UseStackedSelection;

			m_DisplaySettings = new TangraConfig.LightCurvesDisplaySettings();
			m_DisplaySettings.Load();
			m_DisplaySettings.Initialize();
        }

		internal void SetLCFile(LCFile loadedFile)
		{
			m_LoadedLcFile = loadedFile;
		}

        #region IVideoOperation Members

        private void EnsureControlPanel(Panel controlPanel)
        {
            if (m_ControlPanel == null)
            {
                lock(m_SyncRoot)
                {
                    if (m_ControlPanel == null)
                    {
                        m_ControlPanel = new ucLightCurves();

                        controlPanel.Controls.Clear();
                        controlPanel.Controls.Add(m_ControlPanel);
                        m_ControlPanel.Dock = DockStyle.Fill;
                    }
                }
            }            
        }

        public bool InitializeOperation(IVideoController videoContoller, Panel controlPanel, IFramePlayer framePlayer, Form topForm)
        {
	        m_NumberFramesWithBadTracking = 0;

            m_VideoController = (VideoController)videoContoller;
	        m_ControlPanelHolder = controlPanel;

            if (!m_VideoController.EnsureNotADuplicatedFrame())
                return false;

			var configForm = new frmSelectReductionType(m_VideoController, framePlayer);
            if (configForm.ShowDialog(topForm) == DialogResult.OK)
            {
	            EnsureControlPanel(controlPanel);

	            BeginConfiguration();

	            TangraContext.Current.CanChangeTool = true;

				if (m_VideoController.IsPlainAviVideo &&
					!TangraConfig.Settings.Generic.OcrInitialSetupCompleted)
				{
					var frm = new frmChooseFirstTimeOcrSettings();
					frm.ShowDialog(topForm);
				}

	            return true;
            }

            TangraContext.Current.CanLoadBiasFrame = true;
			TangraContext.Current.CanLoadDarkFrame = true;
			TangraContext.Current.CanLoadFlatFrame = true;

            return false;
        }

        public void PlayerStarted()
        { }

        public void MeasuringStarted()
        {
			if (m_StateMachine.m_CurrentState == LightCurvesState.Running &&
				m_StateMachine.MeasuringStars.Count > 0 &&
				!m_StateMachine.m_HasBeenPaused)
			{
				EnsureMeasurer((float)m_Tracker.PositionTolerance);

				m_Measurer.GetImagePixelsCallback +=
					new MeasurementsHelper.GetImagePixelsDelegate(m_Measurer_GetImagePixelsCallback);

				m_CustomZoomBitmap = null;

				if (TangraConfig.Settings.Photometry.PsfFittingMethod == TangraConfig.PsfFittingMethod.LinearFitOfAveragedModel)
				{
					float averageFWHM =
						(float)
						m_StateMachine.MeasuringStars.Average(ms => ms.Gaussian != null ? ms.Gaussian.FWHM : 3.5);
					m_GroupMeasurer = new GroupMeasurer(averageFWHM, m_StateMachine.MeasuringStars, m_Measurer);
				}
				else
					m_GroupMeasurer = null;
			}
        }

		private void EnsureMeasurer(float positionTolerance)
		{
			m_Measurer = new MeasurementsHelper(
								m_VideoController.VideoBitPix,
								LightCurveReductionContext.Instance.NoiseMethod,
								TangraConfig.Settings.Photometry.SubPixelSquareSize,
                                TangraConfig.Settings.Photometry.Saturation.GetSaturationForBpp(m_VideoController.VideoBitPix, m_VideoController.VideoAav16NormVal));

			m_Measurer.SetCoreProperties(
				TangraConfig.Settings.Photometry.AnnulusInnerRadius,
				TangraConfig.Settings.Photometry.AnnulusMinPixels,
				TangraConfig.PhotometrySettings.REJECTION_BACKGROUND_PIXELS_STD_DEV,
				positionTolerance);
		}

		uint[,] m_Measurer_GetImagePixelsCallback(int x, int y, int matrixSize)
		{
			AstroImage currImage = m_VideoController.GetCurrentAstroImage(false);
			
			return currImage.GetMeasurableAreaPixels(x, y, matrixSize);
		}

        public void FinalizeOperation()
        {
            SaveSessionFile();
        }

		public void SaveSessionFile()
		{
			// NOTE: Tracking session are not supported yet
		}

        internal byte FrameBackgroundMode = 0;
        internal int m_PrevMeasuredFrame = -1;

        public void NextFrame(int frameNo, MovementType movementType, bool isLastFrame, AstroImage astroImage, int firstFrameInIntegrationPeriod, string fileName)
        {
            m_AstroImage = astroImage;
			m_CurrFrameNo = frameNo;
            m_CurrFileName = fileName;

            TangraContext.Current.CrashReportInfo.FrameNumber = frameNo;

            if (m_Correcting)
                // Do not track or process the frame while correcting the tracking
                return;

            if (m_Configuring)
            {
                if (m_CurrFrameNo != frameNo) m_StackedAstroImage = null;
                if (frameNo != m_StateMachine.SelectedObjectFrameNo) m_StateMachine.SelectedObject = null;
                ResetStackedSelectionFrame();
            }

            if (m_Refining || m_Measuring)
            {
                if (m_VideoController.IsAstroAnalogueVideo && frameNo == 0)
                {
                    // Do not attempt refining on the first AAV frame as it has unique dynamic range and the refining is not going to work at all.
                }
                else
                {
                    m_Tracker.NextFrame(frameNo, astroImage);

                    if (!m_Tracker.IsTrackedSuccessfully) m_NumberFramesWithBadTracking++;

                    m_VideoController.SetDisplayHueBackgroundModeTargets(m_Tracker.TrackedObjects);

                    if (m_Refining)
                    {
                        if (m_Tracker.RefiningPercentageWorkLeft <= 0)
                        {
                            bool canSwitchFromRefiningToMeasuringNow = true;

                            if (m_VideoController.IsUsingSteppedAveraging)
                            {
                                // When using stepped averaging the measurements should start at the first frame of an integration 'step'
                                // which means we need to flip the switch from 'Refining' to 'Measuring' one frame before that
                                if (firstFrameInIntegrationPeriod + m_VideoController.FramesToIntegrate - 1 > frameNo)
                                {
                                    Trace.WriteLine(string.Format("Skipping frame {0}, waiting for the first frame in the next integration period to start measurments.", frameNo));
                                    canSwitchFromRefiningToMeasuringNow = false;
                                }
                            }

                            if (canSwitchFromRefiningToMeasuringNow)
                            {
	                            float oneSigma;
								m_AavNtpTimestampError = m_VideoController.AstroAnalogueVideoNormaliseNtpDataIfNeeded(out oneSigma);
	                            m_AavNtpFitOneSigmaError = oneSigma;
                                // Begin measurements
                                m_Measuring = true;
                                m_Refining = false;

                                m_ProcessedFrames = 0;
                                m_UnsuccessfulFrames = 0;
                                m_PartiallySuccessfulFrames = 0;
                                m_StopWatch.Reset();
                                m_StopWatch.Start();

                                m_Tracker.BeginMeasurements(astroImage);

                                // IMPORTANT: The finalHeader must be changed as well if changing this
                                LCFile.NewOnTheFlyOutputFile(
                                    m_VideoController.CurrentVideoFileName,
                                    string.Format("Video ({0})", m_VideoController.CurrentVideoFileType),
                                    (byte)m_Tracker.TrackedObjects.Count, (float)m_Tracker.PositionTolerance,
									LightCurveReductionContext.Instance.LightCurveReductionType == LightCurveReductionType.TotalLunarReppearance);

                                m_MinFrame = uint.MaxValue;
                                m_MaxFrame = uint.MinValue;
                                m_TotalFrames = 0;
                                m_FirstMeasuredFrame = frameNo;

                                m_AverageFWHM = astroImage.GetAverageFWHM();

                                m_VideoController.StatusChanged("Measuring");
                            }
                        }
                    }
                }

                if (m_Measuring)
                {
					if (!m_Tracker.IsTrackedSuccessfully &&
						 LightCurveReductionContext.Instance.StopOnLostTracking)
					{
						m_ControlPanel.StopMeasurements(ucLightCurves.StopReason.LostTracking);

						if (TangraConfig.Settings.Tracking.PlaySound)
							Console.Beep(800, 750);

						if (TangraConfig.Settings.Tracking.PopUpOnLostTracking)
							m_VideoController.ShowMessageBox(
								"Use the mouse to pan the object apertures back to the object location and press 'Continue' to continue with the measurements. You can also do adjustments to the objects individually or skip the frame all together.",
								"Tracking has been lost",
								MessageBoxButtons.OK,
								MessageBoxIcon.Exclamation);

						return;
					}

                    m_ProcessedFrames++;
                    if (!m_Tracker.IsTrackedSuccessfully) m_UnsuccessfulFrames++;
	                if (m_Tracker.IsTrackedSuccessfully && m_Tracker.TrackedObjects.Any(x => !x.IsLocated))
		                m_PartiallySuccessfulFrames++;

                    SaveEmbeddedOrORCedTimeStamp(frameNo);

                    MeasureObjects();

                    m_PrevMeasuredFrame = m_CurrFrameNo;

                    m_StopWatch.Stop();
                    m_ControlPanel.UpdateProcessedFrames(m_ProcessedFrames, m_UnsuccessfulFrames, m_PartiallySuccessfulFrames, (int)(m_StopWatch.ElapsedMilliseconds / 1000));
                    m_StopWatch.Start();
                }
            }
			else if (m_ViewingLightCurve && m_LightCurveController.LcFile != null)
			{
				var currentSelection = new LCMeasurement[m_LightCurveController.LcFile.Header.ObjectCount];

				if (m_LightCurveController.LcFile.Header.MinFrame <= m_CurrFrameNo &&
					m_LightCurveController.LcFile.Header.MaxFrame >= m_CurrFrameNo)
				{
					for (int i = 0; i < m_LightCurveController.LcFile.Header.ObjectCount; i++)
					{
						List<LCMeasurement> measurements = m_LightCurveController.Context.AllReadings[i];
						currentSelection[i] = measurements[(int)(m_CurrFrameNo - m_LightCurveController.LcFile.Header.MinFrame)];
					}

					m_LightCurveController.OnNewSelectedMeasurements(currentSelection.ToArray());
				}
			}

            if (m_ControlPanel != null)
                m_ControlPanel.UpdateState();
            if (isLastFrame)
            {
                if (m_Refining)
                {
                    m_ControlPanel.StopRefining();
                    if (TangraConfig.Settings.Tracking.PlaySound)
                        Console.Beep();
                }
                else if (m_Measuring)
                {
                    m_ControlPanel.StopMeasurements(ucLightCurves.StopReason.EndOfFile);

                    if (TangraConfig.Settings.Tracking.PlaySound)
                        Console.Beep();

                    m_ControlPanel.StoppedAtLastFrame();
                }
            }
        }

        public void ImageToolChanged(ImageTool newTool, ImageTool oldTool)
        { }

        private static Font s_MessageFont = new Font(FontFamily.GenericSansSerif, 9);

        public void PreDraw(System.Drawing.Graphics g)
        {
            if (CanShowStackedSelection() && m_StackedSelection)
            {
                if (m_StackedSelectionFrame == null)
                {
                    m_VideoController.SetCursor(Cursors.WaitCursor);
                    try
                    {
                        m_StackedSelectionFrame = new AstroImage(m_VideoController.StackFrames(TangraConfig.Settings.Photometry.StackedViewNumFrames));
                        g.DrawImage(m_StackedSelectionFrame.Pixelmap.DisplayBitmap, new Point(0, 0));
                    }
                    finally
                    {
                        m_VideoController.SetCursor(Cursors.Default);
                    }
                }

                if (m_StackedSelectionFrame != null)
                    m_VideoController.SetStackedSelectionFrameImage(m_StackedSelectionFrame);
            }
        }

        private void ResetStackedSelectionFrame()
        {
            if (m_StackedSelectionFrame != null && m_StackedSelectionFrame.Pixelmap != null)
                m_StackedSelectionFrame.Pixelmap.Dispose();

            m_StackedSelectionFrame = null;
            if (m_VideoController != null)
                m_VideoController.SetStackedSelectionFrameImage(null);
        }

        internal AstroImage GetStackedAstroImage()
        {
            if (m_StackedSelectionFrame != null)
                return m_StackedSelectionFrame.Clone();
            else
                return m_StackedAstroImage;
        }

        private int[] m_ManualTrackingDeltaX = new int[4];
		private int[] m_ManualTrackingDeltaY = new int[4];

        internal void SetManualTrackingCorrection(int targetId, int deltaX, int deltaY)
        {
			m_ManualTrackingDeltaX[targetId] = deltaX;
			m_ManualTrackingDeltaY[targetId] = deltaY;
        }

        internal bool m_Measuring = false;
        private bool m_Configuring = false;
        private bool m_Refining = false;
        private bool m_Correcting = false;
        private bool m_ViewingLightCurve = false;

        private static Font s_FONT = new Font(FontFamily.GenericMonospace, 8);

        public void PostDraw(Graphics g)
        {
            if (CanShowStackedSelection() && m_StackedSelectionFrame != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var pen = i == 0 ? Pens.DodgerBlue : Pens.Lime;
                    g.DrawRectangle(pen, i, i, g.VisibleClipBounds.Width - 1 - 2 * i, g.VisibleClipBounds.Height - 1 - 2 * i);
                }
                g.DrawRectangle(Pens.DodgerBlue, 0, 0, g.VisibleClipBounds.Width - 1, g.VisibleClipBounds.Height - 1);
                for (int i = 3; i < 18; i++)
                {
                    using (var pen = new Pen(Color.FromArgb(Math.Min(255, 128 - (i - 4) * 128 / 15), Color.Lime.R, Color.Lime.G, Color.Lime.B)))
                    {
                        g.DrawRectangle(pen, i, i, g.VisibleClipBounds.Width - 1 - 2 * i, g.VisibleClipBounds.Height - 1 - 2 * i);
                    }
                }
                string message = "FRAMES STACKED FOR EASIER OBJECT SELECTION";
                var sizeF = g.MeasureString(message, s_MessageFont);
                if (sizeF.Width - 40 < 0.75 * g.VisibleClipBounds.Width)
                {
                    g.DrawString(message, s_MessageFont, Brushes.Lime, g.VisibleClipBounds.Width - 20 - sizeF.Width, 20 + sizeF.Height / 2);
                }
            }

            try
            {
				if (m_ViewingLightCurve && m_LightCurveController.LcFile != null)
				{
					// Display the objects for the current frame
					if (m_LightCurveController.LcFile.Header.MinFrame <= m_CurrFrameNo &&
						m_LightCurveController.LcFile.Header.MaxFrame >= m_CurrFrameNo)
					{
						int frameToDisplay;
						if (m_VideoController.CurrentVideoFileEngine == SingleBitmapFileFrameStream.SINGLE_BMP_FILE_ENGINE)
						{
							// The original file cannot be found and we are displaying the embedded image in the .lc file.
							frameToDisplay = (int)m_LightCurveController.LcFile.Header.MinFrame;
						}
						else
						{
							frameToDisplay = m_CurrFrameNo;
						}

						for (int i = 0; i < m_LightCurveController.LcFile.Header.ObjectCount; i++)
						{
							List<LCMeasurement> measurements = m_LightCurveController.Context.AllReadings[i];
							LCMeasurement data = measurements[(int)(frameToDisplay - m_LightCurveController.LcFile.Header.MinFrame)];

							float delta = m_LightCurveController.LcFile.Header.MeasurementApertures[i];

							if (!float.IsNaN(delta))
							{
								g.DrawEllipse(m_DisplaySettings.TargetPens[i], data.X0 - delta, data.Y0 - delta, 2 * delta, 2 * delta);
								g.DrawString(string.Format("x={0}; y={1}", data.X0.ToString("0.0"), data.Y0.ToString("0.0")),
											 s_FONT, m_DisplaySettings.TargetBrushes[i], data.X0, data.Y0 + 2 * delta);

							}
						}

					}
				}

                if (m_Measuring)
                {
                    int iTo = Math.Min(4, m_Tracker.TrackedObjects.Count);

                    if (!m_Correcting)
                    {
                        // This is where we draw the stars while measuring
                        if (m_Tracker.IsTrackedSuccessfully ||
                            LightCurveReductionContext.Instance.IsDriftThrough)
                        {
                            for (int i = 0; i < iTo; i++)
                            {
                                IImagePixel center = m_Tracker.TrackedObjects[i].Center;
                                float app = m_Tracker.TrackedObjects[i].OriginalObject.ApertureInPixels;

                                if (!float.IsNaN(app))
                                {
									g.DrawEllipse(m_DisplaySettings.TargetPens[i], (float)center.XDouble + m_ManualTrackingDeltaX[i] - app, (float)center.YDouble + m_ManualTrackingDeltaY[i] - app, 2 * app, 2 * app);
                                }
                            }
                        }
                    }
                    else
                    {
                        // This is where we draw the stars while we are correcting lost tracking. In this case we use the last good known
                        // position as a reference rather than the latest position
                        for (int i = 0; i < iTo; i++)
                        {
                            IImagePixel center = m_Tracker.TrackedObjects[i].LastKnownGoodPosition;
							float app = m_Tracker.TrackedObjects[i].OriginalObject.ApertureInPixels;
                            if (float.IsNaN(app)) app = 4.0f;

                            if (center != null)
                            {
								g.DrawEllipse(m_DisplaySettings.TargetPens[i], (float)center.XDouble + m_ManualTrackingDeltaX[i] - app, (float)center.YDouble + m_ManualTrackingDeltaY[i] - app, 2 * app, 2 * app);
                            }
                        }

						Tracker trk = m_Tracker as Tracker;
						if (trk != null)
						{
							if (trk.AutoDiscoveredStars != null &&
								trk.AutoDiscoveredStars.Count > 0)
							{
								float app = 4;
								foreach (PSFFit fit in trk.AutoDiscoveredStars)
								{
									g.DrawEllipse(Pens.Yellow, (float)fit.XCenter - app, (float)fit.YCenter - app, 2 * app, 2 * app);
								}
							}							
						}
                    }
                }
                else if (m_Refining)
                {
                    int iTo = Math.Min(4, m_Tracker.TrackedObjects.Count);
                    for (int i = 0; i < iTo; i++)
                    {
                        IImagePixel center = m_Tracker.TrackedObjects[i].Center;
                        float delta = ((float)Math.Min(1, m_Tracker.RefiningPercentageWorkLeft) * 3 + 1) * m_StateMachine.MeasuringApertures[i];

                        if (m_Tracker.TrackedObjects[i].IsLocated)
                        {
                            if (!float.IsNaN(delta))
                                g.DrawArc(
									m_DisplaySettings.TargetPens[i], (float)center.XDouble - delta,
                                    (float)center.YDouble - delta, 2 * delta, 2 * delta,
                                    0, (float)Math.Min(360 * (1 - m_Tracker.RefiningPercentageWorkLeft), 360));
                        }
                    }
                }
                else if (m_Configuring)
                {
                    for (int i = 0; i < m_StateMachine.MeasuringStars.Count; i++)
                    {
                        TrackedObjectConfig star = m_StateMachine.MeasuringStars[i];
                        float aperture = m_StateMachine.MeasuringApertures[i] + 1;
						Pen pen = m_DisplaySettings.TargetPens[i];

						float centerX = star.ApertureStartingX + m_ManualTrackingDeltaX[i];
						float centerY = star.ApertureStartingY + m_ManualTrackingDeltaY[i];

                        g.DrawEllipse(pen, centerX - aperture, centerY - aperture, 2 * aperture, 2 * aperture);

                        if (m_StateMachine.SelectedMeasuringStar == i)
                        {
                            g.DrawLine(pen, centerX - 12, centerY, centerX - 6, centerY);
                            g.DrawLine(pen, centerX + 12, centerY, centerX + 6, centerY);
                            g.DrawLine(pen, centerX, centerY - 12, centerX, centerY - 6);
                            g.DrawLine(pen, centerX, centerY + 12, centerX, centerY + 6);
                        }

                        if (star.TrackingType == TrackingType.GuidingStar)
                        {
                            g.DrawEllipse(pen, centerX - aperture - 1, centerY - aperture - 1, 2 * aperture + 2, 2 * aperture + 2);
                            g.DrawEllipse(pen, centerX - aperture - 2, centerY - aperture - 2, 2 * aperture + 4, 2 * aperture + 4);
                        }
                    }

                    if (m_StateMachine.SelectedMeasuringStar == -1 &&
                        m_StateMachine.SelectedObject != null)
                    {
                        ImagePixel selection = m_StateMachine.SelectedObject;
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble - 12, (float)selection.YDouble, (float)selection.XDouble - 6, (float)selection.YDouble);
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble + 12, (float)selection.YDouble, (float)selection.XDouble + 6, (float)selection.YDouble);
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble, (float)selection.YDouble - 12, (float)selection.XDouble, (float)selection.YDouble - 6);
                        g.DrawLine(Pens.WhiteSmoke, (float)selection.XDouble, (float)selection.YDouble + 12, (float)selection.XDouble, (float)selection.YDouble + 6);
                    }

                    DrawSelectedStarZoomedImage();
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        private void DrawSelectedStarZoomedImage()
        {
            bool isMeasuringStar = m_StateMachine.SelectedMeasuringStar != -1;
            if (isMeasuringStar || m_StateMachine.SelectedObject != null)
            {

                ImagePixel star =
                    isMeasuringStar
                        ? new ImagePixel(m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureStartingX, m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureStartingY)
                        : m_StateMachine.SelectedObject;

                float dx = 0;
                float dy = 0;

                if (isMeasuringStar)
                {
                    dx = m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureDX;
                    dy = m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar].ApertureDX;
                }


                float aperture =
                    isMeasuringStar
                        ? m_StateMachine.MeasuringApertures[m_StateMachine.SelectedMeasuringStar]
                        : 5;

                Pen pen =
                    isMeasuringStar
                    ? m_DisplaySettings.TargetPens[m_StateMachine.SelectedMeasuringStar]
                    : Pens.WhiteSmoke;

                AstroImage astroImage;
                if (CanShowStackedSelection() && IsStackedSelectionEnabled && m_StackedSelectionFrame != null)
                    astroImage = m_StackedSelectionFrame;
                else
                    astroImage = m_AstroImage;

                Pixelmap zoomPixelmap = null;
                Bitmap zoomedBmp = astroImage.GetZoomImagePixels(star.X, star.Y, TangraConfig.Settings.Color.Saturation, TangraConfig.Settings.Photometry.Saturation, out zoomPixelmap);

                using (Graphics gz = Graphics.FromImage(zoomedBmp))
                {
                    float x = 8.0f * (float)(star.XDouble - star.X - 0.5f) + 16 * 8;
                    float y = 8.0f * (float)(star.YDouble - star.Y - 0.5f) + 16 * 8;
                    float ap = aperture * 8;
                    gz.DrawEllipse(pen, x - ap, y - ap, 2 * ap, 2 * ap);

                    if (isMeasuringStar)
                    {
                        Pen bgPen =
                            isMeasuringStar
                            ? m_DisplaySettings.TargetBackgroundPens[m_StateMachine.SelectedMeasuringStar]
                            : Pens.WhiteSmoke;

                        // Draw background annulus
                        float innerRadius = aperture * TangraConfig.Settings.Photometry.AnnulusInnerRadius;
                        float outerRadius = (float)Math.Sqrt(TangraConfig.Settings.Photometry.AnnulusMinPixels / Math.PI + innerRadius * innerRadius);

                        innerRadius *= 8.0f;
                        outerRadius *= 8.0f;

                        gz.DrawEllipse(bgPen, x - innerRadius, y - innerRadius, 2 * innerRadius, 2 * innerRadius);
                        gz.DrawEllipse(bgPen, x - outerRadius, y - outerRadius, 2 * outerRadius, 2 * outerRadius);
                    }

                    gz.Save();
                }

                m_VideoController.UpdateZoomedImage(zoomedBmp, star, zoomPixelmap);
            }
            else
            {
                m_VideoController.ClearZoomedImage();
            }
        }

        public override void MouseClick(ObjectClickEventArgs e)
        {
            if (e.Pixel != null &&
                m_StateMachine.CurrentState < LightCurvesState.ReadyToRun)
            {
                // Keep track on the currently selected object in the state
				if (e.Gausian != null && e.Gausian.IsSolved)
                    m_StateMachine.SelectedObject = new ImagePixel(e.Gausian.Brightness, e.Gausian.XCenter, e.Gausian.YCenter);
                else
                {
                    m_StateMachine.SelectedObject = e.Pixel;
                }

                m_StateMachine.SelectedObjectGaussian = e.Gausian;
                m_StateMachine.SelectedObjectFrameNo = m_CurrFrameNo;

                int existingIndexId = -1;
                if (!m_StateMachine.IsNewObject(e.Pixel, e.Shift, e.Control, ref existingIndexId))
                {
                    m_StateMachine.SelectedMeasuringStar = existingIndexId;
                }
                else
                    m_StateMachine.SelectedMeasuringStar = -1;

                // This will also call RefreshCurrentFrame()
                SelectedTargetChanged(m_StateMachine.SelectedMeasuringStar);
            }
        }

        public bool HasCustomZoomImage
        {
            get
            {
				return m_Measuring || m_Refining || m_ViewingLightCurve || m_Configuring;
            }
        }

        public bool AvoidImageOverlays
        {
            get
            {
                // When configuring - dont show overlays
                return m_Configuring;
            }
        }

        private Bitmap m_CustomZoomBitmap = null;
        private Color m_MedianBackgroundColor = Color.Black;
        private Pen m_MedianBackgroundPen = null;
        private Brush m_MedianBackgroundBrush = null;


		private byte PixelValueToDisplayBitmapByte(uint pixel, int bpp)
		{
			if (bpp == 8)
				return (byte)(pixel & 0xFF);
			else if (bpp == 12)
				return (byte)((pixel >> 4) & 0xFF);
			else if (bpp == 16)
				return (byte)((pixel >> 8) & 0xFF);

			return (byte)(pixel & 0xFF);
		}

        private void InitCustomZoomBitmap(int width, int height)
        {
			m_CustomZoomBitmap = new Bitmap(width, height);
			byte color = PixelValueToDisplayBitmapByte(m_Tracker.MedianValue, m_VideoController.VideoBitPix);
			m_MedianBackgroundColor = Color.FromArgb(color, color, color);

			if (m_MedianBackgroundPen != null) m_MedianBackgroundPen.Dispose();
			m_MedianBackgroundPen = new Pen(m_MedianBackgroundColor);

			if (m_MedianBackgroundBrush != null) m_MedianBackgroundBrush.Dispose();
			m_MedianBackgroundBrush = new SolidBrush(m_MedianBackgroundColor);

			using (Graphics g = Graphics.FromImage(m_CustomZoomBitmap))
			{
				g.Clear(m_MedianBackgroundColor);

				int objHeight = (height / 4) - 20;

				foreach (ITrackedObject obj in m_Tracker.TrackedObjects)
				{
					int beg = obj.TargetNo * (objHeight) + obj.TargetNo * 20 + 10;
					g.FillRectangle(m_DisplaySettings.TargetBrushes[obj.TargetNo], 1, beg, 4, objHeight);
					g.FillRectangle(m_MedianBackgroundBrush, 5, beg, width - 9, objHeight);
				}

				g.Save();
			}
            
        }


        public bool DrawCustomZoomImage(Graphics gMain, int width, int height)
        {
			if (m_Correcting)
				// The frame was redrawn because of manual corrections
				return false;

			if (m_Tracker != null)
			{
				lock (m_SyncRoot)
				{
					if (m_Measuring)
					{
						if (this.MeasuringZoomImageType == MeasuringZoomImageType.Stripe)
						{
							DrawZoomStripes(gMain, width, height);
							return true;
						}
						else if (this.MeasuringZoomImageType == MeasuringZoomImageType.Pixel)
						{
							DrawZoomPixels(gMain, width, height);
							return true;
						}
                        else if (this.MeasuringZoomImageType == MeasuringZoomImageType.PSF)
                        {
                            DrawZoomPSFs(gMain, width, height);
                            return true;
                        }
						else
						{
							gMain.Clear(Color.Gray);
							return true;
						}
					}
					else if (m_Refining &&
						LightCurveReductionContext.Instance.DebugTracking)
					{
						DrawZoomArea(gMain);
						return true;
					}
					else if (m_ViewingLightCurve)
					{
						DrawZoomArea(gMain);
						return true;
					}
				}
			}

	        if (m_Configuring)
				// Don't overwrite the configuration image
                return true;

			if (m_ViewingLightCurve)
			{
				DrawZoomArea(gMain);
				return true;
			}

	        return false;
        }

        private void DrawZoomArea(Graphics gMain)
        {
	        AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);

			if (currentImage != null && m_VideoController.ZoomedCenter != null)
			{
				Rectangle zommedArea = new Rectangle(m_VideoController.ZoomedCenter.X - 15, m_VideoController.ZoomedCenter.Y - 16, 32, 32);

				using (Bitmap bmpZoom = currentImage.GetZoomImagePixels(m_VideoController.ZoomedCenter.X, m_VideoController.ZoomedCenter.Y, TangraConfig.Settings.Color.Saturation, TangraConfig.Settings.Photometry.Saturation))
				{
                    m_VideoController.ApplyDisplayModeAdjustments(bmpZoom);

					gMain.DrawImage(bmpZoom, new PointF(0, 0));

                    try
                    {
                        if (m_Refining)
                        {
                            foreach (ITrackedObject obj in m_Tracker.TrackedObjects)
                            {
                                if (zommedArea.Contains(obj.Center.X, obj.Center.Y))
                                {
                                    float xx = obj.Center.X - (m_VideoController.ZoomedCenter.X - 15);
                                    float yy = obj.Center.Y - (m_VideoController.ZoomedCenter.Y - 16);

                                    xx = xx * 8 + 0.5f;
                                    yy = yy * 8 + 0.5f;

									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx - 16, yy, xx, yy);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx - 16, yy + 1, xx, yy + 1);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx, yy - 16, xx, yy);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx + 1, yy - 16, xx + 1, yy);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx + 16, yy, xx, yy);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx + 16, yy + 1, xx, yy + 1);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx, yy + 16, xx, yy);
									gMain.DrawLine(m_DisplaySettings.TargetPens[obj.TargetNo], xx + 1, yy + 16, xx + 1, yy);
                                }
                            }

                            gMain.Save();
                        }
                        else if (m_ViewingLightCurve && m_LoadedLcFile != null && m_VideoController.CurrentVideoFileEngine != "BMP.Image")
                        {
                            if (DebugContext.DebugSubPixelMeasurements)
                            {
                                DebugContext.CurrentSubPixels.Clear();
                                DebugContext.Width = m_AstroImage.Width;
                                DebugContext.Height = m_AstroImage.Height;
                            }

                            EnsureMeasurer(2.0f);

                            for (int targNo = 0; targNo < m_LoadedLcFile.Header.ObjectCount; targNo++)
                            {
                                if (DebugContext.DebugSubPixelMeasurements)
                                {
                                    DebugContext.TargetNo = targNo;
                                    DebugContext.CurrentSubPixels.Add(new DebugContext.SubPixelData[m_AstroImage.Width, m_AstroImage.Height]);

                                }

                                LCMeasurement mea = m_LoadedLcFile.Data[targNo][m_CurrFrameNo - (int)m_LoadedLcFile.Header.MinFrame];
                                float xx = mea.X0 - (m_VideoController.ZoomedCenter.X - 15);
                                float yy = mea.Y0 - (m_VideoController.ZoomedCenter.Y - 16);

                                xx = xx * 8 + 0.5f;
                                yy = yy * 8 + 0.5f;

                                float halfAper = m_LoadedLcFile.Header.MeasurementApertures[targNo] * 8;

								gMain.DrawEllipse(m_DisplaySettings.TargetPens[targNo], xx - halfAper, yy - halfAper, 2 * halfAper, 2 * halfAper);

                                if (DebugContext.DebugSubPixelMeasurements)
                                {
                                    ITrackedObject trcked = mea.GetTrackedLcMeasurement(m_LoadedLcFile);
                                    float refinedFWHM = trcked.OriginalObject.RefinedFWHM;
                                    float averageFWHM = m_LoadedLcFile.Footer.RefinedAverageFWHM;

                                    MeasureTrackedObject2(trcked, m_Measurer, TangraConfig.PreProcessingFilter.NoFilter, false, null, refinedFWHM, averageFWHM);

                                    float xCent = float.NaN;
                                    float yCent = float.NaN;

                                    for (int y = m_VideoController.ZoomedCenter.Y - 16; y < m_VideoController.ZoomedCenter.Y + 16; y++)
                                        for (int x = m_VideoController.ZoomedCenter.X - 16; x < m_VideoController.ZoomedCenter.X + 16; x++)
                                        {
                                            if (x >= 0 && x <= DebugContext.Width && y >= 0 && y <= DebugContext.Height)
                                            {
                                                DebugContext.SubPixelData debugInfo = DebugContext.CurrentSubPixels[targNo][x, y];
                                                if (debugInfo != null)
                                                {
                                                    xCent = debugInfo.X0;
                                                    yCent = debugInfo.Y0;

                                                    // TODO: Plot the center with a differen colour !!!
                                                    int xs = 8 * (x - (m_VideoController.ZoomedCenter.X - 15));
                                                    int ys = 8 * (y - (m_VideoController.ZoomedCenter.Y - 16));

                                                    if (debugInfo.FullyIncluded)
                                                    {
                                                        gMain.FillRectangle(Brushes.Lime, xs, ys, 8, 8);
                                                    }
                                                    else if (debugInfo.Included != null)
                                                    {
                                                        for (int kk = 0; kk < 4; kk++)
                                                            for (int tt = 0; tt < 4; tt++)
                                                            {
                                                                if (debugInfo.Included[kk, tt])
                                                                    gMain.FillRectangle(Brushes.Lime, xs + 2 * kk, ys + 2 * tt, 2, 2);
                                                            }
                                                    }
                                                }
                                            }

                                        }

                                    if (!float.IsNaN(xCent) && !float.IsNaN(yCent))
                                    {
                                        xx = xCent - (m_VideoController.ZoomedCenter.X - 15);
                                        yy = yCent - (m_VideoController.ZoomedCenter.Y - 16);

                                        xx = xx * 8 + 0.5f;
                                        yy = yy * 8 + 0.5f;

                                        gMain.FillRectangle(Brushes.Orange, xx - 1, yy - 1, 3, 3);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                    }

                }
			}
        }

        private Rectangle[] m_ZoomPixelRects = new Rectangle[]
                                                   {
                                                       new Rectangle(1, 1, 122, 122), 
                                                       new Rectangle(125, 1, 122, 122), 
                                                       new Rectangle(1, 125, 122, 122), 
                                                       new Rectangle(125, 125, 122, 122)
                                                   };

        private Rectangle[] m_BorderRects = new Rectangle[]
                                                   {
                                                       new Rectangle(0, 0, 124, 124), 
                                                       new Rectangle(124, 0, 124, 124), 
                                                       new Rectangle(0, 124, 124, 124), 
                                                       new Rectangle(124, 124, 124, 124)
                                                   };

		private void DrawZoomPixels(Graphics g, int width, int height)
		{
			g.Clear(Color.Gray);

			// IDs are 0, 1, 2 and 3
			foreach (ITrackedObject obj in m_Tracker.TrackedObjects)
			{
				if (obj.PSFFit != null)
				{
					obj.PSFFit.DrawDataPixels(g, m_ZoomPixelRects[obj.TargetNo], obj.OriginalObject.ApertureInPixels, m_DisplaySettings.TargetPens[obj.TargetNo], m_VideoController.VideoBitPix, m_VideoController.VideoAav16NormVal);
				}
				else if (obj.IsLocated)
				{
					AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);
					if (currentImage != null)
					{
						int x0 = (int)Math.Round(obj.Center.XDouble);
						int y0 = (int)Math.Round(obj.Center.YDouble);
						uint[,] pix = currentImage.GetMeasurableAreaPixels(x0, y0);
						pix.DrawDataPixels(g,
							m_ZoomPixelRects[obj.TargetNo],
							m_VideoController.VideoBitPix, m_VideoController.VideoAav16NormVal,
							(float)obj.Center.XDouble - x0 + 8, (float)obj.Center.YDouble - y0 + 8,
							obj.OriginalObject.ApertureInPixels, m_DisplaySettings.TargetPens[obj.TargetNo]);
					}
				}
			}

			foreach (ITrackedObject obj in m_Tracker.TrackedObjects)
				g.DrawRectangle(Pens.WhiteSmoke, m_BorderRects[obj.TargetNo]);
		}

        private void DrawZoomPSFs(Graphics g, int width, int height)
        {
            g.Clear(Color.Gray);

            // IDs are 0, 1, 2 and 3
            foreach (ITrackedObject obj in m_Tracker.TrackedObjects)
            {
                if (obj.PSFFit != null && obj.PSFFit.IsSolved)
                {
					obj.PSFFit.DrawGraph(g, m_ZoomPixelRects[obj.TargetNo], m_VideoController.VideoBitPix, m_VideoController.VideoAav16NormVal);
                }
            }
        }

        private void DrawZoomStripes(Graphics gMain, int width, int height)
		{
			int objHeight = (height / 4) - 20;

			if (m_CustomZoomBitmap == null) InitCustomZoomBitmap(width, height);

			using (Graphics g = Graphics.FromImage(m_CustomZoomBitmap))
			{
				bool isAperturePhotometry = LightCurveReductionContext.Instance.ReductionMethod == TangraConfig.PhotometryReductionMethod.AperturePhotometry;
				AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);

				foreach (ITrackedObject obj in m_Tracker.TrackedObjects)
				{
					int beg = obj.TargetNo * (objHeight) + obj.TargetNo * 20 + 10;

					int w = m_CurrFrameNo - m_FirstMeasuredFrame + 4;
					Pen pen = Pens.Black;
					int h = objHeight - 1;

					if ((!m_Tracker.IsTrackedSuccessfully || obj.OriginalObject.ProcessInPsfGroup) && (!obj.IsLocated || obj.IsOffScreen))
					{
						pen = m_DisplaySettings.TargetPens[obj.TargetNo];
					}
					else if (isAperturePhotometry &&
						!double.IsNaN(obj.Center.XDouble) &&
						!double.IsNaN(obj.Center.YDouble))
					{
						h = (int)Math.Min(objHeight - 1, objHeight * obj.OriginalObject.ApertureInPixels / 7);
						if (h % 2 == 0) h++;
						byte z0 = currentImage.GetDisplayPixel((int)Math.Round(obj.Center.XDouble), (int)Math.Round(obj.Center.YDouble));
						pen = AllGrayPens.GrayPen(z0);
					}
					else if (obj.PSFFit != null)
					{
						h = (int)Math.Min(objHeight - 1, objHeight * obj.PSFFit.FWHM / 7);
						if (h % 2 == 0) h++;
						byte z = (byte)Math.Max(0, Math.Min(255, obj.PSFFit.IMax));

						pen = AllGrayPens.GrayPen(z);
					}
					else
					{
						pen = m_DisplaySettings.TargetPens[obj.TargetNo];
					}

					if (w > width - 4)
					{
						using (Bitmap clone = m_CustomZoomBitmap.Clone(
												 new Rectangle(6, beg, width - 11, objHeight),
												 m_CustomZoomBitmap.PixelFormat))
						{
							g.DrawImage(clone, new Point(5, beg));
						}

						g.DrawLine(m_MedianBackgroundPen, width - 6, beg, width - 6, beg + objHeight);
						g.DrawLine(pen, width - 6, beg + (objHeight - h) / 2, width - 6, beg + (objHeight + h) / 2);
					}
					else
					{
						g.DrawLine(pen, w, beg + (objHeight - h) / 2, w, beg + (objHeight + h) / 2);
					}
				}

				g.Save();
			}

			gMain.DrawImage(m_CustomZoomBitmap, 0, 0);
		}

		internal void SetMeasuringZoomImageType(MeasuringZoomImageType type)
		{
			this.MeasuringZoomImageType = type;
			lock(m_SyncRoot)
			{
				m_CustomZoomBitmap = null;
				m_FirstMeasuredFrame = m_CurrFrameNo;
			}
		}
        #endregion

        #region Event Handlers
        public void SelectedTargetChanged(int newSelectedIndex)
        {
			m_VideoController.SetDisplayHueBackgroundModeTargets(m_StateMachine.MeasuringStars);	
        }

        public void MaxStarsReached(bool maximumReached)
        {
            m_ControlPanel.UpdateState();
        }
        #endregion

        internal bool IsMeasuring
        {
            get { return m_Measuring; }
        }

        internal bool IsRefining
        {
            get { return m_Refining; }
        }

		public static MeasurementsHelper DoConfiguredMeasurement(uint[,] matrix, float aperture, int bitPixCamera, uint maxSignalValue, double bestFindTolerance, ref int matrixSize)
        {
            var measurer = new MeasurementsHelper(
                            bitPixCamera,
							TangraConfig.BackgroundMethod.BackgroundMedian /* We always use Background Median for configuration measurements. */,
							TangraConfig.Settings.Photometry.SubPixelSquareSize,
                            TangraConfig.Settings.Photometry.Saturation.GetSaturationForBpp(bitPixCamera, maxSignalValue));

            measurer.SetCoreProperties(
                    TangraConfig.Settings.Photometry.AnnulusInnerRadius,
                    TangraConfig.Settings.Photometry.AnnulusMinPixels,
                    TangraConfig.Settings.Special.RejectionBackgroundPixelsStdDev,
                    4.0f);

            measurer.BestMatrixSizeDistanceDifferenceTolerance = bestFindTolerance;

            MeasurementsHelper.Filter measurementFilter = MeasurementsHelper.Filter.None;

            switch (LightCurveReductionContext.Instance.DigitalFilter)
            {
                case TangraConfig.PreProcessingFilter.NoFilter:
                    measurementFilter = MeasurementsHelper.Filter.None;
                    break;

                case TangraConfig.PreProcessingFilter.LowPassFilter:
                    measurementFilter = MeasurementsHelper.Filter.LP;
                    break;

                case TangraConfig.PreProcessingFilter.LowPassDifferenceFilter:
                    measurementFilter = MeasurementsHelper.Filter.LPD;
                    break;
            }

            // This will also find the best PSFFit
			measurer.FindBestFit(9, 9, measurementFilter, matrix, bitPixCamera, ref matrixSize, false);

            return measurer;
        }

        public void BeginConfiguration()
        {
            m_MeasurementInterval = 1;
            m_CurrFrameNo = -1;
            m_CurrFileName = null;

            m_StateMachine = new LCStateMachine(this, m_VideoController);

            m_Measuring = false;
            m_Refining = false;
            m_ViewingLightCurve = false;
            m_Configuring = true;

            m_StateMachine.SelectedMeasuringStar = -1;
            m_StateMachine.SelectedObject = null;

            ResetStackedSelectionFrame();

            m_ControlPanel.BeginConfiguration(m_StateMachine, m_VideoController);

            m_VideoController.StatusChanged("Configuring");

            // Lunar reappearances are measured backwards
            bool isLunarR = LightCurveReductionContext.Instance.LightCurveReductionType == LightCurveReductionType.TotalLunarReppearance;
            if (isLunarR)
            {
                m_VideoController.ShowMessageBox(
                    "To help with the tracking of a Lunar Reappearance Tangra will run the video in reverse direction.\r\n\r\n " +
                    "Please select the already reapeared star that you would like to measure.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            m_VideoController.InitializePlayingDirection(isLunarR);
            m_VideoController.RefreshCurrentFrame();
        }

        public void StartOver()
        {
            BeginConfiguration();

            TangraContext.Current.HasVideoLoaded = true;
            m_VideoController.UpdateViews();

            m_VideoController.RefreshCurrentFrame();
        }

        public void BeginMeasurements()
        {
			bool isColourBitmap = false;
			AstroImage currentImage = m_VideoController.GetCurrentAstroImage(false);
			//using (Bitmap nonIntegratedBmp = m_Host.FramePlayer.GetFrame(m_CurrFrameNo, true))
			//{
			//    isColourBitmap = BitmapFilter.IsColourBitmap(nonIntegratedBmp);
			//}

			LightCurveReductionContext.Instance.IsColourVideo = isColourBitmap;

			if (isColourBitmap &&
				TangraConfig.Settings.Photometry.ColourChannel != TangraConfig.ColourChannel.GrayScale)
			{
				string channel = TangraConfig.Settings.Photometry.ColourChannel.ToString();
				DialogResult dlgRes = m_VideoController.ShowMessageBox(
					"Would you like to use the GrayScale band for this measurement only?\r\n\r\n" +
					"This appears to be a colour video but the current band to measure is not set to GrayScale. It is recommended to use the GrayScale band for colour videos. \r\n\r\n" +
					"To use the GrayScale band for this reduction only - press 'Yes', to use the currently set [" + channel + "] " +
					"band press 'No'. To manually set a different band for this and other reductions press 'Cancel' and configure the band from the Tangra settings form before you continue.",
					"Warning",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Warning);

				if (dlgRes == DialogResult.Cancel)
					return;

				if (dlgRes == DialogResult.Yes)
					LightCurveReductionContext.Instance.ColourChannel = TangraConfig.ColourChannel.GrayScale;
				else
					LightCurveReductionContext.Instance.ColourChannel = TangraConfig.Settings.Photometry.ColourChannel;
			}

			LightCurveReductionContext.Instance.ColourChannel = TangraConfig.Settings.Photometry.ColourChannel;
            
            //LightCurveReductionContext.Instance.OSDFrame =
            //    RegistryConfig.Instance.OSDSizes.GetOSDRectangleForFrameSize(
            //        VideoContext.Current.VideoStream.Width, VideoContext.Current.VideoStream.Height);

			m_BackedUpSelectMeasuringStarsState = m_StateMachine.m_CurrentStateObject;
			m_Measuring = false;
			m_Refining = true;
			m_ViewingLightCurve = false;
			m_Configuring = false;
			m_Correcting = false;

            if (m_AveragedFrame == null)
            {
                if (m_StackedAstroImage == null) EnsureStackedAstroImage();
                m_AveragedFrame = new AveragedFrame(GetStackedAstroImage());
            }

            m_StackedSelectionFrame = null;
            m_VideoController.SetStackedSelectionFrameImage(null);

	        string usedTrackerType;
			m_Tracker = TrackerFactory.CreateTracker(
				m_StackedAstroImage.Width,
				m_StackedAstroImage.Height,
				LightCurveReductionContext.Instance.LightCurveReductionType,
				m_VideoController.GetVideoFileFormat(),
                m_VideoController.IsFitsVideo,
				m_StateMachine.MeasuringStars,
				out usedTrackerType);

			LightCurveReductionContext.Instance.UsedTracker = usedTrackerType;
	        TangraContext.Current.CrashReportInfo.Tracker = usedTrackerType;

	        if (!m_Tracker.InitializeNewTracking(m_VideoController.GetCurrentAstroImage(false)))
		        return;

	        for (int i = 0; i < 4; i++)
	        {
				m_ManualTrackingDeltaX[i] = 0;
		        m_ManualTrackingDeltaY[i] = 0;
	        }

            m_VideoController.InitializeTimestampOCR();

            if (m_VideoController.HasTimestampOCR() || m_VideoController.HasEmbeddedTimeStamps())
				LightCurveReductionContext.Instance.HasEmbeddedTimeStamps = true;
			else
				LightCurveReductionContext.Instance.HasEmbeddedTimeStamps = false;

			m_VideoController.StatusChanged("Refining");

			m_StateMachine.ChangeState(LightCurvesState.Running);

			MeasuringStarted();

			m_CameraName = m_VideoController.IsAstroDigitalVideo || m_VideoController.IsAstroAnalogueVideo ? m_VideoController.AstroVideoCameraModel : string.Empty;
            m_AavFrameIntegration = m_VideoController.IsAstroAnalogueVideo ? m_VideoController.AstroAnalogueVideoIntegratedAAVFrames : -1;
			m_AavNativeVideoFormat = m_VideoController.IsAstroAnalogueVideo ? m_VideoController.AstroVideoNativeVideoStandard : string.Empty;
            m_AavStackedFrameRate = m_VideoController.IsAstroAnalogueVideo ? m_VideoController.AstroAnalogueVideoStackedFrameRate : 0;
            m_FitsDynamicFromValue = m_VideoController.DynamicFromValue;
            m_FitsDynamicToValue = m_VideoController.DynamicToValue;

			if (LightCurveReductionContext.Instance.DebugTracking)
			{
				TangraContext.Current.CanPlayVideo = true;
				m_VideoController.UpdateViews();
			}
			else
                m_VideoController.PlayVideo(m_CurrFrameNo > -1 ? (int?)m_CurrFrameNo : null, 1);
        }

        public void EnsureStackedAstroImage()
        {
            if (m_StackedAstroImage == null)
            {
                m_ControlPanel.Cursor = Cursors.WaitCursor;
                try
                {
					m_StackedAstroImage = m_VideoController.GetCurrentAstroImage(false /*!LightCurveReductionContext.Instance.WindOrShaking*/);
                }
                finally
                {
                    m_ControlPanel.Cursor = Cursors.Default;
                }
            }   
        }

        public void StopRefining()
        {
			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = true;

			m_Tracker = null;
			m_VideoController.StatusChanged("Configuring");

			m_VideoController.StopVideo();
			m_VideoController.MoveToFrame(m_StateMachine.m_ConfiguringFrame);

			TangraContext.Current.CanPlayVideo = false;
			TangraContext.Current.CanScrollFrames = false;
	        m_VideoController.UpdateViews();

			m_StateMachine.m_CurrentState = LightCurvesState.SelectMeasuringStars;
			m_StateMachine.m_CurrentStateObject = m_BackedUpSelectMeasuringStarsState;
			m_ControlPanel.UpdateState();
        }

        public void ContinueMeasurements(int continueAtFrame)
        {
			m_Measuring = true;
			m_Correcting = false;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = false;

	        for (int i = 0; i < 4; i++)
	        {
				if (m_Tracker.SupportsManualCorrections && (m_ManualTrackingDeltaX[i] != 0 || m_ManualTrackingDeltaY[i] != 0))
					m_Tracker.DoManualFrameCorrection(i, m_ManualTrackingDeltaX[i], m_ManualTrackingDeltaY[i]);

				m_ManualTrackingDeltaX[i] = 0;
		        m_ManualTrackingDeltaY[i] = 0;
	        }

			m_VideoController.PlayVideo(continueAtFrame, 1);

			m_VideoController.StatusChanged("Measuring");

			// Change back to arrow tool
			m_VideoController.SelectImageTool<ArrowTool>();
        }

	    private bool m_StopMeasurementsRequested = false;

		public void StopMeasurements(Action<CorrectTrackingTool> callback)
		{
			m_StopMeasurementsRequested = true;

			m_VideoController.StopVideo((lastFrame, userRequest) =>
				{
					if (m_StopMeasurementsRequested)
					{
						m_StopMeasurementsRequested = false;

						m_Measuring = true;
						m_Correcting = true;
						m_Refining = false;
						m_ViewingLightCurve = false;
						m_Configuring = false;

						TangraContext.Current.CanPlayVideo = false;
						TangraContext.Current.CanScrollFrames = false;
					}

					m_VideoController.UIThreadInvoke(() =>
					{
						m_VideoController.UpdateViews();

						m_VideoController.StatusChanged("Stopped");

						// Allow correction of the tracking
						CorrectTrackingTool correctTrackingTool =
							m_VideoController.SelectImageTool<CorrectTrackingTool>() as CorrectTrackingTool;
						if (correctTrackingTool != null)
							correctTrackingTool.Initialize(this, m_Tracker, m_VideoController);

						m_StateMachine.m_HasBeenPaused = true;

						callback(correctTrackingTool);

					});
				});
        }

        public void FinishedWithMeasurements()
        {
			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = false;
			m_Configuring = false;
			m_Correcting = false;

			m_VideoController.StatusChanged("Ready");

			SaveSessionFile();
        }

        public void ShowLightCurve(UsedTimeBase timebase)
        {
            if (m_VideoController.IsAstroAnalogueVideo && timebase != UsedTimeBase.NoTimesBaseAvailable)
			{
				List<string> cameras = InstrumentalDelayConfigManager.GetAvailableCameras();

				var frm = new frmInstDelayConfigChooser();
				frm.SetCameraModels(cameras, m_VideoController.AstroVideoCameraModel, m_VideoController.AstroVideoNativeVideoStandard);

				if (m_VideoController.ShowDialog(frm) == DialogResult.OK)
				{
					m_InstumentalDelaySelectedCamera = frm.SelectedCamera;
					if (m_InstumentalDelaySelectedCamera != null)
						m_InstumentalDelaySelectedConfig = InstrumentalDelayConfigManager.GetConfigurationForCamera(m_InstumentalDelaySelectedCamera);
					else
						m_InstumentalDelaySelectedConfig = null;
				}
			}

            if (m_VideoController.RequiresAcquisitionDelayCorrection)
            {
                var knownAcqDelaysMs = m_VideoController.KnownAcquisitionDelayMs;
                if (knownAcqDelaysMs != null && Math.Abs(knownAcqDelaysMs.Value) >= Double.Epsilon)
                {
                    m_AcquisitionDelayMs = knownAcqDelaysMs.Value;
                    m_TimingCorrectionsRequired = false;
                }
                else
                {
                    var frm = new frmAcquisitionDelayChooser(m_VideoController);
                    frm.StartPosition = FormStartPosition.CenterParent;

                    if (m_VideoController.ShowDialog(frm) == DialogResult.OK)
                    {
                        m_AcquisitionDelayMs = frm.AcquisitionDelayMs;
                        m_ReferenceTimeToUtcOffsetMs = frm.ReferenceTimeToUtcOffsetMs;
                        m_TimingCorrectionsRequired = frm.TimingCorrectionsRequired;
                        m_CameraName = frm.CameraModel;
                    }
                    else
                    {
                        m_TimingCorrectionsRequired = true;
                    }
                }
            }
            else
            {
                m_TimingCorrectionsRequired = false;
            }

			LCFile file = FlushLightCurveFile();

			if (file.Header.ReductionType == LightCurveReductionType.Asteroidal)
				UsageStats.Instance.TrackedAsteroidals++;
			else if (file.Header.ReductionType == LightCurveReductionType.MutualEvent)
				UsageStats.Instance.TrackedMutualEvents++;
			else if (file.Header.ReductionType == LightCurveReductionType.UntrackedMeasurement)
				UsageStats.Instance.UntrackedMeasurements++;
			else if (file.Header.ReductionType == LightCurveReductionType.TotalLunarDisappearance || file.Header.ReductionType == LightCurveReductionType.TotalLunarReppearance || file.Header.ReductionType == LightCurveReductionType.LunarGrazingOccultation)
				UsageStats.Instance.TotalLunarMeasurements++;

			if (TangraConfig.Settings.Tracking.SelectedEngine == TangraConfig.TrackingEngine.TrackingWithRefining)
				UsageStats.Instance.TrackingWithRecoverUsed++;
			else if (TangraConfig.Settings.Tracking.SelectedEngine == TangraConfig.TrackingEngine.AdHocTracking)
				UsageStats.Instance.SimplifiedTrackingUsed++;
			else if (TangraConfig.Settings.Tracking.SelectedEngine == TangraConfig.TrackingEngine.LetTangraChoose)
				UsageStats.Instance.AutomaticTrackingUsed++;

			if (file.Footer.ReductionContext.FullDisappearance) UsageStats.Instance.FullDisappearanceFlag++;
			if (file.Footer.ReductionContext.WindOrShaking) UsageStats.Instance.WindOrShakingFlag++;
			if (file.Footer.ReductionContext.HighFlickeringOrLargeStars) UsageStats.Instance.FlickeringFlag++;
			if (file.Footer.ReductionContext.FieldRotation) UsageStats.Instance.FieldRotationFlag++;
			if (file.Footer.ReductionContext.IsDriftThrough) UsageStats.Instance.DriftThroughFlag++;
			if (file.Footer.ReductionContext.NumberFramesToIntegrate > 1) UsageStats.Instance.SoftwareIntegrationUsed++;

			if (Math.Abs(TangraConfig.Settings.Photometry.EncodingGamma - 1) > 0.01) UsageStats.Instance.ReverseGammaUsed++;
			if (TangraConfig.Settings.Photometry.KnownCameraResponse != TangraConfig.KnownCameraResponse.Undefined) UsageStats.Instance.ReverseCameraResponseUsed++;

			if (file.Footer.ReductionContext.UseClipping || file.Footer.ReductionContext.UseStretching || file.Footer.ReductionContext.UseBrightnessContrast)
				UsageStats.Instance.PreProcessingUsed++;

			if (file.Footer.ReductionContext.DigitalFilter != TangraConfig.PreProcessingFilter.NoFilter) UsageStats.Instance.DigitalFilterUsed++;
			if (file.Footer.ReductionContext.ReductionMethod == TangraConfig.PhotometryReductionMethod.AperturePhotometry) UsageStats.Instance.AperturePhotometry++;
			if (file.Footer.ReductionContext.ReductionMethod == TangraConfig.PhotometryReductionMethod.PsfPhotometry) UsageStats.Instance.PSFPhotometryUsed++;
			if (file.Footer.ReductionContext.ReductionMethod == TangraConfig.PhotometryReductionMethod.OptimalExtraction) UsageStats.Instance.OptimalExtractionUsed++;
			if (file.Footer.ReductionContext.NoiseMethod == TangraConfig.BackgroundMethod.AverageBackground) UsageStats.Instance.AverageBackgroundUsed++;
			if (file.Footer.ReductionContext.NoiseMethod == TangraConfig.BackgroundMethod.BackgroundMode) UsageStats.Instance.BackgroundModeUsed++;
			if (file.Footer.ReductionContext.NoiseMethod == TangraConfig.BackgroundMethod.Background3DPolynomial) UsageStats.Instance._3DPolynomialFitUsed++;
			if (file.Footer.ReductionContext.NoiseMethod == TangraConfig.BackgroundMethod.PSFBackground) UsageStats.Instance.PSFFittingBackgroundUsed++;
			if (file.Footer.ReductionContext.NoiseMethod == TangraConfig.BackgroundMethod.BackgroundMedian) UsageStats.Instance.MedianBackgroundUsed++;
			UsageStats.Instance.TrackedObjects += file.Header.ObjectCount;
			UsageStats.Instance.TrackedFrames += file.Header.CountFrames;

			UsageStats.Instance.FramesWithBadTracking += m_NumberFramesWithBadTracking;
            m_VideoController.UpdateOCRStatistics();

			UsageStats.Instance.Save();

			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = true;
			m_Configuring = false;

			m_StateMachine.ChangeState(LightCurvesState.Viewing);

			m_VideoController.InitializePlayingDirection(false);

			DoShowLightCurve(file);

			m_ControlPanel.SetupLCFileInfo(m_LightCurveController.LcFile);
			m_ControlPanel.UpdateState();

            m_VideoController.SubmitOCRErrorsIfAny();
        }

        public void InitGetStartTime()
        {
			m_StateMachine.ChangeState(LightCurvesState.SelectingFrameTimes);

			m_StartFrameTime = DateTime.MaxValue;
			m_EndFrameTime = DateTime.MinValue;

			var avoider = new DuplicateFrameAvoider(m_VideoController, (int)m_MinFrame);
			int firstGoodFrame = avoider.GetFirstGoodFrameId();

			m_VideoController.MoveToFrame(firstGoodFrame);

			TangraContext.Current.CanPlayVideo = false;
			TangraContext.Current.CanScrollFrames = false;
			m_VideoController.UpdateViews();
        }

        public void InitGetEndTime()
        {
			var avoider = new DuplicateFrameAvoider(m_VideoController, (int)m_MaxFrame);
			int lastGoodFrame = avoider.GetLastGoodFrameId();

            m_VideoController.MoveToFrame(lastGoodFrame);
        }

        private DateTime m_StartFrameTime;
        private DateTime m_EndFrameTime;
        private int m_StartTimeFrame;
        private int m_EndTimeFrame;

        public void SetStartTime(DateTime startTime)
        {
            m_StartFrameTime = startTime;
            m_StartTimeFrame = m_CurrFrameNo;
        }

        public void SetEndTime(DateTime endTime)
        {
            m_EndFrameTime = endTime;
            m_EndTimeFrame = m_CurrFrameNo;

            //TODO: Fix the time computations when the entered times are not for the very first and very last frames
        }

        public DialogResult EnteredTimeIntervalLooksOkay()
        {
			if (m_VideoController.IsAstroAnalogueVideo && !m_VideoController.AstroAnalogueVideoHasOcrOrNtpData)
                return CheckAavRate(m_VideoController.GetVideoFileFormat());
	        else
		        return CheckPALOrNTSCRate();
        }

		public DialogResult CheckAavRate(VideoFileFormat videoFileFormat)
	    {
            int totalIntegratedFrames = 0;

            FileProgressManager.BeginFileOperation(m_EndTimeFrame - m_StartTimeFrame);
		    try
		    {
                for (int i = m_StartTimeFrame; i < m_EndTimeFrame; i++)
                {
                    FrameStateData frameState = m_VideoController.GetFrameStateData(i);
                    totalIntegratedFrames += frameState.NumberIntegratedFrames.Value;
                    FileProgressManager.FileOperationProgress(i - m_StartTimeFrame + 1);
                }
		    }
		    finally
		    {
                FileProgressManager.EndFileOperation();
		    }

			// The actual integration could even be of PAL or NTSC frames

			TimeSpan ts = new TimeSpan(Math.Abs(m_EndFrameTime.Ticks - m_StartFrameTime.Ticks)/*Taking ABS to handle backwards measuring*/);
			double videoTimeInSecPAL = totalIntegratedFrames / 25.0;
			double videoTimeInSecNTSC = totalIntegratedFrames / 29.97;

            bool isTimeOk = false;
            double timeDiscrepancy = 0;
            string nativeFormat = m_VideoController.GetVideoFormat(videoFileFormat);
            if (nativeFormat == "PAL")
            {
                timeDiscrepancy = Math.Abs((videoTimeInSecPAL - ts.TotalSeconds) * 1000);
                isTimeOk = (videoTimeInSecPAL > 0 && timeDiscrepancy < TangraConfig.Settings.Special.MaxAllowedTimestampShiftInMilliSecs);
            }
            else if (nativeFormat == "PAL")
            {
                timeDiscrepancy = Math.Abs((videoTimeInSecNTSC - ts.TotalSeconds) * 1000);
                isTimeOk = (videoTimeInSecNTSC > 0 && timeDiscrepancy < TangraConfig.Settings.Special.MaxAllowedTimestampShiftInMilliSecs);
            }

			if (!isTimeOk)
			{
				if (MessageBox.Show(string.Format(
                    "The time computed from the measured number of frames in this {0} AAV video is off by {1} ms from the entered time. This may indicate " +
					"incorrectly entered start or end time or an almanac update or a leap second event. Do you want to enter the start and end times again?",
                    nativeFormat,
                    timeDiscrepancy.ToString("0.00")),
					"Warning",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					return DialogResult.Retry;
				}

				return DialogResult.Ignore;
			}

			return DialogResult.OK;
	    }


	    public DialogResult CheckPALOrNTSCRate()
		{
			if (m_VideoController.VideoFrameRate < 24.0 || m_VideoController.VideoFrameRate > 31.0)
			{
				MessageBox.Show(
					string.Format("This video has an unusual frame rate of {0}. Tangra cannot run internal checks for the correctness of the entered frame times.", m_VideoController.VideoFrameRate.ToString("0.00")),
					"Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return DialogResult.Ignore;
			}

			double acceptedVideoFrameRate = m_VideoController.VideoFrameRate > 24.0 && m_VideoController.VideoFrameRate < 26.0
												? 25.0 /* PAL */
												: 29.97 /* NTSC */;

	        string videoType = "Unusual Framerate";
            if (m_VideoController.VideoFrameRate > 24.0 && m_VideoController.VideoFrameRate < 26.0) videoType = "PAL";
            else if (m_VideoController.VideoFrameRate > 29.0 && m_VideoController.VideoFrameRate < 31.0) videoType = "NTSC";

			TimeSpan ts = new TimeSpan(Math.Abs(m_EndFrameTime.Ticks - m_StartFrameTime.Ticks)/*Taking ABS to handle backwards measuring*/);
			double videoTimeInSec = (m_EndTimeFrame - m_StartTimeFrame) / acceptedVideoFrameRate;

            if (m_VideoController.IsPlainAviVideo && (videoTimeInSec < 0 || Math.Abs((videoTimeInSec - ts.TotalSeconds) * 1000) > TangraConfig.Settings.Special.MaxAllowedTimestampShiftInMilliSecs))
			{
				if (MessageBox.Show(
					string.Format("The time computed from the measured number of frames in this {1} video is off by more than {0} ms from the entered time. This may indicate " +
					"incorrectly entered start or end time or an almanac update or a leap second event. Do you want to enter the start and end times again?",
                    (TangraConfig.Settings.Special.MaxAllowedTimestampShiftInMilliSecs).ToString("0.00"),
					videoType),
					"Warning",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					return DialogResult.Retry;
				}

				return DialogResult.Ignore;
			}

			double derivedFrameRate = (m_EndTimeFrame - m_StartTimeFrame) / ts.TotalSeconds;

			// 1) compute 1 ms plus 1ms for each 30 sec up to the max of 4ms. e.g. if this is a PAL video and we have measured 780 frames, this makes 780 / 25fps (PAL) = 31.2 sec. So we take excess = 1 + 1 sec.
			// 2) max allowed difference = 1.33 * Module[video frame rate - (video frame rate * num frames + excess) / num frames]
			int allowedExcess = 1 + Math.Min(4, (int)((m_EndTimeFrame - m_StartTimeFrame) / acceptedVideoFrameRate) / 30);
			double maxAllowedFRDiff = 1.33 * Math.Abs(acceptedVideoFrameRate - ((acceptedVideoFrameRate * (m_EndTimeFrame - m_StartTimeFrame) + allowedExcess) / (m_EndTimeFrame - m_StartTimeFrame)));

			if (m_VideoController.IsPlainAviVideo && Math.Abs(derivedFrameRate - acceptedVideoFrameRate) > maxAllowedFRDiff)
			{
				if (MessageBox.Show(
					"Based on your entered frame times it appears that there may be dropped frames, incorrectly entered start " +
					"or end time, an almanac update or a leap second event. Do you want to enter the start and end times again?",
					"Warning",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					return DialogResult.Retry;
				}

				return DialogResult.Ignore;
			}

			return DialogResult.OK;			
		}

		internal void EnterViewLightCurveMode(LCFile lcFile, IVideoController videoController, Panel controlPanelHolder)
		{
			m_ControlPanelHolder = controlPanelHolder;

			m_VideoController = (VideoController)videoController;
			EnsureControlPanel(m_ControlPanelHolder);

			m_LightCurveController.SetLcFile(lcFile);

			m_Measuring = false;
			m_Refining = false;
			m_ViewingLightCurve = true;
			m_Configuring = false;

			m_StateMachine = new LCStateMachine(this, m_VideoController);
			m_StateMachine.ChangeState(LightCurvesState.Viewing);

			m_StateMachine.SelectedMeasuringStar = -1;
			m_StateMachine.SelectedObject = null;

			m_MeasurementInterval = 1;
			m_CurrFrameNo = -1;
		    m_CurrFileName = null;

			m_ControlPanel.BeginConfiguration(m_StateMachine, m_VideoController);
			m_ControlPanel.SetupLCFileInfo(m_LightCurveController.LcFile);
			m_ControlPanel.UpdateState();

		}

        internal void ToggleShowFields(bool showFields)
        {
			m_VideoController.RedrawCurrentFrame(showFields);
        }

        private long prevFrameId;
        public void SaveEmbeddedOrORCedTimeStamp(int frameNo)
        {
            if (m_VideoController.HasAstroImageState && !m_VideoController.HasTimestampOCR() && !m_VideoController.IsAstroAnalogueVideoWithNtpTimestampsInNtpDebugMode)
			{
                if (m_VideoController.HasEmbeddedTimeStamps())
                {
                    FrameStateData frameState = m_VideoController.GetCurrentFrameState();
                    Trace.Assert(prevFrameId != frameState.VideoCameraFrameId || frameState.VideoCameraFrameId == 0 /* When VideoCameraFrameId is not supported */);
                    prevFrameId = frameState.VideoCameraFrameId;

                    int frameDuration = (int)Math.Round(frameState.ExposureInMilliseconds);
	                if (frameDuration == 0) frameDuration = 1;
                    LCFile.SaveOnTheFlyFrameTiming(new LCFrameTiming(frameState.CentralExposureTime, frameDuration));
                }
                else
                {
                    // Nothing to save
                }
			}
			else if (m_VideoController.HasTimestampOCR())
			{
				int frameDuration = (int)Math.Round(1000.0/m_VideoController.VideoFrameRate /*MillisecondsPerFrame*/);
                m_OCRedTimeStamp = m_VideoController.OCRTimestamp();
				var frameTime = new LCFrameTiming(m_OCRedTimeStamp, frameDuration);

                if (m_VideoController.IsAstroAnalogueVideoWithNtpTimestampsInNtpDebugMode)
                {
                    m_VideoController.GetAdditionalAAVTimes(frameNo, ref frameTime.FrameMidTimeNTPRaw, ref frameTime.FrameMidTimeNTPTangra, ref frameTime.FrameMidTimeWindowsRaw);
                    frameTime.FrameMidTime = new DateTime(
                        frameTime.FrameMidTimeNTPRaw.Value.Year, frameTime.FrameMidTimeNTPRaw.Value.Month,
                        frameTime.FrameMidTimeNTPRaw.Value.Day,
                        frameTime.FrameMidTime.Hour, frameTime.FrameMidTime.Minute, frameTime.FrameMidTime.Second,
                        frameTime.FrameMidTime.Millisecond);
                }

				LCFile.SaveOnTheFlyFrameTiming(frameTime);
			}
        }

        public void MeasureObjects()
        {
            if (m_MinFrame > m_CurrFrameNo) m_MinFrame = (uint)m_CurrFrameNo;
            if (m_MaxFrame < m_CurrFrameNo) m_MaxFrame = (uint)m_CurrFrameNo;
            m_TotalFrames++;

			foreach (ITrackedObject trackedObject in m_Tracker.TrackedObjects)
			{
				IImagePixel center = trackedObject.Center;

				List<ITrackedObject> objectsInGroup = null;
				if (trackedObject.OriginalObject.GroupId > -1)
					objectsInGroup = m_Tracker.TrackedObjects.Where(x => x.OriginalObject.GroupId == trackedObject.OriginalObject.GroupId).ToList();

				if ((ImagePixel)center != ImagePixel.Unspecified)
				{
					MeasureTrackedObject2(trackedObject,
											m_Measurer,
											LightCurveReductionContext.Instance.DigitalFilter,
                                            false,
											objectsInGroup,
											m_Tracker.RefinedFWHM[trackedObject.TargetNo],
											m_Tracker.RefinedAverageFWHM);
				}
				else
				{
					uint[,] data = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(
						(int)Math.Round(center.XDouble), (int)Math.Round(center.YDouble), 35);

					uint flags = trackedObject.GetTrackingFlags();

					// Add unsuccessfull measurement for this object and this frame
					LCFile.SaveOnTheFlyMeasurement(LCMeasurement.UnsuccessfulMeasurement(
													(uint)m_CurrFrameNo,
                                                    m_CurrFileName,
													(byte)trackedObject.TargetNo,
													flags,
						/* but we want to use the actual measurement fit. This only matters for reviewing the light curve when not opened from a file. */
													data /* save the original non filtered data */, m_OCRedTimeStamp,
													trackedObject.OriginalObject.GroupId));
				}
            }
        }

        private SpinLock m_WriterLock;

		private void MeasureTrackedObject2(
			ITrackedObject trackedObject,
			MeasurementsHelper measurer,
			TangraConfig.PreProcessingFilter filter,
			bool synchronise,
			List<ITrackedObject> objectsInGroup,
			float refinedFWHM,
			float averageFWHM)
		{
			IImagePixel center = trackedObject.Center;
			int areaSize = 17;
			if ((objectsInGroup != null && objectsInGroup.Count > 1) || LightCurveReductionContext.Instance.NoiseMethod == TangraConfig.BackgroundMethod.Background3DPolynomial)
				// for double PSF fits and/or 3d polynomial background fits we need the largest area
				areaSize = 35;

			IImagePixel[] groupCenters = new IImagePixel[0];
			float[] aperturesInGroup = new float[0];

			if (objectsInGroup != null && objectsInGroup.Count > 1)
			{
				groupCenters = new IImagePixel[objectsInGroup.Count];
				aperturesInGroup = new float[objectsInGroup.Count];

				for (int i = 0; i < objectsInGroup.Count; i++)
				{
					groupCenters[i] = objectsInGroup[i].Center;
					aperturesInGroup[i] = objectsInGroup[i].OriginalObject.ApertureInPixels;
				}
			}

			int areaDigitalFilterEdge = 0;
			if (LightCurveReductionContext.Instance.DigitalFilter != TangraConfig.PreProcessingFilter.NoFilter)
				areaDigitalFilterEdge = 2; // The extra 2 pixels will be cut after the filter is applied before the measurement
			
			int centerX = (int)Math.Round(center.XDouble);
			int centerY = (int)Math.Round(center.YDouble);

			uint[,] data = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(centerX, centerY, areaSize + areaDigitalFilterEdge);
			uint[,] backgroundPixels = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(centerX, centerY, 35 + areaDigitalFilterEdge);

			float msrX0 = (float)trackedObject.Center.XDouble;
			float msrY0 = (float)trackedObject.Center.YDouble;

			var measuredObject = new LCMeasurement()
			{
				CurrFrameNo = (uint) m_CurrFrameNo,
                CurrFileName = m_CurrFileName,
				TargetNo = (byte) trackedObject.TargetNo,
				X0 = msrX0,
				Y0 = msrY0,
				PixelDataX0 = centerX,
				PixelDataY0 = centerY,
				OSDTimeStamp = m_OCRedTimeStamp,
				FlagsDWORD = trackedObject.GetTrackingFlags()
			};

			IMeasurableObject measurableObject = (IMeasurableObject)trackedObject;

			NotMeasuredReasons rv = MeasureObject(
				center,
				data,				
				backgroundPixels,
				m_VideoController.VideoBitPix,
				measurer,
				filter,
				LightCurveReductionContext.Instance.ReductionMethod,
				LightCurveReductionContext.Instance.PsfQuadratureMethod,
                TangraConfig.Settings.Photometry.PsfFittingMethod,
				trackedObject.OriginalObject.ApertureInPixels,
				refinedFWHM,
				averageFWHM,
				measurableObject,
				groupCenters,
				aperturesInGroup,
				LightCurveReductionContext.Instance.FullDisappearance);

			measuredObject.SetIsMeasured(rv);

			uint[,] pixelsToSave = trackedObject.IsOffScreen
									   ? new uint[35, 35]
									   // As the background may have been pre-processed for measuring, we need to take another copy for saving in the file
									   : m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels(centerX, centerY, 35);



			bool lockTaken = false;

			if (synchronise)
				m_WriterLock.TryEnter(ref lockTaken);

			try
			{
				uint flags = trackedObject.GetTrackingFlags();

				measuredObject.TotalReading = (uint)Math.Round(measurer.TotalReading);
				measuredObject.TotalBackground = (uint)Math.Round(measurer.TotalBackground);
				measuredObject.PixelData = pixelsToSave; /* save the original non filtered data */
				measuredObject.FlagsDWORD |= flags;
				measuredObject.ApertureX = measurer.XCenter;
				measuredObject.ApertureY = measurer.YCenter;
				measuredObject.ApertureSize = measurer.Aperture;

				m_LastTotalReading[trackedObject.TargetNo] = measuredObject.TotalReading;
				m_LastTotalBackground[trackedObject.TargetNo] = measuredObject.TotalBackground;
				m_LastApertureX[trackedObject.TargetNo] = measuredObject.ApertureX;
				m_LastApertureY[trackedObject.TargetNo] = measuredObject.ApertureY;
				m_LastApertureSize[trackedObject.TargetNo] = measuredObject.ApertureSize;
				m_LastOCRedTimeStamp[trackedObject.TargetNo] = measuredObject.OSDTimeStamp;

				LCFile.SaveOnTheFlyMeasurement(measuredObject);
			}
			finally
			{
				if (synchronise && lockTaken)
					m_WriterLock.Exit();
			}
		}

		internal static NotMeasuredReasons MeasureObject(
			IImagePixel center,
			uint[,] data,
			uint[,] backgroundPixels,
			int bpp,
			MeasurementsHelper measurer,
			TangraConfig.PreProcessingFilter filter,
			TangraConfig.PhotometryReductionMethod reductionMethod,
			TangraConfig.PsfQuadrature psfQuadrature,
            TangraConfig.PsfFittingMethod psfFittngMethod,
			float aperture,
			double refinedFWHM,
			float refinedAverageFWHM,
			IMeasurableObject measurableObject,
			IImagePixel[] groupCenters,
			float[] aperturesInGroup,
			bool fullDisappearance
			)
		{
			return measurer.MeasureObject(
				center, data, backgroundPixels, bpp, filter, reductionMethod, psfQuadrature, psfFittngMethod,
				aperture, refinedFWHM, refinedAverageFWHM, measurableObject, groupCenters, aperturesInGroup, fullDisappearance);
		}

		internal LCFile FlushLightCurveFile()
		{
			var matrixSizes = new List<int>();
			var apertures = new List<float>();
			var fixedFlags = new List<bool>();
			var psfGroupIds = new List<int>();

			m_Tracker.TrackedObjects.ForEach(
				delegate(ITrackedObject o)
				{
					matrixSizes.Add(o.OriginalObject.PsfFitMatrixSize);
					apertures.Add(o.OriginalObject.ApertureInPixels);
					fixedFlags.Add(o.OriginalObject.IsWeakSignalObject);
					psfGroupIds.Add(o.OriginalObject.GroupId);
				}
			);

			MeasurementTimingType measurementTimingType = MeasurementTimingType.UserEnteredFrameReferences;
			if (m_VideoController.HasEmbeddedTimeStamps())
				measurementTimingType = MeasurementTimingType.EmbeddedTimeForEachFrame;
			else if (m_VideoController.HasTimestampOCR())
				measurementTimingType = MeasurementTimingType.OCRedTimeForEachFrame;

            SerUseTimeStamp serTimingType = SerUseTimeStamp.None;
            SerTimeStampReference serTimeStampReference = SerTimeStampReference.MidFrame;
		    if (m_VideoController.IsSerVideo)
		    {
                serTimingType = m_VideoController.GetSerTimingType();
                serTimeStampReference = m_VideoController.GetSerTimeStampReference();
		    }

			LCMeasurementHeader finalHeader = new LCMeasurementHeader(
				m_VideoController.CurrentVideoFileName,
				string.Format("Video ({0})", m_VideoController.CurrentVideoFileType),
				m_VideoController.VideoFirstFrame,
				m_VideoController.VideoCountFrames,
				m_VideoController.VideoFrameRate,
				m_MinFrame,
				m_MaxFrame,
				(uint)m_TotalFrames,
				(uint)m_MeasurementInterval,
				(byte)m_Tracker.TrackedObjects.Count,
				LightCurveReductionContext.Instance.LightCurveReductionType,
				measurementTimingType,
                serTimingType,
                serTimeStampReference,
				(int)LightCurveReductionContext.Instance.NoiseMethod,
				(int)LightCurveReductionContext.Instance.DigitalFilter,
				matrixSizes.ToArray(), apertures.ToArray(), fixedFlags.ToArray(), psfGroupIds.ToArray(), (float)m_Tracker.PositionTolerance);

			finalHeader.FirstTimedFrameTime = m_StartFrameTime;
			finalHeader.SecondTimedFrameTime = m_EndFrameTime;

			finalHeader.FirstTimedFrameNo = m_StartTimeFrame;
			finalHeader.LastTimedFrameNo = m_EndTimeFrame;

			if (m_AveragedFrame == null)
			{
				if (m_StackedAstroImage == null) EnsureStackedAstroImage();
				m_AveragedFrame = new AveragedFrame(GetStackedAstroImage());
			}

            PreProcessingInfo preProcessingInfo;
            PInvoke.TangraCore.PreProcessors.PreProcessingGetConfig(out preProcessingInfo);

			LCMeasurementFooter footer = new LCMeasurementFooter(
				m_AveragedFrame.Pixelmap,
				TangraConfig.Settings,
				LightCurveReductionContext.Instance,
				m_StateMachine.MeasuringStars,
				m_Tracker,
                m_VideoController.GetTimestampOCRNameAndVersion(),
				null,
				m_InstumentalDelaySelectedConfig,
				m_InstumentalDelaySelectedCamera,
                m_CameraName,
				m_AavNativeVideoFormat,
                m_AavFrameIntegration,
				m_AavNtpTimestampError,
				m_AavNtpFitOneSigmaError,
				TangraConfig.Settings.AAV.NtpTimeUseDirectTimestamps,
				PSFFit.BitPix,
				PSFFit.NormVal,
                m_AavStackedFrameRate,
                m_FitsDynamicFromValue,
                m_FitsDynamicToValue,
                preProcessingInfo.RotateFlipType,
                m_TimingCorrectionsRequired,
                m_AcquisitionDelayMs,
                m_ReferenceTimeToUtcOffsetMs);

			return LCFile.FlushOnTheFlyOutputFile(finalHeader, footer, m_VideoController);
		}

		internal void DoShowLightCurve(LCFile file)
		{
			m_LightCurveController.EnsureLightCurveFormClosed();
			m_VideoController.EnsureLightCurveForm();

			TangraContext.Current.HasVideoLoaded = true;
			TangraContext.Current.CanPlayVideo = false;
			m_VideoController.UpdateViews();

			m_LightCurveController.SetLcFile(file);
			m_VideoController.MoveToFrame((int)m_LightCurveController.LcFile.Header.MinFrame);
		}

		internal bool ConfirmNewObject(int objectId, bool tryAutoDoubleFind)
        {
            if (LightCurveReductionContext.Instance.LightCurveReductionType == LightCurveReductionType.MutualEvent)
            {
				var frmAddMutualTarget = new frmAddOrEditMutualEventsTarget(objectId, m_StateMachine.SelectedObject, m_StateMachine.SelectedObjectGaussian, m_StateMachine, m_VideoController, tryAutoDoubleFind);
                if (frmAddMutualTarget.ShowDialog() == DialogResult.Cancel)
                    return false;

				m_StateMachine.ObjectSelected(frmAddMutualTarget.ObjectToAdd, false, false);

				if (frmAddMutualTarget.ObjectToAdd2 != null)
					m_StateMachine.ObjectSelected(frmAddMutualTarget.ObjectToAdd2, false, false);
            }
            else
            {
                var frmAddSingleTarget = new frmAddOrEditSingleTarget(objectId, m_StateMachine.SelectedObject, m_StateMachine.SelectedObjectGaussian, m_StateMachine, m_VideoController);
                if (frmAddSingleTarget.ShowDialog() == DialogResult.Cancel)
                    return false;

				m_StateMachine.ObjectSelected(frmAddSingleTarget.ObjectToAdd, false, false);
            }

            return true;
        }

        internal bool EditCurrentObject()
        {
			TrackedObjectConfig selectedObject = m_StateMachine.MeasuringStars[m_StateMachine.SelectedMeasuringStar];
	        int selectedStarId = m_StateMachine.SelectedMeasuringStar;

			if (LightCurveReductionContext.Instance.LightCurveReductionType == LightCurveReductionType.MutualEvent)
			{
				var frmAddMutualTarget = new frmAddOrEditMutualEventsTarget(m_StateMachine.SelectedMeasuringStar, selectedObject, m_StateMachine, m_VideoController);
				DialogResult result = frmAddMutualTarget.ShowDialog();
				if (result == DialogResult.Cancel)
					return false;

				if (result == DialogResult.Abort)
				{
					m_StateMachine.ObjectSelected(frmAddMutualTarget.ObjectToAdd, false, true /* Ctrl = 'DELETE' */);
					if (frmAddMutualTarget.ObjectToAdd2 != null)
						m_StateMachine.ObjectSelected(frmAddMutualTarget.ObjectToAdd2, false, true /* Ctrl = 'DELETE' */);

					m_StateMachine.SelectedMeasuringStar = -1;
					m_VideoController.RefreshCurrentFrame();

					return false;
				}

				if (selectedObject.GroupId != frmAddMutualTarget.ObjectToAdd.GroupId)
				{
					if (selectedObject.GroupId > -1)
					{
						// Remove the previous group as now the group is different or is not a group
						List<TrackedObjectConfig> objectsInGroup = m_StateMachine.MeasuringStars.Where(x => x.GroupId == selectedObject.GroupId).ToList();
						foreach (TrackedObjectConfig prevObj in objectsInGroup)
							m_StateMachine.ObjectSelected(prevObj, false, true /* Ctrl = 'DELETE' */);
					}
					else
						// Remove the previous object as now we have a group of objects
						m_StateMachine.ObjectSelected(selectedObject, false, true /* Ctrl = 'DELETE' */);
				}

				m_StateMachine.ObjectEdited(selectedStarId, frmAddMutualTarget.ObjectToAdd);

				if (frmAddMutualTarget.ObjectToAdd2 != null)
					m_StateMachine.ObjectEdited(selectedStarId, frmAddMutualTarget.ObjectToAdd2);
			}
			else
			{				
				frmAddOrEditSingleTarget frmAddSingleTarget =
					new frmAddOrEditSingleTarget(m_StateMachine.SelectedMeasuringStar, selectedObject, m_StateMachine, m_VideoController);

				DialogResult result = frmAddSingleTarget.ShowDialog();
				if (result == DialogResult.Cancel)
					return false;

				if (result == DialogResult.Abort)
				{
					m_StateMachine.ObjectSelected(frmAddSingleTarget.ObjectToAdd, false, true /* Ctrl = 'DELETE' */);
					m_StateMachine.SelectedMeasuringStar = -1;
					m_VideoController.RefreshCurrentFrame();

					return false;
				}

				m_StateMachine.ObjectEdited(selectedStarId, frmAddSingleTarget.ObjectToAdd);
			}

            return true;
        }

        internal int SkipNextFrames(int framesToSkip, bool synchronise)
        {
            int nextFrameNo;
            int skippedFrames = 0;
            do
            {
                nextFrameNo = SkipCurrentFrame(synchronise);
                skippedFrames++;
            }
            while (nextFrameNo < m_VideoController.VideoLastFrame && skippedFrames < framesToSkip);

            return nextFrameNo;
        }

		internal int SkipCurrentFrame(bool synchronise)
		{
			if (m_VideoController.CurrentFrameIndex < m_VideoController.VideoLastFrame)
			{
				m_TotalFrames++;

				m_ProcessedFrames++;
				if (!m_Tracker.IsTrackedSuccessfully) m_UnsuccessfulFrames++;
				if (m_Tracker.IsTrackedSuccessfully && m_Tracker.TrackedObjects.Any(x => !x.IsLocated))
					m_PartiallySuccessfulFrames++;

				SaveEmbeddedOrORCedTimeStamp(m_CurrFrameNo);

				m_PrevMeasuredFrame = m_CurrFrameNo;

				foreach (ITrackedObject trackedObject in m_Tracker.TrackedObjects)
				{
					IImagePixel center = trackedObject.LastKnownGoodPosition;

					uint[,] data = m_VideoController.GetCurrentAstroImage(false).GetMeasurableAreaPixels((int)Math.Round(center.XDouble), (int)Math.Round(center.YDouble), 35);

					var measuredObject = new LCMeasurement()
					{
						CurrFrameNo = (uint)m_CurrFrameNo,
						TargetNo = (byte)trackedObject.TargetNo,
						X0 = (float)center.XDouble,
						Y0 = (float)center.YDouble,
						PixelDataX0 = center.X,
						PixelDataY0 = center.Y,
						OSDTimeStamp = m_OCRedTimeStamp,
						FlagsDWORD = 7 /* FitSuspectAsNoGuidingStarsAreLocated */,
						PixelData = data,
						PsfGroupId = trackedObject.OriginalObject.GroupId
					};

					bool lockTaken = false;
					 
					if (synchronise)
						m_WriterLock.TryEnter(ref lockTaken);

					try
					{
						measuredObject.TotalReading = m_LastTotalReading[trackedObject.TargetNo];
						measuredObject.TotalBackground = m_LastTotalBackground[trackedObject.TargetNo];
						measuredObject.ApertureX = m_LastApertureX[trackedObject.TargetNo];
						measuredObject.ApertureY = m_LastApertureY[trackedObject.TargetNo];
						measuredObject.ApertureSize = m_LastApertureSize[trackedObject.TargetNo];
						measuredObject.OSDTimeStamp = m_LastOCRedTimeStamp[trackedObject.TargetNo]; 

						LCFile.SaveOnTheFlyMeasurement(measuredObject);
					}
					finally
					{
						if (synchronise && lockTaken)
							m_WriterLock.Exit();
					}
				}

				m_VideoController.StepForward();
			}

			return m_CurrFrameNo;
		}

        public bool ToggleStackedSelection()
        {
            m_StackedSelection = !m_StackedSelection;
            
            TangraConfig.Settings.Photometry.UseStackedSelection = m_StackedSelection;
            TangraConfig.Settings.Save();

            ResetStackedSelectionFrame();
            return m_StackedSelection;
        }

        public bool IsStackedSelectionEnabled
        {
            get { return m_StackedSelection; }
        }

        public bool HasStackedSelectionFrame
        {
            get { return m_StackedSelection && m_StackedSelectionFrame != null; }
        }

        public bool CanShowStackedSelection()
        {
            return m_VideoController != null && m_VideoController.SupportsSoftwareIntegration && 
                   (m_StateMachine == null ||
                   m_StateMachine.CurrentState == LightCurvesState.SelectMeasuringStars ||
                   m_StateMachine.CurrentState == LightCurvesState.SelectTrackingStars ||
                   m_StateMachine.CurrentState == LightCurvesState.ReadyToRun);
        }

        public override void DisplayIntensifyModeChanged()
        {
            base.DisplayIntensifyModeChanged();

            ResetStackedSelectionFrame();
        }
    }

    internal enum MeasuringZoomImageType
    {
        Stripe,
        Pixel,
        PSF,
        None
    }

}
