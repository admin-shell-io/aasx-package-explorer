using AasCore.Aas3_0_RC02;
using AdminShellNS.Display;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendOperation
    {
        #region AasxPackageExplorer

        public static object AddChild(this Operation operation, ISubmodelElement childSubmodelElement, EnumerationPlacmentBase placement = null)
        {
            // not enough information to select list of children?
            var pl = placement as EnumerationPlacmentOperationVariable;
            if (childSubmodelElement == null || pl == null)
                return null;

            // ok, use information
            var ov = new OperationVariable(childSubmodelElement);

            if (childSubmodelElement != null)
                childSubmodelElement.Parent = operation;

            if (pl.Direction == OperationVariableDirection.In)
            {
                operation.InputVariables ??= new List<OperationVariable>();
                operation.InputVariables.Add(ov);
            }

            if (pl.Direction == OperationVariableDirection.Out)
            {
                operation.OutputVariables ??= new List<OperationVariable>();
                operation.OutputVariables.Add(ov);
            }

            if (pl.Direction == OperationVariableDirection.InOut)
            {
                operation.InoutputVariables ??= new List<OperationVariable>();
                operation.InoutputVariables.Add(ov);
            }

            return ov;
        }

        public static EnumerationPlacmentBase GetChildrenPlacement(this Operation operation, ISubmodelElement child)
        {
            // trivial
            if (child == null)
                return null;

            // search
            OperationVariableDirection? dir = null;
            OperationVariable opvar = null;
            if (operation.InputVariables != null)
                foreach (var ov in operation.InputVariables)
                    if (ov?.Value == child)
                    {
                        dir = OperationVariableDirection.In;
                        opvar = ov;
                    }

            if (operation.OutputVariables != null)
                foreach (var ov in operation.OutputVariables)
                    if (ov?.Value == child)
                    {
                        dir = OperationVariableDirection.Out;
                        opvar = ov;
                    }

            if (operation.InoutputVariables != null)
                foreach (var ov in operation.InoutputVariables)
                    if (ov?.Value == child)
                    {
                        dir = OperationVariableDirection.InOut;
                        opvar = ov;
                    }

            // found
            if (!dir.HasValue)
                return null;
            return new EnumerationPlacmentOperationVariable()
            {
                Direction = dir.Value,
                OperationVariable = opvar
            };
        }
        #endregion
    }
}
