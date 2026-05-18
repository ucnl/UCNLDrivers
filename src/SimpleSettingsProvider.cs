
namespace UCNLDrivers
{    
    public abstract class SimpleSettingsProvider<T> where T : SimpleSettingsContainer, new()
    {
        #region Properties

        public T Data { get; set; }
        public bool isSwallowExceptions { get; set; }

        #endregion

        #region Constructor

        protected SimpleSettingsProvider()
        {
            isSwallowExceptions = true;
        }

        #endregion

        #region Methods

        public abstract void Save(string fileName);

        public abstract void Load(string fileName);

        #endregion
    }
}
