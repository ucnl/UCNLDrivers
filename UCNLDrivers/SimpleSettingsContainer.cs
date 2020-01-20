using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCNLDrivers
{
    [Serializable]
    public abstract class SimpleSettingsContainer 
    {
        public SimpleSettingsContainer()
        {
            SetDefaults();
        }

        #region Methods

        public abstract void SetDefaults();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\r\n");

            var fields = this.GetType().GetFields();

            foreach (var item in fields)
            {

                if (item.FieldType.IsArray)
                {
                    sb.AppendFormat("-- {0}: ", item.Name);
                    foreach (var aItem in (Array)item.GetValue(this))
                    {
                        sb.AppendFormat("{0}, ", aItem);
                    }
                    sb.Append("\r\n");
                }
                else
                    sb.AppendFormat("-- {0}: {1}\r\n", item.Name, item.GetValue(this));
            }

            return sb.ToString();

        }

        #endregion
    }
}
