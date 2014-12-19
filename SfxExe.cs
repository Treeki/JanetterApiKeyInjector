using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JanetterApiKeyInjector
{
    class SfxExe
    {
        public byte[] Preamble, Zip;

        public SfxExe(Stream inputExe)
        {
            // Read the whole EXE into RAM (it shouldn't be THAT big...!)
            long exeSize = inputExe.Length - inputExe.Position;
            if (exeSize > (32 * 1024 * 1024))
            {
                throw new Exception("Input EXE is larger than 32MB; something seems wrong here!");
            }

            byte[] exe = new byte[exeSize];
            inputExe.Read(exe, 0, (int)exeSize);

            int zipPosition = FindZipHeader(exe);
            if (zipPosition == -1)
            {
                throw new Exception("Could not locate the embedded .zip file inside the EXE. Make sure you picked JanetterSrv.exe and not Janetter.exe!");
            }

            // Now split this up into two arrays
            Preamble = new byte[zipPosition];
            Array.Copy(exe, 0, Preamble, 0, Preamble.Length);

            Zip = new byte[exe.Length - zipPosition];
            Array.Copy(exe, zipPosition, Zip, 0, Zip.Length);
        }

        int FindZipHeader(byte[] exe)
        {
            // Search for something that looks like a ZIP header.
            // This is rather crude, but works for Janetter 4.3.0.2 at least

            // In 4.3.0.2, the zip is at offset 0x26400, and there's 0xDA
            // bytes worth of padding preceding it. This signifies that the
            // zip may always start at a position aligned to 0x100 bytes
            // - which may help to reduce false positives - but I haven't
            // verified this.

            for (int i = 0; i < (exe.Length - 3); i++)
            {
                if (exe[i] == 0x50 && exe[i + 1] == 0x4B &&
                    exe[i + 2] == 0x03 && exe[i + 3] == 0x04)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Write(Stream stream)
        {
            stream.Write(Preamble, 0, Preamble.Length);
            stream.Write(Zip, 0, Zip.Length);
        }
    }
}
