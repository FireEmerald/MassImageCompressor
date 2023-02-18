#define MULTITHREADING

using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Linq;
using System.Threading;

namespace NeoFotonCommon
{
    public class ImageCompressor
    {
        #region CompressDirectory

        private string PreviewSave(
            string filePath,
            string savePath,
            int quality,
            ref string compression,
            bool fixedHeight,
            int dimension,
            SupportedMimeType type)
        {
            long compressionRatio;
            FileInfo toBeCompressed = new FileInfo(filePath);

            if (!fixedHeight)
                CompressImage(filePath, savePath, quality, dimension, type);
            else
                CompressImage(filePath, savePath, quality, dimension, 0, type);

            FileInfo compressed = new FileInfo(savePath);
            compressionRatio = 100 * (toBeCompressed.Length - compressed.Length) / toBeCompressed.Length;
            compression = "Reduced From " + Helper.GetReadableBytes(toBeCompressed.Length)
                + " to " + Helper.GetReadableBytes(compressed.Length)
                + " (" + compressionRatio + "% reduced)";

            return savePath;
        }

        public string CompressDirectoryPreview(
            string openPath,
            int quality,
            ref string compression,
            bool fixedHeight,
            int dimension,
            decimal fileSize,
            SupportedMimeType type,
            List<string> previewedImages,
            bool? next)
        {
            DirectoryInfo saveDir;
            if (!Directory.Exists(openPath)) return "";

            string saveDirPath =
                    Helper.AddDirectorySeparatorAtEnd(Path.GetTempPath())
                    + "MassImageCompressor300889D794E649e896387D28A2EB5836";

            if ((next == null || !next.Value) && previewedImages.Count > 0)
            {
                string fileToPreview;
                if (next == null)
                    fileToPreview = previewedImages[previewedImages.Count - 1];
                else if (next != null && previewedImages.Count > 1)
                {
                    fileToPreview = previewedImages[previewedImages.Count - 2];
                    if (next != null)
                        previewedImages.RemoveAt(previewedImages.Count - 1);
                }
                else
                    return "NO_PREV";

                return PreviewSave(
                                fileToPreview,
                                Helper.ChangeExensionToMimeType(Helper.AddDirectorySeparatorAtEnd(saveDirPath) + Path.GetFileName(fileToPreview), type),
                                quality,
                                ref compression,
                                fixedHeight,
                                dimension,
                                type);


            }

            if (!Directory.Exists(saveDirPath))
                saveDir = Directory.CreateDirectory(saveDirPath);
            else
                saveDir = new DirectoryInfo(saveDirPath);
            DirectoryInfo openDir = new DirectoryInfo(openPath);


            //Build list of Files to be compressed
            foreach (FileInfo file in openDir.GetFiles())
                if (Helper.IsSupportedImage(file.FullName))
                {
                    if (previewedImages.Contains(file.FullName))
                        continue;
                    if (file.Length < fileSize)
                        continue;
                    previewedImages.Add(file.FullName);
                    return PreviewSave(
                            file.FullName,
                            Helper.ChangeExensionToMimeType(Helper.AddDirectorySeparatorAtEnd(saveDirPath) + file.Name, type),
                            quality,
                            ref compression,
                            fixedHeight,
                            dimension,
                            type);
                }
            return "NO_NEXT";
        }

