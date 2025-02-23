﻿#region
// The MIT License (MIT)
//
// Copyright (c) 2014 Hristo Pavlov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AdvLibTestApp
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			AdvRecorder recorder = new AdvRecorder();

			// First set the values of the standard file metadata
			recorder.FileMetaData.RecorderName = "Genika";
			recorder.FileMetaData.RecorderVersion = "x.y.z";
			recorder.FileMetaData.RecorderTimerFirmwareVersion = "a.b.c";

			recorder.FileMetaData.CameraModel = "Flea3 FL3-FW-03S3M";
			recorder.FileMetaData.CameraSerialNumber = "10210906";
			recorder.FileMetaData.CameraVendorNumber = "Point Grey Research";
			recorder.FileMetaData.CameraSensorInfo = "Sony ICX414AL (1/2\" 648x488 CCD)";
			recorder.FileMetaData.CameraSensorResolution = "648x488";
			recorder.FileMetaData.CameraFirmwareVersion = "1.22.3.0";
			recorder.FileMetaData.CameraFirmwareBuildTime = "Mon Dec 28 20:15:45 2009";
			recorder.FileMetaData.CameraDriverVersion = "2.2.1.6";

			// Then define additional metadata, if required
			recorder.FileMetaData.AddUserTag("TELESCOPE-NAME", "Large Telescope");
			recorder.FileMetaData.AddUserTag("TELESCOPE-FL", "8300");
			recorder.FileMetaData.AddUserTag("TELESCOPE-FD", "6.5");
			recorder.FileMetaData.AddUserTag("CAMERA-DIGITAL-SAMPLIG", "xxx");
			recorder.FileMetaData.AddUserTag("CAMERA-HDR-RESPONSE", "yyy");
			recorder.FileMetaData.AddUserTag("CAMERA-OPTICAL-RESOLUTION", "zzz");

			if (cbxLocationData.Checked)
			{
				recorder.LocationData.LongitudeWgs84 = "150*38'27.7\"";
				recorder.LocationData.LatitudeWgs84 = "-33*39'49.3\"";
				recorder.LocationData.AltitudeMsl = "284.4M";
				recorder.LocationData.MslWgs84Offset = "22.4M";
				recorder.LocationData.GpsHdop = "0.7";				
			}

			// Define the image size and bit depth
			byte dynaBits = 16;
			if (rbPixel16.Checked) dynaBits = 16;
			else if (rbPixel12.Checked) dynaBits = 12;
			else if (rbPixel8.Checked) dynaBits = 8;

			byte cameraDepth = 16;
			if (rbCamera16.Checked) cameraDepth = 16;
			else if (rbCamera12.Checked) cameraDepth = 12;
			else if (rbCamera8.Checked) cameraDepth = 8;

			recorder.ImageConfig.SetImageParameters(640, 480, cameraDepth, dynaBits);

			// By default no status section values will be recorded. The user must enable the ones they need recorded and 
			// can also define additional status parameters to be recorded with each video frame
			recorder.StatusSectionConfig.RecordGain = true;
			recorder.StatusSectionConfig.RecordGamma = true;
			int customTagIdCustomGain = recorder.StatusSectionConfig.AddDefineTag("EXAMPLE-GAIN", AdvTagType.UInt32);
			int customTagIdMessages = recorder.StatusSectionConfig.AddDefineTag("EXAMPLE-MESSAGES", AdvTagType.List16OfAnsiString255);
			
		    string fileName = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"\Filename.adv");
            recorder.StartRecordingNewFile(fileName);

			AdvStatusEntry status = new AdvStatusEntry();
			status.AdditionalStatusTags = new object[2];

			int imagesCount = GetTotalImages();
			bool useCompression = cbxCompress.Checked;

			for (int i = 0; i < imagesCount; i++)
			{
				// NOTE: Moking up some test data
				uint exposure = GetCurrentImageExposure(i);
				DateTime timestamp = GetCurrentImageTimeStamp(i);
				status.Gain = GetCurrentImageGain(i);
				status.Gamma = GetCurrentImageGamma(i);
				status.AdditionalStatusTags[customTagIdMessages] = GetCurrentExampleMassages(i);
				status.AdditionalStatusTags[customTagIdCustomGain] = GetCurrentExampleCustomGain(i);

				if (rb16BitUShort.Checked)
				{
					ushort[] imagePixels = GetCurrentImageBytesIn16(i, dynaBits);

					recorder.AddVideoFrame(
						imagePixels,

						// NOTE: Use with caution! Using compression is slower and may not work at high frame rates 
						// i.e. it may take longer to compress the data than for the next image to arrive on the buffer
						useCompression,

						AdvTimeStamp.FromDateTime(timestamp),
						exposure,
						status);
				}
				else if (rb16BitByte.Checked)
				{
					byte[] imageBytes = GetCurrentImageBytes(i, dynaBits);

					recorder.AddVideoFrame(
						imageBytes,

						// NOTE: Use with caution! Using compression is slower and may not work at high frame rates 
						// i.e. it may take longer to compress the data than for the next image to arrive on the buffer
						useCompression,
						AdvImageData.PixelDepth16Bit,
						AdvTimeStamp.FromDateTime(timestamp),
						exposure,
						status);
				}
				else if (rb8BitByte.Checked)
				{
					byte[] imageBytes = GetCurrentImageBytes(i, dynaBits);

					recorder.AddVideoFrame(
						imageBytes,

						// NOTE: Use with caution! Using compression is slower and may not work at high frame rates 
						// i.e. it may take longer to compress the data than for the next image to arrive on the buffer
						useCompression,
						AdvImageData.PixelDepth8Bit,
						AdvTimeStamp.FromDateTime(timestamp),
						exposure,
						status);
				}
			}

			recorder.StopRecording();

            MessageBox.Show(string.Format("'{0}' has been created.", fileName));
		}

		private int GetTotalImages()
		{
			// TODO: In this file conversion example, return the number of images to be recorded
			return 1;
		}

		private uint GetCurrentImageExposure(int frameId)
		{
			// TODO: Get the image exposure in 1/10-th of milliseconds
			return 400;
		}

		private DateTime GetCurrentImageTimeStamp(int frameId)
		{
			// TODO: Get the image timestamp. Alternatevly return windows Ticks or year/month/day/hour/min/sec/milliseconds
			return DateTime.Now;
		}

		private float GetCurrentImageGamma(int frameId)
		{
			// TODO: Get the image gamma
			return 1.0f;
		}

		private float GetCurrentImageGain(int frameId)
		{
			// TODO: Get the image gain in dB
			return 36.0f;
		}

		private string[] GetCurrentExampleMassages(int frameId)
		{
			// TODO: Get the image custom defined "EXAMPLE-MESSAGES" value.
			return new string[] { "Message 1", "Message 2", "Message 3" }; ;
		}

		private uint GetCurrentExampleCustomGain(int frameId)
		{
			// TODO: e.g. return an integer gain value reported by the camera which cannot be converted to dB
			return 0x293;
		}

        private ushort[] GetCurrentImageBytesIn16(int frameId, byte dynaBits)
		{
			ushort[] pixels = new ushort[640 * 480];

			// Background values are all half way 0x0FFF / 2 = 0x07FF
			for (int i = 0; i < pixels.Length; i++)
			{
                if (dynaBits == 16)
				    pixels[i] = 0x7FF0;
                else if (dynaBits == 12)
                    pixels[i] = 0x07FF;
			}

			// There is a pixel wide line from top left - down and right with full intensity (0x0FFF)
			for (int x = 0; x < 480; x++)
			{
                if (dynaBits == 16)
				    pixels[(x * 640 + x)] = 0xFFF0;
                else if (dynaBits == 12)
                    pixels[(x * 640 + x)] = 0x0FFF;
			}

			// There is a pixel wide line from top right - down and left with zero intensity (0x0000)
			for (int x = 0; x < 480; x++)
			{
				pixels[(x + 1) * 640 - x - 1] = 0x0000;
			}

			return pixels;
		}

        private byte[] GetCurrentImageBytes(int frameId, byte dynaBits)
		{
			// NOTE: In this TEST example we mock up 12 bit pixels (640, 480), where 
			
			byte[] pixels;

	        if (dynaBits == 12 || dynaBits == 16)
			{
				pixels = new byte[640 * 480 * 2];

				// Background values are all half way 0x0FFF / 2 = 0x07FF. This "scaled" to 16 bit is 0x7FF0
				for (int i = 0; i < pixels.Length / 2; i++)
				{
					if (dynaBits == 16)
					{
						pixels[2 * i] = 0xF0;
						pixels[2 * i + 1] = 0x7F;
					}
					else if (dynaBits == 12)
					{
						pixels[2 * i] = 0xFF;
						pixels[2 * i + 1] = 0x07;
					}
				}

				// There is a pixel wide line from top left - down and right with full intensity (0x0FFF). This "scaled" to 16 bit is 0xFFF0
				for (int x = 0; x < 480; x++)
				{
					if (dynaBits == 16)
					{
						pixels[2 * (x * 640 + x)] = 0xF0;
						pixels[2 * (x * 640 + x) + 1] = 0xFF;
					}
					else if (dynaBits == 12)
					{
						pixels[2 * (x * 640 + x)] = 0xFF;
						pixels[2 * (x * 640 + x) + 1] = 0x0F;
					}
				}

				// There is a pixel wide line from top right - down and left with zero intensity (0x0000)
				for (int x = 0; x < 480; x++)
				{
					pixels[2 * ((x + 1) * 640 - x - 1)] = 0x00;
					pixels[2 * ((x + 1) * 640 - x - 1) + 1] = 0x00;
				}				
			}
			else
			{
				pixels = new byte[640 * 480];

				for (int i = 0; i < pixels.Length; i++)
				{
					pixels[i] = 0x7F;
				}

				for (int x = 0; x < 480; x++)
				{
					pixels[x * 640 + x + 1] = 0xFF;
				}

				for (int x = 0; x < 480; x++)
				{
					pixels[(x + 1) * 640 - x - 1] = 0x00;
				}
			}

	        return pixels;
		}

		private void OnImageFormatChanged(object sender, EventArgs e)
		{
			if (rb16BitUShort.Checked || rb16BitByte.Checked)
			{
				rbPixel16.Checked = true;
				rbPixel16.Enabled = true;
				rbPixel12.Enabled = true;
				rbPixel8.Enabled = false;
				rbCamera16.Checked = true;
				rbCamera16.Enabled = true;
				rbCamera12.Enabled = true;
				rbCamera8.Enabled = false;
			}
			else
			{
				rbPixel8.Checked = true;
				rbPixel16.Enabled = false;
				rbPixel12.Enabled = false;
				rbPixel8.Enabled = true;
				rbCamera8.Checked = true;
				rbCamera16.Enabled = false;
				rbCamera12.Enabled = false;
				rbCamera8.Enabled = true;
			}
		}

		private void OnPixelFormatChanged(object sender, EventArgs e)
		{			
			if (rbPixel12.Checked)
			{
				rbCamera16.Enabled = false;
				rbCamera12.Checked = true;
			}
			else
			{
				rbCamera16.Enabled = true;
			}
		}

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr LoadLibraryA(string lpFileName);

		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		static class NativeMethods
		{
			[DllImport("kernel32.dll")]
			public static extern IntPtr LoadLibrary(string dllToLoad);

			[DllImport("kernel32.dll")]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);


			[DllImport("kernel32.dll")]
			public static extern bool FreeLibrary(IntPtr hModule);
		}

		private void btnVerifyLibrary_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
			{


			}
		}
	}
}
