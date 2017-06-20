﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCaptureTool.Settings
{
    public sealed class SettingsManager : BaseSettings<SettingsManager>
    {
        public SettingsManager()
        {

        }

        private string appname;
        public string AppName
        {
            get { return "VCT"; }
            set { return; }
        }

        private Boolean keepAspectRatio = true;
        public Boolean KeepAspectRatio
        {
            get { return keepAspectRatio; }
            set 
            { 
                keepAspectRatio = value;
                SaveSettings();
            }
        }

        private Boolean allowStandby = false;
        public Boolean AllowStandby
        {
            get { return allowStandby; }
            set 
            { 
                allowStandby = value;
                SaveSettings();
            }
        }
        
        
    }
}
