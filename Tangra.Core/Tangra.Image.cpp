/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#include "Tangra.Image.h"
#include "math.h"
#include <cmath>
#include <vector>
#include <algorithm>
#include <stack>

unsigned int GetMaxValueForBitPix(int bpp)
{
	if (bpp == 8)
		return 0xFF;
	else if (bpp == 12)
		return 0xFFF;
	else if (bpp == 14)
		return 0x3FFF;
	else if (bpp == 16)
		return 0xFFFF;
	else
		return 0xFFFFFFFF;
}

HRESULT Convolution(unsigned int* data, int bpp, int width, int height, const double* convMatrix, bool cutEdges, bool calculateAverage, unsigned int* average)
{
	if (width > MAX_CONVOLUTION_WIDTH || width > MAX_CONVOLUTION_WIDTH)
		return E_FAIL;

	//uint[] result = new uint[cutEdges ? (width - 1) * (height - 1) : data.Length];

	unsigned int maxValue = GetMaxValueForBitPix(bpp);

	int nPixel;
	double Pixel;
	double sum = 0;
	const double FOUR_PIXEL_FACTOR = 9.0 / 4.0;
	const double SIX_PIXEL_FACTOR = 9.0 / 6.0;

	double convTopLeft = convMatrix[0];
	double convTopMid = convMatrix[1];
	double convTopRight = convMatrix[2];
	double convMidLeft = convMatrix[3];
	double convPixel = convMatrix[4];
	double convMidRight = convMatrix[5];
	double convBottomLeft = convMatrix[6];
	double convBottomMid = convMatrix[7];
	double convBottomRight = convMatrix[8];

	for (int y = 0; y < height; ++y) {
		for (int x = 0; x < width; ++x) {

			if (cutEdges && (x == 0 || y == 0 || x == width - 1 || y == height - 1))
				continue;

			if (y == 0 && x == 0) {
				// . . .
				// . # #
				// . # #
				Pixel = ((
				             (data[0] * convPixel) +
				             (data[1] * convMidRight) +
				             (data[width] * convBottomMid) +
				             (data[width + 1] * convBottomRight)
				         ) * FOUR_PIXEL_FACTOR);
			} else if (y == height - 1 && x == 0) {
				// . # #
				// . # #
				// . . .
				Pixel = ((
				             (data[width * (height - 2)] * convTopMid) +
				             (data[width * (height - 2) + 1] * convTopRight) +
				             (data[width * (height - 1)] * convPixel) +
				             (data[width * (height - 1) + 1] * convMidRight)
				         ) * FOUR_PIXEL_FACTOR);
			} else if (y == 0 && x == width - 1) {
				// . . .
				// # # .
				// # # .
				Pixel = ((
				             (data[width - 2] * convMidLeft) +
				             (data[width - 1] * convPixel) +
				             (data[2 * width - 2] * convBottomLeft) +
				             (data[2 * width - 1] * convBottomMid)
				         ) * FOUR_PIXEL_FACTOR);
			} else if (y == height - 1 && x == width - 1) {
				// # # .
				// # # .
				// . . .
				Pixel = ((
				             (data[width * height - width - 1] * convTopLeft) +
				             (data[width * height - width - 2] * convTopMid) +
				             (data[width * height - 2] * convMidLeft) +
				             (data[width * height - 1] * convPixel)
				         ) * FOUR_PIXEL_FACTOR);

			} else if (y == 0) {
				// . . .
				// # # #
				// # # #
				Pixel = ((
				             (data[x - 1 + y * width] * convMidLeft) +
				             (data[x + y * width] * convPixel) +
				             (data[x + 1 + y * width] * convMidRight) +
				             (data[x - 1 + (y + 1) * width] * convBottomLeft) +
				             (data[x + (y + 1) * width] * convBottomMid) +
				             (data[x + 1 + (y + 1) * width] * convBottomRight)
				         ) * SIX_PIXEL_FACTOR);
			} else if (x == 0) {
				// . # #
				// . # #
				// . # #
				Pixel = ((
				             (data[x + (y - 1) * width] * convTopMid) +
				             (data[x + 1 + (y - 1) * width] * convTopRight) +
				             (data[x + y * width] * convPixel) +
				             (data[x + 1 + y * width] * convMidRight) +
				             (data[x + (y + 1) * width] * convBottomMid) +
				             (data[x + 1 + (y + 1) * width] * convBottomRight)
				         ) * SIX_PIXEL_FACTOR);
			} else if (y == height - 1) {
				// # # #
				// # # #
				// . . .
				Pixel = ((
				             (data[x - 1 + (y - 1) * width] * convTopLeft) +
				             (data[x + (y - 1) * width] * convTopMid) +
				             (data[x + 1 + (y - 1) * width] * convTopRight) +
				             (data[x - 1 + y * width] * convMidLeft) +
				             (data[x + y * width] * convPixel) +
				             (data[x + 1 + y * width] * convMidRight)
				         ) * SIX_PIXEL_FACTOR);
			} else if (x == width - 1) {
				// # # .
				// # # .
				// # # .
				Pixel = ((
				             (data[x - 1 + (y - 1) * width] * convTopLeft) +
				             (data[x + (y - 1) * width] * convTopMid) +
				             (data[x - 1 + y * width] * convMidLeft) +
				             (data[x + y * width] * convPixel) +
				             (data[x - 1 + (y + 1) * width] * convBottomLeft) +
				             (data[x + (y + 1) * width] * convBottomMid)
				         ) * SIX_PIXEL_FACTOR);
			} else {
				// # # #
				// # # #
				// # # #
				Pixel = (
				            (data[x - 1 + (y - 1) * width] * convTopLeft) +
				            (data[x + (y - 1) * width] * convTopMid) +
				            (data[x + 1 + (y - 1) * width] * convTopRight) +
				            (data[x - 1 + y * width] * convMidLeft) +
				            (data[x + y * width] * convPixel) +
				            (data[x + 1 + y * width] * convMidRight) +
				            (data[x - 1 + (y + 1) * width] * convBottomLeft) +
				            (data[x + (y + 1) * width] * convBottomMid) +
				            (data[x + 1 + (y + 1) * width] * convBottomRight));
			}

			nPixel = (unsigned int)round(Pixel);

			if (cutEdges) {
				if (nPixel < 0)
					RESULT_BUFFER[(x - 1) + (y - 1) * width] = 0;
				else if (nPixel > maxValue)
					RESULT_BUFFER[(x - 1) + (y - 1) * width] = maxValue;
				else
					RESULT_BUFFER[(x - 1) + (y - 1) * width] = (unsigned int)nPixel;

				if (calculateAverage)
					sum += RESULT_BUFFER[(x - 1) + (y - 1) * width];
			} else {
				if (nPixel < 0)
					RESULT_BUFFER[x + y * width] = 0;
				else if (nPixel > maxValue)
					RESULT_BUFFER[x + y * width] = maxValue;
				else
					RESULT_BUFFER[x + y * width] = (unsigned int)nPixel;

				if (calculateAverage)
					sum += RESULT_BUFFER[x + y * width];
			}
		}
	}

	int totalPixels = cutEdges ? (width - 1) * (height - 1) : width * height;

	if (calculateAverage)
		*average = (unsigned int)round(sum / totalPixels);

	memcpy(&data[0], &RESULT_BUFFER[0], totalPixels * sizeof(unsigned int));

	return S_OK;
}

