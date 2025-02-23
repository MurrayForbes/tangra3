/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#ifndef PSFFIT_H
#define PSFFIT_H

#define CERTAINTY_CONST (0.5 / 0.03)
#define MAX_MATRIX_SIZE 35

enum PSFFittingMethod {
    NonLinearFit = 0,
    LinearFitOfAveragedModel = 1,
    NonLinearAsymetricFit = 2
};

enum PSFFittingDataRange {
    DataRange8Bit = 0,
    DataRange12Bit = 1,
    DataRange14Bit = 2,
	DataRange16Bit = 3
};

class PsfFit
{
private:
	int m_xCenter;
	int m_yCenter;

	int m_HalfWidth;
	int m_MatrixSize;
	bool m_IsSolved;
	int m_Saturation;

	double m_X0;
	double m_Y0;
	double m_IBackground;
	double m_IStarMax;
	PSFFittingDataRange m_DataRange;
	
	double* m_Residuals;

	void SetNewFieldCenterFrom17PixMatrix(int x, int y);
	void SetNewFieldCenterFrom35PixMatrix(int x, int y);
	double GetPSFValueInternal(double x, double y);
	double GetPSFValueInternalAsymetric(double x, double y);

	void DoNonLinearFit(unsigned int* intensity, int width);
	void DoNonLinearAsymetricFit(unsigned int* intensity, int width);
	void DoLinearFitOfAveragedModel(unsigned int* intensity, int width);
	
	void SetDataRange(PSFFittingDataRange dataRange, unsigned int maxPixelValue);

public:
	PsfFit(PSFFittingDataRange dataRange, unsigned int maxPixelValue);
	PsfFit(int xCenter, int yCenter, PSFFittingDataRange dataRange, unsigned int maxPixelValue);
	~PsfFit();

	PSFFittingMethod FittingMethod;

	double R0;
	double RX0;
	double RY0;

	bool IsSolved();
	double GetValue(double x, double y);
	double XCenter();
	double YCenter();

	int MatrixSize();
	int X0();
	int Y0();
	double X0_Matrix();
	double Y0_Matrix();
	double I0();
	double IMax();

	unsigned int Brightness();
	double FWHM();
	double ElongationPercentage();

	double Certainty();

	void Fit(unsigned int* intensity, int width);
	void Fit(int xCenter, int yCenter, unsigned int* intensity, int width);
	void CopyResiduals(double* buffer, int matrixSize);
};

#endif // PSFFIT_H
