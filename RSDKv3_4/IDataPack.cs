using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSDKv3_4
{
    public interface IDataPack
    {
        bool FileExists(string fileName);

        byte[] GetFileData(string fileName);

        bool TryGetFileData(string fileName, out byte[] fileData);
    }
}
