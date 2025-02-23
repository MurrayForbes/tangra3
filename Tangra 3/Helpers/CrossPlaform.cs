﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace Tangra.Helpers
{
	public static class CrossPlaform
	{
		public static int GetAvailableMemoryInMegabytes()
		{
#if WIN32
			try
			{
				var winQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");

				var searcher = new ManagementObjectSearcher(winQuery);

				foreach (ManagementObject item in searcher.Get())
				{
					return (int)(Math.Round((ulong)item["FreePhysicalMemory"] * 1.0 / 1024));
				}
			}
			catch
			{
				return 1000;
			}
#endif

			return 1000; /* Asumed value for non Windows environments */
		}
	}
}