void Convolution_Area(unsigned int* data, unsigned int* result, unsigned int maxValue, int x0, int y0, int areaWidth, int areaHeight, int width, int height, const double* convMatrix, bool cutEdges, bool calculateAverage, unsigned int* average)
{
	if (width > MAX_CONVOLUTION_WIDTH || width > MAX_CONVOLUTION_WIDTH)
		return;

	int nPixel;
	double Pixel;
	double sum = 0;
	const double FOUR_PIXEL_FACTOR = 9.0 / 4.0;
	const double SIX_PIXEL_FACTOR = 9.0 / 6.0;

	double convTopLeft = convMatrix[0];
	double convTopMid = convMatrix[1];
	double convTopRight = convMatrix[2];
	double convMidLeft = convMatrix[3];
	double convPixel = convMatrix[4];
	double convMidRight = convMatrix[5];
	double convBottomLeft = convMatrix[6];
	double convBottomMid = convMatrix[7];
	double convBottomRight = convMatrix[8];

	for (int y = y0; y < y0 + areaHeight; ++y) {
		for (int x = x0; x < x0 + areaWidth; ++x) {

			if (cutEdges && (x == 0 || y == 0 || x == width - 1 || y == height - 1))
				continue;

			if (y == 0 && x == 0) {
				// . . .
				// . # #
				// . # #
				Pixel = ((
				             (data[0] * convPixel) +
				             (data[1] * convMidRight) +
				             (data[width] * convBottomMid) +
				             (data[width + 1] * convBottomRight)
				         ) * FOUR_PIXEL_FACTOR);
			} else if (y == height - 1 && x == 0) {
				// . # #
				// . # #
				// . . .
				Pixel = ((
				             (data[width * (height - 2)] * convTopMid) +
				             (data[width * (height - 2) + 1] * convTopRight) +
				             (data[width * (height - 1)] * convPixel) +
				             (data[width * (height - 1) + 1] * convMidRight)
				         ) * FOUR_PIXEL_FACTOR);
			} else if (y == 0 && x == width - 1) {
				// . . .
				// # # .
				// # # .
				Pixel = ((
				             (data[width - 2] * convMidLeft) +
				             (data[width - 1] * convPixel) +
				             (data[2 * width - 2] * convBottomLeft) +
				             (data[2 * width - 1] * convBottomMid)
				         ) * FOUR_PIXEL_FACTOR);
			} else if (y == height - 1 && x == width - 1) {
				// # # .
				// # # .
				// . . .
				Pixel = ((
				             (data[width * height - width - 1] * convTopLeft) +
				             (data[width * height - width - 2] * convTopMid) +
				             (data[width * height - 2] * convMidLeft) +
				             (data[width * height - 1] * convPixel)
				         ) * FOUR_PIXEL_FACTOR);

			} else if (y == 0) {
				// . . .
				// # # #
				// # # #
				Pixel = ((
				             (data[x - 1 + y * width] * convMidLeft) +
				             (data[x + y * width] * convPixel) +
				             (data[x + 1 + y * width] * convMidRight) +
				             (data[x - 1 + (y + 1) * width] * convBottomLeft) +
				             (data[x + (y + 1) * width] * convBottomMid) +
				             (data[x + 1 + (y + 1) * width] * convBottomRight)
				         ) * SIX_PIXEL_FACTOR);
			} else if (x == 0) {
				// . # #
				// . # #
				// . # #
				Pixel = ((
				             (data[x + (y - 1) * width] * convTopMid) +
				             (data[x + 1 + (y - 1) * width] * convTopRight) +
				             (data[x + y * width] * convPixel) +
				             (data[x + 1 + y * width] * convMidRight) +
				             (data[x + (y + 1) * width] * convBottomMid) +
				             (data[x + 1 + (y + 1) * width] * convBottomRight)
				         ) * SIX_PIXEL_FACTOR);
			} else if (y == height - 1) {
				// # # #
				// # # #
				// . . .
				Pixel = ((
				             (data[x - 1 + (y - 1) * width] * convTopLeft) +
				             (data[x + (y - 1) * width] * convTopMid) +
				             (data[x + 1 + (y - 1) * width] * convTopRight) +
				             (data[x - 1 + y * width] * convMidLeft) +
				             (data[x + y * width] * convPixel) +
				             (data[x + 1 + y * width] * convMidRight)
				         ) * SIX_PIXEL_FACTOR);
			} else if (x == width - 1) {
				// # # .
				// # # .
				// # # .
				Pixel = ((
				             (data[x - 1 + (y - 1) * width] * convTopLeft) +
				             (data[x + (y - 1) * width] * convTopMid) +
				             (data[x - 1 + y * width] * convMidLeft) +
				             (data[x + y * width] * convPixel) +
				             (data[x - 1 + (y + 1) * width] * convBottomLeft) +
				             (data[x + (y + 1) * width] * convBottomMid)
				         ) * SIX_PIXEL_FACTOR);
			} else {
				// # # #
				// # # #
				// # # #
				Pixel = (
				            (data[x - 1 + (y - 1) * width] * convTopLeft) +
				            (data[x + (y - 1) * width] * convTopMid) +
				            (data[x + 1 + (y - 1) * width] * convTopRight) +
				            (data[x - 1 + y * width] * convMidLeft) +
				            (data[x + y * width] * convPixel) +
				            (data[x + 1 + y * width] * convMidRight) +
				            (data[x - 1 + (y + 1) * width] * convBottomLeft) +
				            (data[x + (y + 1) * width] * convBottomMid) +
				            (data[x + 1 + (y + 1) * width] * convBottomRight));
			}

			nPixel = (unsigned int)round(Pixel);

			int x1 = x - x0;
			int y1 = y - y0;
			if (cutEdges) {
				if (nPixel < 0)
					result[(x1 - 1) + (y1 - 1) * areaWidth] = 0;
				else if (nPixel > maxValue)
					result[(x1 - 1) + (y1 - 1) * areaWidth] = maxValue;
				else
					result[(x1 - 1) + (y1 - 1) * areaWidth] = (unsigned int)nPixel;

				if (calculateAverage)
					sum += result[(x1 - 1) + (y1 - 1) * areaWidth];
			} else {
				if (nPixel < 0)
					result[x1 + y1 * areaWidth] = 0;
				else if (nPixel > maxValue)
					result[x1 + y1 * areaWidth] = maxValue;
				else
					result[x1 + y1 * areaWidth] = (unsigned int)nPixel;

				if (calculateAverage)
					sum += result[x1 + y1 * areaWidth];
			}
		}
	}

	int totalPixels = cutEdges ? (width - 1) * (height - 1) : width * height;

	if (calculateAverage)
		*average = (unsigned int)round(sum / totalPixels);
}

