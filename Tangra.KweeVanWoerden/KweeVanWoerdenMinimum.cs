﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Tangra.SDK;

namespace Tangra.KweeVanWoerden
{
	static class NativeMethods
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);
	}

	[Serializable]
	public class KweeVanWoerdenMinimum : MarshalByRefObject, ITangraAddinAction
	{
		// Fortran Interoperability Links:
		// http://fortranwiki.org/fortran/show/c%23+interoperability
		// http://stackoverflow.com/questions/10317691/making-fortran-dll-and-calling-it-from-c-sharp
		// https://www.uni-muenster.de/imperia/md/content/physik_ct/pdf/05_intel_optimierung.pdf

		// Subroutine Kwee_van_Woerden ( Number_Obs, Time_First, Time, Variable_Star_DN, Variable_Sky_DN, Comparison_Star_DN, Comparison_Sky_DN, Directory_Name )
		// ! Input values
		// integer                    Number_Obs                              ! Number of observed data points
		// double precision           Time_First                              ! Absolute time for the first observation
		// double precision           Time(10000)                             ! Mid-times of each observation
		// double precision           Variable_Star_DN(10000)                 ! Pixels values for the variable star brightness (including sky background)
		// double precision           Variable_Sky_DN(10000)                  ! Pixels values for the variable star sky background to be subtracted
		// double precision           Comparison_Star_DN(10000)               ! Pixels values for the comparison star brightness (including sky background)
		// double precision           Comparison_Sky_DN(10000)                ! Pixels values for the comparison star sky background to be subtracted
		// character*100              Directory_Name                          ! Location for the output files
		public delegate void KweeVanWoerdenDelegate(
			ref int number_obs,
			ref double first_time,
			[In, Out] double[] time,
			[In, Out] double[] var_star,
			[In, Out] double[] var_sky,
			[In, Out] double[] comp_star,
			[In, Out] double[] comp_sky,
			[In, Out] char[] directory_name);

		private string FORTRAN_DLL_DIRECTORY_PATH = AppDomain.CurrentDomain.BaseDirectory + @"Addins";

		private ITangraHost m_TangraHost;
		private KweeVanWoerdenAddinSettings m_Settings;
		private bool m_Running;

		internal KweeVanWoerdenMinimum(ITangraHost tangraHost, KweeVanWoerdenAddinSettings settings)
		{
			m_TangraHost = tangraHost;
			m_Settings = settings;
			m_Running = false;
		}

		public string DisplayName
		{
			get { return "Eclipsing Binary Time of Minima Analysis"; }
		}

		public AddinActionType ActionType
		{
			get { return AddinActionType.LightCurveEventTimeExtractorSupportsBinning; }
		}

		public IntPtr Icon
		{
			get { return Properties.Resource.EclipsingBinaries.ToBitmap().GetHbitmap(); }
		}

		public int IconTransparentColorARGB
		{
			get { return System.Drawing.Color.Transparent.ToArgb(); }
		}

		//----------------------------------------------------------------------------------------
		// UTC DateTime to UTC Julian date
		//----------------------------------------------------------------------------------------
		private double DateUtcToJulian(DateTime dt)
		{
			double tNow = (double)dt.Ticks - 6.30822816E+17;	// .NET ticks at 01-Jan-2000T00:00:00
			double j = 2451544.5 + (tNow / 8.64E+11);		// Tick difference to days difference
			return j;
		}

		public void Execute()
		{
			if (m_Running)
			{
				ShowErrorMessage("Extract Eclipsing Binary Time of Minima is already running.");
				return;
			}

			m_Running = true;
			try
			{
				if (m_Settings.UseSimulatedDataSet)
				{
					MessageBox.Show(
						m_TangraHost.ParentWindow,
					    "A simulated dataset will be used rather than real data.\r\n\r\nTo use actual light curve data from Tangra please reconfigure the add-in from the Settings.",
						"Eclipsing Binaries Addin for Tangra", 
						MessageBoxButtons.OK, 
						MessageBoxIcon.Warning);

					ExecuteWithSimulatedData();
				}
				else
					ExecuteWithRealData();
			}
			finally
			{
				m_Running = false;
			}
		}

		private void ExecuteWithRealData()
		{
			ILightCurveDataProvider dataProvider = m_TangraHost.GetLightCurveDataProvider();

			if (dataProvider != null)
			{
				if (dataProvider.NumberOfMeasuredComparisonObjects == 0)
				{
					ShowErrorMessage("At least one comparison object is required to determine the target eclipsing binary minimum.");
					return;
				}

				ISingleMeasurement[] measurements = dataProvider.GetTargetMeasurements();

				bool hasReliableTimeBase = dataProvider.HasReliableTimeBase;

				double[] frameIds = measurements.Select(x => (double)x.CurrFrameNo).ToArray();
                double[] dataWithBg;
				double[] dataBg;

                var frm = new frmConfigureRun();
			    frm.DataProvider = dataProvider;
                frm.NumStars = dataProvider.NumberOfMeasuredComparisonObjects + 1;
				frm.FromFrameNo = dataProvider.MinFrameNumber;
				frm.ToFrameNo = dataProvider.MaxFrameNumber;
				frm.TargetData = dataProvider.GetTargetMeasurements();
				if (frm.ShowDialog(m_TangraHost.ParentWindow) == DialogResult.Cancel)
					return;

				bool useCurveFitting = frm.RunCurveFitting;
                bool useFineGrainedBins = frm.UseFineGrainedBins;

                if (frm.VariableStarIndex == 0)
                {
                    dataWithBg = measurements.Select(x => x.IsSuccessful ? (double)(x.Measurement + x.Background) : double.NaN).ToArray();
                    dataBg = measurements.Select(x => x.IsSuccessful ? (double)x.Background : double.NaN).ToArray();
                }
                else
                {
                    ISingleMeasurement[] altMeasurements = dataProvider.GetComparisonObjectMeasurements(frm.VariableStarIndex - 1);

                    dataWithBg = altMeasurements.Select(x => x.IsSuccessful ? (double)(x.Measurement + x.Background) : double.NaN).ToArray();
                    dataBg = altMeasurements.Select(x => x.IsSuccessful ? (double)x.Background : double.NaN).ToArray();                    
                }

				List<DateTime> timestamps = measurements.Select(x => x.Timestamp).ToList();

				hasReliableTimeBase = hasReliableTimeBase &&
					timestamps[0].Date != DateTime.MinValue &&
					timestamps[measurements.Length - 1].Date != DateTime.MinValue &&
					timestamps[0].Date.Ticks < timestamps[measurements.Length - 1].Ticks;

				int numAvailableTimestamps = timestamps.Count(x => x != DateTime.MinValue && x != DateTime.MaxValue);
				if (numAvailableTimestamps < 2)
				{
					MessageBox.Show(
						m_TangraHost.ParentWindow,
						"This light curve does not seem to have timestamps saved and cannot be processed.",
						"Eclipsing Binaries Addin for Tangra",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					return;
				}

				if (!hasReliableTimeBase)
				{
                    if (MessageBox.Show(
                        m_TangraHost.ParentWindow,
                        "This light curve may not have a reliable time base. Do you want to continue?",
                        "Eclipsing Binaries Addin for Tangra",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return;
                    }
				}

                // Fill in missing timestamps when times are entered manually
                if (!dataProvider.HasEmbeddedTimeStamps)
                {
                    int idx = 0;
                    for (; idx < timestamps.Count / 2 && timestamps[idx] == DateTime.MinValue; idx++);
                    int firstTimeStampIndex = idx;
                    DateTime firstTimeStamp = timestamps[idx];

                    idx = timestamps.Count - 1;
                    for (; idx > timestamps.Count / 2 && timestamps[idx] == DateTime.MinValue; idx--) ;
                    int lastTimeStampIndex = idx;
                    DateTime lastTimeStamp = timestamps[idx];

                    long ticksPerFrame = (lastTimeStamp.Ticks - firstTimeStamp.Ticks) / (lastTimeStampIndex - firstTimeStampIndex);
                    for (int i = 0; i < timestamps.Count; i++)
                        timestamps[i] = firstTimeStamp.AddTicks(ticksPerFrame * (i - firstTimeStampIndex));
                }

				double[] secondsFromUTMidnight = new double[timestamps.Count];
				long startFrameStartDayTicks = timestamps[0].Date.Ticks;

                for (int i = 0; i < timestamps.Count; i++)
				{
					if (timestamps[i] != DateTime.MinValue)
						secondsFromUTMidnight[i] = Math.Truncate(new TimeSpan(timestamps[i].Ticks - startFrameStartDayTicks).TotalSeconds * 10000) / 10000.0;
					else
						secondsFromUTMidnight[i] = 0;
				}

				// Now go and get the comparison star data
				if (dataProvider.NumberOfMeasuredComparisonObjects > 0)
				{
                    ISingleMeasurement[] compMeasurements = frm.ComparisonStarIndex == 0 ? dataProvider.GetTargetMeasurements() : dataProvider.GetComparisonObjectMeasurements(frm.ComparisonStarIndex - 1);

					if (compMeasurements != null)
					{
					    double[] compDataWithBg;
					    double[] compBg;

                        if (frm.UseNormalisation)
                        {
                            compDataWithBg = compMeasurements.Select(x => x.IsSuccessful ? (double)(x.Measurement + x.Background) : double.NaN).ToArray();
                            compBg = compMeasurements.Select(x => x.IsSuccessful ? (double)x.Background : double.NaN).ToArray();
                        }
                        else
                        {
                            double compDataWithBgAverage = compMeasurements.Average(x => x.IsSuccessful ? x.Measurement + x.Background : double.NaN);
                            double compBgAverage = compMeasurements.Average(x => x.IsSuccessful ? x.Background : double.NaN);
                            compDataWithBg = compMeasurements.Select(x => compDataWithBgAverage).ToArray();
                            compBg = compMeasurements.Select(x => compBgAverage).ToArray();                            
                        }

						double jdAtUtcMidnight = DateUtcToJulian(new DateTime(startFrameStartDayTicks));


					    KweeVanWoerdenResult result;
					    PolynomialFitResult polyResult;

                        var times = new List<double>();
                        var dataWithBgLst = new List<double>();
                        var dataBgLst = new List<double>();
                        var compDataWithBgLst = new List<double>();
                        var compBgLst = new List<double>();

                        if (frm.IncludeDataFrom > 0 || frm.IncludeDataTo < dataWithBg.Length - 1)
                        {
                            double[] timesAlt = new double[frm.IncludeDataTo - frm.IncludeDataFrom];
                            double[] dataWithBgAlt = new double[frm.IncludeDataTo - frm.IncludeDataFrom];
                            double[] dataBgAlt = new double[frm.IncludeDataTo - frm.IncludeDataFrom];
                            double[] compDataWithBgAlt = new double[frm.IncludeDataTo - frm.IncludeDataFrom];
                            double[] compBgAlt = new double[frm.IncludeDataTo - frm.IncludeDataFrom];
                            for (int i = frm.IncludeDataFrom; i < frm.IncludeDataTo; i++)
                            {
                                timesAlt[i - frm.IncludeDataFrom] = secondsFromUTMidnight[i];
                                dataWithBgAlt[i - frm.IncludeDataFrom] = dataWithBg[i];
                                dataBgAlt[i - frm.IncludeDataFrom] = dataBg[i];
                                compDataWithBgAlt[i - frm.IncludeDataFrom] = compDataWithBg[i];
                                compBgAlt[i - frm.IncludeDataFrom] = compBg[i];
                            }

                            times.AddRange(timesAlt);
                            dataWithBgLst.AddRange(dataWithBgAlt);
                            dataBgLst.AddRange(dataBgAlt);
                            compDataWithBgLst.AddRange(compDataWithBgAlt);
                            compBgLst.AddRange(compBgAlt);

                            RemoveInvalidMeasurements(times, dataWithBgLst, dataBgLst, compDataWithBgLst, compBgLst);               
                        }
                        else
                        {
                            times.AddRange(secondsFromUTMidnight);
                            dataWithBgLst.AddRange(dataWithBg);
                            dataBgLst.AddRange(dataBg);
                            compDataWithBgLst.AddRange(compDataWithBg);
                            compBgLst.AddRange(compBg);

                            RemoveInvalidMeasurements(times, dataWithBgLst, dataBgLst, compDataWithBgLst, compBgLst);                          
                        }


                        result = Kwee_van_Woerden(times.Count, jdAtUtcMidnight, times.ToArray(), dataWithBgLst.ToArray(), dataBgLst.ToArray(), compDataWithBgLst.ToArray(), compBgLst.ToArray(), useFineGrainedBins);
						polyResult = useCurveFitting
							? PolynomialFit(times.Count, jdAtUtcMidnight, times.ToArray(), dataWithBgLst.ToArray(), dataBgLst.ToArray(), compDataWithBgLst.ToArray(), compBgLst.ToArray(), (int)result.Start_Light_Curve_Obs_Index, (int)result.Stop_Light_Curve_Obs_Index, result.Time_Of_Minimum)
							: null;

						PresentResults(frm.VariableStarIndex, result, polyResult);
					}
				}
			}
		}

        private void RemoveInvalidMeasurements(List<double> times, List<double> dataWithBg, List<double> dataBg, List<double> compDataWithBg, List<double> compBg)
        {
            var badDataIndexes = new List<int>();

            for (int i = 0; i < dataWithBg.Count; i++) 
                if (double.IsNaN(dataWithBg[i])) badDataIndexes.Add(i);
            for (int i = 0; i < compDataWithBg.Count; i++)
                if (double.IsNaN(dataWithBg[i])) badDataIndexes.Add(i);
            for (int i = 0; i < dataBg.Count; i++)
                if (double.IsNaN(dataBg[i])) badDataIndexes.Add(i);
            for (int i = 0; i < compBg.Count; i++)
                if (double.IsNaN(compBg[i])) badDataIndexes.Add(i);

            var sortedIndexes = badDataIndexes.Distinct().ToList();
            sortedIndexes.Sort((x, y) => y.CompareTo(x));

            for (int i = 0; i < sortedIndexes.Count ; i++)
            {
                times.RemoveAt(sortedIndexes[i]);
                dataWithBg.RemoveAt(sortedIndexes[i]);
                dataBg.RemoveAt(sortedIndexes[i]);
                compDataWithBg.RemoveAt(sortedIndexes[i]);
                compBg.RemoveAt(sortedIndexes[i]);
            }
        }

		private void PresentResults(int targetId, KweeVanWoerdenResult result, PolynomialFitResult polyResult = null)
		{
			var frmResults = new frmResults();
			frmResults.Results = result;
			frmResults.PolyResults = polyResult;
		    frmResults.TangraHost = m_TangraHost;
			frmResults.TargetId = targetId;
			frmResults.StartPosition = FormStartPosition.CenterParent;
			frmResults.ShowDialog(m_TangraHost.ParentWindow);
		}

		public static byte[] LoadBytesFromEmbeddedResource(string fileNameOnly)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			using (Stream str = assembly.GetManifestResourceStream("Tangra.KweeVanWoerden.Resources." + fileNameOnly))
			using (BinaryReader rdr = new BinaryReader(str))
			{
				return rdr.ReadBytes((int)str.Length);
			}
		}

		private void ExecuteWithSimulatedData()
		{
			double[] times;
			double[] varStar;
			double[] varSky;
			double[] compStar;
			double[] compSky;

			
			int dataPoints = 0;
			
			byte[] bytes = LoadBytesFromEmbeddedResource("Input_Observations.bin");
			double jdAtUtcMidnight = 2456967.1234567;

			using (var memStr = new MemoryStream(bytes))
			using (var rdr = new BinaryReader(memStr))
			{
				dataPoints = rdr.ReadInt32();

				times = new double[dataPoints];
				varStar = new double[dataPoints];
				varSky = new double[dataPoints];
				compStar = new double[dataPoints];
				compSky = new double[dataPoints];

				for (int i = 0; i < dataPoints; i++)
				{
					times[i] = rdr.ReadDouble() * 86400.0; // Convert from days to seconds
					varStar[i] = rdr.ReadDouble();
					varSky[i] = rdr.ReadDouble();
					compStar[i] = rdr.ReadDouble();
					compSky[i] = rdr.ReadDouble();
				}
			}

			KweeVanWoerdenResult result = Kwee_van_Woerden(dataPoints, jdAtUtcMidnight, times, varStar, varSky, compStar, compSky, false);

            PolynomialFitResult polyResult = PolynomialFit(dataPoints, jdAtUtcMidnight, times, varStar, varSky, compStar, compSky, (int)result.Start_Light_Curve_Obs_Index, (int)result.Stop_Light_Curve_Obs_Index, result.Time_Of_Minimum);

			PresentResults(0, result, polyResult);
		}

		[Obsolete("The old implementation calling the Fortran library")]
		private string CallKweeVanWoerden(int numObs, double jdAtUtcMidnight, double[] timePoints, double[] dataWithBg, double[] dataBg, double[] compDataWithBg, double[] compBg)
		{			
			string nativeDllPath = Path.GetFullPath(FORTRAN_DLL_DIRECTORY_PATH + @"\kwee_van_woerden_subroutine.dll");
			Trace.WriteLine("DLL Path: " + nativeDllPath, "Occulting Binaries Addin for Tangra");

			if (!File.Exists(nativeDllPath))
			{
				MessageBox.Show(m_TangraHost.ParentWindow, "Cannot find kwee_van_woerden_subroutine.dll", "Eclipsing Binaries Addin for Tangra", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return null;
			}

			IntPtr pDll = NativeMethods.LoadLibrary(nativeDllPath);

			if (pDll != IntPtr.Zero)
			{
				try
				{
					IntPtr pAddressOfFunctionToCall = NativeMethods.GetProcAddress(pDll, "Kwee_van_Woerden");

					if (pAddressOfFunctionToCall != IntPtr.Zero)
					{
						KweeVanWoerdenDelegate kweeVanWoerden = (KweeVanWoerdenDelegate)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(KweeVanWoerdenDelegate));

						double[] times = new double[10000];
						double[] varStar = new double[10000];
						double[] varSky = new double[10000];
						double[] compStar = new double[10000];
						double[] compSky = new double[10000];

						int idx = 0;
						for (int i = 0; i < numObs; i++)
						{
							times[idx] = timePoints[i];
							varStar[idx] = dataWithBg[i];
							varSky[idx] = dataBg[i];
							compStar[idx] = compDataWithBg[i];
							compSky[idx] = compBg[i];
							idx++;
						}

						string outputDir = Path.GetFullPath(Path.GetTempPath() + "\\Tangra3Temp");
						if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
						string outputFile = Path.GetFullPath(outputDir + "\\Time_Of_Minimum_Summary.Txt");
						if (File.Exists(outputFile)) File.Delete(outputFile);

						char[] outputDirChars = new char[256];
						Array.Copy(outputDir.ToCharArray(), outputDirChars, outputDir.Length);
						outputDirChars[outputDir.Length] = ' '; // Must be space terminated

						double lastUTDateInJD = jdAtUtcMidnight;

						kweeVanWoerden(ref idx, ref lastUTDateInJD, times.ToArray(), varStar.ToArray(), varSky.ToArray(), compStar.ToArray(), compSky.ToArray(), outputDirChars);

						if (File.Exists(outputFile))
							return outputFile;
						else
							MessageBox.Show(m_TangraHost.ParentWindow, "No solution was found with the current data", "Eclipsing Binaries Addin for Tangra", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					}
				}
				finally
				{
					NativeMethods.FreeLibrary(pDll);
				}
			}
			else 
			{
				MessageBox.Show(m_TangraHost.ParentWindow, "Cannot load kwee_van_woerden_subroutine.dll", "Eclipsing Binaries Addin for Tangra", MessageBoxButtons.OK, MessageBoxIcon.Stop);
			}

			return null;
		}

		public void Finalise()
		{
			
		}

		private void ShowErrorMessage(string errorMessage)
		{
			MessageBox.Show(
				m_TangraHost.ParentWindow,
				errorMessage,
				"Eclipsing Binaries Addin for Tangra",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}

		private PolynomialFitResult PolynomialFit(long Number_Obs, double Time_First_JD, double[] SecondsFromTimeFirstJD,
		                             double[] Variable_Star_DN, double[] Variable_Sky_DN, double[] Comparison_Star_DN,
		                             double[] Comparison_Sky_DN,
                                     int firstObsIndex, int lastObsIndex, double kweeVanWoerdenT0)
		{
            // http://var2.astro.cz/library/1350745528_ebfit.pdf
            //
            // Mag(t) = M0 + C1 * F(t, t0, d, G)
            // F(t, t0, d, G) = 1 - {1 - Exp[1 - cosh((t - t0 / d))] } ^ G;
            // t0 - time of minimum
            // d > 0 is the minimum width
            // G > 0 - the pointedness, G > 1 corresponds to flat minima (total eclipse)
            // 
            // -----------------------------------------------------------------------------
            //
            // We use a simplified model:
            //
            // Mag(t) = M0 - C * {1 - Exp[1 - cosh((t - T0 / D))] } ^ G
            //
            // Where M0 is a constrant and we don't fit for it
            // C has been fixed to +1
            // t0 is taken from the KweeVanWoerden solution
            // ------------------------------------------------------------------------------

			var rv = new PolynomialFitResult();
			rv.TimePoints = new List<double>();


			var dataPointsVar = new List<double>();
			var dataPointsComp = new List<double>();

            for (int i = 0; i < Number_Obs; i++)
			{
                double y = (Variable_Star_DN[i] - Variable_Sky_DN[i]) / (Comparison_Star_DN[i] - Comparison_Sky_DN[i]);
                if (i >= firstObsIndex && i < lastObsIndex)
                {
                    double x = SecondsFromTimeFirstJD[i] / (24 * 3600);
                    rv.TimePoints.Add(x);
                    dataPointsVar.Add(Variable_Star_DN[i] - Variable_Sky_DN[i]);
                    dataPointsComp.Add(Comparison_Star_DN[i] - Comparison_Sky_DN[i]);
                }

                rv.DataPoints.Add(y);
			}

			var fitter = new ModelFitter(rv.TimePoints.Count, rv.TimePoints.ToArray(), dataPointsVar.ToArray(), dataPointsComp.ToArray());
			fitter.Solve(true, kweeVanWoerdenT0);

			for (int i = 0; i < Number_Obs; i++)
			{
				if (i >= firstObsIndex && i < lastObsIndex)
					rv.FittedValues.Add(fitter.NormIntensities[i - firstObsIndex]);
				else
					rv.FittedValues.Add(double.NaN);
			}

			rv.G = fitter.G;
			rv.M0 = fitter.M0;
			rv.C = fitter.C;
			rv.D = fitter.D;
			rv.T0 = fitter.T0;

		    rv.StartIndex = firstObsIndex;
            rv.StopIndex = lastObsIndex;

            rv.Time_Of_Minimum_JD = Time_First_JD + fitter.T0;
		    rv.Time_Of_Minimum_Uncertainty = 0; // Math.Sqrt((4.0 * rv.A * rv.C - rv.B * rv.B) / (4.0 * rv.A * rv.A) / ((float)(rv.TimePoints.Count / 4.0 - 1.0)));

			return rv;
		}

		private KweeVanWoerdenResult Kwee_van_Woerden(
            long Number_Obs, double Time_First_JD, 
            double[] SecondsFromTimeFirstJD, double[] Variable_Star_DN, double[] Variable_Sky_DN, double[] Comparison_Star_DN, double[] Comparison_Sky_DN,
            bool fineGranedBins)
		{
            long BASE_NUMBER = fineGranedBins ? 1000 : 100;
			/* Constant parameter */
			long         Normal_Points = BASE_NUMBER + 1;         /* Number of evenly spaced data pairs used in the analysis */
			long         Normal_Point_Middle;                     /* Index of the middle element of the normal point array */

			/* Luminosity values */
			double[]     Luminosity_Ratio;						  /* Variable luminosity divided by comparison luminosity */
			double       Luminosity_Normal_Sum;                   /* Summation of luminosities for normal point computation */
			long         Luminosity_Normal_Count;                 /* Counter for data points in one normal point */
			double[] Luminosity_Normal = new double[BASE_NUMBER + 2];     /* Normalized luminosities */

			/* Time values */
			double       Time_Interval;                           /* Time interval between observations */
			double       Time_Start, Time_Stop;                   /* Limits for normal points */
			double[] Time_Normal = new double[BASE_NUMBER + 2];           /* Times corresponding to normal points */

			/* Symmetry analysis */
			double       Luminosity_Faintest;                     /* Faintest normalized luminosity */
			long         Luminosity_Faintest_Index;               /* Index of the faintest normalized luminosity */
			long         Start_Light_Curve;                       /* Start element of normalized light curve to analyze */
			//long         Middle_Light_Curve;                      /* Middle element of normalized light curve to analyze */
			long         Stop_Light_Curve;                        /* Stop element of normalized light curve to analyze */
			long         Start_Sum_Squares;                       /* Start element of the sum-of-squares array */
			long         Stop_Sum_Squares;                        /* Stop element of the sum-of-squares array */
			double       Sum_Of_Squares;                          /* Sum of squares across a time of symmetry */
			long[] Sum_Of_Squares_Count = new long[BASE_NUMBER + 2];    /* Number of squares accumulated */
			double[] Sum_Of_Squares_Mean = new double[BASE_NUMBER + 2];   /* Sum of squares divided by count */
			long         Sum_Of_Squares_Smallest_Index;           /* Index of the smallest sum of squares */
			double       Sum_Of_Squares_Smallest;                 /* Smallest sum of squares */

			/* Time of minimum and uncertainty */
			double[]     Quad = new double[4];                    /* Inputs to the quadratic equation taken from the sums of squares */
			double       A, B, C;                                 /* Intermediate values for quadratic equation */
			double       T0;                                      /* Time of symmetry */
			double       T0_Uncertainty;                          /* Time of symmetry uncertainty */
			//double       Time_Of_Minimum;                         /* Time of minimum light */
			//double       Time_Of_Minimum_Uncertainty;             /* Time of minimum light uncertainty */

			/* Loop indices */
			long         i, j;                                    /* Loop indices */

			var rv = new KweeVanWoerdenResult()
			{
				NumberObservations = Number_Obs
			};

			rv.Observations_File.Add("                Time     Luminosity_Ratio     Variable_Star_DN      Variable_Sky_DN   Comparison_Star_DN    Comparison_Sky_DN");
			rv.Normals_File.Add("                Time    Luminosity_Ratio");


		    double[] Time = new double[Number_Obs];
            for (int k = 0; k < Number_Obs; k++)
                Time[k] = SecondsFromTimeFirstJD[k] / 86400.0;

			Luminosity_Ratio = new double[Number_Obs];
			/* Determine luminosity ratios */
			for (i = 0; i < Number_Obs; i++) 
			{
				Luminosity_Ratio[i] = ( Variable_Star_DN[i] - Variable_Sky_DN[i] ) / ( Comparison_Star_DN[i] - Comparison_Sky_DN[i] );

				rv.Observations_File.Add(
					Time[i].ToString("#########0.000000").PadLeft(20) + " " +
					Luminosity_Ratio[i].ToString("#########0.000000").PadLeft(20) + " " +
					Variable_Star_DN[i].ToString("#########0.000000").PadLeft(20) + " " +
					Variable_Sky_DN[i].ToString("#########0.000000").PadLeft(20) + " " +
					Comparison_Star_DN[i].ToString("#########0.000000").PadLeft(20) + " " +
					Comparison_Sky_DN[i].ToString("#########0.000000").PadLeft(20));
			}

		    bool hasEmptyBins = false;

			/* Compute normal point times and luminosities */
			Time_Interval = ( Time[Number_Obs - 1] - Time[0] ) / (float)Normal_Points;
			for (i = 0; i < Normal_Points; i++) 
			{
				Time_Start = Time[0] + i * Time_Interval;
				Time_Normal[i] = Time_Start + Time_Interval / 2.0;
				Time_Stop = Time_Start + Time_Interval;
				Luminosity_Normal_Sum = 0.0;
				Luminosity_Normal_Count = 0;
				for (j = 0; j < Number_Obs; j++ ) 
				{
					if ( ( Time[j] >= Time_Start ) && ( Time[j] < Time_Stop) ) 
					{
						Luminosity_Normal_Sum = Luminosity_Normal_Sum + Luminosity_Ratio[j];
						Luminosity_Normal_Count = Luminosity_Normal_Count + 1;
					}
				}

				if ( Luminosity_Normal_Count > 0 ) 
				{
					Luminosity_Normal[i] = Luminosity_Normal_Sum / (float)Luminosity_Normal_Count;
                    rv.Normals_File.Add(Time_Normal[i].ToString("#########0.000000").PadLeft(20) + " " + Luminosity_Normal[i].ToString("#########0.000000").PadLeft(20));
				}
				else
				{
				    hasEmptyBins = true;
				    Luminosity_Normal[i] = double.NaN;
				}
			}

            if (hasEmptyBins)
            {
                for (i = 0; i < Normal_Points; i++)
                {
                    bool isEmptyBin = double.IsNaN(Luminosity_Normal[i]);
                    if (isEmptyBin)
                    {
                        double? prevBin2 = i <= 1 ? (double?)null : Luminosity_Normal[i - 2];
                        double? prevBin = i == 0 ? (double?) null : Luminosity_Normal[i - 1];
                        var emptyBins = new List<double>();
                        long k = i;
                        while (k < Normal_Points && double.IsNaN(Luminosity_Normal[k]))
                        {
                            k++;
                            emptyBins.Add(double.NaN);
                        }
                        double? nextBin = k == Normal_Points ? (double?)null : Luminosity_Normal[k];
						double? nextBin2 = k + 1 < Normal_Points || k + 1 >= Luminosity_Normal.Length ? (double?)null : Luminosity_Normal[k + 1];

                        if ((prevBin == null && nextBin2 == null) || (prevBin2 == null && nextBin == null))
                        {
                            rv.ErrorMessage = "Too many empty bins!";
                            return rv;
                        }

                        double step = 0;
                        if (prevBin == null)
                            step = nextBin2.Value - nextBin.Value;
                        else if (nextBin == null)
                            step = prevBin.Value - prevBin2.Value;
                        else
                            step = (nextBin.Value - prevBin.Value) / (1 + emptyBins.Count);


                        for (int l = 0; l < emptyBins.Count; l++)
                        {
                            Luminosity_Normal[i + l] = prevBin.Value + l * step;
                        }
                    }
                }
            }

		    rv.Buckets.Clear();
            rv.Buckets.AddRange(Luminosity_Normal);

			/* Locate the faintest luminosity */
			Luminosity_Faintest = 1000000000.0;
			Luminosity_Faintest_Index = 0;
			for (i = 0; i < Normal_Points; i++) 
			{
				if ( Luminosity_Normal[i] < Luminosity_Faintest ) 
				{
					Luminosity_Faintest_Index = i;
					Luminosity_Faintest = Luminosity_Normal[i];
				}
			}

			/* Set the limits of the light curve to be symmetrical around the faintest luminosity */
			Start_Light_Curve = 0;
			Stop_Light_Curve = Normal_Points - 1;
			Normal_Point_Middle = Normal_Points / 2;
			if ( Luminosity_Faintest_Index < Normal_Point_Middle ) { Stop_Light_Curve = 2 * Luminosity_Faintest_Index; }
			if ( Luminosity_Faintest_Index > Normal_Point_Middle ) { Start_Light_Curve = 2 * Luminosity_Faintest_Index - Normal_Points + 1; }
			rv.IncludedObservations = 100 * (Stop_Light_Curve - Start_Light_Curve - 2) / BASE_NUMBER;

		    rv.Start_Light_Curve = Start_Light_Curve;
            rv.Stop_Light_Curve = Stop_Light_Curve;

            double startTime = Time[0] + Start_Light_Curve * Time_Interval;
            double stopTime = Time[0] + (Stop_Light_Curve + 1) * Time_Interval;
            long startIndex = 0;
		    long stopIndex = Number_Obs - 1;
		    for (int k = 0; k < Number_Obs; k++)
		    {
                if (Start_Light_Curve > 0 && Time[k] <= startTime) startIndex = k + 1;
                if (Stop_Light_Curve < Number_Obs - 1 && k > 1 && Time[k] > stopTime && Time[k - 1] < stopTime) stopIndex = k - 1;
		    }
		    rv.Start_Light_Curve_Obs_Index = startIndex;
            rv.Stop_Light_Curve_Obs_Index = stopIndex;

			/* Compute the normalized sums of squares of luminosity differences across an array of times */
			Start_Sum_Squares = Start_Light_Curve + 1;
			Stop_Sum_Squares  = Stop_Light_Curve  - 1;
			for (i = Start_Sum_Squares; i <= Stop_Sum_Squares; i++) 
			{
				Sum_Of_Squares = 0.0;
				Sum_Of_Squares_Count[i] = 0;

				for (j = 0; j < Normal_Points; j++) 
				{
					if ( i - j >= 0 && i + j < Normal_Points ) 
					{
						Sum_Of_Squares = Sum_Of_Squares + Math.Pow(Luminosity_Normal[i-j] - Luminosity_Normal[i+j], 2);
						Sum_Of_Squares_Count[i] = Sum_Of_Squares_Count[i] + 1;
					}
				}

				Sum_Of_Squares_Mean[i] = Sum_Of_Squares / (float) Sum_Of_Squares_Count[i];
			}

            rv.Sum_Of_Squares_Count.Clear();
            rv.Sum_Of_Squares_Count.AddRange(Sum_Of_Squares_Count);
			rv.Sum_Of_Squares_Mean.Clear();
			rv.Sum_Of_Squares_Mean.AddRange(Sum_Of_Squares_Mean);

			/* Find the smallest normalized sum of squares */
			Sum_Of_Squares_Smallest = 1000000000;
			Sum_Of_Squares_Smallest_Index = 0;
			for (i = Start_Sum_Squares; i <= Stop_Sum_Squares; i++) 
			{
				if ( Sum_Of_Squares_Mean[i] < Sum_Of_Squares_Smallest ) 
				{
					/* Must also have a reasonable sample of points */
					if ( Sum_Of_Squares_Count[i] > Normal_Points / 10 ) 
					{
						Sum_Of_Squares_Smallest_Index = i;
						Sum_Of_Squares_Smallest = Sum_Of_Squares_Mean[i];
					}
				}
			}

			rv.Sum_Of_Squares_Smallest_Index = Sum_Of_Squares_Smallest_Index;

            if (Sum_Of_Squares_Smallest_Index - 2 < 0 ||
                Sum_Of_Squares_Smallest_Index + 2 > Sum_Of_Squares_Mean.Length - 1)
            {
                rv.ErrorMessage = string.Format("Cannot locate the smallest normalized sum of squares (Index = {0})", Sum_Of_Squares_Smallest_Index);
                return rv;
            }

			/* Solve the quadratic equation */
			for (i = 0; i <= 3; i++) {
				Quad[i] = Sum_Of_Squares_Mean[Sum_Of_Squares_Smallest_Index+i-2];
			}

			A = -Quad[2] + ( Quad[3] + Quad[1] ) / 2.0;
			B = -3.0 * A + Quad[2] - Quad[1];
			C = Quad[1] - A - B;

			/* Calculate the time of minimum and uncertainty */			
			T0 = -B / ( 2.0 * A );
			T0_Uncertainty = Math.Sqrt((4.0 * A * C - B * B) / (4.0 * A * A) / ((float)Normal_Points / 4.0 - 1.0));

			rv.T0 = T0;
			rv.T0_Uncertainty = T0_Uncertainty;			
			rv.Time_Of_Minimum = Time_Normal[1] + ((Sum_Of_Squares_Smallest_Index - 1) + (T0 - 2.0)) * Time_Interval;
            rv.Time_Of_Minimum_JD = Time_First_JD + rv.Time_Of_Minimum;
			rv.Time_Of_Minimum_Uncertainty = T0_Uncertainty * Time_Interval;
			rv.Success = true;

			rv.Summary_File.Add(string.Format("      Time_First               Number_Obs"));
            rv.Summary_File.Add(Time_First_JD.ToString("#########0.000000").PadLeft(20) + " " + Number_Obs.ToString("#########0").PadLeft(20));
			rv.Summary_File.Add(string.Format("\n               Time of minimum: \n"));
			rv.Summary_File.Add(string.Format("      Bins-Quadratic          Uncertainty"));
			rv.Summary_File.Add(T0.ToString("#########0.000000").PadLeft(20) + " " + T0_Uncertainty.ToString("#########0.000000").PadLeft(20));
			rv.Summary_File.Add(string.Format("      From Time_First         Uncertainty"));
			rv.Summary_File.Add(rv.Time_Of_Minimum.ToString("#########0.000000").PadLeft(20) + " " + rv.Time_Of_Minimum_Uncertainty.ToString("#########0.000000").PadLeft(20));
			rv.Summary_File.Add(string.Format("      Absolute                Uncertainty"));
			rv.Summary_File.Add(rv.Time_Of_Minimum_JD.ToString("#########0.000000").PadLeft(20) + " " + rv.Time_Of_Minimum_Uncertainty.ToString("#########0.000000").PadLeft(20));

			/* Finish */
			return rv;
		}

		internal class PolynomialFitResult
		{
			public double Time_Of_Minimum_JD;
			public double Time_Of_Minimum_Uncertainty;
			public List<double> TimePoints = new List<double>();
            public List<double> DataPoints = new List<double>();
			public List<double> FittedValues = new List<double>();

			public double M0 { get; set; }
			public double C { get; set; }
			public double T0 { get; set; }
			public double D { get; set; }
			public double G { get; set; }

		    public int StartIndex;
            public int StopIndex;
		}

		internal class KweeVanWoerdenResult
		{
			public long NumberObservations;
			public long IncludedObservations;
			public double T0;			
			public double T0_Uncertainty;
			public double Time_Of_Minimum;
			public double Time_Of_Minimum_JD;
			public double Time_Of_Minimum_Uncertainty;

			public bool Success;
			public string ErrorMessage;

			public List<string> Summary_File = new List<string>();
			public List<string> Observations_File = new List<string>();
			public List<string> Normals_File = new List<string>();
		    public List<double> Buckets = new List<double>();
            public List<long> Sum_Of_Squares_Count = new List<long>();
			public List<double> Sum_Of_Squares_Mean = new List<double>();
		    public long Start_Light_Curve;
            public long Stop_Light_Curve;
			public long Sum_Of_Squares_Smallest_Index;

		    public long Start_Light_Curve_Obs_Index;
		    public long Stop_Light_Curve_Obs_Index;
		}
	}
}
