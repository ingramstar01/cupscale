﻿using MozJpegSharp;
using System;
using System.Drawing;
using System.IO;

namespace Cupscale.ImageUtils
{
  class MozJpeg
  {
    public static void Encode(string inPath, string outPath, int q, bool chromaSubSample = true)
    {
      try
      {
        Bitmap bmp = (Bitmap)ImgUtils.GetImage(inPath);
        var commpressor = new TJCompressor();
        byte[] compressed;
        TJSubsamplingOption subSample = TJSubsamplingOption.Chrominance420;
        if (!chromaSubSample)
          subSample = TJSubsamplingOption.Chrominance444;
        compressed = commpressor.Compress(bmp, subSample, q, TJFlags.None);
        File.WriteAllBytes(outPath, compressed);
        Logger.Log("[MozJpeg] Written image to " + outPath);
      }
      catch (TypeInitializationException e)
      {
        Logger.ErrorMessage($"MozJpeg Initialization Error: {e.InnerException.Message}\n", e);
      }
      catch (Exception e)
      {
        Logger.ErrorMessage("MozJpeg Error: ", e);
      }
    }
  }
}