        public int CompressDirectory(
            string openPath,
            string savePath,
            int quality,
            bool fixedHeight,
            int dimension,
            decimal fileSize,
            SupportedMimeType type,
            ReportProgress progress)
        {
            if (!Directory.Exists(openPath)) return 0;

            List<string> filesToBeCompressed = new List<string>();

            DirectoryInfo saveDir;
            string saveDirPath = savePath;// Helper.AddDirectorySeparatorAtEnd(openPath) + "compressed";

            if (!Directory.Exists(saveDirPath))
                saveDir = Directory.CreateDirectory(saveDirPath);
            else
                saveDir = new DirectoryInfo(saveDirPath);

            DirectoryInfo openDir = new DirectoryInfo(openPath);

            //Build list of Files to be compressed
            foreach (FileInfo file in openDir.GetFiles())
                if (Helper.IsSupportedImage(file.FullName) && file.Length >= fileSize)
                    filesToBeCompressed.Add(file.FullName);

            int totalFliesToCompress = filesToBeCompressed.Count;
            int compressed = 0;

            Forker imageProcessingThreadManager = new Forker();

            int ThreadCount = 0;

            foreach (string file in filesToBeCompressed)
            {
                var tmpFile = file;
#if MULTITHREADING
                imageProcessingThreadManager.Fork(delegate
                {

#endif
                    CompressTask(savePath, quality, fixedHeight, dimension, type, progress, totalFliesToCompress, ref compressed, tmpFile);
#if MULTITHREADING
                });
                ThreadCount++;

                if (ThreadCount == Environment.ProcessorCount)
                {
                    ThreadCount = 0;
                    imageProcessingThreadManager.Join(); //hold your horses, don't hog more than # of cores.
                }
#endif
            }
            imageProcessingThreadManager.Join();
            return filesToBeCompressed.Count;
        }

        private void CompressTask(string savePath, int quality, bool fixedHeight, int dimension, SupportedMimeType type, ReportProgress progress, int totalFliesToCompress, ref int compressed, string file)
        {
            //Set file matching extension so 'replace' existing file doesn't duplicate 
            //file if compressed to same directory. For Example, abc.JPG is compressed then 
            //compressed file extension should be 'JPG' and not 'jpeg'
            Helper.SetExtension(file);

            if (!fixedHeight)
                CompressImage(file, Helper.AddDirectorySeparatorAtEnd(savePath) + Path.GetFileName(file), quality, dimension, type);
            else
                CompressImage(file, Helper.AddDirectorySeparatorAtEnd(savePath) + Path.GetFileName(file), quality, dimension, 0, type);

            System.Threading.Interlocked.Increment(ref compressed);
            progress((int)(100m * compressed / (decimal)totalFliesToCompress));
        }
        #endregion

        #region CompressImage
        private void CompressImage(string filePath, string savePath)
        {
            if (savePath != null)
            {
                if (!Helper.isPathWithFile(savePath))
                    savePath =
                        Helper.AddDirectorySeparatorAtEnd(savePath)
                        + Path.GetFileName(filePath);
            }
        }

        private void CompressImage(string filePath, string savePath, int quality, int dimension, SupportedMimeType type)
        {
            Bitmap img = Helper.GetBitmap(filePath);
            this.CompressImage(
                filePath,
                img,
                savePath,
                quality,
                new Size((int)(img.Width * dimension / 100), (int)(img.Height * dimension / 100)),
                type
                    );
            img.Dispose();
        }

        private void CompressImage(string filePath, string savePath, int quality, int sizeX, int sizeY, SupportedMimeType type)
        {
            Bitmap img = Helper.GetBitmap(filePath);
            if (sizeY == 0)
                sizeY = img.Size.Height * sizeX / img.Size.Width;

            this.CompressImage(
                filePath,
                img,
                savePath,
                quality,
                new Size(sizeX, sizeY),
                type
                    );
            img.Dispose();
        }


