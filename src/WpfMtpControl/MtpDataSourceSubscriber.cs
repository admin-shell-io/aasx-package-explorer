/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMtpControl
{
    public class MtpDataSourceSubscriber
    {
        public enum ChangeType { Value }

        public class SubscriberItem
        {
            public string DataSourceItemId = null;
            public Type ConvertToType = null;
            public Action<ChangeType, object> ActionLambda = null;
        }

        private Dictionary<string, SubscriberItem> items = new Dictionary<string, SubscriberItem>();

        public void Subscribe<T>(string dataSourceItemId, Action<ChangeType, object> action)
        {
            if (items.ContainsKey(dataSourceItemId))
                items.Remove(dataSourceItemId);

            var si = new SubscriberItem();
            si.DataSourceItemId = dataSourceItemId;
            si.ConvertToType = typeof(T);
            si.ActionLambda = action;

            items[dataSourceItemId] = si;
        }

        public void SubscribeToAmlIdRefWith<T>(
            Aml.Engine.CAEX.AttributeSequence aseq, string attrName, Action<ChangeType, object> action)
        {
            var idRef = MtpAmlHelper.FindAttributeValueByName(aseq, attrName);
            if (idRef != null && idRef.Length > 0)
                this.Subscribe<T>(idRef, action);
        }

        public void Invoke(string dataSourceItemId, ChangeType change, object o)
        {
            if (items.ContainsKey(dataSourceItemId))
            {
                var si = items[dataSourceItemId];
                if (si != null)
                {
                    // try to convert?
                    try
                    {
                        if (si.ConvertToType == typeof(double))
                            o = Convert.ToDouble(o);
                        if (si.ConvertToType == typeof(float))
                            o = Convert.ToSingle(o);
                        if (si.ConvertToType == typeof(int))
                            o = Convert.ToInt32(o);
                        if (si.ConvertToType == typeof(uint))
                            o = Convert.ToUInt32(o);
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);

                        // on any error, simply do not call lambda
                        return;
                    }

                    if (si.ActionLambda != null)
                        si.ActionLambda(change, o);
                }
            }
        }
    }
}
