using AdminShellNS.Display;
using System.Collections.Generic;
using System.Linq;

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
                operation.InputVariables ??= new List<IOperationVariable>();
                operation.InputVariables.Add(ov);
            }

            if (pl.Direction == OperationVariableDirection.Out)
            {
                operation.OutputVariables ??= new List<IOperationVariable>();
                operation.OutputVariables.Add(ov);
            }

            if (pl.Direction == OperationVariableDirection.InOut)
            {
                operation.InoutputVariables ??= new List<IOperationVariable>();
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
            IOperationVariable opvar = null;
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
                OperationVariable = opvar as OperationVariable
            };
        }

        public static List<IOperationVariable> GetVars(this Operation op, OperationVariableDirection dir)
        {
            if (dir == OperationVariableDirection.In)
                return op.InputVariables;
            if (dir == OperationVariableDirection.Out)
                return op.OutputVariables;
            return op.InoutputVariables;
        }

        public static List<IOperationVariable> SetVars(
            this Operation op, OperationVariableDirection dir, List<IOperationVariable> value)
        {
            if (dir == OperationVariableDirection.In)
            {
                op.InputVariables = value;
                return op.InputVariables;
            }
            if (dir == OperationVariableDirection.Out)
            {
                op.OutputVariables = value;
                return op.OutputVariables;
            }

            op.InoutputVariables = value;
            return op.InoutputVariables;
        }

        #endregion

        public static Operation UpdateFrom(
            this Operation elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is SubmodelElementCollection srcColl)
            {
                if (srcColl.Value != null)
                {
                    List<OperationVariable> operationVariables = srcColl.Value.Copy().Select(
                        (isme) => new OperationVariable(isme)).ToList();
                    elem.InputVariables = operationVariables.ConvertAll(op => (IOperationVariable)op);
                }

            }

            if (source is SubmodelElementCollection srcList)
            {
                if (srcList.Value != null)
                {
                    List<OperationVariable> operationVariables = srcList.Value.Copy().Select(
                        (isme) => new OperationVariable(isme)).ToList();
                    elem.InputVariables = operationVariables.ConvertAll(op => (IOperationVariable)op);
                }
            }

            return elem;
        }
    }
}
