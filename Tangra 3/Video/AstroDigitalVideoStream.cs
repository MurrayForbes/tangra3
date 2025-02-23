﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Tangra.Controller;
using Tangra.Helpers;
using Tangra.Model.Config;
using Tangra.Model.Context;
using Tangra.Model.Helpers;
using Tangra.Model.Image;
using Tangra.Model.Numerical;
using Tangra.Model.Video;
using Tangra.Model.VideoOperations;
using Tangra.PInvoke;
using Tangra.Video.AstroDigitalVideo;
using Tangra.View.CustomRenderers;
using Tangra.View.CustomRenderers.AavTimeAnalyser;

namespace Tangra.Video
{
	public class ADVFormatException : Exception
	{
		public ADVFormatException(string message)
			: base(message)
		{ }
	}

    public class AdvFileMetadataInfo
    {
        public string Recorder;
        public string AdvrVersion;
        public string HtccFirmareVersion;
        public string Camera;
        public string SensorInfo;
        public string Engine;
	    public bool HasNTPTimeStamps;
        public string ObjectName;
    }

	public class AstroDigitalVideoStream : IFrameStream
	{
        public static IFrameStream OpenFile(string fileName, VideoController videoController, out AdvFileMetadataInfo fileMetadataInfo, out GeoLocationInfo geoLocation)
		{
            fileMetadataInfo = new AdvFileMetadataInfo();
			geoLocation = new GeoLocationInfo();
			try
			{
                int version = TangraCore.ADV2GetFormatVersion(fileName);

                IFrameStream rv;
			    if (version == 1)
			    {
			        var adv1 = new AstroDigitalVideoStream(fileName, ref fileMetadataInfo, ref geoLocation);
			        if (adv1.IsStatusChannelOnly)
			        {
			            TangraContext.Current.CustomRenderer = new AavStatusChannelOnlyRenderer(adv1);
			            return null;
			        }
			        rv = adv1;
			    }
			    else
			    {
                    rv = AstroDigitalVideoStreamV2.OpenFile(fileName, out fileMetadataInfo, out geoLocation);
			    }

			    TangraContext.Current.RenderingEngine = fileMetadataInfo.Engine == "AAV" ? "AstroAnalogueVideo" : "AstroDigitalVideo";

				if (fileMetadataInfo.Engine == "AAV")
					UsageStats.Instance.ProcessedAavFiles++;
				else
                    UsageStats.Instance.ProcessedAdvFiles++;
				UsageStats.Instance.Save();

			    return rv;
			}
			catch(ADVFormatException ex)
			{
				MessageBox.Show(ex.Message, "Error opening ADV/AAV file", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return null;
		}

	    private string m_FileName;
		private int m_Width;
		private int m_Height;
		private double m_FrameRate;

		private int m_FirstFrame;
		private int m_CountFrames;

		private int m_BitPix;
		private uint m_Aav16NormVal;
	    private string m_Engine;
		private string m_CameraModel;
		private string m_VideoStandard;
		private double m_NativeFrameRate;
	    private double m_EffectiveFrameRate;
	    private int m_IntegratedAAVFrames;
		private int m_OsdFirstLine = 0;
		private int m_OsdLastLine = 0;
		private bool m_UsesNtpTimestamps;
	    private int m_StackingRate = 0;

		private int m_AlmanacOffsetLastFrame;
		private bool m_AlamanacOffsetLastFrameIsGood;
	    private bool m_IsStatusChannelOnly;

		private AdvFrameInfo m_CurrentFrameInfo;

	    private object m_SyncLock = new object();

		internal AstroDigitalVideoStream(string fileName, ref AdvFileMetadataInfo fileMetadataInfo, ref GeoLocationInfo geoLocation)
		{
			CheckAdvFileFormat(fileName, ref fileMetadataInfo, ref geoLocation);

			m_FileName = fileName;
			var fileInfo = new AdvFileInfo();
			
			TangraCore.ADVOpenFile(fileName, ref fileInfo);

			m_FirstFrame = 0;
			m_CountFrames = fileInfo.CountFrames;

			m_BitPix = fileInfo.Bpp;
			m_Width = fileInfo.Width;
			m_Height = fileInfo.Height;
			m_Aav16NormVal = fileInfo.Aav16NormVal;

			m_FrameRate = fileInfo.FrameRate;
			

			// Get the last frame in the video and read the Almanac Offset and Almanac Status so they are applied 
			// to the frames that didn't have Almanac Status = Updated
			if (m_CountFrames > 0)
			{
				GetPixelmap(m_FirstFrame + m_CountFrames - 1);

				m_AlamanacOffsetLastFrameIsGood = m_CurrentFrameInfo.AlmanacStatusIsGood;
				m_AlmanacOffsetLastFrame = m_CurrentFrameInfo.GetSignedAlamancOffset();
			}
			else
			{
				m_AlamanacOffsetLastFrameIsGood = false;
				m_AlmanacOffsetLastFrame = 0;
			}

		    m_Engine = fileMetadataInfo.Engine;
			m_CameraModel = fileMetadataInfo.Camera;
			m_UsesNtpTimestamps = fileMetadataInfo.HasNTPTimeStamps;

			if (m_Engine == "AAV")
			{
				m_VideoStandard = GetFileTag("NATIVE-VIDEO-STANDARD");
				double.TryParse(GetFileTag("NATIVE-FRAME-RATE"), out m_NativeFrameRate);

				int.TryParse(GetFileTag("OSD-FIRST-LINE"), out m_OsdFirstLine);
				int.TryParse(GetFileTag("OSD-LAST-LINE"), out m_OsdLastLine);

			    if (m_OsdLastLine > m_Height) m_OsdLastLine = m_Height;
                if (m_OsdFirstLine < 0) m_OsdFirstLine = 0;

			    m_IntegratedAAVFrames = -1;

                if (double.TryParse(GetFileTag("EFFECTIVE-FRAME-RATE"), out m_EffectiveFrameRate) && m_NativeFrameRate != 0)
                {
					m_IntegratedAAVFrames = (int)Math.Round(m_NativeFrameRate / m_EffectiveFrameRate);
                }

			    int.TryParse(GetFileTag("FRAME-STACKING-RATE"), out m_StackingRate);
			    if (m_StackingRate == 1) m_StackingRate = 0; // Video stacked at x1 is a non-stacked video

			    m_IsStatusChannelOnly = GetFileTag("STATUS-CHANNEL-ONLY") == "1";
			}
			else
			{
				m_OsdFirstLine = 0;
				m_OsdLastLine = 0;
			}

		    m_OcrDataAvailable = null;
		}

		public string FileName
		{
			get { return m_FileName; }
		}

		public int Width
		{
			get { return m_Width; }
		}

		public int Height
		{
			get { return m_Height; }
		}

		public int BitPix
		{
			get { return m_BitPix; }
		}

		public uint GetAav16NormVal()
		{
			return m_Aav16NormVal;
		}

		public int FirstFrame
		{
			get { return 0; }
		}

		public int LastFrame
		{
			get { return m_CountFrames - 1; }
		}

		public int CountFrames
		{
			get { return m_CountFrames; }
		}

		public double FrameRate
		{
			get { return m_FrameRate; }
		}

		public double NativeFrameRate
		{
			get { return m_NativeFrameRate; }
		}

        public int IntegratedAAVFrames
		{
            get { return m_IntegratedAAVFrames; }
		}

        public int AAVStackingRate
        {
            get { return m_StackingRate; }
        }

		public string VideoStandard
		{
			get { return m_VideoStandard; }
		}
		
		public double MillisecondsPerFrame
		{
			get { return (1000 / m_FrameRate); }
		}

		public AdvFrameInfo GetStatusChannel(int index)
		{
			var frameInfo = new AdvFrameInfoNative();

			byte[] gpsFix = new byte[256 * 16];
			byte[] userCommand = new byte[256 * 16];
			byte[] systemError = new byte[256 * 16];

            lock (m_SyncLock)
            {
                TangraCore.ADVGetFrameStatusChannel(index, frameInfo, gpsFix, userCommand, systemError);    
            }

			var rv = new AdvFrameInfo(frameInfo)
			{
				UserCommandString = AdvFrameInfo.GetStringFromBytes(userCommand),
				SystemErrorString = AdvFrameInfo.GetStringFromBytes(systemError),
				GPSFixString = AdvFrameInfo.GetStringFromBytes(gpsFix)
			};

			return rv;
		}

		public Pixelmap GetPixelmap(int index)
		{
			if (index >= m_FirstFrame + m_CountFrames)
				throw new ApplicationException("Invalid frame position: " + index);

			uint[] pixels = new uint[m_Width * m_Height];
            uint[] unprocessedPixels = new uint[m_Width * m_Height];
			byte[] displayBitmapBytes = new byte[m_Width * m_Height];
            byte[] rawBitmapBytes = new byte[Pixelmap.GetBitmapBIRGBPixelArraySize(24, m_Width, m_Height) + 40 + 14 + 1];
			var frameInfo = new AdvFrameInfoNative();

			byte[] gpsFix = new byte[256 * 16];
			byte[] userCommand = new byte[256 * 16];
			byte[] systemError = new byte[256 * 16];

		    lock (m_SyncLock)
		    {
		        TangraCore.ADVGetFrame(index, pixels, unprocessedPixels, rawBitmapBytes, displayBitmapBytes, frameInfo, gpsFix, userCommand, systemError);
		    }

		    m_CurrentFrameInfo = new AdvFrameInfo(frameInfo);
			m_CurrentFrameInfo.UserCommandString = AdvFrameInfo.GetStringFromBytes(userCommand);
			m_CurrentFrameInfo.SystemErrorString = AdvFrameInfo.GetStringFromBytes(systemError);
			m_CurrentFrameInfo.GPSFixString = AdvFrameInfo.GetStringFromBytes(gpsFix);

			if (m_Engine == "AAV" && m_CurrentFrameInfo.IntegratedFrames > 0 && TangraConfig.Settings.AAV.SplitFieldsOSD && m_OsdFirstLine * m_OsdLastLine != 0)
			{
				TangraCore.BitmapSplitFieldsOSD(rawBitmapBytes, m_OsdFirstLine, m_OsdLastLine);
			}

            if (frameInfo.HasNtpTimeStamp && m_CurrentFrameInfo.Exposure10thMs == 0 &&
                index + 1 < m_FirstFrame + m_CountFrames)
            {
                lock (m_SyncLock)
                {
                    TangraCore.ADVGetFrameStatusChannel(index + 1, frameInfo, gpsFix, userCommand, systemError);
                }
                if (frameInfo.HasNtpTimeStamp)
                    m_CurrentFrameInfo.Exposure10thMs = (int)Math.Round(new TimeSpan(frameInfo.EndExposureNtpTimeStamp.Ticks - m_CurrentFrameInfo.EndExposureNtpTimeStamp.Ticks).TotalMilliseconds * 10); 
            }

			using (MemoryStream memStr = new MemoryStream(rawBitmapBytes))
			{
			    Bitmap displayBitmap;

				if (m_Engine == "AAV" && m_CurrentFrameInfo.IntegratedFrames == 0)
				{
					// This is a VTI Split reference frame. Put some mark on it to mark it as such??
					displayBitmap = Pixelmap.ConstructBitmapFromBitmapPixels(pixels, m_Width, m_Height);
					for (int i = 0; i < pixels.Length; i++) displayBitmapBytes[i] = (byte) pixels[i];
				}
				else
				{
					try
					{
						displayBitmap = (Bitmap)Bitmap.FromStream(memStr);
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex.GetFullStackTrace());
						displayBitmap = new Bitmap(m_Width, m_Height);
					}					
				}

                var rv = new Pixelmap(m_Width, m_Height, m_BitPix, pixels, displayBitmap, displayBitmapBytes);
			    rv.SetMaxSignalValue(m_Aav16NormVal);
 				rv.FrameState = GetCurrentFrameState(index);
			    rv.UnprocessedPixels = unprocessedPixels;
				return rv;
			}
		}

		public string GetFileTag(string tagName)
		{
			byte[] tagValue = new byte[256 * 2];

			TangraCore.ADVGetFileTag(tagName, tagValue);

			return AdvFrameInfo.GetStringFromBytes(tagValue);
		}

		public FrameStateData GetFrameStatusChannel(int index)
		{
			if (index >= m_FirstFrame + m_CountFrames)
				throw new ApplicationException("Invalid frame position: " + index);
			
			var frameInfo = new AdvFrameInfoNative();

			byte[] gpsFix = new byte[256 * 16];
			byte[] userCommand = new byte[256 * 16];
			byte[] systemError = new byte[256 * 16];

		    lock (m_SyncLock)
		    {
		        TangraCore.ADVGetFrameStatusChannel(index, frameInfo, gpsFix, userCommand, systemError);
		    }

		    var frameStatusChannel = new AdvFrameInfo(frameInfo);
			frameStatusChannel.UserCommandString = AdvFrameInfo.GetStringFromBytes(userCommand);
			frameStatusChannel.SystemErrorString = AdvFrameInfo.GetStringFromBytes(systemError);
			frameStatusChannel.GPSFixString = AdvFrameInfo.GetStringFromBytes(gpsFix);

			return AdvFrameInfoToFrameStateData(frameStatusChannel, index);
		}

		public int RecommendedBufferSize
		{
            get { return Math.Min(8, CountFrames); }
		}

        public bool SupportsSoftwareIntegration
        {
            get { return false; }
        }

        public bool SupportsIntegrationByMedian
        {
            get { return false; }
        }

        public string GetFrameFileName(int index)
        {
            throw new NotSupportedException();
        }

        public bool SupportsFrameFileNames
        {
            get { return false; }
        }

		public string VideoFileType
		{
            get { return string.Format("{0}.{1}", m_Engine, BitPix); }
		}

		public Rectangle VideoFrameSize
		{
			get { return new Rectangle(0, 0, Width, Height); }
		}

		public string CameraModel
		{
			get { return m_CameraModel; }
		}

		private void AddExtraNtpDebugTimes(ref FrameStateData stateData, AdvFrameInfo frameInfo)
		{
			if (stateData.AdditionalProperties == null)
				stateData.AdditionalProperties = new SafeDictionary<string, object>();

			stateData.AdditionalProperties.Add("MidTimeNTPRaw", stateData.EndFrameNtpTime.AddMilliseconds(-0.5 * stateData.ExposureInMilliseconds));
			stateData.AdditionalProperties.Add("MidTimeNTPFitted", stateData.CentralExposureTime);
			stateData.AdditionalProperties.Add("MidTimeWindowsRaw", frameInfo.EndExposureSecondaryTimeStamp.AddMilliseconds(-0.5 * stateData.ExposureInMilliseconds));
		}

		private FrameStateData GetCurrentFrameState(int frameNo)
		{
			if (m_CurrentFrameInfo != null)
			{
				var rv = new FrameStateData();
				rv.VideoCameraFrameId = m_CurrentFrameInfo.VideoCameraFrameId;
				rv.CentralExposureTime = m_CurrentFrameInfo.MiddleExposureTimeStamp;
				rv.SystemTime = m_CurrentFrameInfo.SystemTime;
				rv.EndFrameNtpTime = m_CurrentFrameInfo.EndExposureNtpTimeStamp;
				rv.NtpTimeStampError = m_CurrentFrameInfo.NtpTimeStampError;
				rv.ExposureInMilliseconds = m_CurrentFrameInfo.Exposure10thMs / 10.0f;

				rv.NumberIntegratedFrames = (int)m_CurrentFrameInfo.IntegratedFrames;
			    rv.NumberStackedFrames = m_StackingRate;

				int almanacStatus = m_CurrentFrameInfo.GPSAlmanacStatus;
				int almanacOffset = m_CurrentFrameInfo.GetSignedAlamancOffset();

				if (!m_CurrentFrameInfo.AlmanacStatusIsGood && m_AlamanacOffsetLastFrameIsGood)
				{
					// When the current almanac is not good, but last frame is, then apply the known almanac offset automatically
					almanacOffset = m_AlmanacOffsetLastFrame;
					rv.CentralExposureTime = rv.CentralExposureTime.AddSeconds(m_AlmanacOffsetLastFrame);
					almanacStatus = 2; // Certain
				}
				

				rv.Gain = m_CurrentFrameInfo.Gain;
				rv.Gamma = m_CurrentFrameInfo.Gamma;
				rv.Temperature = m_CurrentFrameInfo.Temperature;
				rv.Offset = m_CurrentFrameInfo.Offset;

				rv.NumberSatellites = m_CurrentFrameInfo.GPSTrackedSattelites;

				rv.AlmanacStatus = AdvStatusValuesHelper.TranslateGpsAlmanacStatus(almanacStatus);

				rv.AlmanacOffset = AdvStatusValuesHelper.TranslateGpsAlmanacOffset(almanacStatus, almanacOffset, almanacStatus > 0);

				rv.GPSFixStatus = m_CurrentFrameInfo.GPSFixStatus.ToString("#");

				rv.Messages = string.Empty;
				if (m_CurrentFrameInfo.SystemErrorString != null)
					rv.Messages = string.Concat(rv.Messages, m_CurrentFrameInfo.SystemErrorString, "\r\n");
				if (m_CurrentFrameInfo.UserCommandString != null)
					rv.Messages = string.Concat(rv.Messages, m_CurrentFrameInfo.UserCommandString, "\r\n");
				if (m_CurrentFrameInfo.GPSFixString != null)
					rv.Messages = string.Concat(rv.Messages, m_CurrentFrameInfo.GPSFixString, "\r\n");

				if (m_UseNtpTimeAsCentralExposureTime)
				{
					rv.CentralExposureTime = ComputeCentralExposureTimeFromNtpTime(frameNo, m_CurrentFrameInfo.EndExposureNtpTimeStamp);
				}

				if (m_FrameRate > 0)
					rv.ExposureInMilliseconds = (float)(1000 / m_FrameRate);

				if (m_UsesNtpTimestamps && !OcrDataAvailable && m_UseNtpTimeAsCentralExposureTime)
					AddExtraNtpDebugTimes(ref rv, m_CurrentFrameInfo);

				return rv;
			}
			else
				return new FrameStateData();
		}

		private FrameStateData AdvFrameInfoToFrameStateData(AdvFrameInfo frameInfo, int frameIndex)
		{
			if (frameInfo != null)
			{
				var rv = new FrameStateData();
				rv.VideoCameraFrameId = frameInfo.VideoCameraFrameId;
				rv.CentralExposureTime = frameInfo.MiddleExposureTimeStamp;
				rv.SystemTime = frameInfo.SystemTime;
				rv.EndFrameNtpTime = frameInfo.EndExposureNtpTimeStamp;
				rv.NtpTimeStampError = frameInfo.NtpTimeStampError;
				rv.ExposureInMilliseconds = frameInfo.Exposure10thMs / 10.0f;

				rv.NumberIntegratedFrames = (int)frameInfo.IntegratedFrames;

				int almanacStatus = frameInfo.GPSAlmanacStatus;
				int almanacOffset = frameInfo.GetSignedAlamancOffset();

				if (!frameInfo.AlmanacStatusIsGood && frameInfo.AlmanacStatusIsGood)
				{
					// When the current almanac is not good, but last frame is, then apply the known almanac offset automatically
					almanacOffset = frameInfo.GPSAlmanacOffset;
					rv.CentralExposureTime = rv.CentralExposureTime.AddSeconds(frameInfo.GPSAlmanacOffset);
					almanacStatus = 2; // Certain
				}


				rv.Gain = frameInfo.Gain;
				rv.Gamma = frameInfo.Gamma;
				rv.Temperature = frameInfo.Temperature;
				rv.Offset = frameInfo.Offset;

				rv.NumberSatellites = frameInfo.GPSTrackedSattelites;

				rv.AlmanacStatus = AdvStatusValuesHelper.TranslateGpsAlmanacStatus(almanacStatus);

				rv.AlmanacOffset = AdvStatusValuesHelper.TranslateGpsAlmanacOffset(almanacStatus, almanacOffset, almanacStatus > 0);

				rv.GPSFixStatus = frameInfo.GPSFixStatus.ToString("#");

				rv.Messages = string.Empty;
				if (frameInfo.SystemErrorString != null)
					rv.Messages = string.Concat(rv.Messages, frameInfo.SystemErrorString, "\r\n");
				if (frameInfo.UserCommandString != null)
					rv.Messages = string.Concat(rv.Messages, frameInfo.UserCommandString, "\r\n");
				if (frameInfo.GPSFixString != null)
					rv.Messages = string.Concat(rv.Messages, frameInfo.GPSFixString, "\r\n");

				if (m_UseNtpTimeAsCentralExposureTime)
				{
					rv.CentralExposureTime = ComputeCentralExposureTimeFromNtpTime(frameIndex, frameInfo.EndExposureNtpTimeStamp);
				}

				if (m_FrameRate > 0)
					rv.ExposureInMilliseconds = (float)(1000 / m_FrameRate);

				if (m_UsesNtpTimestamps && !OcrDataAvailable && m_UseNtpTimeAsCentralExposureTime)
					AddExtraNtpDebugTimes(ref rv, frameInfo);

				return rv;
			}
			else
				return new FrameStateData();
		}

		public Pixelmap GetIntegratedFrame(int startFrameNo, int framesToIntegrate, bool isSlidingIntegration, bool isMedianAveraging)
		{
			if (startFrameNo < 0 || startFrameNo >= m_FirstFrame + m_CountFrames)
				throw new ApplicationException("Invalid frame position: " + startFrameNo);

			int actualFramesToIntegrate = Math.Min(startFrameNo + framesToIntegrate, m_FirstFrame + m_CountFrames - 1) - startFrameNo;

			uint[] pixels = new uint[m_Width * m_Height];
            uint[] unprocessedPixels = new uint[m_Width * m_Height];
			byte[] displayBitmapBytes = new byte[m_Width * m_Height];
            byte[] rawBitmapBytes = new byte[Pixelmap.GetBitmapBIRGBPixelArraySize(24, Width, Height) + 40 + 14 + 1];
			var frameInfo = new AdvFrameInfoNative();

		    lock (m_SyncLock)
		    {
		        TangraCore.ADVGetIntegratedFrame(startFrameNo, actualFramesToIntegrate, isSlidingIntegration, isMedianAveraging, pixels, unprocessedPixels, rawBitmapBytes, displayBitmapBytes, frameInfo);
		    }

		    m_CurrentFrameInfo = new AdvFrameInfo(frameInfo);

			using (MemoryStream memStr = new MemoryStream(rawBitmapBytes))
			{
				Bitmap displayBitmap = (Bitmap)Bitmap.FromStream(memStr);

                var rv =  new Pixelmap(m_Width, m_Height, m_BitPix, pixels, displayBitmap, displayBitmapBytes);
                rv.SetMaxSignalValue(m_Aav16NormVal);
                rv.UnprocessedPixels = unprocessedPixels;
			    return rv;
			}
		}

	    private string engine = null;

		public string Engine
		{
            get { return engine; }
		}

        public static long MISSING_TIMESTAMP_TICKS = 633979008000000000;

        public string OcrEngine = null;
	    public bool? m_OcrDataAvailable = null;

        public bool OcrDataAvailable
        {
			get
			{
			    if (string.IsNullOrEmpty(OcrEngine))
			        return false;

				if (TangraConfig.Settings.AAV.NtpTimeDebugFlag)
					return false;

                if (!m_OcrDataAvailable.HasValue)
                {
                    m_OcrDataAvailable = false;

                    // Checking the first up to 1000 frames for embedded timestamp. If none found then assume no embedded timestamps are available
                    for (int i = m_FirstFrame; i < m_FirstFrame + Math.Min(1000, m_CountFrames); i++)
                    {
                        FrameStateData stateChannel = GetFrameStatusChannel(i);
                        if (stateChannel.CentralExposureTime.Ticks != MISSING_TIMESTAMP_TICKS)
                        {
                            m_OcrDataAvailable = true;
                            break;
                        }
                    }
                   
                }

			    return m_OcrDataAvailable.Value;
			}
        }

		public bool? m_NtpDataAvailable = null;

		public bool NtpDataAvailable
		{
			get
			{
				if (!m_NtpDataAvailable.HasValue)
				{
					
					m_NtpDataAvailable = false;

					if (m_Engine == "AAV" && m_UsesNtpTimestamps)
					{
						for (int i = m_FirstFrame; i < m_FirstFrame + m_CountFrames; i++)
						{
							FrameStateData stateChannel = GetFrameStatusChannel(i);
							if (stateChannel.HasValidNtpTimeStamp)
							{
								m_NtpDataAvailable = true;
								break;
							}
						}
					}

				}

				return m_NtpDataAvailable.Value;
			}
		}

	    public bool IsStatusChannelOnly
	    {
	        get { return m_IsStatusChannelOnly; }
	    }

		private bool m_UseNtpTimeAsCentralExposureTime = false;
		private long m_CalibratedNtpTimeZeroPoint;
		private double m_NtpTimeFitSigma;
		private double m_NtpTimeAverageNetworkError;
		private LinearRegression m_CalibratedNtpTimeSource;

		public int AstroAnalogueVideoNormaliseNtpDataIfNeeded(Action<int> progressCallback, out float oneSigmaError)
		{
			int ntpError = -1;
			oneSigmaError = float.NaN;

			if (NtpDataAvailable && !OcrDataAvailable && !m_UseNtpTimeAsCentralExposureTime)
			{
				if (m_CountFrames > 1 /* No Timestamp for first frame */ + 1 /* No Timestamp for last frame*/ + 3 /* Minimum timestamped frames for a FIT */)
				{
					try
					{
						double frameDurationMilliseconds = 1000 / m_FrameRate;
						var lr = new LinearRegression();

						long zeroPointTicks = -1;

						int percentDone = 0;
						int percentDoneCalled = 0;						
						if (progressCallback != null) progressCallback(percentDone);

						long ntpTimestampErrorSum = 0;
						int ntpTimestampErrorDatapoints = 0;

						for (int i = m_FirstFrame; i < m_FirstFrame + m_CountFrames; i++)
						{
							FrameStateData stateChannel = GetFrameStatusChannel(i);
							if (stateChannel.HasValidNtpTimeStamp)
							{
								long centralTicks = stateChannel.EndFrameNtpTime.AddMilliseconds(-0.5 * frameDurationMilliseconds).Ticks;
								if (zeroPointTicks == -1)
									zeroPointTicks = centralTicks;
								lr.AddDataPoint(i, new TimeSpan(centralTicks - zeroPointTicks).TotalMilliseconds);
								ntpTimestampErrorSum += stateChannel.NtpTimeStampError;
								ntpTimestampErrorDatapoints++;
							}

							percentDone = 100 * (i - m_FirstFrame) / m_CountFrames;
							if (progressCallback != null && percentDone - percentDoneCalled> 5)
							{
								progressCallback(percentDone);
								percentDoneCalled = percentDone;
							}
						}

						if (lr.NumberOfDataPoints > 3)
						{
							lr.Solve();

							m_CalibratedNtpTimeZeroPoint = zeroPointTicks;
							m_CalibratedNtpTimeSource = lr;
							m_UseNtpTimeAsCentralExposureTime = true;
							m_NtpTimeFitSigma = lr.StdDev;
							m_NtpTimeAverageNetworkError = (ntpTimestampErrorSum * 1.0 / ntpTimestampErrorDatapoints);
							ntpError = (int)Math.Round(m_NtpTimeFitSigma + m_NtpTimeAverageNetworkError);

							Trace.WriteLine(string.Format("NTP Timebase Established. 1-Sigma = {0} ms", lr.StdDev.ToString("0.00")));

							oneSigmaError = (float)m_NtpTimeFitSigma;
						}

						progressCallback(100);
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex.GetFullStackTrace());
					}
				}
			}

			return ntpError;
		}

