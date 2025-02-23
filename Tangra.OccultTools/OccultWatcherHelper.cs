﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;

namespace Tangra.OccultTools
{
    public class OccultWatcherHelper
    {
        public static string GetConfiguredOccultLocation()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\OccultWatcher");
            if (registryKey != null)
            {
                return Convert.ToString(registryKey.GetValue("OccultSetupDir", null));
            }

            return null;
        }
    }
}
