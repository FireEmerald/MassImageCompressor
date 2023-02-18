using System;
using System.Collections.Generic;
using System.Text;

namespace NeoFotonCommon
{
	public class ImageCompressorPresenter : IImageCompressorPresenter
	{
		private ImageCompressor imgCompressorLogic = new ImageCompressor();

		public IImgCompressorView View { get; set; }


		public string GetPreviewImage(List<string> previewedImages, bool? next)
		{
			string message = string.Empty;
			string opImagePath 
				= imgCompressorLogic.CompressDirectoryPreview(
							View.InputDirPath,
							View.QualityCompression,
							ref message,
							View.FixedHeight,
							View.Height,
                            View.FileSize,
                            View.mimeTypeToSave,
                            previewedImages,
                            next);

			View.Message = message;
			return opImagePath;
		}

        public int CompressDirectory(string dirPath, ReportProgress progress, bool overwrite)
		{
            if(overwrite)
                return imgCompressorLogic.CompressDirectory(
                            dirPath,
                            dirPath,
                            View.QualityCompression,
                            View.FixedHeight,
                            View.Height,
                            View.FileSize,
                            View.mimeTypeToSave,
                            progress);

			return imgCompressorLogic.CompressDirectory(
                        dirPath,
						View.OutputDirPath,
						View.QualityCompression,
						View.FixedHeight,
						View.Height,
                        View.FileSize,
                        View.mimeTypeToSave,
                        progress);
		}
	}
}
