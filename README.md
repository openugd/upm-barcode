# Barcode decoder for Unity

**You can decode barcodes in Unity using this library.**

**It uses a ZXing library for decoding barcodes.**

Support for all platforms that support **UnityEngine.WebCamTexture**.

---

Simple way to decode barcodes from camera and preview.
```csharp
using System;
using System.Threading.Tasks;
using OpenUGD.Barcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class Example : MonoBehaviour
{
   public RawImage rawImage;

    async void Start()
    {
        if(rawImage == null)
            rawImage = GetComponent<RawImage>();
        
        var barcode = await Barcode.DecodeFromCameraAndPreviewAsync(rawImage);
        Debug.Log(barcode);
    }
}
```