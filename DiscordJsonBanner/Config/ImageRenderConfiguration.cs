using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace DiscordJsonBanner.Config
{
    [DebuggerDisplay("Configuration Data")]
    public sealed class ImageRenderConfiguration
    {
        /// <summary>
        /// Text color: Red value (0-255)
        /// </summary>
        [JsonProperty("text_color_r", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public byte RenderColorR { get; set; }
        /// <summary>
        /// Text color: Green value (0-255)
        /// </summary>
        [JsonProperty("text_color_g", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public byte RenderColorG { get; set; }
        /// <summary>
        /// Text color: Blue value (0-255)
        /// </summary>
        [JsonProperty("text_color_b", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public byte RenderColorB { get; set; }
        /// <summary>
        /// Text color
        /// </summary>
        public Color RenderColor => Color.FromArgb(RenderColorR, RenderColorG, RenderColorB);

        /// <summary>
        /// Render position: X value
        /// </summary>
        [JsonProperty("render_x", DefaultValueHandling = DefaultValueHandling.Populate)]
        public float RenderPositionX { get; set; }
        /// <summary>
        /// Render position: Y value
        /// </summary>
        [JsonProperty("render_Y", DefaultValueHandling = DefaultValueHandling.Populate)]
        public float RenderPositionY { get; set; }
        /// <summary>
        /// Origin of render, may be left, center or right
        /// </summary>
        [JsonProperty("render_orig")]
        public string RenderOrigin { get; set; }
        /// <summary>
        /// Render position
        /// </summary>
        public PointF RenderPosition => new PointF(RenderPositionX, RenderPositionY);
        /// <summary>
        /// Render angle (By default 0)
        /// </summary>
        [JsonProperty("render_angle", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public float RenderAngle { get; set; }

        /// <summary>
        /// Font size
        /// </summary>
        [JsonProperty("font_size", DefaultValueHandling = DefaultValueHandling.Populate)]
        public float RenderFontSize { get; set; }

        /// <summary>
        /// Font path
        /// </summary>
        [JsonProperty("font", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("font.ttf")]
        public string FontPath { get; set; }

        public override bool Equals(object obj)
        {
            Type type = typeof(ImageRenderConfiguration);
            if (obj.GetType() == type)
            {
                var properties = type.GetProperties();
                foreach (var property in properties)
                {
                    if (property.CanRead && !property.GetValue(obj).Equals(property.GetValue(this)))
                        return false;
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
