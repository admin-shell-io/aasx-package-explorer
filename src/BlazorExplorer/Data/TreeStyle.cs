/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

namespace MW.Blazor
{
    public class TreeStyle
    {
        public static readonly TreeStyle Bootstrap = new TreeStyle
        {
            ExpandNodeIconClass = "far fa-plus-square curosr-pointer",
            CollapseNodeIconClass = "far fa-minus-square curosr-pointer",
            NodeTitleClass = "p-1 curosr-pointer",
            NodeTitleSelectedClass = "bg-secondary text-white"
        };

        public string ExpandNodeIconClass { get; set; }
        public string CollapseNodeIconClass { get; set; }
        public string NodeTitleClass { get; set; }
        public string NodeTitleSelectedClass { get; set; }
    }
}
