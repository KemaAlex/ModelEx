using System;
using System.IO;
using System.Collections.Generic;
using CDC.Objects.Models;

namespace CDC.Objects
{
    public class GexFile : SRFile
    {
        public const UInt32 RETAIL_VERSION = 0x3C20413B;

        public GexFile(String strFileName, CDC.Objects.ExportOptions options)
            : base(strFileName, Game.Gex, options)
        {
        }

        protected override void ReadHeaderData(BinaryReader xReader, CDC.Objects.ExportOptions options)
        {
            _dataStart = 0;

            // Could use unit version number instead of thing below.
            // Check that's what SR2 does.
            //xReader.BaseStream.Position = m_uDataStart + 0xF0;
            //UInt32 unitVersionNumber = xReader.ReadUInt32();
            //if (unitVersionNumber != 0x3C20413B)

            // Moved to ResolvePointers due to not knowing how else to tell.
            //xReader.BaseStream.Position = 0x00000000;
            //if (xReader.ReadUInt32() == 0x00000000)
            //{
            //    m_eFileType = FileType.Unit;
            //}
            //else
            //{
            //    m_eFileType = FileType.Object;
            //}
        }

        protected override void ReadObjectData(BinaryReader xReader, CDC.Objects.ExportOptions options)
        {
            // Object name
            xReader.BaseStream.Position = _dataStart + 0x00000024;
            xReader.BaseStream.Position = _dataStart + xReader.ReadUInt32();
            String strModelName = new String(xReader.ReadChars(8));
            _name = Utility.CleanName(strModelName);

            // Texture type
            if (options.ForcedPlatform == CDC.Platform.None)
            {
                _platform = Platform.PSX;
            }
            else
            {
                _platform = options.ForcedPlatform;
            }

            // Model data
            xReader.BaseStream.Position = _dataStart + 0x00000008;
            _modelCount = xReader.ReadUInt16();
            _animCount = xReader.ReadUInt16();
            _modelStart = _dataStart + xReader.ReadUInt32();
            _animStart = _dataStart + xReader.ReadUInt32();

            _models = new GexModel[_modelCount];
            Platform ePlatform = _platform;
            for (UInt16 m = 0; m < _modelCount; m++)
            {
                Console.WriteLine(string.Format("Debug: reading object model {0} / {1}", m, (_modelCount - 1)));
                _models[m] = GexObjectModel.Load(xReader, _dataStart, _modelStart, _name, _platform, m, _version, options);
            }
            if (options.ForcedPlatform == CDC.Platform.None)
            {
                _platform = ePlatform;
            }
        }

