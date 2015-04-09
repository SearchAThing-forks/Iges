﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Iges.Directory;

namespace IxMilia.Iges.Entities
{
    public class IgesColorDefinition : IgesEntity
    {
        public override IgesEntityType EntityType { get { return IgesEntityType.ColorDefinition; } }

        // properties
        public double RedIntensity { get; set; }
        public double GreenIntensity { get; set; }
        public double BlueIntensity { get; set; }
        public string Name { get; set; }

        public IgesColorDefinition()
            : this(1.0, 1.0, 1.0, null)
        {
        }

        public IgesColorDefinition(double r, double g, double b, string name = null)
            : base()
        {
            this.LineCount = 1;
            this.SubordinateEntitySwitchType = IgesSubordinateEntitySwitchType.Independent;
            this.EntityUseFlag = IgesEntityUseFlag.Definition;
            this.RedIntensity = r;
            this.GreenIntensity = g;
            this.BlueIntensity = b;
            this.Name = name;
        }

        protected override void ReadParameters(List<string> parameters)
        {
            this.RedIntensity = Double(parameters, 0);
            this.GreenIntensity = Double(parameters, 1);
            this.BlueIntensity = Double(parameters, 2);
            this.Name = String(parameters, 3);
        }

        protected override void WriteParameters(List<object> parameters)
        {
            parameters.Add(this.RedIntensity);
            parameters.Add(this.GreenIntensity);
            parameters.Add(this.BlueIntensity);
            parameters.Add(this.Name);
        }

        internal override void OnAfterRead(IgesDirectoryData directoryData)
        {
            Debug.Assert(SubordinateEntitySwitchType == IgesSubordinateEntitySwitchType.Independent);
            Debug.Assert(EntityUseFlag == IgesEntityUseFlag.Definition);
        }
    }
}