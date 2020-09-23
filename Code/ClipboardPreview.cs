﻿using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cupscale
{
    class ClipboardPreview
    {
        public static Bitmap originalPreview;
        public static Bitmap resultPreview;

        public static async void CopyToClipboardSideBySide(bool saveToFile, bool fullImage = false)
        {
            //if (resultPreview == null)
            //return;
            int footerHeight = 45;

            try
            {
                if (fullImage)
                {
                    originalPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewOutPath, "preview-input-scaled.png")));
                    resultPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewOutPath, "preview-merged.png")));
                }
                else
                {
                    originalPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewPath, "preview.png")));
                    resultPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewOutPath, "preview.png")));
                }
            }
            catch
            {
                MessageBox.Show("Error creating clipboard preview!", "Error");
            }

            int comparisonMod = 1;
            //int.TryParse(comparisonMod_comboBox.SelectedValue.ToString(), out comparisonMod);
            int newWidth = comparisonMod * resultPreview.Width, newHeight = comparisonMod * resultPreview.Height;

            Bitmap outputImage = new Bitmap(2 * newWidth, newHeight + footerHeight);
            string modelName = Program.lastModelName;
            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.DrawImage(originalPreview, new Rectangle(0, 0, newWidth, newHeight),
                    new Rectangle(new Point(), originalPreview.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(resultPreview, new Rectangle(newWidth, 0, newWidth, newHeight),
                    new Rectangle(new Point(), resultPreview.Size), GraphicsUnit.Pixel);

                Bitmap Bmp = new Bitmap(2 * newWidth, footerHeight);
                Color color = Color.FromArgb(22, 22, 22);
                using (Graphics gfx = Graphics.FromImage(Bmp))
                using (SolidBrush brush = new SolidBrush(color))
                {
                    gfx.FillRectangle(brush, 0, 0, 2 * newWidth, footerHeight);
                }
                graphics.DrawImage(Bmp, 0, newHeight);

                GraphicsPath p = new GraphicsPath();
                int fontSize = 19;
                SizeF s = new Size(999999999, 99999999);

                Font font = new Font("Times New Roman", graphics.DpiY * fontSize / 72);

                string barString = "[CS] " + Path.GetFileName(Program.lastFilename) + " - " + modelName;

                int cf = 0, lf = 0;
                while (s.Width >= 2 * newWidth)
                {
                    fontSize--;
                    font = new Font(FontFamily.GenericSansSerif, graphics.DpiY * fontSize / 72, FontStyle.Regular);
                    s = graphics.MeasureString(barString, font, new SizeF(), new StringFormat(), out cf, out lf);
                }
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;

                double a = graphics.DpiY * fontSize / 72;
                stringFormat.LineAlignment = StringAlignment.Center;

                double contrastW = GetColorContrast(color, Color.White);
                double contrastB = GetColorContrast(color, Color.Black);
                Brush textBrush = contrastW < 3.0 ? Brushes.Black : Brushes.White;

                graphics.DrawString(
                    $"{barString}",
                    font,
                    textBrush,
                    new Rectangle(0, newHeight, 2 * newWidth, footerHeight - 0),
                    stringFormat);
            }
            try
            {
                if (saveToFile)
                {
                    string comparisonSavePath = Path.ChangeExtension(Program.lastFilename, null) + "-comparison.png";
                    outputImage.Save(comparisonSavePath);
                    await ImageProcessing.ConvertImage(comparisonSavePath, ImageProcessing.Format.PngFast, false, false);
                    MessageBox.Show("Saved current comparison to:\n\n" + comparisonSavePath, "Message");
                }
                else
                {
                    Clipboard.SetDataObject(outputImage);
                }
            }
            catch
            {
                MessageBox.Show("Failed to save comparison.", "Error");
            }
        }

        public static void CopyToClipboardSlider(bool fullImage = false)
        {
            //if (resultPreview == null)
            //return;
            int footerHeight = 45;

            try
            {
                if (fullImage)
                {
                    originalPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewOutPath, "preview-input-scaled.png")));
                    resultPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewOutPath, "preview-merged.png")));
                }
                else
                {
                    originalPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewPath, "preview.png")));
                    resultPreview = new Bitmap(IOUtils.GetImage(Path.Combine(IO.Paths.previewOutPath, "preview.png")));
                }
            }
            catch
            {
                MessageBox.Show("Error creating clipboard preview!", "Error");
            }
            

            int comparisonMod = 1;
            //int.TryParse(comparisonMod_comboBox.SelectedValue.ToString(), out comparisonMod);
            int newWidth = comparisonMod * resultPreview.Width, newHeight = comparisonMod * resultPreview.Height;

            Bitmap outputImage = new Bitmap(newWidth, newHeight + footerHeight);
            string modelName = Program.lastModelName;
            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                int halfWidth = (int)Math.Round(newWidth * 0.5f);
                Bitmap croppedOutput = resultPreview.Clone(new Rectangle(halfWidth, 0, newWidth - halfWidth, newHeight), resultPreview.PixelFormat);

                graphics.DrawImage(originalPreview, 0, 0, newWidth, newHeight);     // First half
                graphics.DrawImage(croppedOutput, halfWidth, 0);        // Second half
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(192, Color.Black)), halfWidth - 2, 0, 4, newHeight);   // Line

                Bitmap Bmp = new Bitmap(newWidth, footerHeight);
                Color color = Color.FromArgb(22, 22, 22);
                using (Graphics gfx = Graphics.FromImage(Bmp))
                using (SolidBrush brush = new SolidBrush(color))
                {
                    gfx.FillRectangle(brush, 0, 0, newWidth, footerHeight); ;
                }
                graphics.DrawImage(Bmp, 0, newHeight);

                GraphicsPath p = new GraphicsPath();
                int fontSize = 19;
                SizeF s = new Size(999999999, 99999999);

                Font font = new Font("Times New Roman", graphics.DpiY * fontSize / 72);

                string barString = "[CS] " + Path.GetFileName(Program.lastFilename) + " - " + modelName;

                int cf = 0, lf = 0;
                while (s.Width >= newWidth)
                {
                    fontSize--;
                    font = new Font(FontFamily.GenericSansSerif, graphics.DpiY * fontSize / 72, FontStyle.Regular);
                    s = graphics.MeasureString(barString, font, new SizeF(), new StringFormat(), out cf, out lf);
                }
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;

                double a = graphics.DpiY * fontSize / 72;
                stringFormat.LineAlignment = StringAlignment.Center;

                double contrastW = GetColorContrast(color, Color.White);
                double contrastB = GetColorContrast(color, Color.Black);

                Brush textBrush = contrastW < 3.0 ? Brushes.Black : Brushes.White;

                graphics.DrawString(
                    $"{barString}",
                    font,
                    textBrush,
                    new Rectangle(0, newHeight, newWidth, footerHeight - 0),
                    stringFormat);

                try
                {
                    Clipboard.SetDataObject(outputImage);
                }
                catch
                { }
            }
        }

        static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }

        static double GetColorContrast(Color background, Color text)
        {
            double L1 = 0.2126 * background.R / 255 + 0.7152 * background.G / 255 + 0.0722 * background.B / 255;
            double L2 = 0.2126 * text.R / 255 + 0.7152 * text.G / 255 + 0.0722 * text.B / 255;
            if (L1 > L2)
                return (L1 + 0.05) / (L2 + 0.05);
            else
                return (L2 + 0.05) / (L1 + 0.05);

        }
    }
}