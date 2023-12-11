/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.IO;
using Aas = AasCore.Aas3_0;

namespace AasOpcUaServer
{
    public class AasUaPackageFileHandler
    {
        private AdminShellPackageEnv _package = null;
        private Aas.IFile _file = null;

        private class PackageFileHandle
        {
            public UInt32 handle = 0;
            public UInt64 filepos = 0;
            public Stream packStream = null;

            public int UaMode = 0;
            public bool IsRead = true;
            public bool IsWrite = true;

            public PackageFileHandle() { }
            public PackageFileHandle(UInt32 handle)
            {
                this.handle = handle;
            }
        }

        private Dictionary<UInt32, PackageFileHandle> handles = new Dictionary<uint, PackageFileHandle>();

        public AasUaPackageFileHandler(AdminShellPackageEnv package, Aas.IFile file)
        {
            this._package = package;
            this._file = file;
        }

        private UInt32 GetMaxHandle()
        {
            UInt32 mh = 0;
            foreach (var x in handles.Keys)
                mh = Math.Max(x, mh);
            return mh;
        }

        public UInt32 Open(Byte mode)
        {
            // access file handle
            if (this._package == null || this._file == null)
                throw new InvalidOperationException("no admin-shell package or file");
            var nh = GetMaxHandle() + 1;
            var fh = new PackageFileHandle(nh);
            fh.packStream = _package.GetLocalStreamFromPackage(_file.Value);
            if (fh.packStream == null)
                throw new InvalidOperationException("no admin-shell package or file");
            handles.Add(nh, fh);

            // care about the particularities of write
            fh.UaMode = mode;
            fh.IsRead = ((mode & 0x01) > 0);
            fh.IsWrite = ((mode & 0x02) > 0);
            if ((mode & 0x04) > 0)
            {
                // Erase existing
                fh.packStream.Seek(0, SeekOrigin.Begin);
                fh.packStream.SetLength(0);
                fh.packStream.Flush();
            }
            if ((mode & 0x08) > 0)
            {
                // Append
                fh.packStream.Seek(0, SeekOrigin.End);
                fh.packStream.Flush();
            }

            // done
            return nh;
        }

        public long GetLength(UInt32 handle)
        {
            if (!handles.ContainsKey(handle))
                throw new InvalidOperationException("handle is unknown");
            var h = handles[handle];
            return h.packStream.Length;
        }

        public void Close(UInt32 handle)
        {
            if (!handles.ContainsKey(handle))
                throw new InvalidOperationException("handle is unknown");
            handles.Remove(handle);
        }

        public void SetPosition(UInt32 handle, UInt64 pos)
        {
            if (!handles.ContainsKey(handle))
                throw new InvalidOperationException("handle is unknown");
            var h = handles[handle];
            h.filepos = pos;
        }

        public UInt64 GetPosition(UInt32 handle)
        {
            if (!handles.ContainsKey(handle))
                throw new InvalidOperationException("handle is unknown");
            if (this._package == null || this._file == null)
                throw new InvalidOperationException("no admin-shell package or file");
            var h = handles[handle];
            return h.filepos;
        }

        public Byte[] Read(UInt32 handle, UInt32 readlen)
        {
            if (!handles.ContainsKey(handle))
                throw new InvalidOperationException("handle is unknown");
            if (this._package == null || this._file == null)
                throw new InvalidOperationException("no admin-shell package or file");
            var h = handles[handle];
            if (h.packStream == null)
                throw new InvalidOperationException("no open stream");
            if (h.filepos >= (ulong)h.packStream.Length)
                throw new InvalidOperationException("invalid file position");

            // Finding: 4.194.304 Bytes as requested by the UA Expert is too much
            // Reduce zu 16bit Size, to show suitablility also for small devices
            readlen = Math.Min(readlen, (2 << 15) - 1);

            byte[] res = new byte[readlen];
            h.packStream.Seek((long)h.filepos, SeekOrigin.Begin);
            var redd = h.packStream.Read(res, 0, (int)readlen);
            h.filepos += (UInt64)redd;
            return res;
        }

