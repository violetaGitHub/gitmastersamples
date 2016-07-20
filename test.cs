using System.Collections.Generic;
using System.IO;

namespace DatalayerFs.Serializers
{
    internal class FindByRepositoryId : IReadCondition<RepositoryRow>
    {
        internal FindByRepositoryId(uint id)
        {
            mTargetId = id;
        }

        bool IReadCondition<RepositoryRow>.Matches(ref RepositoryRow current)
        {
            return current.Id == mTargetId; // Improve check
        }

        int mTargetId; // change 2
    }

    internal class FindByRepositoryName : IReadCondition<RepositoryRow>
    {
        internal FindByRepositoryName(string name)
        {
            mTargetName = name;
        }

        bool IReadCondition<RepositoryRow>.Matches(ref RepositoryRow current)
        {
            return current.Name == mTargetName;
        }

        string mTargetName;
    }

    internal static class RepositorySerializer
    {
        internal static List<RepositoryRow> Read(
            IReadCondition<RepositoryRow> condition, string path)
        {
            List<RepositoryRow> result = new List<RepositoryRow>();
            RepositoryRow row = new RepositoryRow();

            using (BinaryReader reader = IOBuilder.CreateBinaryReader(path))
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Read(ref row, reader);
                if (condition == null || condition.Matches(ref row))
                    result.Add(row);
            }
            return result;
        }

        internal static RepositoryRow ReadFirst(
            IReadCondition<RepositoryRow> condition, string path)
        {
            RepositoryRow row = new RepositoryRow();

            using (BinaryReader reader = IOBuilder.CreateBinaryReader(path))
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Read(ref row, reader);
                if (condition == null || condition.Matches(ref row))
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
