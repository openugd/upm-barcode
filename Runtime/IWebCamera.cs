using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.Barcode
{
    public interface IWebCamera : IDisposable
    {
        /// <summary>
        /// It returns the current texture of the camera
        /// </summary>
        WebCamTexture Texture { get; }

        /// <summary>
        /// It returns a snapshot of the current frame by RawImage UVRect
        /// </summary>
        /// <param name="rawImage"></param>
        /// <returns></returns>
        Texture2D Snapshot(RawImage rawImage);

        /// <summary>
        /// It returns a snapshot of web camera texture
        /// </summary>
        /// <returns></returns>
        Texture2D Snapshot();

        /// <summary>
        /// It sets texture to the RawImage and applies UVRect
        /// </summary>
        /// <param name="rawImage"></param>
        IDisposable Bind(RawImage rawImage);

        /// <summary>
        /// It tries to decode until it finds a barcode or cancellation token is cancelled
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DecodeResult> DecodeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// It tries to decode until it finds a barcode or cancellation token is cancelled, but uses only a part of the image with the given rect
        /// </summary>
        /// <param name="rawImage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DecodeResult> DecodeAsync(RawImage rawImage, CancellationToken cancellationToken = default);
    }
}