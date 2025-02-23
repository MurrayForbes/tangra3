﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Tangra.Model.Config;
using Tangra.Model.Numerical;

namespace Tangra.MotionFitting
{
    public interface IMeasurementPositionProvider
    {
        IEnumerable<MeasurementPositionEntry> Measurements { get; }
        int NumberOfMeasurements { get; }
        string ObjectDesignation { get; }
        string ObservatoryCode { get; }
        string CatalogueCode { get; }
        bool Unmeasurable { get; }
        DateTime? ObservationDate { get; }
        decimal InstrumentalDelaySec { get; }
        double ArsSecsInPixel { get; }
    }

    public class ReductionSettings
    {
        public decimal InstrumentalDelaySec;
        public WeightingMode Weighting;
        public bool RemoveOutliers;
        public double OutliersSigmaThreashold;
        public int NumberOfChunks;
        public int ConstraintPattern;
        public double BestPositionUncertaintyArcSec;
        public double SmallestReportedUncertaintyArcSec;
        public bool FactorInPositionalUncertainty;
        public ErrorMethod ErrorMethod;
    }
}
