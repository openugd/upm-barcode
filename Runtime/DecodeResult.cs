using ZXing;

namespace OpenUGD.Barcode
{
    public class DecodeResult
    {
        private readonly Result _decode;

        public bool Success { get; }
        public string Text => _decode?.Text;
        public Result Result => _decode;

        public DecodeResult(Result decode)
        {
            _decode = decode;
            Success = decode != null;
        }

        public override string ToString()
        {
            if (Success) return Text;
            return "Failed";
        }
    }
}