		private DateTime ComputeCentralExposureTimeFromNtpTime(int frameNo, DateTime endFrameNtpTimestamp)
		{
			if (TangraConfig.Settings.AAV.NtpTimeUseDirectTimestamps)
			{
				if (endFrameNtpTimestamp.Ticks != 633979008000000000)
				{
					double frameDurationMilliseconds = 1000 / m_FrameRate;
					long centralTicks = endFrameNtpTimestamp.AddMilliseconds(-0.5 * frameDurationMilliseconds).Ticks;

					return new DateTime(centralTicks);
				}

				return DateTime.MinValue;
			}
			else
			{
				return new DateTime(m_CalibratedNtpTimeZeroPoint).AddMilliseconds(m_CalibratedNtpTimeSource.ComputeY(frameNo));
			}
		}

		private GeoLocationInfo geoLocation;

		public GeoLocationInfo GeoLocation
		{
			get { return geoLocation; }
		}

        private void CheckAdvFileFormat(string fileName, ref AdvFileMetadataInfo fileMetadataInfo, ref GeoLocationInfo geoLocation)
		{
			AdvFile advFile = AdvFile.OpenFile(fileName);

			CheckAdvFileFormatInternal(advFile);

            fileMetadataInfo.Recorder = advFile.AdvFileTags["RECORDER"];
            fileMetadataInfo.Camera = advFile.AdvFileTags["CAMERA-MODEL"];
            fileMetadataInfo.Engine = advFile.AdvFileTags["FSTF-TYPE"];

            engine = fileMetadataInfo.Engine;

            if (engine == "ADV")
            {
				advFile.AdvFileTags.TryGetValue("ADVR-SOFTWARE-VERSION", out fileMetadataInfo.AdvrVersion);
				if (string.IsNullOrWhiteSpace(fileMetadataInfo.AdvrVersion))
					advFile.AdvFileTags.TryGetValue("RECORDER-SOFTWARE-VERSION", out fileMetadataInfo.AdvrVersion);

				advFile.AdvFileTags.TryGetValue("HTCC-FIRMWARE-VERSION", out fileMetadataInfo.HtccFirmareVersion);
                fileMetadataInfo.SensorInfo = advFile.AdvFileTags["CAMERA-SENSOR-INFO"];

                advFile.AdvFileTags.TryGetValue("OBJNAME", out fileMetadataInfo.ObjectName);

                advFile.AdvFileTags.TryGetValue("LONGITUDE-WGS84", out geoLocation.Longitude);
                advFile.AdvFileTags.TryGetValue("LATITUDE-WGS84", out geoLocation.Latitude);
                advFile.AdvFileTags.TryGetValue("ALTITUDE-MSL", out geoLocation.Altitude);
                advFile.AdvFileTags.TryGetValue("MSL-WGS84-OFFSET", out geoLocation.MslWgs84Offset);

				if (!string.IsNullOrEmpty(geoLocation.MslWgs84Offset) &&
				    !geoLocation.MslWgs84Offset.StartsWith("-"))
				{
					geoLocation.MslWgs84Offset = "+" + geoLocation.MslWgs84Offset;
				}

                advFile.AdvFileTags.TryGetValue("GPS-HDOP", out geoLocation.GpsHdop);
            }
            else if (engine == "AAV")
            {
                advFile.AdvFileTags.TryGetValue("OCR-ENGINE", out OcrEngine);
	            string sCorr;
	            int iCorr;
	            if (advFile.AdvFileTags.TryGetValue("CAPHNTP-TIMING-CORRECTION", out sCorr) &&
	                int.TryParse(sCorr, out iCorr))
	            {
					fileMetadataInfo.HasNTPTimeStamps = true;
	            }
                advFile.AdvFileTags.TryGetValue("OBJECT", out fileMetadataInfo.ObjectName);
            }

            this.geoLocation = new GeoLocationInfo(geoLocation);
		}

