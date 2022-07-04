using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RSDKv3_4
{
    public static class MD5Hasher
    {
        private static readonly MD5 MD5Provider = MD5.Create();

        public static byte[] GetHash(byte[] data) => MD5Provider.ComputeHash(data);
    }
}