        public void Write(UInt32 handle, Byte[] data)
        {
            if (!handles.ContainsKey(handle))
                throw new InvalidOperationException("handle is unknown");
            if (this._package == null || this._file == null)
                throw new InvalidOperationException("no admin-shell package or file");
            var h = handles[handle];
            if (h.packStream == null)
                throw new InvalidOperationException("no open stream");

            h.packStream.Write(data, 0, data.Length);
            h.filepos += (ulong)data.Length;
        }
    }

    public class AasUaEntityFileType : AasUaBaseEntity
    {
        private class InstanceData
        {
            public AasUaPackageFileHandler packHandler = null;
            public MethodState mOpen, mClose, mRead, mWrite, mSetPosition, mGetPosition;
            public PropertyState<UInt16> nodeOpenCount = null;
            public PropertyState<UInt64> nodeSize = null;

            public void UpdateSize(UInt64 size)
            {
                if (nodeSize != null)
                    nodeSize.Value = size;
            }
        }

        public AasUaEntityFileType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as they shall be all there in the UA constants
        }

        /// <summary>
        /// Checks, if a FileType node is advisable for representing an AASX file ..
        /// Shall be TRUE for local, existing files ..
        /// </summary>
        /// <returns></returns>
        public bool CheckSuitablity(AdminShellPackageEnv package, Aas.IFile file)
        {
            // trivial
            if (package == null || file == null)
                return false;

            // try get a stream ..
            Stream s = null;
            try
            {
                s = package.GetLocalStreamFromPackage(file.Value);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return false;
            }

            // ok?
            return s != null;
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            AdminShellPackageEnv package = null, Aas.IFile file = null)
        {
            // access
            if (parent == null)
                return null;

            // for sake of complexity, differentiate early
            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(
                            parent, mode, "File", ReferenceTypeIds.HasComponent, ObjectTypeIds.FileType);
                return o;
            }
            else
            {
                if (package == null || file == null)
                    return null;

                var instData = new InstanceData();
                instData.packHandler = new AasUaPackageFileHandler(package, file);

                // containing element
                var o = this.entityBuilder.CreateAddObject(
                            parent, mode, "File", ReferenceTypeIds.HasComponent, ObjectTypeIds.FileType);


                // this first information is to provide a "off-the-shelf" size information; a Open() will re-new this
                var fileLen = Convert.ToUInt64(package.GetStreamSizeFromPackage(file.Value));

                // populate attributes from the spec
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "MimeType",
                    DataTypeIds.String, file.ContentType, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);
                instData.nodeOpenCount = this.entityBuilder.CreateAddPropertyState<UInt16>(o, mode, "OpenCount",
                    DataTypeIds.UInt16, 0, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);
                instData.nodeSize = this.entityBuilder.CreateAddPropertyState<UInt64>(o, mode, "Size",
                    DataTypeIds.UInt64, fileLen, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType,
                    valueRank: -1);
                this.entityBuilder.CreateAddPropertyState<bool>(o, mode, "UserWritable",
                    DataTypeIds.Boolean, true, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);
                this.entityBuilder.CreateAddPropertyState<bool>(o, mode, "Writable",
                    DataTypeIds.Boolean, true, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);

                // Open
                instData.mOpen = this.entityBuilder.CreateAddMethodState(o, mode, "Open",
                    inputArgs: new[] {
                        new Argument("Mode", DataTypeIds.Byte, -1, "")
                    },
                    outputArgs: new[] {
                        new Argument("FileHandle", DataTypeIds.UInt32, -1, "")
                    }, referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    methodDeclarationId: MethodIds.FileType_Open, onCalled: this.OnMethodCalled);

                this.entityBuilder.AddNodeStateAnnotation(instData.mOpen, instData);

