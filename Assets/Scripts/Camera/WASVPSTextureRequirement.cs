using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace WASVPS
{
    /// <summary>
    /// Represents texture requirements for WAS VPS (Visual Positioning System) processing.
    /// Contains width, height, and format specifications needed for camera texture conversion.
    /// </summary>
    public class WASVPSTextureRequirement
    {
        /// <summary>
        /// The required width of the texture in pixels
        /// </summary>
        public readonly int Width;
        
        /// <summary>
        /// The required height of the texture in pixels
        /// </summary>
        public readonly int Height;
        
        /// <summary>
        /// The texture format (e.g., R8 for grayscale, RGB24 for color)
        /// </summary>
        public readonly TextureFormat Format;

        /// <summary>
        /// Initializes a new instance of WASVPSTextureRequirement with specified dimensions and format
        /// </summary>
        /// <param name="width">The required width of the texture in pixels</param>
        /// <param name="height">The required height of the texture in pixels</param>
        /// <param name="format">The texture format to use for processing</param>
        public WASVPSTextureRequirement(int width, int height, TextureFormat format)
        {
            Width = width;
            Height = height;
            Format = format;
        }

        /// <summary>
        /// Creates conversion parameters for converting XRCpuImage to Texture2D with proper cropping and scaling.
        /// Applies Y-axis mirroring and calculates appropriate crop rectangle based on aspect ratio requirements.
        /// </summary>
        /// <param name="image">The source XRCpuImage to convert</param>
        /// <param name="width">Target width for the converted texture</param>
        /// <param name="height">Target height for the converted texture</param>
        /// <returns>Conversion parameters configured for the specified image and target dimensions</returns>
        public XRCpuImage.ConversionParams GetConversionParams(XRCpuImage image, int width, int height)
        {
            XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams(image, Format, XRCpuImage.Transformation.MirrorY);
            conversionParams.inputRect = GetCropRect(image.width, image.height, ((float)height) / ((float)width));
            conversionParams.outputDimensions = new Vector2Int(width, height);
            return conversionParams;
        }

        /// <summary>
        /// Calculates a centered crop rectangle based on the source image dimensions and target aspect ratio.
        /// Handles both portrait and landscape orientations, ensuring the crop area maintains the required aspect ratio
        /// while being centered within the source image bounds.
        /// </summary>
        /// <param name="width">Source image width in pixels</param>
        /// <param name="height">Source image height in pixels</param>
        /// <param name="cropCoefficient">Target aspect ratio (height/width) for the crop area</param>
        /// <returns>Rectangle defining the crop area with proper centering and aspect ratio</returns>
        public static RectInt GetCropRect(int width, int height, float cropCoefficient)
        {
            int requiredWidth;
            int requiredHeight;
            var xPos = 0;
            var yPos = 0;

            if (Screen.orientation == ScreenOrientation.Portrait)
            {
                requiredWidth = width;
                requiredHeight = (int)(width * cropCoefficient);

                if (requiredHeight > height)
                {
                    requiredHeight = height;
                    requiredWidth = (int)(width * (1 / cropCoefficient));
                    xPos = (width - requiredWidth) / 2;
                }
                else
                {
                    yPos = (height - requiredHeight) / 2;
                }
            }
            else
            {
                requiredHeight = height; 
                requiredWidth = (int)(height / cropCoefficient);

                if (requiredWidth > width)
                {
                    requiredWidth = width;
                    requiredHeight = (int)(height * (1 * cropCoefficient));
                    yPos = (height - requiredHeight) / 2;
                }
                else
                {
                    xPos = (width - requiredWidth) / 2;
                }
            }

            return new RectInt(xPos, yPos, requiredWidth, requiredHeight);
        }

        /// <summary>
        /// Returns the number of color channels for the current texture format.
        /// Used to determine the appropriate data structure size for texture processing.
        /// </summary>
        /// <returns>Number of channels (1 for grayscale, 3 for RGB, -1 for unsupported formats)</returns>
        public int ChannelsCount()
        {
            return Format switch
            {
                TextureFormat.R8 => 1,
                TextureFormat.RGB24 => 3,
                _ => -1
            };
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || GetType() != obj.GetType())
            {
                return false;
            }

            var requirement = (WASVPSTextureRequirement)obj;
            return (Width == requirement.Width) && (Height == requirement.Height) && (Format == requirement.Format);
        }

        public override int GetHashCode()
        {
            return Width.GetHashCode() ^ Height.GetHashCode() ^ Format.GetHashCode();
        }
    }
}
