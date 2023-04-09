using System;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

namespace OpenUGD.Barcode
{
    public class WebCameraOptions
    {
        public BarcodeFormat Format { get; set; } = BarcodeFormat.QR_CODE;
        public int RequestWidth { get; set; } = Screen.width;
        public int RequestHeight { get; set; } = Screen.height;
        public bool AutoRotate { get; set; } = true;
        public bool TryInverted { get; set; } = true;
        public string DeviceName { get; set; }
        public int RequestFPS { get; set; } = 30;
        public TimeSpan DecodeInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public BarcodePreviewMaterialPreset MaterialPreset { get; set; }
        public Barcode.RequestCamera RequestCamera { get; set; } = Barcode.RequestCamera.Any;

        public WebCameraOptions CopySizeFrom(WebCameraOptions options)
        {
            RequestWidth = options.RequestWidth;
            RequestHeight = options.RequestHeight;
            return this;
        }

        public WebCameraOptions CopySizeFrom(RawImage image)
        {
            var rect = image.rectTransform.rect;
            RequestWidth = (int)rect.width;
            RequestHeight = (int)rect.height;
            return this;
        }
    }
}