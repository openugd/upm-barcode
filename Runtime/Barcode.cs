using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

namespace OpenUGD.Barcode
{
    /// <summary>
    /// Unity tools to decode barcodes
    /// </summary>
    public static class Barcode
    {
        public enum RequestCamera
        {
            Any = 0,
            FrontFacing = 1,
            RearFacing = 2,
        }

        private static IWebCamera _currentWebCamera;

        /// <summary>
        /// Decodes barcode from texture, it work async on all platforms except WebGL
        /// </summary>
        /// <param name="texture">Must be a Texture2D with read/write option or a WebCamTexture</param>
        /// <param name="format">Default QR CODE</param>
        /// <param name="autoRotate">Auto rotates image to decodes it</param>
        /// <param name="tryInverted">Tries to invert it</param>
        /// <param name="cancellationToken">Does nothing on WebGL</param>
        /// <exception cref="Exception">It throws exception on cancel or other errors</exception>
        /// <returns>It returns a result of decoding</returns>
        public static Task<DecodeResult> DecodeAsync(
            Texture texture
            , BarcodeFormat format = BarcodeFormat.QR_CODE
            , bool autoRotate = true
            , bool tryInverted = true
            , CancellationToken cancellationToken = default
        )
        {
            if (!(texture is Texture2D || texture is WebCamTexture))
                throw new ArgumentException("Texture must be Texture2D or WebCamTexture");

            // It checks if the cancellation token is default, it creates a new one with 60 seconds
            if (cancellationToken == default)
                cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

            Color32[] pixels = null;
            if (texture is Texture2D texture2D)
                pixels = texture2D.GetPixels32();
            else if (texture is WebCamTexture webCamTexture)
                pixels = webCamTexture.GetPixels32();

            return DecodeAsync(
                pixels: pixels
                , width: texture.width
                , height: texture.height
                , format: format
                , autoRotate: autoRotate
                , tryInverted: tryInverted
                , cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Decodes barcode from texture, it work async on all platforms except WebGL
        /// </summary>
        /// <param name="pixels">Array of pixel-colors</param>
        /// <param name="width">width of pixels</param>
        /// <param name="height">height of pixels</param>
        /// <param name="format">Default QR CODE</param>
        /// <param name="autoRotate">Auto rotates image to decodes it</param>
        /// <param name="tryInverted">Tries to invert it</param>
        /// <param name="cancellationToken">Does nothing on WebGL</param>
        /// <exception cref="Exception">It throws exception on cancel or other errors</exception>
        /// <returns>It returns a result of decoding</returns>
        public static Task<DecodeResult> DecodeAsync(
            Color32[] pixels
            , int width
            , int height
            , BarcodeFormat format = BarcodeFormat.QR_CODE
            , bool autoRotate = true
            , bool tryInverted = true
            , CancellationToken cancellationToken = default
        )
        {
            var reader = new BarcodeReader
            {
                AutoRotate = autoRotate,
                Options =
                {
                    TryInverted = tryInverted,
                    PossibleFormats = new[]
                    {
                        format
                    }
                }
            };

            DecodeResult DecodeFromTexture()
            {
                return new DecodeResult(
                    decode: reader.Decode(
                        rawColor32: pixels,
                        width: width,
                        height: height
                    )
                );
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return Task.FromResult(DecodeFromTexture());

            return Task.Run(DecodeFromTexture, cancellationToken);
        }

        /// <summary>
        /// It is a very simple to uses, it tries to decode from camera and preview it to RawImage
        /// </summary>
        /// <param name="rawImage"></param>
        /// <param name="format"></param>
        /// <param name="autoRotate"></param>
        /// <param name="tryInverted"></param>
        /// <param name="requestCamera"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Exception">It throws exception on cancel or other errors</exception>
        /// <returns></returns>
        public static async Task<DecodeResult> DecodeFromCameraAndPreviewAsync(
            RawImage rawImage
            , BarcodeFormat format = BarcodeFormat.QR_CODE
            , bool autoRotate = true
            , bool tryInverted = true
            , RequestCamera requestCamera = RequestCamera.Any
            // If you want to cancel the decoding, default is 60 seconds
            , CancellationToken cancellationToken = default
        )
        {
            // It checks if the cancellation token is default, it creates a new one with 60 seconds
            if (cancellationToken == default)
                cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

            // It gets the current camera
            IWebCamera camera = await WebCamera(
                option: options =>
                {
                    options.Format = format;
                    options.AutoRotate = autoRotate;
                    options.TryInverted = tryInverted;
                    options.RequestCamera = requestCamera;

                    var rect = rawImage.rectTransform.rect;
                    options.RequestWidth = (int)rect.width;
                    options.RequestHeight = (int)rect.height;
                },
                cancellationToken: cancellationToken
            );

            if (cancellationToken.IsCancellationRequested)
            {
                camera.Dispose();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // It previews the camera texture to the RawImage
            var bindPreview = camera.Bind(rawImage);

            // It decodes the camera texture
            DecodeResult result = await camera.DecodeAsync(cancellationToken);

            bindPreview.Dispose();

            // It disposes the camera
            camera.Dispose();

            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }

        /// <summary>
        /// Creates a new WebCamera and starts it, 
        /// </summary>
        /// <param name="option">Options to create WebCamera</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception">It throws exception on cancel or other errors</exception>
        public static async Task<IWebCamera> WebCamera(
            Action<WebCameraOptions> option = null
            , CancellationToken cancellationToken = default
        )
        {
            // check if the camera is already created
            if (_currentWebCamera != null)
                throw new InvalidOperationException("WebCamera is already created");

            var options = new WebCameraOptions();

            // setup options
            if (option != null) option(options);

            if (options.MaterialPreset == null)
            {
                var preset = await Resources.LoadAsync<BarcodePreviewMaterialPreset>("BarcodePreviewMaterialPreset")
                    .AsTask();
                options.MaterialPreset = preset.asset as BarcodePreviewMaterialPreset;

                if (options.MaterialPreset == null)
                    throw new ArgumentException($"{nameof(options.MaterialPreset)} must be greater than 0");
            }

            // check options
            if (options.DecodeInterval <= TimeSpan.Zero)
                throw new ArgumentException($"{nameof(options.DecodeInterval)} must be greater than 0");

            if (options.RequestWidth <= 0)
                throw new ArgumentException($"{nameof(options.RequestWidth)} must be greater than 0");

            if (options.RequestHeight <= 0)
                throw new ArgumentException($"{nameof(options.RequestHeight)} must be greater than 0");

            // check if the user has accepted the camera request
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                // request camera
                await Application.RequestUserAuthorization(UserAuthorization.WebCam).AsTask();
            }

            // throw if cancel 
            cancellationToken.Register(OnDispose);
            cancellationToken.ThrowIfCancellationRequested();

            // throw if not accept
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                throw new Exception("User has not accepted the camera request");

            // creates a camera and starts it
            _currentWebCamera = new WebCameraImpl(
                options: options
                , onDispose: OnDispose
                , cancellationToken: cancellationToken
            ).Start();

            void OnDispose()
            {
                _currentWebCamera = null;
            }

            return _currentWebCamera;
        }

        class WebCameraImpl : IWebCamera
        {
            private readonly Action _onDispose;
            private readonly WebCameraOptions _options;
            private WebCamTexture _webCamTexture;
            private bool _isDisposed;
            private List<RawImage> _bind;
            private List<RawImage> _tempBind;
            private CancellationTokenSource _cancellationTokenSource;
            private EveryFrameUpdate _everyFrame;
            private bool _isFrontFacing;
            private Color32[] _tempPixels;

            public WebCamTexture Texture => _webCamTexture;

            public WebCameraImpl(
                WebCameraOptions options
                , Action onDispose
                , CancellationToken cancellationToken
            )
            {
                _bind = new List<RawImage>();
                _tempBind = new List<RawImage>();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _options = options;
                _onDispose = onDispose;
            }

            public IWebCamera Start()
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                string deviceName = _options.DeviceName;
                if (string.IsNullOrWhiteSpace(deviceName) && _options.RequestCamera != RequestCamera.Any)
                {
                    List<WebCamDevice> devices = new List<WebCamDevice>();
                    foreach (var device in WebCamTexture.devices)
                    {
                        if (device.isFrontFacing && _options.RequestCamera == RequestCamera.FrontFacing)
                            devices.Add(device);
                        else if (!device.isFrontFacing && _options.RequestCamera == RequestCamera.RearFacing)
                            devices.Add(device);
                    }

                    if (devices.Count != 0)
                    {
                        deviceName = devices.Where(c => c.kind == WebCamKind.WideAngle).Select(x => x.name)
                            .FirstOrDefault();
                        if (string.IsNullOrWhiteSpace(deviceName))
                        {
                            deviceName = devices.Where(c => c.kind == WebCamKind.UltraWideAngle).Select(x => x.name)
                                .FirstOrDefault();
                        }

                        if (string.IsNullOrWhiteSpace(deviceName))
                        {
                            deviceName = devices.Where(c => c.kind == WebCamKind.Telephoto).Select(x => x.name)
                                .FirstOrDefault();
                        }

                        if (string.IsNullOrWhiteSpace(deviceName))
                        {
                            deviceName = devices.Select(x => x.name).FirstOrDefault();
                        }
                    }
                }

                _webCamTexture = new WebCamTexture(
                    deviceName: deviceName,
                    requestedWidth: _options.RequestWidth,
                    requestedHeight: _options.RequestHeight,
                    requestedFPS: _options.RequestFPS
                );

                if (_webCamTexture != null)
                    _webCamTexture.Play();

                if (WebCamTexture.devices != null)
                {
                    foreach (var webCamDevice in WebCamTexture.devices)
                    {
                        if (_webCamTexture.deviceName == webCamDevice.name)
                        {
                            _isFrontFacing = webCamDevice.isFrontFacing;
                        }
                    }
                }

                _everyFrame = new GameObject("WebCameraEveryFrame").AddComponent<EveryFrameUpdate>();
                GameObject.DontDestroyOnLoad(_everyFrame.gameObject);
                _everyFrame.OnUpdate = () =>
                {
                    if (!_isDisposed && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _tempBind.Clear();
                        _tempBind.AddRange(_bind);
                        foreach (var rawImage in _tempBind)
                        {
                            PreviewTo(rawImage);
                        }

                        _tempBind.Clear();
                    }
                };

                return this;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;

                    _everyFrame.OnUpdate = null;
                    GameObject.Destroy(_everyFrame.gameObject);
                    _everyFrame = null;

                    _onDispose.Invoke();
                    _bind.Clear();
                    _cancellationTokenSource.Cancel();

                    if (_webCamTexture != null)
                    {
                        var texture = _webCamTexture;
                        _webCamTexture = null;
                        texture.Stop();
                        GameObject.Destroy(texture);
                    }
                }
            }

            public Texture2D Snapshot(RawImage rawImage)
            {
                if (_isDisposed)
                    throw new Exception("WebCamera is already disposed");

                return ReadTexture2DFromRawImage(rawImage);
            }

            public Texture2D Snapshot()
            {
                if (_isDisposed)
                    throw new Exception("WebCamera is already disposed");

                // Создаем временный RenderTexture
                RenderTexture rt = RenderTexture.GetTemporary(_webCamTexture.width, _webCamTexture.height, 0,
                    RenderTextureFormat.Default);
                RenderTexture.active = rt;

                // Рендерим текущий WebCamTexture
                Graphics.Blit(_webCamTexture, rt);

                Texture2D outputTexture = new Texture2D(_webCamTexture.width, _webCamTexture.height,
                    TextureFormat.RGB24, false);

                // Считываем пиксели из RenderTexture в Texture2D
                outputTexture.ReadPixels(new Rect(0, 0, _webCamTexture.width, _webCamTexture.height), 0, 0);
                outputTexture.Apply();

                // Очищаем временный RenderTexture
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                return outputTexture;
            }

            public IDisposable Bind(RawImage rawImage)
            {
                if (_isDisposed)
                    throw new Exception("WebCamera is already disposed");

                _bind.Add(rawImage);
                var disposable = new Disposable(() => { _bind.Remove(rawImage); });
                _cancellationTokenSource.Token.Register(disposable.Dispose);
                PreviewTo(rawImage);
                return disposable;
            }

            public async Task<DecodeResult> DecodeAsync(CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new Exception("WebCamera is already disposed");

                // It checks if the cancellation token is default, it creates a new one with 60 seconds
                if (cancellationToken == default)
                    cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

                var internalCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token
                    , cancellationToken
                );

                while (!_isDisposed && !internalCancellation.IsCancellationRequested)
                {
                    var result = await Barcode.DecodeAsync(
                        _webCamTexture
                        , format: _options.Format
                        , autoRotate: _options.AutoRotate
                        , tryInverted: _options.TryInverted
                        , cancellationToken: internalCancellation.Token
                    );

                    if (result.Success)
                        return result;

                    await _options.DecodeInterval.DelayAsync(cancellationToken: internalCancellation.Token);
                }

                throw new OperationCanceledException();
            }

            public async Task<DecodeResult> DecodeAsync(RawImage rawImage,
                CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                    throw new Exception("WebCamera is already disposed");

                // It checks if the cancellation token is default, it creates a new one with 60 seconds
                if (cancellationToken == default)
                    cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

                var internalCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token
                    , cancellationToken
                );

                while (!_isDisposed && !internalCancellation.IsCancellationRequested)
                {
                    var (status, pixels, resultWidth, resultHeight) =
                        await ReadPixelsFromRawImage(rawImage, _tempPixels);
                    internalCancellation.Token.ThrowIfCancellationRequested();
                    if (status)
                    {
                        _tempPixels = pixels;
                        var result = await Barcode.DecodeAsync(
                            pixels: pixels
                            , width: resultWidth
                            , height: resultHeight
                            , format: _options.Format
                            , autoRotate: _options.AutoRotate
                            , tryInverted: _options.TryInverted
                            , cancellationToken: internalCancellation.Token
                        );

                        if (result.Success)
                            return result;
                    }

                    await _options.DecodeInterval.DelayAsync(cancellationToken: internalCancellation.Token);
                }

                throw new OperationCanceledException();
            }

