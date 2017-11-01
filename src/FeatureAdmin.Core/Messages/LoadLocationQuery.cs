﻿using FeatureAdmin.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeatureAdmin.Core.Messages
{
    public class LoadLocationQuery
    {
        public LoadLocationQuery(SPLocation spLocation)
        {
            SPLocation = spLocation;
        }
        public SPLocation SPLocation { get; private set; }
    }
}