void Convolution_GaussianBlur(unsigned int* pixels, int bpp, int width, int height)
{
	Convolution(pixels, bpp, width, height, GAUSSIAN_BLUR_MATRIX, false, false, NULL);
}

void Convolution_GaussianBlur_Area(unsigned int* pixels, unsigned int maxPixelValue, int x0, int y0, int areaWidth, int areaHeight, int width, int height)
{
	unsigned int blur[areaWidth * areaHeight];
	Convolution_Area(pixels, &blur[0], maxPixelValue, x0, y0, areaWidth, areaHeight, width, height, GAUSSIAN_BLUR_MATRIX, false, false, NULL);

	for(int y = y0; y < y0 + areaHeight; y++) {
		for(int x = x0; x < x0 + areaWidth; x++) {
			if (x >= width) continue;
			if (y >= height) continue;
			pixels[x + y * width] = blur[x - x0, y - y0];
		}
	}
}

void Convolution_Sharpen(unsigned int* pixels, int bpp, int width, int height, unsigned int* average)
{
	Convolution(pixels, bpp, width, height, SHARPEN_MATRIX, false, true, average);
}

void Convolution_Denoise(unsigned int* pixels, int bpp, int width, int height, unsigned int* average, bool cutEdges)
{
	Convolution(pixels, bpp, width, height, DENOISE_MATRIX, cutEdges, true, average);
}