            private void PreviewTo(RawImage rawImage)
            {
                Material material;
                Rect sourceRect;

                Rect rawImageTransformRect = rawImage.rectTransform.rect;
                Rect targetRect = new Rect(0, 0, rawImageTransformRect.width, rawImageTransformRect.height);
                bool iOS = Application.platform == RuntimePlatform.IPhonePlayer;
                int rotation = -_webCamTexture.videoRotationAngle;

                if (Math.Abs(rotation) == 90 || Math.Abs(rotation) == 270)
                {
                    sourceRect = new Rect(0, 0, _webCamTexture.height, _webCamTexture.width);
                    material = _options.MaterialPreset.Clock90Left;
                }
                else
                {
                    sourceRect = new Rect(0, 0, _webCamTexture.width, _webCamTexture.height);
                    material = _options.MaterialPreset.Default;
                }

                float sourceAspect = sourceRect.width / sourceRect.height;
                float targetAspect = targetRect.width / targetRect.height;

                float newWidth, newHeight;

                if (sourceAspect > targetAspect)
                {
                    newWidth = targetAspect / sourceAspect;
                    newHeight = 1;
                }
                else
                {
                    newWidth = 1;
                    newHeight = sourceAspect / targetAspect;
                }

                float newX = (1 - newWidth) * 0.5f;
                float newY = (1 - newHeight) * 0.5f;

                Vector2 uvScale;
                Vector2 uvOffset;

                if (rotation == 90 || rotation == 270)
                {
                    uvScale = new Vector2(newHeight, newWidth);
                    uvOffset = new Vector2(newY, newX);
                }
                else
                {
                    uvScale = new Vector2(newWidth, newHeight);
                    uvOffset = new Vector2(newX, newY);
                }

                if ((iOS && !_webCamTexture.videoVerticallyMirrored) ||
                    (!iOS && _webCamTexture.videoVerticallyMirrored))
                {
                    uvScale.y *= -1;
                    uvOffset.y = 1 - uvOffset.y - uvScale.y;
                }

                if (iOS)
                {
                    uvOffset.x = 1 - uvOffset.x;
                    uvScale.x *= -1;
                }

                if (_isFrontFacing)
                {
                    uvOffset.x = 1 - uvOffset.x;
                    uvScale.x *= -1;
                }

                rawImage.material = material;
                rawImage.texture = _webCamTexture;
                rawImage.uvRect = new Rect(uvOffset, uvScale);
            }

