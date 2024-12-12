using System.Diagnostics;
using NetVips;

namespace TIFFConversionComparison;

internal class Program
{
    private const string TestImagesDirectory = "TestImages";


    private static void Main(string[] args)
    {
        // libvips will show show a lot of warnings, because DNR images are generally pretty weird. If libvips ever
        // crashes on an image, rerun with this line commented out so you can get more info.
        Log.SetLogHandler("VIPS", Enums.LogLevelFlags.Warning, null);

        var basePath = AppContext.BaseDirectory;
        var files = Directory.GetFiles(Path.Combine(basePath, TestImagesDirectory));
        foreach (var file in files)
        {
            Debug.Assert(Path.GetExtension(file) == ".tif");

            var imageBytes = File.ReadAllBytes(file);
            var watch = Stopwatch.StartNew();
            var images = VipsConvert(imageBytes);
            watch.Stop();
            var filename = Path.GetFileName(file);
            Console.WriteLine($"{filename} | {FormatTimeSpan(watch.Elapsed)} | {GetTotalSizeFormatted(images)}");
        }
    }

    private static void SaveImages(List<byte[]> images, string filename)
    {
        var i = 0;
        foreach (var image in images)
        {
            File.WriteAllBytes($"{i}_{filename}", image);
            i++;
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        var seconds = timeSpan.TotalSeconds - 60 * timeSpan.Minutes;
        return $"{timeSpan.Minutes}:{seconds:F2}";
    }

    private static string GetTotalSizeFormatted(List<byte[]> images)
    {
        var sizeInBytes = images.Sum(image => image.Length);
        var sizeInMegabytes = (double)sizeInBytes / 1024 / 1024;
        return $"{sizeInMegabytes:F2} MB";
    }

    private static List<byte[]> VipsConvert(byte[] bytes, Enums.ForeignWebpPreset? preset = null)
    {
        var stream = new MemoryStream(bytes);
        var image = Image.TiffloadStream(stream, access: Enums.Access.Sequential, page: 0);

        var numPages = (int)image.Get("n-pages");

        return Enumerable.Range(0, numPages).Select(i => VipsConvertPage(bytes, i)).ToList();
    }

    private static byte[] VipsConvertPage(byte[] bytes, int pageNum, Enums.ForeignWebpPreset? preset = null)
    {
        var image = Image.TiffloadBuffer(bytes, access: Enums.Access.Sequential, page: pageNum);


        // 16383 is WebP's maximum width or height.
        var largestValue = image.Width > image.Height ? image.Width : image.Height;
        if (largestValue > 16383)
        {
            var scale = 16383.0 / largestValue;
            image = image.Resize(scale);
        }

        // Need to make this an option so it's easy to disable if we want to.
        // We figure that most of the documents are 9.5x11 pages, and we want to retain a resolution of 300 pixels per
        // inch.
        var dpi = 300.0;
        var maxHeight = dpi * 11.0;
        var maxWidth = dpi * 9.5;

        if (image.Height > maxHeight)
        {
            var scale = maxHeight / image.Height;
            image = image.Resize(scale);
        }

        if (image.Width > maxWidth)
        {
            var scale = maxWidth / image.Width;
            image = image.Resize(scale);
        }


        // The DNR images are 1-band greyscale, and WebP requires 3 or 4 bands.
        image = image.Colourspace(Enums.Interpretation.Srgb);

        return image.WebpsaveBuffer(preset: preset);
    }
}