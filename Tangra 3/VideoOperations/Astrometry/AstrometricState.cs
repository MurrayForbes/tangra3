﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tangra.Astrometry;
using Tangra.Astrometry.Recognition;
using Tangra.AstroServices;
using Tangra.Config;
using Tangra.ImageTools;
using Tangra.Model.Astro;
using Tangra.Model.Config;
using Tangra.Model.Helpers;
using Tangra.Model.Video;
using Tangra.MotionFitting;
using Tangra.StarCatalogues;
using Tangra.VideoOperations.Astrometry.Engine;
using Tangra.VideoOperations.LightCurves;

namespace Tangra.VideoOperations.Astrometry
{
	internal enum AstrometryInFramesState
	{
		AttemptingInitialFit,
		Ready,
		RunningMeasurements,
		Aborting,
		Paused
	}

	internal class UserObjectContext
	{
		internal double RADeg { get; set; }
		internal double DEDeg { get; set; }
		internal float X0 { get; set; }
		internal float Y0 { get; set; }

		internal PSFFit Gaussian { get; set; }
		internal LeastSquareFittedAstrometry AstrometricFit { get; set; }
	}

	internal class MeasurementContext
	{
		internal UserObjectContext ObjectToMeasure { get; set; }
		internal double MaxStdDev { get; set; }
		internal int FrameInterval { get; set; }
		internal int MaxMeasurements { get; set; }
		internal DateTime FirstFrameUtcTime { get; set; }
		internal int FirstFrameId { get; set; }
		internal double FrameRate { get; set; }

		internal float ApertureSize { get; set; }
		internal float AnnulusInnerRadius { get; set; }
		internal int AnnulusMinPixels { get; set; }

		internal bool PerformPhotometricFit { get; set; }
		internal TangraConfig.PhotometryReductionMethod PhotometryReductionMethod { get; set; }
		internal TangraConfig.BackgroundMethod PhotometryBackgroundMethod { get; set; }

        internal Guid PhotometryCatalogBandId { get; set; }
        public StarCatalogueFacade StarCatalogueFacade { get; set; }
		internal TangraConfig.MagOutputBand PhotometryMagOutputBand { get; set; }
        internal double AssumedTargetVRColour { get; set; }

		internal bool ExportCSV { get; set; }
		internal bool StopOnNoFit { get; set; }

		internal MovementExpectation MovementExpectation { get; set; }
		internal ObjectExposureQuality ObjectExposureQuality { get; set; }
		internal FrameTimeType FrameTimeType { get; set; }
		internal double InstrumentalDelay { get; set; }
		internal InstrumentalDelayUnits InstrumentalDelayUnits { get; set; }
		internal int IntegratedFramesCount { get; set; }
	    internal double IntegratedExposureSeconds { get; set; }
	    internal bool AavStackedMode { get; set; }
		internal bool AavIntegration { get; set; }
		internal VideoFileFormat VideoFileFormat { get; set; }
        internal string NativeVideoFormat { get; set; }

	    internal FittingContext ToFittingContext()
	    {
	        return new FittingContext()
	        {
	            FirstFrameUtcTime = this.FirstFrameUtcTime,
	            FirstFrameIdInIntegrationPeroid = this.FirstFrameId,
	            FrameRate = this.FrameRate,
	            MovementExpectation = this.MovementExpectation,
	            FrameTimeType = this.FrameTimeType,
	            InstrumentalDelay = this.InstrumentalDelay,
	            InstrumentalDelayUnits = this.InstrumentalDelayUnits,
	            IntegratedFramesCount = this.IntegratedFramesCount,
	            AavStackedMode = this.AavStackedMode,
	            VideoFileFormat = this.VideoFileFormat,
	            NativeVideoFormat = this.NativeVideoFormat,
	            ObjectExposureQuality = this.ObjectExposureQuality
	        };
	    }
	}

	public class AstrometricState
	{
		private List<IIdentifiedObject> m_IdentifiedObjects = new List<IIdentifiedObject>();

		internal List<IIdentifiedObject> IdentifiedObjects
		{
			get { return m_IdentifiedObjects; }
		}

		internal void AddIdentifiedObjects(List<IIdentifiedObject> objects)
		{
			if (objects != null)
				m_IdentifiedObjects.AddRange(objects);
		}

		internal void Reset()
		{
			m_IdentifiedObjects.Clear();
			ObjectToMeasure = null;
			MeasuringState = AstrometryInFramesState.AttemptingInitialFit;
			Measurements.Clear();
			ObjectToMeasureSelected = false;
		}

		internal IIdentifiedObject GetIdentifiedObjectAt(double raDeg, double deDeg)
		{
			foreach (IIdentifiedObject entry in m_IdentifiedObjects)
			{
				double distance = AngleUtility.Elongation(raDeg, deDeg, entry.RAHours * 15, entry.DEDeg);
				if (distance < CoreAstrometrySettings.Default.IdentifiedObjectResidualInArcSec)
					return entry;
			}

			return null;
		}

		public LeastSquareFittedAstrometry AstrometricFit { get; set; }

		internal AstrometryInFramesState MeasuringState = AstrometryInFramesState.Ready;
	    internal PerformMatchResult MatchResult;
		internal UserObjectContext ObjectToMeasure;
	    internal string IdentifiedObjectToMeasure;

		private static object s_SyncRoot = new object();
		internal static AstrometricState EnsureAstrometricState()
		{
			lock (s_SyncRoot)
			{
				AstrometricState state = AstrometryContext.Current.AstrometricState;
				if (state == null)
				{
					state = new AstrometricState();
					AstrometryContext.Current.AstrometricState = state;
				}

				return state;
			}
		}

		internal List<SingleMultiFrameMeasurement> Measurements = new List<SingleMultiFrameMeasurement>();

		internal SelectedObject SelectedObject;

		internal bool ObjectToMeasureSelected = false;

		internal bool ManualStarIdentificationMode = false;

		internal Dictionary<PSFFit, IStar> ManuallyIdentifiedStars = new Dictionary<PSFFit, IStar>();
	}
}