            private async Task<(
                bool status
                , Color32[] pixels
                , int resultWidth
                , int resultHeight
                )> ReadPixelsFromRawImage(RawImage rawImage, Color32[] pixels)
            {
                int sourceWidth = _webCamTexture.width;
                int sourceHeight = _webCamTexture.height;

                var rect = rawImage.rectTransform.rect;
                int targetWidth = (int)rect.width;
                int targetHeight = (int)rect.height;

                Rect uvRect = rawImage.uvRect;

                int x = (int)(sourceWidth * uvRect.x);
                int y = (int)(sourceHeight * uvRect.y);
                int width = (int)(sourceWidth * uvRect.width);
                int height = (int)(sourceHeight * uvRect.height);

                if (uvRect.width < 0)
                {
                    x = (int)(sourceWidth * (1 - uvRect.x));
                    width = (int)(sourceWidth * (uvRect.width * -1));
                }

                // Учитываем отражение для iOS
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    x = sourceWidth - x - width;
                }

                if (
                    x < 0
                    || y < 0
                    || width <= 0
                    || height <= 0
                    || x >= sourceWidth
                    || y >= sourceHeight
                    || width > sourceWidth
                    || height > sourceHeight
                    || x + width > sourceWidth
                    || y + height > sourceHeight
                )
                {
                    return (status: false, pixels: null, resultWidth: 0, resultHeight: 0);
                }