		private void CheckAdvFileFormatInternal(AdvFile advFile)
		{
			bool fileIsCorrupted = true;
			bool isADVFormat = false;
			int advFormatVersion = -1;
			DoConsistencyCheck(advFile, ref fileIsCorrupted, ref isADVFormat, ref advFormatVersion);

			if (!isADVFormat)
				throw new ADVFormatException("The file is not in ADV/AAV format.");

			if (advFormatVersion > 1)
                throw new ADVFormatException("The file ADV/AAV version is not supported yet.");

			if (fileIsCorrupted)
                throw new ADVFormatException("The ADV/AAV file may be corrupted.\r\n\r\nTry to recover it from Tools -> ADV/AAV Tools -> Repair ADV/AAV File");
		}

		private void DoConsistencyCheck(AdvFile advFile, ref bool fileIsCorrupted, ref bool isADVFormat, ref int advFormatVersion)
		{
			fileIsCorrupted = advFile.IsCorrupted;

			string fstsTypeStr;
			if (!advFile.AdvFileTags.TryGetValue("FSTF-TYPE", out fstsTypeStr))
				isADVFormat = false;
			else
                isADVFormat = fstsTypeStr == "ADV" || fstsTypeStr == "AAV";

            if (fstsTypeStr == "ADV")
            {
                string advVersionStr;
                if (!advFile.AdvFileTags.TryGetValue("ADV-VERSION", out advVersionStr))
                    advFormatVersion = -1;
                else
                    if (!int.TryParse(advVersionStr, out advFormatVersion))
                        advFormatVersion = -1;                
            }
            else if (fstsTypeStr == "AAV")
            {
                string advVersionStr;
                if (!advFile.AdvFileTags.TryGetValue("AAV-VERSION", out advVersionStr))
                    advFormatVersion = -1;
                else
                    if (!int.TryParse(advVersionStr, out advFormatVersion))
                        advFormatVersion = -1;                
            }
		}
	}
}
