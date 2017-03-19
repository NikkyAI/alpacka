using System;
using System.Security.Cryptography;

namespace GitMC.Lib.Net
{
    internal class MD5Transform : IDisposable, ICryptoTransform
    {
        private readonly IncrementalHash _incrementalHash =
            IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        
        public byte[] Hash { get; private set; }
        
        public void Dispose()
        {
            _incrementalHash.Dispose();
            GC.SuppressFinalize(this);
        }
        
        // ICryptoTransform implementation
        
        public int InputBlockSize => 1;
        public int OutputBlockSize => 1;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => true;
        
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount,
                                  byte[] outputBuffer, int outputOffset)
        {
            _incrementalHash.AppendData(inputBuffer, inputOffset, inputCount);
            if ((outputBuffer != null) && ((inputBuffer != outputBuffer) || (inputOffset != outputOffset)))
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
            return inputCount;
        }
        
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            _incrementalHash.AppendData(inputBuffer, inputOffset, inputCount);
            Hash = _incrementalHash.GetHashAndReset();
            byte[] outputBytes;
            if (inputCount != 0) {
                outputBytes = new byte[inputCount];
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBytes, 0, inputCount);
            } else outputBytes = Array.Empty<byte>();
            return outputBytes;
        }
    }
}