                var colors = _webCamTexture.GetPixels(x, y, width, height);
                if (colors == null)
                {
                    return (status: false, pixels: null, resultWidth: 0, resultHeight: 0);
                }

                if (pixels == null || pixels.Length != colors.Length)
                {
                    pixels = new Color32[colors.Length];
                }

                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    for (var i = 0; i < colors.Length; i++)
                    {
                        pixels[i] = colors[i];
                    }
                }
                else
                {
                    await Task.Run(() =>
                    {
                        for (var i = 0; i < colors.Length; i++)
                        {
                            pixels[i] = colors[i];
                        }
                    });
                }

                return (status: true, pixels: pixels, resultWidth: width, resultHeight: height);
            }

            private Texture2D ReadTexture2DFromRawImage(RawImage rawImage)
            {
                int sourceWidth = _webCamTexture.width;
                int sourceHeight = _webCamTexture.height;

                var rect = rawImage.rectTransform.rect;
                int targetWidth = (int)rect.width;
                int targetHeight = (int)rect.height;

                Rect uvRect = rawImage.uvRect;

                int x = (int)(sourceWidth * uvRect.x);
                int y = (int)(sourceHeight * uvRect.y);
                int width = (int)(sourceWidth * uvRect.width);
                int height = (int)(sourceHeight * uvRect.height);

                if (uvRect.width < 0)
                {
                    x = (int)(sourceWidth * (1 - uvRect.x));
                    width = (int)(sourceWidth * (uvRect.width * -1));
                }

                // Учитываем отражение для iOS
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    x = sourceWidth - x - width;
                }

                // Создаем временный RenderTexture
                RenderTexture rt =
                    RenderTexture.GetTemporary(sourceWidth, sourceHeight, 0, RenderTextureFormat.Default);
                RenderTexture.active = rt;

                // Рендерим текущий WebCamTexture
                Graphics.Blit(_webCamTexture, rt);

                Texture2D outputTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

                // Считываем пиксели из RenderTexture в Texture2D
                outputTexture.ReadPixels(new Rect(x, y, width, height), 0, 0);
                outputTexture.Apply();

                // Очищаем временный RenderTexture
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                return outputTexture;
            }
        }

        class EveryFrameUpdate : MonoBehaviour
        {
            public Action OnUpdate;
            private void Update() => OnUpdate?.Invoke();
        }

        class Disposable : IDisposable
        {
            private Action _action;

            public Disposable(Action action) => _action = action;

            public void Dispose()
            {
                if (_action != null)
                {
                    var action = _action;
                    _action = null;
                    action();
                }
            }
        }
    }
}