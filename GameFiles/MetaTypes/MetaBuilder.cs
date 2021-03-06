﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWalker.GameFiles
{
    public class MetaBuilder
    {

        List<MetaBuilderBlock> Blocks = new List<MetaBuilderBlock>();

        int MaxBlockLength = 0x2000; //TODO: figure what this should be!


        public MetaBuilderBlock EnsureBlock(MetaName type)
        {
            foreach (var block in Blocks)
            {
                if (block.StructureNameHash == type)
                {
                    if (block.TotalSize < MaxBlockLength)
                    {
                        return block;
                    }
                }
            }
            MetaBuilderBlock b = new MetaBuilderBlock();
            b.StructureNameHash = type;
            b.Index = Blocks.Count;
            Blocks.Add(b);
            return b;
        }

        public MetaBuilderPointer AddItem<T>(MetaName type, T item) where T : struct
        {
            MetaBuilderBlock block = EnsureBlock(type);
            byte[] data = MetaTypes.ConvertToBytes(item);
            int brem = data.Length % 16;
            if (brem > 0)
            {
                int newlen = data.Length - brem + 16;
                byte[] newdata = new byte[newlen];
                Buffer.BlockCopy(data, 0, newdata, 0, data.Length);
                data = newdata; //make sure item size is multiple of 16... so pointers don't need sub offsets!
            }
            int idx = block.AddItem(data);
            MetaBuilderPointer r = new MetaBuilderPointer();
            r.Block = block.Index + 1;
            r.Offset = (idx * data.Length) / 16;
            r.Length = data.Length;
            return r;
        }
        public MetaBuilderPointer AddItemArray<T>(MetaName type, T[] items) where T : struct
        {
            MetaBuilderBlock block = EnsureBlock(type);
            byte[] data = MetaTypes.ConvertArrayToBytes(items);
            int datalen = data.Length;
            int newlen = datalen;
            int lenrem = newlen % 16;
            if (lenrem != 0)
            {
                newlen += (16 - lenrem);
            }
            byte[] newdata = new byte[newlen];
            Buffer.BlockCopy(data, 0, newdata, 0, datalen);
            int offs = block.TotalSize / 16;
            int idx = block.AddItem(newdata);
            MetaBuilderPointer r = new MetaBuilderPointer();
            r.Block = block.Index + 1;
            r.Offset = offs; //(idx * data.Length) / 16;
            r.Length = items.Length;
            return r;
        }
        public MetaBuilderPointer AddString(string str)
        {
            MetaBuilderBlock block = EnsureBlock(MetaName.STRING);
            byte[] data = Encoding.ASCII.GetBytes(str);
            int datalen = data.Length;
            int newlen = datalen;
            int lenrem = newlen % 16;
            if (lenrem != 0)  //need to pad the data length up to multiple of 16.
            {
                newlen += (16 - lenrem);
            }
            byte[] newdata = new byte[newlen];
            Buffer.BlockCopy(data, 0, newdata, 0, datalen);
            int offs = block.TotalSize / 16;
            int idx = block.AddItem(newdata);
            MetaBuilderPointer r = new MetaBuilderPointer();
            r.Block = block.Index + 1;
            r.Offset = offs;// (idx * data.Length) / 16;//not sure if this is correct! should also use sub-offset!
            r.Length = datalen; //actual length of string.
            return r;
        }

        public MetaPOINTER AddItemPtr<T>(MetaName type, T item) where T : struct //helper method for AddItem<T>
        {
            var ptr = AddItem(type, item);
            return new MetaPOINTER((ushort)ptr.Block, (ushort)ptr.Offset, 0);
        }
        public Array_Structure AddItemArrayPtr<T>(MetaName type, T[] items) where T : struct //helper method for AddItemArray<T>
        {
            if ((items == null) || (items.Length == 0)) return new Array_Structure();
            var ptr = AddItemArray(type, items);
            return new Array_Structure(ptr);
        }
        public Array_uint AddHashArrayPtr(MetaHash[] items)
        {
            if ((items == null) || (items.Length == 0)) return new Array_uint();
            var ptr = AddItemArray(MetaName.HASH, items);
            return new Array_uint(ptr);
        }
        public Array_ushort AddUshortArrayPtr(ushort[] items)
        {
            if ((items == null) || (items.Length == 0)) return new Array_ushort();
            var ptr = AddItemArray(MetaName.USHORT, items);
            return new Array_ushort(ptr);
        }
        public CharPointer AddStringPtr(string str) //helper method for AddString
        {
            var ptr = AddString(str);
            return new CharPointer(ptr);
        }


        public Array_StructurePointer AddPointerArray(MetaPOINTER[] arr)
        {
            if ((arr == null) || (arr.Length == 0)) return new Array_StructurePointer();
            var ptr = AddItemArray(MetaName.POINTER, arr);
            Array_StructurePointer sp = new Array_StructurePointer();
            sp.Count1 = (ushort)arr.Length;
            sp.Count2 = sp.Count1;
            sp.Pointer = ptr.Pointer;
            return sp;
        }

        public Array_StructurePointer AddItemPointerArrayPtr<T>(MetaName type, T[] items) where T : struct
        {
            //helper method for creating a pointer array
            if ((items == null) || (items.Length == 0)) return new Array_StructurePointer();

            MetaPOINTER[] ptrs = new MetaPOINTER[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                ptrs[i] = AddItemPtr(type, items[i]);
            }
            return AddPointerArray(ptrs);

            //Array_StructurePointer sp = new Array_StructurePointer();
            //sp.Count1 = (ushort)items.Length;
            //sp.Count2 = sp.Count1;
            //for (int i = 0; i < items.Length; i++)
            //{
            //    var item = items[i];
            //    var meptr = AddItemPtr(type, item);
            //    var mptr = AddItem(MetaName.POINTER, meptr);
            //    if (i == 0)
            //    {
            //        sp.Pointer = mptr.Pointer; //main pointer points to the first item.
            //    }
            //}
            //return sp;
        }


        public Array_StructurePointer AddWrapperArrayPtr(MetaWrapper[] items)
        {
            if ((items == null) || (items.Length == 0)) return new Array_StructurePointer();


            MetaPOINTER[] ptrs = new MetaPOINTER[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                ptrs[i] = items[i].Save(this);
            }
            return AddPointerArray(ptrs);

            //Array_StructurePointer sp = new Array_StructurePointer();
            //sp.Count1 = (ushort)items.Length;
            //sp.Count2 = sp.Count1;
            //for (int i = 0; i < items.Length; i++)
            //{
            //    var item = items[i];
            //    var meptr = item.Save(this);
            //    var mptr = AddItem(MetaName.POINTER, meptr);
            //    if (i == 0)
            //    {
            //        sp.Pointer = mptr.Pointer; //main pointer points to the first item.
            //    }
            //}
            //return sp;
        }

        public Array_Structure AddWrapperArray(MetaWrapper[] items)
        {
            if ((items == null) || (items.Length == 0)) return new Array_Structure();

            var sa = new Array_Structure();
            sa.Count1 = (ushort)items.Length;
            sa.Count2 = sa.Count1;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var meptr = item.Save(this);
                if (i == 0)
                {
                    MetaBuilderPointer mbp = new MetaBuilderPointer();
                    mbp.Block = meptr.BlockID;
                    mbp.Offset = meptr.ItemOffset;
                    sa.Pointer = mbp.Pointer;
                }
            }
            return sa;
        }


        public byte[] GetData()
        {
            int totlen = 0;
            for (int i = 0; i < Blocks.Count; i++)
            {
                totlen += Blocks[i].TotalSize;
            }
            byte[] data = new byte[totlen];
            int offset = 0;
            for (int i = 0; i < Blocks.Count; i++)
            {
                var block = Blocks[i];
                for (int j = 0; j < block.Items.Count; j++)
                {
                    var bdata = block.Items[j];
                    Buffer.BlockCopy(bdata, 0, data, offset, bdata.Length);
                    offset += bdata.Length;
                }
            }
            if (offset != data.Length)
            { }
            return data;
        }



        Dictionary<MetaName, MetaStructureInfo> StructureInfos = new Dictionary<MetaName, MetaStructureInfo>();
        Dictionary<MetaName, MetaEnumInfo> EnumInfos = new Dictionary<MetaName, MetaEnumInfo>();

        public void AddStructureInfo(MetaName name)
        {
            if (!StructureInfos.ContainsKey(name))
            {
                MetaStructureInfo si = MetaTypes.GetStructureInfo(name);
                if (si != null)
                {
                    StructureInfos[name] = si;
                }
            }
        }
        public void AddEnumInfo(MetaName name)
        {
            if (!EnumInfos.ContainsKey(name))
            {
                MetaEnumInfo ei = MetaTypes.GetEnumInfo(name);
                if (ei != null)
                {
                    EnumInfos[name] = ei;
                }
            }
        }




        public Meta GetMeta()
        {
            Meta m = new Meta();
            m.FileVFT = 0x405bc808;
            m.FileUnknown = 1;
            m.Unknown_10h = 0x50524430;
            m.Unknown_14h = 0x0079;

            m.RootBlockIndex = 1; //assume first block is root. todo: make adjustable?

            m.StructureInfos = new ResourceSimpleArray<MetaStructureInfo>();
            foreach (var si in StructureInfos.Values)
            {
                m.StructureInfos.Add(si);
            }
            m.StructureInfosCount = (short)m.StructureInfos.Count;

            m.EnumInfos = new ResourceSimpleArray<MetaEnumInfo>();
            foreach (var ei in EnumInfos.Values)
            {
                m.EnumInfos.Add(ei);
            }
            m.EnumInfosCount = (short)m.EnumInfos.Count;

            m.DataBlocks = new ResourceSimpleArray<MetaDataBlock>();
            foreach (var bb in Blocks)
            {
                m.DataBlocks.Add(bb.GetMetaDataBlock());
            }
            m.DataBlocksCount = (short)m.DataBlocks.Count;

            return m;
        }


    }


    public class MetaBuilderBlock
    {
        public MetaName StructureNameHash { get; set; }
        public List<byte[]> Items { get; set; } = new List<byte[]>();
        public int TotalSize { get; set; } = 0;
        public int Index { get; set; } = 0;

        public int AddItem(byte[] item)
        {
            int idx = Items.Count;
            Items.Add(item);
            TotalSize += item.Length;
            return idx;
        }

        public uint BasePointer
        {
            get
            {
                return (((uint)Index + 1) & 0xFFF);
            }
        }


        public MetaDataBlock GetMetaDataBlock()
        {
            if (TotalSize <= 0) return null;

            byte[] data = new byte[TotalSize];
            int offset = 0;
            for (int j = 0; j < Items.Count; j++)
            {
                var bdata = Items[j];
                Buffer.BlockCopy(bdata, 0, data, offset, bdata.Length);
                offset += bdata.Length;
            }

            MetaDataBlock db = new MetaDataBlock();
            db.StructureNameHash = StructureNameHash;
            db.DataLength = TotalSize;
            db.Data = data;

            return db;
        }


    }

    public struct MetaBuilderPointer
    {
        public int Block { get; set; } //0-based index
        public int Offset { get; set; } //(byteoffset/16)
        public int Length { get; set; } //for temp use...
        public uint Pointer
        {
            get
            {
                uint bidx = (((uint)Block) & 0xFFF);
                uint offs = (((uint)Offset) & 0xFFFF) << 16;
                return bidx + offs;
            }
        }
    }


}
