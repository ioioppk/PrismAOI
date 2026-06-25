using System;
using System.Collections.Generic;
using System.IO;
using ImageLib.Core;

#if HAS_HALCON
using HalconDotNet;
#endif

namespace ImageLib.HalconBridge
{
    internal sealed class HalconImageData : IImageData
    {
#if HAS_HALCON
        private HObject _image;
#endif
        private bool _disposed;

        public string Id { get; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Channels { get; private set; }
        public string PixelType { get; private set; }
        public DateTime CaptureTime { get; set; }
        public IReadOnlyDictionary<string, object> Metadata { get; private set; }

        public HalconImageData()
        {
            Id = Guid.NewGuid().ToString("N");
            CaptureTime = DateTime.Now;
            Metadata = new Dictionary<string, object>();
        }

#if HAS_HALCON
        public void SetHalconImage(HObject image)
        {
            if (_image != null && _image != image)
            {
                _image.Dispose();
            }
            _image = image;

            HTuple width, height, channels;
            HOperatorSet.GetImageSize(image, out width, out height);
            Width = width.I;
            Height = height.I;
            HOperatorSet.CountChannels(image, out channels);
            Channels = channels.I;

            HTuple type;
            HOperatorSet.GetImageType(image, out type);
            PixelType = type.S;
        }
#endif

        public object GetNativeHandle()
        {
#if HAS_HALCON
            return _image;
#else
            throw new NotSupportedException("HALCON is not available.");
#endif
        }

        public IImageData Clone()
        {
            var clone = new HalconImageData();
#if HAS_HALCON
            if (_image != null)
            {
                HObject clonedImage = _image.Clone();
                clone.SetHalconImage(clonedImage);
            }
#endif
            clone.CaptureTime = CaptureTime;
            clone.Metadata = CopyMetadata(Metadata);
            return clone;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
#if HAS_HALCON
            if (_image != null)
            {
                _image.Dispose();
                _image = null;
            }
#endif
        }

        private static Dictionary<string, object> CopyMetadata(IReadOnlyDictionary<string, object> source)
        {
            var copy = new Dictionary<string, object>();
            foreach (var kv in source)
                copy[kv.Key] = kv.Value;
            return copy;
        }
    }

    internal sealed class HalconRegionData : IRegionData
    {
#if HAS_HALCON
        private HObject _region;
#endif
        private bool _disposed;

        public string Id { get; }
        public int Area { get; private set; }
        public string Type { get; private set; }
        public IReadOnlyDictionary<string, object> Metadata { get; private set; }

        public HalconRegionData()
        {
            Id = Guid.NewGuid().ToString("N");
            Metadata = new Dictionary<string, object>();
        }

#if HAS_HALCON
        public void SetHalconRegion(HObject region)
        {
            if (_region != null && _region != region)
            {
                _region.Dispose();
            }
            _region = region;

            HTuple area, type;
            HOperatorSet.RegionFeatures(region, "area", out area);
            Area = (int)area.D;
            HOperatorSet.GetRegionType(region, out type);
            Type = type.S;
        }
#endif

        public object GetNativeHandle()
        {
#if HAS_HALCON
            return _region;
#else
            throw new NotSupportedException("HALCON is not available.");
#endif
        }

        public IRegionData Clone()
        {
            var clone = new HalconRegionData();
#if HAS_HALCON
            if (_region != null)
            {
                HObject clonedRegion = _region.Clone();
                clone.SetHalconRegion(clonedRegion);
            }
#endif
            clone.Metadata = CopyMetadata(Metadata);
            return clone;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
#if HAS_HALCON
            if (_region != null)
            {
                _region.Dispose();
                _region = null;
            }
#endif
        }

        private static Dictionary<string, object> CopyMetadata(IReadOnlyDictionary<string, object> source)
        {
            var copy = new Dictionary<string, object>();
            foreach (var kv in source)
                copy[kv.Key] = kv.Value;
            return copy;
        }
    }

    internal static class ImagePool
    {
        public static HalconImageData Rent()
        {
            return new HalconImageData();
        }

        public static HalconRegionData RentRegion()
        {
            return new HalconRegionData();
        }

        public static void Return(HalconImageData image)
        {
            image?.Dispose();
        }

        public static void ReturnRegion(HalconRegionData region)
        {
            region?.Dispose();
        }
    }

