using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace JanetterApiKeyInjector
{
    class PointlessDataSource : IStaticDataSource
    {
        byte[] _data = null;

        public PointlessDataSource(byte[] data)
        {
            _data = data;
        }

        public System.IO.Stream GetSource()
        {
            return new System.IO.MemoryStream(_data);
        }
    }
}
