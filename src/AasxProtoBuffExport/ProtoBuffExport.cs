using System;
using System.Collections.Generic;
using System.IO;
using AdminShellNS;
using static AdminShellNS.AdminShellV20;

namespace AasxProtoBuffExport
{
    public class ProtoBuffExport
    {
        public ProtoBuffExport()
        {

        }

        public void exportProtoFile(string protoFileName, string aasxPackageFileName,
                                    string assetID, Submodel subModel, bool createRestApi)
        {
            string subModelID = subModel.idShort;
            List<String> protoService = new List<String>();
            List<String> protoMsg = new List<String>();

            foreach (AdminShellNS.AdminShell.SubmodelElementWrapper sme in subModel.submodelElements)
            {
                //Operations
                if (sme.submodelElement is AdminShellV20.Operation)
                {
                    AdminShellV20.Operation op = sme.submodelElement as AdminShellV20.Operation;

                    String inVar = "";
                    String outVar = "";
                    int varIndex = 1;
                    //inputVariable
                    if (op.inputVariable.Count > 0)
                    {

                        inVar = "InVar" + assetID + subModelID + op.idShort;
                        protoMsg.Add(
                            "// InputVariablen of Operation " + sme.submodelElement.idShort +
                            " in Submodule " + subModel.idShort + " of Asset " + assetID);
                        protoMsg.Add("message " + inVar + " {");
                        foreach (AdminShellV20.OperationVariable inPut in op.inputVariable)
                        {
                            if (inPut.value.submodelElement is AdminShellV20.Property)
                            {
                                AdminShellV20.Property inProp = inPut.value.submodelElement as AdminShellV20.Property;

                                protoMsg.Add(
                                    "  " + toProtoType(inProp.valueType) + " " + inProp.idShort + " = " +
                                    varIndex + ";");
                            }
                            else
                            {

                                protoMsg.Add("  string " + inPut.value.submodelElement.idShort
                                             + " = " + varIndex + ";");

                            }
                            varIndex++;
                        }
                        protoMsg.Add("}\n\n");
                    }
                    else
                    {
                        inVar = "google.protobuf.Empty";
                    }
                    //outputVariable
                    outVar = "OutVar" + assetID + subModelID + op.idShort;
                    protoMsg.Add(
                        "// outputVariablen of Operation " + sme.submodelElement.idShort +
                        " in Submodule " + subModel.idShort + " of Asset " + assetID);
                    protoMsg.Add("message " + outVar + " {");
                    varIndex = 1;
                    foreach (AdminShellV20.OperationVariable output in op.outputVariable)
                    {
                        AdminShellV20.Property outProp = output.value.submodelElement as AdminShellV20.Property;
                        protoMsg.Add("  " + toProtoType(outProp.valueType) + " " + outProp.idShort
                                     + " = " + varIndex + ";");
                        varIndex++;
                    }
                    protoMsg.Add("}\n\n");

                    String probufEntry = "  rpc " + op.idShort + " (" + inVar + ") returns (" + outVar + ")";

                    if (createRestApi)
                    {
                        probufEntry +=
                            "{ \n    option (google.api.http) = {\n      post: \"/" + assetID
                            + "/" + subModel.idShort + "/"
                            + op.idShort + "\"\n	  body: \"*\"\n    };\n  }\n";
                    }
                    else
                    {
                        probufEntry += ";";
                    }

                    protoService.Add(probufEntry);
                }
                //Properties
                else if (sme.submodelElement is AdminShellV20.Property)
                {
                    AdminShellV20.Property prop = sme.submodelElement as AdminShellV20.Property;

                    String propVar = "PropVar" + assetID + subModelID + prop.idShort;
                    protoMsg.Add("// Variablen of Property " + sme.submodelElement.idShort + " in Submodul "
                                 + subModel.idShort + " of Asset " + assetID);
                    protoMsg.Add("message " + propVar + " {");
                    protoMsg.Add("  " + toProtoType(prop.valueType) + " " + prop.idShort + " = 1;");
                    protoMsg.Add("}\n\n");

                    //Getter
                    String probufEntry = "  rpc Get_" + prop.idShort + " (google.protobuf.Empty)"
                        +" returns (" + propVar + ")";
                    if (createRestApi)
                    {
                        probufEntry += "{ \n    option (google.api.http) = {\n      get: \"/" + assetID + "/" +
                            subModel.idShort + "/" + prop.idShort + "\"\n    };\n  }\n";
                    }
                    else
                    {
                        probufEntry += ";";
                    }
                    protoService.Add(probufEntry);


                    //Setter
                    if (!prop.category.ToUpper().Equals("CONSTANT"))
                    {
                        probufEntry = "  rpc Set_" + prop.idShort + " (" + propVar + ")  "
                            + "returns (google.protobuf.Empty)";
                        if (createRestApi)
                        {

                            probufEntry +=
                               "{ \n    option (google.api.http) = {\n      post: \"/" +
                               assetID + "/" + subModel.idShort + "/" + prop.idShort +
                               "\"\n	  body: \"*\"\n    };\n  }\n";
                        }
                        else
                        {
                            probufEntry += ";";
                        }
                        protoService.Add(probufEntry);
                    }
                }
            }

            using (var s = new StreamWriter(protoFileName))
            {
                s.WriteLine("/*############################################################");
                s.WriteLine("###### AASX IDL .proto-File from AASX Package Explorer  ######");
                s.WriteLine("###### Generated from {0} at {1}  ######", aasxPackageFileName,
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
                s.WriteLine("############################################################*/");
                s.WriteLine("syntax = \"proto3\";");
                s.WriteLine("");
                s.WriteLine("option csharp_namespace = \"FL4." + assetID + "\";");

                s.WriteLine("");

                s.WriteLine("package FL4." + assetID + ";");

                s.WriteLine("");
                if (createRestApi)
                {
                    s.WriteLine("import \"google/api/annotations.proto\";");
                }
                s.WriteLine("import \"google/protobuf/empty.proto\";");
                s.WriteLine("");
                s.WriteLine("");
                s.WriteLine("// The Service definitions for Asset " + assetID + " Submodel " + subModel.idShort);
                s.WriteLine("service " + subModel.idShort + "{\n");

                foreach (String probufEntry in protoService)
                {
                    s.WriteLine(probufEntry);
                }
                s.WriteLine("}\n");
                foreach (String probufEntry in protoMsg)
                {
                    s.WriteLine(probufEntry);
                }
            }
        }

        private string toProtoType(string valueType)
        {
            switch (valueType)
            {
                case "anyType": return "string";
                case "complexType": return "string";
                case "anySimpleType": return "string";
                case "anyAtomicType": return "string";
                case "anyURI": return "string";
                case "base64Binary": return "bytes";
                case "boolean": return "bool";
                case "date": return "string";
                case "dateTime": return "string";
                case "dateTimeStamp": return "string";
                case "decimal": return "string";
                case "integer": return "int32";
                case "long": return "int64";
                case "int": return "int32";
                case "short": return "string";
                case "byte": return "bytes";
                case "nonNegativeInteger": return "int32";
                case "positiveInteger": return "int32";
                case "unsignedLong": return "int64";
                case "unsignedShort": return "int32";
                case "unsignedByte": return "bytes";
                case "nonPositiveInteger": return "uint32";
                case "negativeInteger": return "uint32";
                case "double": return "double";
                case "duration": return "duration";
                case "dayTimeDuration": return "duration";
                case "yearMonthDuration": return "duration";
                case "float": return "float";
                case "hexBinary": return "string";
                case "string": return "string";
                case "langString": return "string";
                case "time": return "string";
                default: return "string";
            }
        }
    }
}