int* s_CheckedPixels = NULL;
int s_ChunkDenoiseIndex;
int s_ChunkDenoiseWidth;
int s_ChunkDenoiseHeight;
int s_ChunkDenoiseMaxIndex;
int* s_ObjectPixelsIndex = NULL;
int s_ObjectPixelsCount;
int s_ObjectPixelsXFrom;
int s_ObjectPixelsXTo;
int s_ObjectPixelsYFrom;
int s_ObjectPixelsYTo;
int s_MaxLowerBoundNoiseChunkPixels;
int s_MinUpperBoundNoiseChunkPixels;
int s_MinLowerBoundNoiseChunkHeight;
int s_MaxUpperBoundNoiseChunkWidth;
std::stack<int> s_ObjectPixelsPath;

void SetObjectPixelsXFromTo(int pixelIndex)
{
	int width = pixelIndex % s_ChunkDenoiseWidth;
	int height = pixelIndex / s_ChunkDenoiseWidth;

	if (s_ObjectPixelsXFrom > width) s_ObjectPixelsXFrom = width;
	if (s_ObjectPixelsXTo < width) s_ObjectPixelsXTo = width;
	if (s_ObjectPixelsYFrom > height) s_ObjectPixelsYFrom = height;
	if (s_ObjectPixelsYTo < height) s_ObjectPixelsYTo = height;
}

