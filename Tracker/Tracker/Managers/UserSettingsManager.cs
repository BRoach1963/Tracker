using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.DataModels;

namespace Tracker.Managers
{
    public class UserSettingsManager
    {
        #region Fields

        private bool _initialized;

        #endregion

        #region Singleton Instance

        private static UserSettingsManager? _instance;
        private static readonly object SyncRoot = new object();

        public static UserSettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new UserSettingsManager();
                    }
                }

                return _instance;
            }
        }

        public async void Initialize()
        {
            if (_initialized) return;
            LoadSettings();
            _initialized = true;
        }

        public void Shutdown()
        {
            SaveSettingsAndCleanup();
        }

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
             
        }

        private void SaveSettingsAndCleanup()
        {
             
        }

        #endregion
    }
}
