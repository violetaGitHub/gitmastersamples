using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace DatalayerFs.Serializers
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RepositoryRow
    {
        internal uint Id;
        internal uint ModuleId;
        internal uint ParentModuleId;
        internal string Name;
        internal uint OwnerId;
        internal long GuidHigh;
        internal long GuidLow;

        internal RepositoryRow(
            uint id,
            uint moduleId,
            uint parentModuleId,
            string name,
            uint ownerId,
            long guidHigh,
            long guidLow)
        {
            Id = id;
            ModuleId = moduleId;
            ParentModuleId = parentModuleId;
            Name = name;
            OwnerId = ownerId;
            GuidHigh = guidHigh;
            GuidLow = guidLow;
        }

        public static bool operator ==(RepositoryRow x, RepositoryRow y)
        {
            return x.Id == y.Id
                && x.GuidHigh == y.GuidHigh
                && x.GuidLow == y.GuidLow;
        }

        public static bool operator !=(RepositoryRow x, RepositoryRow y)
        {
            return !(x == y);
        }

        internal static readonly RepositoryRow Empty = new RepositoryRow();
    }

    internal static class RepositorySerializer
    {
        internal static void Write(RepositoryRow repository, string path)
        {
            using (BinaryWriter writer = IOBuilder.CreateBinaryWriter(path, false))
            {
                writer.Write(repository.Id);
                writer.Write(repository.ModuleId);
                writer.Write(repository.ParentModuleId);
                writer.Write(repository.Name);
                writer.Write(repository.OwnerId);
                writer.Write(repository.GuidHigh);
                writer.Write(repository.GuidLow);
            }
        }

        internal static IEnumerable<RepositoryRow> ReadAll(string path)
        {
            if (!System.IO.File.Exists(path))
                yield break;

            RepositoryRow row = new RepositoryRow();

            using (BinaryReader reader = IOBuilder.CreateBinaryReader(path))
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Read(ref row, reader);
                yield return row;
            }
        }

        internal static RepositoryRow Find(
            Func<RepositoryRow, bool> condition, string path)
        {
            if (!System.IO.File.Exists(path))
                return RepositoryRow.Empty;

            RepositoryRow row = new RepositoryRow();

            using (BinaryReader reader = IOBuilder.CreateBinaryReader(path))
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Read(ref row, reader);
                if (condition != null && condition(row))
                    return row;
            }

            return RepositoryRow.Empty;
        }

        static void Read(ref RepositoryRow row, BinaryReader reader)
        {
            row.Id = reader.ReadUInt32();
            row.ModuleId = reader.ReadUInt32();
            row.ParentModuleId = reader.ReadUInt32();
            row.Name = reader.ReadString();
            row.OwnerId = reader.ReadUInt32();
            row.GuidHigh = reader.ReadInt64();
            row.GuidLow = reader.ReadInt64();
        }
    }
}
