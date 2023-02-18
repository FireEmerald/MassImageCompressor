using System;
using System.Collections.Generic;
using System.Text;

namespace NeoFotonCommon
{
    public interface IImgCompressorView
    {
		/// <summary>
		/// Reserved for future Use
		/// </summary>
		bool CompressImage { get; set; }

		/// <summary>
		/// Reserved for future Use
		/// </summary>
		string ImagePath { get; set; }

		string InputDirPath { get; set; }

		int QualityCompression { get; set; }

		bool FixedHeight { get; set; }

		/// <summary>
		///  If FixedHeight is false then Height represents % of current height
		/// </summary>
		int ScaleHeight { get; set; }

        decimal FileSize { get; }

		SupportedMimeType mimeTypeToSave { get; set; }
		
		string OutputDirPath { get; set; }

		string Message { get; set; }

    }
}