void EnsureChunkDenoiseBuffers(int width, int height)
{
	if (s_ChunkDenoiseWidth != width || s_ChunkDenoiseHeight != height) {
		if (NULL == s_CheckedPixels) {
			delete s_CheckedPixels;
			s_CheckedPixels = NULL;
		}

		if (NULL == s_ObjectPixelsIndex) {
			delete s_ObjectPixelsIndex;
			s_ObjectPixelsIndex = NULL;
		}

		s_ChunkDenoiseWidth = width;
		s_ChunkDenoiseHeight = height;
		s_ChunkDenoiseMaxIndex = width * height;
		s_CheckedPixels = (int*)malloc(sizeof(int) * s_ChunkDenoiseMaxIndex);
		s_ObjectPixelsIndex = (int*)malloc(sizeof(int) * s_ChunkDenoiseMaxIndex);

		// The max noise chink part to be removed is 50% of the pixels in "1".
		// This value is determined experimentally and varied based on the area hight
		// Block(22x16), AreaHeight(20) -> '1' is 70 pixels (50% = 35 pixels)
		s_MaxLowerBoundNoiseChunkPixels = (int)round(35.0 * height / 20);
		// The min noise chunk is 80% of a fully black square
		s_MinUpperBoundNoiseChunkPixels = (int)round(0.8 * width * height);
		s_MinLowerBoundNoiseChunkHeight = (int)round(0.5 * (height - 4));
		s_MaxUpperBoundNoiseChunkWidth = height + 4;
	}
}

#define UNCHECKED 0
#define WENT_UP 1
#define WENT_LEFT 2
#define WENT_RIGHT 3
#define WENT_DOWN 4
#define CHECKED 5

int FindNextObjectPixel(unsigned int* pixels, unsigned int onColour)
{
	while (s_ChunkDenoiseIndex < s_ChunkDenoiseMaxIndex - 1) {
		s_ChunkDenoiseIndex++;
		if (s_CheckedPixels[s_ChunkDenoiseIndex] == UNCHECKED) {
			if (pixels[s_ChunkDenoiseIndex] != onColour)
				s_CheckedPixels[s_ChunkDenoiseIndex] = CHECKED;
			else
				return s_ChunkDenoiseIndex;
		}
	}

	return -1;
}