                // Close
                instData.mClose = this.entityBuilder.CreateAddMethodState(o, mode, "Close",
                    inputArgs: new[] {
                        new Argument("FileHandle", DataTypeIds.UInt32, -1, "")
                    },
                    outputArgs: null,
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    methodDeclarationId: MethodIds.FileType_Close, onCalled: this.OnMethodCalled);

                this.entityBuilder.AddNodeStateAnnotation(instData.mClose, instData);

                // Read
                instData.mRead = this.entityBuilder.CreateAddMethodState(o, mode, "Read",
                    inputArgs: new[] {
                        new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                        new Argument("Length", DataTypeIds.Int32, -1, "")
                    },
                    outputArgs: new[] {
                        new Argument("Data", DataTypeIds.ByteString, -1, "")
                    }, referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    methodDeclarationId: MethodIds.FileType_Read, onCalled: this.OnMethodCalled);

                this.entityBuilder.AddNodeStateAnnotation(instData.mRead, instData);

                // Write
                instData.mWrite = this.entityBuilder.CreateAddMethodState(o, mode, "Write",
                    inputArgs: new[] {
                        new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                        new Argument("Data", DataTypeIds.ByteString, -1, "")
                    },
                    outputArgs: null,
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    methodDeclarationId: MethodIds.FileType_Write, onCalled: this.OnMethodCalled);

                this.entityBuilder.AddNodeStateAnnotation(instData.mWrite, instData);

                // GetPosition
                instData.mGetPosition = this.entityBuilder.CreateAddMethodState(o, mode, "GetPosition",
                    inputArgs: new[] {
                        new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                    },
                    outputArgs: new[] {
                        new Argument("Position", DataTypeIds.UInt64, -1, "")
                    },
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    methodDeclarationId: MethodIds.FileType_GetPosition, onCalled: this.OnMethodCalled);

                this.entityBuilder.AddNodeStateAnnotation(instData.mGetPosition, instData);

                // SetPosition
                instData.mSetPosition = this.entityBuilder.CreateAddMethodState(o, mode, "SetPosition",
                    inputArgs: new[] {
                        new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                        new Argument("Position", DataTypeIds.UInt64, -1, "")
                    },
                    outputArgs: null,
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent,
                    methodDeclarationId: MethodIds.FileType_SetPosition, onCalled: this.OnMethodCalled);

                this.entityBuilder.AddNodeStateAnnotation(instData.mSetPosition, instData);

                // result
                return o;
            }
        }

        private ServiceResult OnMethodCalled(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            var instData = this.entityBuilder.FindNoteStateAnnotation<InstanceData>(method);
            if (instData == null || instData.packHandler == null)
                return new ServiceResult(StatusCodes.BadInvalidArgument);

            try
            {
                if (method == instData.mOpen)
                {
                    var fh = instData.packHandler.Open((byte)inputArguments[0]);
                    instData.UpdateSize(Convert.ToUInt64(instData.packHandler.GetLength(fh)));
                    outputArguments[0] = fh;
                }

                if (method == instData.mClose)
                {
                    instData.packHandler.Close((UInt32)inputArguments[0]);
                }

                if (method == instData.mSetPosition)
                {
                    instData.packHandler.SetPosition((UInt32)inputArguments[0], (UInt64)inputArguments[1]);
                }

                if (method == instData.mGetPosition)
                {
                    outputArguments[0] = instData.packHandler.GetPosition((UInt32)inputArguments[0]);
                }

                if (method == instData.mRead)
                {
                    var data = instData.packHandler.Read(
                                Convert.ToUInt32(inputArguments[0]), Convert.ToUInt32(inputArguments[1]));
                    outputArguments[0] = data;
                }

                if (method == instData.mWrite)
                {
                    var fh = Convert.ToUInt32(inputArguments[0]);
                    instData.packHandler.Write(fh, (byte[])inputArguments[1]);
                    instData.UpdateSize(Convert.ToUInt64(instData.packHandler.GetLength(fh)));
                }
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.Error(ex, "The method could not be executed.");

                // treat every exception the same
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }

            return new ServiceResult(StatusCodes.Good);
        }

    }
}
