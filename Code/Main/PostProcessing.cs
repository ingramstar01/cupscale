﻿using Cupscale.UI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Cupscale.Main
{
  class PostProcessing
  {
    public static async Task PostprocessingSingle(string path, bool dontResize = false, int retryCount = 20, bool trimPngExt = true)
    {
      if (!IoUtils.IsFileValid(path))
        return;

      string newPath = "";

      if (trimPngExt)
        newPath = path.Substring(0, path.Length - 4);

      Logger.Log($"PostProc: Trimmed filename from '{Path.GetFileName(path)}' to '{Path.GetFileName(newPath)}'");

      try
      {
        File.Move(path, newPath);
      }
      catch (Exception e)     // An I/O error can appear if the file is still locked by python (?)
      {
        Logger.Log($"Failed to move/rename! ('{path}' => '{newPath}') {e.Message}\n{e.StackTrace}");

        if (retryCount > 0)
        {
          await Task.Delay(500);      // Wait and retry up to 20 times
          int newRetryCount = retryCount - 1;
          Logger.Log("Retrying - " + newRetryCount + " attempts left.");
          await PostprocessingSingle(path, dontResize, newRetryCount);
        }
        else
        {
          Logger.ErrorMessage($"Failed to move/rename '{Path.GetFileName(path)}' and ran out of retries!", e);
        }

        return;
      }

      path = newPath;
      string format = PreviewUi.outputFormat.Text;

      if (Program.lastUpscaleIsVideo || format == Upscale.ImgExportMode.PNG.ToStringTitleCase())
      {
        await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.Png50, dontResize);
        return;
      }

      if (format == Upscale.ImgExportMode.SameAsSource.ToStringTitleCase())
        await ImageProcessing.ConvertImageToOriginalFormat(path, true, false, dontResize);

      if (format == Upscale.ImgExportMode.JPEG.ToStringTitleCase())
        await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.Jpeg, dontResize);

      if (format == Upscale.ImgExportMode.WEBP.ToStringTitleCase())
        await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.Weppy, dontResize);

      if (format == Upscale.ImgExportMode.BMP.ToStringTitleCase())
        await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.BMP, dontResize);

      if (format == Upscale.ImgExportMode.TGA.ToStringTitleCase())
        await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.TGA, dontResize);

      if (format == Upscale.ImgExportMode.DDS.ToStringTitleCase())
        await ImageProcessing.PostProcessDDS(path);

      if (format == Upscale.ImgExportMode.GIF.ToStringTitleCase())
        await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.GIF, dontResize);

    }
  }
}