bool ProcessNoiseObjectPixel(unsigned int* pixels,int* pixelRef, unsigned int onColour)
{
	int pixel = *pixelRef;
	int x = pixel % s_ChunkDenoiseWidth;
	int y = pixel / s_ChunkDenoiseWidth;
	int width = s_ChunkDenoiseWidth;
	int nextPixel;

	if (s_CheckedPixels[pixel] == UNCHECKED) {
		nextPixel = (y - 1) * width + x;

		s_CheckedPixels[pixel] = WENT_UP;

		if (y > 0) {
			if (s_CheckedPixels[nextPixel] == UNCHECKED) {
				if (pixels[nextPixel] == onColour) {
					s_ObjectPixelsPath.push(nextPixel);
					*pixelRef = nextPixel;
					s_ObjectPixelsIndex[s_ObjectPixelsCount] = nextPixel;
					s_ObjectPixelsCount++;
					SetObjectPixelsXFromTo(nextPixel);
				} else
					s_CheckedPixels[nextPixel] = CHECKED;
			}
		}

		return true;
	} else if (s_CheckedPixels[pixel] == WENT_UP) {
		nextPixel = y * width + (x - 1);

		s_CheckedPixels[pixel] = WENT_LEFT;

		if (x > 0) {
			if (s_CheckedPixels[nextPixel] == UNCHECKED) {
				if (pixels[nextPixel] == onColour) {
					s_ObjectPixelsPath.push(nextPixel);
					*pixelRef = nextPixel;
					s_ObjectPixelsIndex[s_ObjectPixelsCount] = nextPixel;
					s_ObjectPixelsCount++;
					SetObjectPixelsXFromTo(nextPixel);
				} else
					s_CheckedPixels[nextPixel] = CHECKED;
			}
		}

		return true;
	} else if (s_CheckedPixels[pixel] == WENT_LEFT) {
		nextPixel = y * width + (x + 1);

		s_CheckedPixels[pixel] = WENT_RIGHT;

		if (x < width - 1) {
			if (s_CheckedPixels[nextPixel] == UNCHECKED) {
				if (pixels[nextPixel] == onColour) {
					s_ObjectPixelsPath.push(nextPixel);
					*pixelRef = nextPixel;
					s_ObjectPixelsIndex[s_ObjectPixelsCount] = nextPixel;
					s_ObjectPixelsCount++;
					SetObjectPixelsXFromTo(nextPixel);
				} else
					s_CheckedPixels[nextPixel] = CHECKED;
			}
		}

		return true;
	} else if (s_CheckedPixels[pixel] == WENT_RIGHT) {
		nextPixel = (y + 1) * width + x;

		s_CheckedPixels[pixel] = WENT_DOWN;

		if (y < s_ChunkDenoiseHeight - 1) {
			if (s_CheckedPixels[nextPixel] == UNCHECKED) {
				if (pixels[nextPixel] == onColour) {
					s_ObjectPixelsPath.push(nextPixel);
					*pixelRef = nextPixel;
					s_ObjectPixelsIndex[s_ObjectPixelsCount] = nextPixel;
					s_ObjectPixelsCount++;
					SetObjectPixelsXFromTo(nextPixel);
				} else
					s_CheckedPixels[nextPixel] = CHECKED;
			}
		}

		return true;
	} else if (s_CheckedPixels[pixel] >= WENT_DOWN) {
		if (s_ObjectPixelsPath.empty())
			return false;

		s_CheckedPixels[pixel] = CHECKED;

		nextPixel = s_ObjectPixelsPath.top();
		s_ObjectPixelsPath.pop();

		if (pixel == nextPixel && !s_ObjectPixelsPath.empty()) {
			nextPixel = s_ObjectPixelsPath.top();
			s_ObjectPixelsPath.pop();
		}

		*pixelRef = nextPixel;
		return true;
	} else
		return false;
}

bool CurrentObjectChunkIsNoise()
{
	return
	    s_ObjectPixelsCount < s_MaxLowerBoundNoiseChunkPixels ||
	    s_ObjectPixelsCount > s_MinUpperBoundNoiseChunkPixels ||
	    (s_ObjectPixelsYTo - s_ObjectPixelsYFrom) < s_MinLowerBoundNoiseChunkHeight ||
	    (s_ObjectPixelsXTo - s_ObjectPixelsXFrom) > s_MaxUpperBoundNoiseChunkWidth;
}

int CheckAndRemoveNoiseObjectAsNecessary(unsigned int* pixels, int firstPixel, unsigned int onColour, unsigned int offColour)
{
	s_ObjectPixelsCount = 0;
	s_ObjectPixelsXFrom = 0xFFFF;
	s_ObjectPixelsXTo = 0;
	s_ObjectPixelsYFrom = 0xFFFF;
	s_ObjectPixelsYTo = 0;
	while(!s_ObjectPixelsPath.empty()) s_ObjectPixelsPath.pop();
	s_ObjectPixelsPath.push(firstPixel);

	int currPixel = firstPixel;

	s_ObjectPixelsIndex[s_ObjectPixelsCount] = firstPixel;
	s_ObjectPixelsCount++;
	SetObjectPixelsXFromTo(firstPixel);

	while (ProcessNoiseObjectPixel(pixels, &currPixel, onColour))
	{ }

	if (CurrentObjectChunkIsNoise()) {
		for (int i = 0; i < s_ObjectPixelsCount; i++) {
			pixels[s_ObjectPixelsIndex[i]] = offColour;
		}
	}
}

