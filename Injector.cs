using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace JanetterApiKeyInjector
{
    class Injector : IDisposable
    {
        SfxExe _sfxExe = null;
        MemoryStream _zipStream = null;
        ZipFile _zipFile = null;
        byte[] _twapiBefore = null, _twapiAfter = null;

        public string[] ConsumerKeys;
        public string[] ConsumerSecrets;


        public Injector(Stream sourceStream)
        {
            _sfxExe = new SfxExe(sourceStream);
            
            // This silly dance is because a MemoryStream based upon a
            // byte array is not resizable
            _zipStream = new MemoryStream(_sfxExe.Zip.Length);
            _zipStream.Write(_sfxExe.Zip, 0, _sfxExe.Zip.Length);
            _zipStream.Seek(0, SeekOrigin.Begin);

            _zipFile = new ZipFile(_zipStream);

            // Does this file contain the Twitter API code?
            var entry = _zipFile.GetEntry("twapi.pyo");
            if (entry == null)
                throw new Exception("twapi.pyo not found inside this file, cannot patch");

            var twapiStream = _zipFile.GetInputStream(entry);
            byte[] twapi = new byte[entry.Size];
            twapiStream.Read(twapi, 0, twapi.Length);
            twapiStream.Dispose();

            if (!LoadTwapi(twapi))
                throw new Exception("Unrecognised version of twapi.pyo, cannot patch");
        }


        public void Dispose()
        {
            if (_zipStream != null)
                _zipStream.Dispose();
            _zipStream = null;

            if (_zipFile != null)
                _zipFile.Close();
            _zipFile = null;
        }


        public void Save(Stream output)
        {
            RewriteTwapi();
            _sfxExe.Zip = _zipStream.ToArray();
            _sfxExe.Write(output);
        }


        bool LoadTwapi(byte[] twapi)
        {
            // Validate the header
            byte[] knownHeader = new byte[] {
                0x03, 0xF3, 0x0D, 0x0A, // Python 2.7 magic

                // Don't bother checking for the timestamp, since
                // the latter checks will probably take care of this
                // anyway
                //0xC5, 0xDF, 0x2E, 0x54, // Module timestamp
            };

            if (twapi.Length < 0x200)
                return false;

            for (int i = 0; i < knownHeader.Length; i++)
                if (twapi[i] != knownHeader[i])
                    return false;

            // Extract the bytes of the file preceding this
            _twapiBefore = new byte[0x1D7];
            Array.Copy(twapi, _twapiBefore, _twapiBefore.Length);

            // Now, verify + parse the rest of the data
            using (MemoryStream ms = new MemoryStream(twapi))
            {
                // These two strings directly precede the API keys, and are
                // used as a little safety check:
                // If we can read them successfully AND read 6 other strings
                // following these, then it's presumed these are the API keys.

                ms.Seek(0x1BD, SeekOrigin.Begin);
                string verifyA = ReadMarshalString(ms);
                string verifyB = ReadMarshalString(ms);

                if (verifyA != "get_host" || verifyB != "get_port")
                    return false;


                ConsumerKeys = new string[3];
                ConsumerSecrets = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    ConsumerKeys[i] = ReadMarshalString(ms);
                    ConsumerSecrets[i] = ReadMarshalString(ms);
                }


                _twapiAfter = new byte[ms.Length - ms.Position];
                ms.Read(_twapiAfter, 0, _twapiAfter.Length);
            }

            return true;
        }


        void RewriteTwapi()
        {
            byte[] twapi;

            // Generate new file
            using (var ms = new MemoryStream())
            {
                ms.Write(_twapiBefore, 0, _twapiBefore.Length);
                for (int i = 0; i < ConsumerKeys.Length; i++)
                {
                    WriteMarshalString(ms, ConsumerKeys[i]);
                    WriteMarshalString(ms, ConsumerSecrets[i]);
                }
                ms.Write(_twapiAfter, 0, _twapiAfter.Length);

                twapi = ms.ToArray();
            }

            // Add it to the zip!
            _zipFile.BeginUpdate();
            _zipFile.Add(new PointlessDataSource(twapi), "twapi.pyo");
            _zipFile.CommitUpdate();
        }


        string ReadMarshalString(Stream stream)
        {
            if (stream.ReadByte() != 0x74)
                throw new Exception("Invalid Python string magic byte");

            byte[] value = new byte[4];
            stream.Read(value, 0, 4);
            uint length = BitConverter.ToUInt32(value, 0);

            // Quick and dirty validity check
            if (length > 0x400)
                throw new Exception("Invalid Python string length");

            byte[] str = new byte[length];
            stream.Read(str, 0, str.Length);

            // I really don't know if this is actually stored as UTF-8 or not,
            // but in the case of these strings it doesn't really matter ;p
            return Encoding.UTF8.GetString(str);
        }

        void WriteMarshalString(Stream stream, string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] length = BitConverter.GetBytes((uint)data.Length);

            stream.WriteByte(0x74);
            stream.Write(length, 0, length.Length);
            stream.Write(data, 0, data.Length);
        }
    }
}
