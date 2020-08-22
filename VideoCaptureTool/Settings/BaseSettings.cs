using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VideoCaptureTool.Settings
{
    public abstract class BaseSettings<SettingType> where SettingType : class, new()
    {
        protected static string filename = "settings.xml";
        protected static readonly object padlock = new object();
        protected static bool loadingSettings = false;
        protected static SettingType settings;
        protected static SettingType Settings
        {
            get
            {
                if (settings == null)
                {
                    lock (padlock)
                    {
                        //place all init of the settings here
                        settings = new SettingType();
                        LoadSettings();
                    }
                }
                return settings;
            }
            private set
            {
                if (value != null)
                {
                    lock (padlock)
                    {
                        settings = value;
                    }
                }
            }
        }      

        protected readonly string version = "0.0.1";
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                return;
            }
        }

        public string AppName { get; set; }

        //-------------------------
        //Functions
        //-------------------------
        protected BaseSettings(string appName)
        {
            if (String.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentNullException("Settings : appName is Null");
            }
            AppName = appName;
        }
        static public SettingType GetSettings()
        {
            return Settings;
        }

        static protected void LoadSettings()
        {
            Type T = Settings.GetType();
            System.Xml.Serialization.XmlSerializer serializer = new
            System.Xml.Serialization.XmlSerializer(T);

            System.IO.FileStream fs = null;


            // A FileStream is needed to read the XML document.
            try
            {
                fs = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            }
            catch (System.IO.FileNotFoundException fex)
            {
                //file not found. create file by saving current(probably defaults) and then load it
                SaveSettings();
                fs = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            }
            catch (Exception e)
            {
                return;
            }
            if (fs != null)
            {
                try
                {
                    XmlReader reader = XmlReader.Create(fs);

                    // Use the Deserialize method to restore the object's state.
                    lock (padlock)
                    {
                        loadingSettings = true;
                        try
                        {
                            var loadedSettings = serializer.Deserialize(reader);
                            if (loadedSettings.GetType() == Settings.GetType())
                                Settings = Cast(loadedSettings, T);
                            else
                            {
                                throw new ArgumentException("Settings wrong type!");
                            }
                        }
                        catch (Exception e)
                        {
                            loadingSettings = false;
                            //error loading settings. we'll have to remake the file with the default settings :)
                            fs.Close();
                            System.IO.File.Delete(filename);
                            SaveSettings();
                        }
                    }

                    fs.Close();
                }
                catch (Exception e)
                {
                    fs.Close();
                }
            }
            loadingSettings = false;
            return;
        }
        static private dynamic Cast(dynamic obj, Type castTo)
        {
            return Convert.ChangeType(obj, castTo);
        }
        static protected void SaveSettings()
        {
            if (settings == null)
                settings = new SettingType();

            if (loadingSettings)
                return;

            System.Xml.Serialization.XmlSerializer writer = null;
            try
            {
                writer = new System.Xml.Serialization.XmlSerializer(Settings.GetType());
            }
            catch (Exception e)
            {
                return;
            }
            System.IO.FileStream file = System.IO.File.Create(filename);
            writer.Serialize(file, settings);
            file.Close();
        }
    }
}