HRESULT LargeChunkDenoise(unsigned int* pixels, int width, int height, unsigned int onColour, unsigned int offColour)
{
	EnsureChunkDenoiseBuffers(width, height);

	s_ObjectPixelsCount = -1;
	s_ChunkDenoiseIndex = -1;

	memset(s_CheckedPixels, 0, s_ChunkDenoiseMaxIndex * sizeof(int));

	do {
		int nextObjectPixelId = FindNextObjectPixel(pixels, onColour);

		if (nextObjectPixelId != -1)
			CheckAndRemoveNoiseObjectAsNecessary(pixels, nextObjectPixelId, onColour, offColour);
		else
			break;

	} while (true);
}

HRESULT PrepareImageForOCR(unsigned int* pixels, int bpp, int width, int height)
{
	int size = width * height;

	// Dark median correct
	std::vector<unsigned int> pixelsVec;
	pixelsVec.insert(pixelsVec.end(), &pixels[0], &pixels[size]);
	size_t n = pixelsVec.size() / 2;
	std::nth_element(pixelsVec.begin(), pixelsVec.begin() + n, pixelsVec.end());
	int median = pixelsVec[n];

	for (int i = 0; i < size; i++) {
		int darkCorrectedValue = (int) pixels[i] - (int) median;
		if (darkCorrectedValue < 0) darkCorrectedValue = 0;
		pixels[i] = (unsigned int) darkCorrectedValue;
	}

	// Blur
	Convolution_GaussianBlur(pixels, 8, width, height);

	// Sharpen
	unsigned int average = 128;
	Convolution_Sharpen(pixels, 8, width, height, &average);

	// Binerize and Inverse
	for (int i = 0; i < size; i++) {
		pixels[i] = pixels[i] > average ? (unsigned int)0 : (unsigned int)255;
	}

	// Denoise
	Convolution_Denoise(pixels, 8, width, height, &average, false);


	// Binerize again (after the Denise)
	for (int i = 0; i < size; i++) {
		pixels[i] = pixels[i] < 127 ? (unsigned int)0 : (unsigned int)255;
	}

	LargeChunkDenoise(pixels, width, height, 0, 255);

	return S_OK;
}

HRESULT PrepareImageForOCRSingleStep(unsigned int* pixels, int bpp, int width, int height, int stepNo, unsigned int* average)
{
	int size = width * height;

	if (stepNo == 0) {
		// Dark median correct
		std::vector<unsigned int> pixelsVec;
		pixelsVec.insert(pixelsVec.end(), &pixels[0], &pixels[size]);
		size_t n = pixelsVec.size() / 2;
		std::nth_element(pixelsVec.begin(), pixelsVec.begin() + n, pixelsVec.end());
		int median = pixelsVec[n];

		for (int i = 0; i < size; i++) {
			int darkCorrectedValue = (int) pixels[i] - (int) median;
			if (darkCorrectedValue < 0) darkCorrectedValue = 0;
			pixels[i] = (unsigned int) darkCorrectedValue;
		}
	}

	if (stepNo == 1)
		// Blur
		Convolution_GaussianBlur(pixels, 8, width, height);

	if (stepNo == 2) {
		*average = 128;

		// Sharpen
		Convolution_Sharpen(pixels, 8, width, height, average);
	}

	if (stepNo == 3) {
		// Binerize and Inverse
		for (int i = 0; i < size; i++) {
			pixels[i] = pixels[i] > *average ? (unsigned int)0 : (unsigned int)255;
		}
	}

	if (stepNo == 4) {
		*average = 128;

		// Denoise
		Convolution_Denoise(pixels, 8, width, height, average, false);
	}

	if (stepNo == 5) {
		// Binerize again (after the Denise)
		for (int i = 0; i < size; i++) {
			pixels[i] = pixels[i] < 127 ? (unsigned int)0 : (unsigned int)255;
		}
	}

	return S_OK;
}
