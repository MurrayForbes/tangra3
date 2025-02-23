﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tangra.Model.Astro;
using Tangra.Model.Image;

namespace Tangra.VideoOperations.LightCurves
{
	internal class AveragedFrame
	{
		private AstroImage m_Image;

		public AveragedFrame(AstroImage image)
		{
            m_Image = image.Clone();
		}

		public Pixelmap Pixelmap
		{
			get { return m_Image.Pixelmap; }
		}
	}
}
