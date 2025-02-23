/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// http://stackoverflow.com/questions/7136450/using-dll-in-visual-studio-2010-c
// http://www.steptools.com/support/stdev_docs/help/howto_dlls.html
// http://www.transmissionzero.co.uk/computing/advanced-mingw-dll-topics/
// G++ Linking :
// Tangra.Core.def -Wl,--subsystem,windows,--out-implib,./Debug/TangraCoredll.lib

#ifdef  __cplusplus
extern "C" {
#endif

namespace TangraCore
{
	void IntergationManagerStartNew(int width, int height, bool isMedianAveraging);
	void IntegrationManagerAddFrame(unsigned long* framePixels);
	void IntegrationManagerAddFrameEx(unsigned long* framePixels, bool isLittleEndian, int bpp);
	void IntegrationManagerProduceIntegratedFrame(unsigned long* framePixels);
	void IntegrationManagerFreeResources();
	int IntegrationManagerGetFirstFrameToIntegrate(int producedFirstFrame, int frameCount, bool isSlidingIntegration);

	HRESULT GetBitmapPixels(long width, long height, unsigned long* pixels, BYTE* bitmapPixels, BYTE* bitmapBytes, bool isLittleEndian, int bpp);
	HRESULT GetPixelMapBits(BYTE* pDIB, long* width, long* height, DWORD imageSize, unsigned long* pixels, BYTE* bitmapPixels, BYTE* bitmapBytes);
	HRESULT GetPixelMapPixelsOnly(BYTE* pDIB, long width, long height, unsigned long* pixels);

	long ApplyPreProcessing(unsigned long* pixels, long width, long height, int bpp, BYTE* bitmapPixels, BYTE* bitmapBytes);
	long ApplyPreProcessingPixelsOnly(unsigned long* pixels, long width, long height, int bpp, float exposureSeconds);
	bool UsesPreProcessing();
}

#ifdef  __cplusplus
}
#endif