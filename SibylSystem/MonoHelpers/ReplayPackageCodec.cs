using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ReplayPackageCodec
{
    public static List<Package> Decode(byte[] buffer)
    {
        List<Package> packages = new List<Package>();
        try
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(buffer)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    Package package = new Package();
                    package.Fuction = reader.ReadByte();
                    package.Data = new BinaryMaster(reader.ReadBytes((int)reader.ReadUInt32()));
                    packages.Add(package);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        return packages;
    }

    public static byte[] Encode(IList<Package> packages)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            for (int i = 0; i < packages.Count; i++)
            {
                Package package = packages[i];
                writer.Write((byte)package.Fuction);
                writer.Write((UInt32)package.Data.getLength());
                writer.Write(package.Data.get());
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
