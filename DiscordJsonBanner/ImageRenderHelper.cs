using DiscordJsonBanner.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace DiscordJsonBanner
{
    class ImageRenderHelper
    {
        // This code is unusable
        /*static FontFamily GetFontFromBytes(byte[] font)
        {
            PrivateFontCollection pfc = new PrivateFontCollection();
            IntPtr ptr = Marshal.AllocHGlobal(font.Length);
            try
            {
                Marshal.Copy(font, 0, ptr, font.Length);
                pfc.AddMemoryFont(ptr, font.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to load font: {ex.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return pfc.Families.Length > 0 ? pfc.Families[0] : default;
        }*/

        static FontFamily GetFontFromFile(string FilePath)
        {
            FilePath = Path.GetFullPath(FilePath, Environment.CurrentDirectory);
            if (!File.Exists(FilePath)) return null;
            PrivateFontCollection pfc = new PrivateFontCollection();
            pfc.AddFontFile(FilePath);
            return pfc.Families.Length > 0 ? pfc.Families[0] : default;
        }

        public static Image RenderImage(Image background, int number, ImageRenderConfiguration settings)
        {
            using Graphics g = Graphics.FromImage(background);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.PageUnit = GraphicsUnit.Pixel;

            var renderFont = new Font(GetFontFromFile(settings.FontPath) ?? throw new FileNotFoundException("Font wasn't found at the specified location"),
                settings.RenderFontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            Brush textRenderBrush = new SolidBrush(settings.RenderColor);

            g.RotateTransform(settings.RenderAngle, MatrixOrder.Append);
            g.TranslateTransform(settings.RenderPositionX, settings.RenderPositionY, MatrixOrder.Append);
            var content = number.ToString();
            var measurement = g.MeasureString(content, renderFont);
            g.DrawString(number.ToString(), renderFont, textRenderBrush, settings.RenderOrigin switch
            {
                "left" => 0,
                "center" => -measurement.Width / 2,
                "right" => -measurement.Width,
                _ => throw new ArgumentOutOfRangeException()
            }, 0);
            return background;
        }
        public static Image RenderImage(Stream stream, int number, ImageRenderConfiguration settings)
        {
            if (!stream.CanRead) throw new NotSupportedException("The specified stream can't be read");
            if (!stream.CanSeek && stream.Position != 0) throw new NotSupportedException("The specified stream can't be seeked");
            return RenderImage(Image.FromStream(stream), number, settings);
        }
        public static Image RenderImage(string backgroundPath, int number, ImageRenderConfiguration settings)
        {
            backgroundPath = Path.GetFullPath(backgroundPath, Environment.CurrentDirectory);
            if (!File.Exists(backgroundPath)) throw new FileNotFoundException("Image file wasn't found at the specified location");
            return RenderImage(Image.FromFile(backgroundPath), number, settings);
        }
    }
}