    public sealed class HalconEngine
    {
        private static readonly Lazy<HalconEngine> _instance = new Lazy<HalconEngine>(() => new HalconEngine());
        public static HalconEngine Instance => _instance.Value;

        private bool _initialized;

        private HalconEngine() { }

        public void Initialize()
        {
#if HAS_HALCON
            _initialized = true;
#else
            _initialized = true;
#endif
        }

        public bool IsLicenseValid()
        {
#if HAS_HALCON
            try
            {
                HObject testImage = new HObject();
                HOperatorSet.GenImageConst(out testImage, "byte", 1, 1);
                testImage.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        public string GetVersion()
        {
#if HAS_HALCON
            try
            {
                HTuple version;
                HOperatorSet.GetSystem("version", out version);
                return version.S;
            }
            catch
            {
                return "Unknown";
            }
#else
            return "HALCON not available";
#endif
        }

        public IImageData ReadImage(string filePath)
        {
#if HAS_HALCON
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Image file not found.", filePath);

            HObject image;
            HOperatorSet.ReadImage(out image, filePath);

            var imageData = ImagePool.Rent();
            imageData.SetHalconImage(image);
            return imageData;
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }

        public void WriteImage(IImageData imageData, string filePath)
        {
#if HAS_HALCON
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var nativeHandle = imageData.GetNativeHandle();
            if (nativeHandle is HObject hImage)
            {
                HOperatorSet.WriteImage(hImage, "png", 0, filePath);
            }
            else
            {
                throw new InvalidOperationException("ImageData does not contain a valid HALCON image handle.");
            }
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }

        public IImageData Rgb1ToGray(IImageData imageData)
        {
#if HAS_HALCON
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            var nativeHandle = imageData.GetNativeHandle();
            if (!(nativeHandle is HObject hImage))
                throw new InvalidOperationException("ImageData does not contain a valid HALCON image handle.");

            HObject grayImage;
            HOperatorSet.Rgb1ToGray(hImage, out grayImage);

            var result = ImagePool.Rent();
            result.SetHalconImage(grayImage);
            return result;
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }

        public IRegionData Threshold(IImageData imageData, double minGray, double maxGray)
        {
#if HAS_HALCON
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            var nativeHandle = imageData.GetNativeHandle();
            if (!(nativeHandle is HObject hImage))
                throw new InvalidOperationException("ImageData does not contain a valid HALCON image handle.");

            HObject region;
            HOperatorSet.Threshold(hImage, out region, new HTuple(minGray), new HTuple(maxGray));

            var result = ImagePool.RentRegion();
            result.SetHalconRegion(region);
            return result;
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }

        public IRegionData Connection(IRegionData regionData)
        {
#if HAS_HALCON
            if (regionData == null)
                throw new ArgumentNullException(nameof(regionData));

            var nativeHandle = regionData.GetNativeHandle();
            if (!(nativeHandle is HObject hRegion))
                throw new InvalidOperationException("RegionData does not contain a valid HALCON region handle.");

            HObject connectedRegions;
            HOperatorSet.Connection(hRegion, out connectedRegions);

            var result = ImagePool.RentRegion();
            result.SetHalconRegion(connectedRegions);
            return result;
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }

        public IRegionData SelectShape(IRegionData regionData, string feature, string operation, double min, double max)
        {
#if HAS_HALCON
            if (regionData == null)
                throw new ArgumentNullException(nameof(regionData));

            var nativeHandle = regionData.GetNativeHandle();
            if (!(nativeHandle is HObject hRegion))
                throw new InvalidOperationException("RegionData does not contain a valid HALCON region handle.");

            HObject selectedRegions;
            HOperatorSet.SelectShape(hRegion, out selectedRegions, feature, operation, min, max);

            var result = ImagePool.RentRegion();
            result.SetHalconRegion(selectedRegions);
            return result;
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }

        public int CountObj(IRegionData regionData)
        {
#if HAS_HALCON
            if (regionData == null)
                throw new ArgumentNullException(nameof(regionData));

            var nativeHandle = regionData.GetNativeHandle();
            if (!(nativeHandle is HObject hRegion))
                throw new InvalidOperationException("RegionData does not contain a valid HALCON region handle.");

            HTuple count;
            HOperatorSet.CountObj(hRegion, out count);
            return count.I;
#else
            throw new NotSupportedException("HALCON is not available. Image operations require HALCON library.");
#endif
        }
    }
}