        protected override void ReadUnitData(BinaryReader xReader, CDC.Objects.ExportOptions options)
        {
            bool validVersion = false;

            if (options.ForcedPlatform != Platform.None)
            {
                _platform = options.ForcedPlatform;
            }

            if (!validVersion)
            {
                xReader.BaseStream.Position = _dataStart + 0xF0;
                _version = xReader.ReadUInt32();
                //if (_version == RETAIL_VERSION)
                //{
                    validVersion = true;
                //}
            }

            if (!validVersion)
            {
                throw new Exception("Wrong version number for level x");
            }

            // Adjacent units are seperate from portals.
            // There can be multiple portals to the same unit.
            // Portals
            xReader.BaseStream.Position = _dataStart;
            UInt32 m_uConnectionData = _dataStart + xReader.ReadUInt32(); // Same as m_uModelData?
            xReader.BaseStream.Position = m_uConnectionData + 0x30;
            xReader.BaseStream.Position = _dataStart + xReader.ReadUInt32();
            portalCount = xReader.ReadUInt32();
            _portalNames = new String[portalCount];
            for (int i = 0; i < portalCount; i++)
            {
                String strUnitName = new String(xReader.ReadChars(12));
                _portalNames[i] = Utility.CleanName(strUnitName);
                xReader.BaseStream.Position += 0x50;
            }

            // Instances
            xReader.BaseStream.Position = _dataStart + 0x78;

            _instanceCount = xReader.ReadUInt32();
            _instanceStart = _dataStart + xReader.ReadUInt32();
            _instanceNames = new String[_instanceCount];
            for (int i = 0; i < _instanceCount; i++)
            {
                xReader.BaseStream.Position = _instanceStart + 0x4C * i;
                String strInstanceName = new String(xReader.ReadChars(8));
                _instanceNames[i] = Utility.CleanName(strInstanceName);
            }

            // Instance types
            xReader.BaseStream.Position = _dataStart + 0x8C;
            _instanceTypeStart = _dataStart + xReader.ReadUInt32();
            xReader.BaseStream.Position = _instanceTypeStart;
            List<String> xInstanceList = new List<String>();
            while (xReader.ReadByte() != 0xFF)
            {
                xReader.BaseStream.Position--;
                String strInstanceTypeName = new String(xReader.ReadChars(8));
                xInstanceList.Add(Utility.CleanName(strInstanceTypeName));
                xReader.BaseStream.Position += 0x08;
            }
            _instanceTypeNames = xInstanceList.ToArray();

            // Unit name
            xReader.BaseStream.Position = _dataStart + 0x98;
            xReader.BaseStream.Position = _dataStart + xReader.ReadUInt32();
            String strModelName = new String(xReader.ReadChars(8));
            _name = Utility.CleanName(strModelName);

            // Texture type
            bool handledSpecificVersion = false;
            if (options.ForcedPlatform == CDC.Platform.None)
            {
                _platform = Platform.PSX;
            }

            // Connected unit list. (unreferenced?)
            //xReader.BaseStream.Position = m_uDataStart + 0xC0;
            //m_uConnectedUnitsStart = m_uDataStart + xReader.ReadUInt32() + 0x08;
            //xReader.BaseStream.Position = m_uConnectedUnitsStart;
            //xReader.BaseStream.Position += 0x18;
            //String strUnitName0 = new String(xReader.ReadChars(16));
            //strUnitName0 = strUnitName0.Substring(0, strUnitName0.IndexOf(','));
            //xReader.BaseStream.Position += 0x18;
            //String strUnitName1 = new String(xReader.ReadChars(16));
            //strUnitName1 = strUnitName1.Substring(0, strUnitName1.IndexOf(','));
            //xReader.BaseStream.Position += 0x18;
            //String strUnitName2 = new String(xReader.ReadChars(16));
            //strUnitName2 = strUnitName2.Substring(0, strUnitName2.IndexOf(','));

            // Model data
            xReader.BaseStream.Position = _dataStart;
            _modelCount = 1;
            _modelStart = _dataStart;
            _models = new GexModel[_modelCount];
            xReader.BaseStream.Position = _modelStart;
            UInt32 m_uModelData = _dataStart + xReader.ReadUInt32();

            // Material data
            Console.WriteLine("Debug: reading area model 0");
            _models[0] = GexUnitModel.Load(xReader, _dataStart, m_uModelData, _name, _platform, _version, options);

            //if (m_axModels[0].Platform == Platform.Dreamcast ||
            //    m_axModels[1].Platform == Platform.Dreamcast)
            //{
            //    m_ePlatform = Platform.Dreamcast;
            //}
        }

        protected override void ResolvePointers(BinaryReader xReader, BinaryWriter xWriter)
        {
            UInt32 uDataStart = ((xReader.ReadUInt32() >> 9) << 11) + 0x00000800;
            if (xReader.ReadUInt32() == 0x00000000)
            {
                _asset = Asset.Unit;
            }
            else
            {
                _asset = Asset.Object;
            }

            xReader.BaseStream.Position = uDataStart;
            xWriter.BaseStream.Position = 0;

            xReader.BaseStream.CopyTo(xWriter.BaseStream);
        }
    }
}