        private void CompressImage(string filePath, Bitmap img, string savePath, int quality, Size size, SupportedMimeType type, bool isOnlyForDisplay = true)
        {
            try
            {

                byte[] originalFile = null;
                if (Helper.SavingAsSameMimeType(filePath, type))
                {
                    originalFile = File.ReadAllBytes(filePath);
                }

                ImageCodecInfo imageCodecInfo;

                if (type == SupportedMimeType.JPEG || Helper.IsRaw(filePath))
                    imageCodecInfo = Helper.GetEncoderInfo("image/jpeg");
                else if (type == SupportedMimeType.PNG)
                    imageCodecInfo = Helper.GetEncoderInfo("image/png");
                else
                    imageCodecInfo = Helper.GetEncoderInfoFromOriginalFile(filePath);

                if (imageCodecInfo == null)
                    return;

                EncoderParameters encoderParameters;

                bool keepOriginalSize = false;
                long OriginalFileSize = Helper.GetFileSize(filePath);
                if (img.Size.Height <= size.Height || img.Size.Width <= size.Width)
                {
                    size = img.Size;
                    keepOriginalSize = true;
                }

                //Bitmap imgCompressed = new Bitmap(img, size);


                Bitmap imgCompressed = new Bitmap(size.Width, size.Height);
                using (Graphics gr = Graphics.FromImage(imgCompressed))
                {
                    gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    gr.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
                }

                /*if(isOnlyForDisplay)
                    imgCompressed.SetResolution(96.0F, 96.0F);*/

                foreach (var id in img.PropertyIdList)
                {
                    imgCompressed.SetPropertyItem(img.GetPropertyItem(id));
                }
                img.Dispose();

                
                if (quality > GetQualityIfCompressed(imgCompressed))
                    quality = GetQualityIfCompressed(imgCompressed); //don't save higher qulaity than required.

                //SetImageComments(imgCompressed, quality); if set, is shown on various places, which is a bit too annoying

                encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] =
                    new EncoderParameter(Encoder.Quality, quality);

                string fileSavePath = Helper.ChangeExensionToMimeType(savePath, type);
                imgCompressed.Save(fileSavePath, imageCodecInfo, encoderParameters);

                imgCompressed.Dispose();

                if (fileSavePath.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (quality < 100)
                        Helper.CompressPNG(fileSavePath, quality, true);
                    Helper.OptimizePNG(fileSavePath, true);
                }
                else if (fileSavePath.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) || fileSavePath.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase))
                {
                    Helper.MakeJpegProgressive(fileSavePath, true);
                }

                if (keepOriginalSize && Helper.SavingAsSameMimeType(filePath,type) && Helper.GetFileSize(fileSavePath) > OriginalFileSize)
                {
                    File.WriteAllBytes(fileSavePath, originalFile);
                }
            }
            catch
            {
                //ignore, generic GDI+ error in rare case while generating preview.
            }
        }

        public void SetImageComments(Bitmap bmp, int quality)
        {
            string newVal = "Mass Image Compressor Compressed this image. https://sourceforge.net/projects/icompress/ with Quality:" + quality;
            try
            {
                PropertyItem propItem;

                if (bmp.PropertyIdList.Contains(0x9286))
                    propItem = bmp.GetPropertyItem(0x9286);
                else
                {
                    propItem = (PropertyItem)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                    propItem.Id = 0x9286;
                }


                propItem.Len = newVal.Length + 1;

                // This header indicates that the comment is in plain ASCII.
                //byte[] header = { 0x41, 0x53, 0x43, 0x49, 0x49, 0x00, 0x00, 0x00 };
                byte[] newValb = System.Text.Encoding.UTF8.GetBytes(newVal + "\0");

                // Merge the two byte arrays.
                /*byte[] c = new byte[header.Length + newValb.Length];
                System.Buffer.BlockCopy(header, 0, c, 0, header.Length);
                System.Buffer.BlockCopy(newValb, 0, c, header.Length, newValb.Length);*/

                propItem.Value = newValb;
                propItem.Type = 2;
                bmp.SetPropertyItem(propItem);
            }
            catch (Exception)
            {
                //Console.WriteLine("Exception in SetUserComments: " + ex.Message);
            }
        }

        public int GetQualityIfCompressed(Bitmap bmp)
        {
            try
            {
                PropertyItem propItem;

                if (bmp.PropertyIdList.Contains(0x9286))
                    propItem = bmp.GetPropertyItem(0x9286);
                else
                    return 100;

                string comment = System.Text.Encoding.UTF8.GetString(propItem.Value);

                if(comment.Contains('\0'))
                    comment = string.Join("",comment.Split('\0'));

                string[] splits = comment.Split(':');
                if (splits.Length > 1)
                {
                    return int.Parse(splits[2]);
                }
                else
                    return 100;
            }
            catch
            {
                //ignore
            }
            return 100; //default to 100 in case of error.
        }



        #endregion
    }
}
