﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tangra.Video.AstroDigitalVideo
{
	public partial class frmBuildingNtpTimebase : Form
	{
		public frmBuildingNtpTimebase()
		{
			InitializeComponent();
		}

		public void SetProgress(int progress)
		{
			pbar.Value = Math.Max(Math.Min(100, progress), 0);
			pbar.Update();
		}
	}
}
