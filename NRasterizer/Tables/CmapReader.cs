﻿using System;
using System.Collections.Generic;
using System.IO;

namespace NRasterizer.Tables
{
    internal class CmapReader
    {
        private static UInt16[] ReadUInt16Array(BinaryReader input, int length)
        {
            var result = new UInt16[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = input.ReadUInt16();
            }
            return result;
        }

        private static CharacterMap ReadCharacterMap(CMapEntry entry, BinaryReader input)
        {
            // I want to thank Microsoft for not giving a simple count on the glyphIdArray
            long tableStart = input.BaseStream.Position;

            var format = input.ReadUInt16();
            var length = input.ReadUInt16();
            if (format == 4)
            {
                var version = input.ReadUInt16();
                var segCountX2 = input.ReadUInt16();
                var searchRange = input.ReadUInt16();
                var entrySelector = input.ReadUInt16();
                var rangeShift = input.ReadUInt16();
                
                var segCount = segCountX2 / 2;

                var endCode = ReadUInt16Array(input, segCount); // last = 0xffff. What does that mean??

                input.ReadUInt16(); // Reserved = 0               

                var startCode = ReadUInt16Array(input, segCount);
                var idDelta = ReadUInt16Array(input, segCount);
                var idRangeOffset = ReadUInt16Array(input, segCount);

                // I want to thank Microsoft for not giving a simple count on the glyphIdArray
                var glyphIdArrayLength = (int)((input.BaseStream.Position - tableStart) / sizeof(UInt16));
                var glyphIdArray = ReadUInt16Array(input, glyphIdArrayLength);

                return new CharacterMap(segCount, startCode, endCode, idDelta, idRangeOffset, glyphIdArray);
            }
            throw new NRasterizerException("Unknown cmap subtable: " + format);
        }

        private class CMapEntry
        {
            private readonly UInt16 _platformId;
            private readonly UInt16 _encodingId;
            private readonly UInt32 _offset;

            public CMapEntry(UInt16 platformId, UInt16 encodingId, UInt32 offset)
            {
                _platformId = platformId;
                _encodingId = encodingId;
                _offset = offset;
            }
            public UInt16 PlatformId { get { return _platformId; } }
            public UInt16 EncodingId { get { return _encodingId; } }
            public UInt32 Offset { get { return _offset; } }
        }

        internal static List<CharacterMap> From(TableEntry table)
        {
            var input = table.GetDataReader();

            var version = input.ReadUInt16(); // 0
            var tableCount = input.ReadUInt16();

            var entries = new List<CMapEntry>(tableCount);
            for (int i = 0; i < tableCount; i++)
            {
                var platformId = input.ReadUInt16();
                var encodingId = input.ReadUInt16();
                var offset = input.ReadUInt32();
                entries.Add(new CMapEntry(platformId, encodingId, offset));
            }

            var result = new List<CharacterMap>(tableCount);
            foreach (var entry in entries)
            {
                var subtable = table.GetDataReader();
                subtable.BaseStream.Seek(entry.Offset, SeekOrigin.Current);
                result.Add(ReadCharacterMap(entry, subtable));
            }

            return result;
        }
    }
}