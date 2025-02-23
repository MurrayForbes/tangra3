﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tangra.Model.Config;
using Tangra.Model.Video;
using Tangra.VideoOperations.LightCurves;
using Tangra.VideoOperations.LightCurves.Tracking;

namespace Tangra.VideoOperations.LightCurves.Tracking
{
    internal static class TrackerFactory
    {
		public static ITracker CreateTracker(
            int imageWidth, 
            int imageHeight, 
            LightCurveReductionType lightCurveReductionType,
            VideoFileFormat fileFormat, 
            bool isFitsVideo,
            List<TrackedObjectConfig> measuringStars, 
            out string usedTrackerType)
        {
            // NOTE: Figure out what tracker to create based on the type of event, number of objects and their intensity
            bool createRefiningTracker = TangraConfig.Settings.Tracking.SelectedEngine == TangraConfig.TrackingEngine.TrackingWithRefining;

            if (TangraConfig.Settings.Tracking.SelectedEngine == TangraConfig.TrackingEngine.LetTangraChoose)
            {
                if (LightCurveReductionContext.Instance.WindOrShaking ||
                    LightCurveReductionContext.Instance.StopOnLostTracking ||
                    LightCurveReductionContext.Instance.IsDriftThrough ||
                    LightCurveReductionContext.Instance.HighFlickeringOrLargeStars ||
                    LightCurveReductionContext.Instance.FullDisappearance)
                {
                    createRefiningTracker = true;
                }
            }

			bool createFITSFileTracker = lightCurveReductionType == LightCurveReductionType.VariableStarOrTransit &&
                                         fileFormat == VideoFileFormat.FITS && !isFitsVideo;

			if (lightCurveReductionType == LightCurveReductionType.Asteroidal || lightCurveReductionType == LightCurveReductionType.VariableStarOrTransit)
            {
				if (createFITSFileTracker && lightCurveReductionType == LightCurveReductionType.VariableStarOrTransit)
	            {
					usedTrackerType = "Star field tracker";
					return new StarFieldTracker(measuringStars);
	            }
	            else if (createRefiningTracker)
	            {
		            if (measuringStars.Count == 1)
		            {
			            usedTrackerType = "One star tracking";
			            return new OneStarTracker(measuringStars);
		            }
		            else
		            {
						usedTrackerType = "Tracking with recovery";
			            return new OccultationTracker(measuringStars);
		            }
	            }
	            else
	            {
#if WIN32
					if (TangraConfig.Settings.Tracking.UseNativeTracker)
					{
						usedTrackerType = "Simplified Native";
						return new NativeSimplifiedTracker(imageWidth, imageHeight, measuringStars, LightCurveReductionContext.Instance.FullDisappearance);
					}
					else
#endif
                    {
						usedTrackerType = "Simplified";
						return new SimplifiedTracker(measuringStars);
					}					
	            }
            }
			else if (lightCurveReductionType == LightCurveReductionType.MutualEvent)
			{
				if (measuringStars.Any(x => x.ProcessInPsfGroup))
				{
					usedTrackerType = "Mutual Event M";
					return new MutualEventTracker(measuringStars, LightCurveReductionContext.Instance.FullDisappearance);
				}
				else
				{
                    if (createRefiningTracker)
                    {
                        usedTrackerType = "Mutual Event O";
                        return new OccultationTracker(measuringStars);
                    }

                    usedTrackerType = "Mutual Event S";
                        
#if WIN32
					if (TangraConfig.Settings.Tracking.UseNativeTracker)
						return new NativeSimplifiedTracker(imageWidth, imageHeight, measuringStars, LightCurveReductionContext.Instance.FullDisappearance);
					else
#endif
						return new SimplifiedTracker(measuringStars);
				}
			}
			else if (lightCurveReductionType == LightCurveReductionType.TotalLunarDisappearance || 
				lightCurveReductionType == LightCurveReductionType.TotalLunarReppearance ||
				lightCurveReductionType == LightCurveReductionType.LunarGrazingOccultation)
            {
				usedTrackerType = "Lunar Occuration Tracker";
				return new LunarOccultationTracker(measuringStars);
			}
            else if (lightCurveReductionType == LightCurveReductionType.UntrackedMeasurement)
            {
	            usedTrackerType = "Untracked";
                return new UntrackedTracker(measuringStars);
            }

			throw new NotSupportedException();
        }
    }
}
