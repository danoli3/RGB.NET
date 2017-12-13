﻿using System.Collections.Generic;
using RGB.NET.Core;

namespace RGB.NET.Devices.CoolerMaster
{
    /// <summary>
    /// Contains all the hardware-id mappings for CoolerMaster devices.
    /// </summary>
    internal static class CoolerMasterMouseLedMappings
    {
        #region Properties & Fields

        /// <summary>
        /// Contains all the hardware-id mappings for CoolerMaster devices.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly Dictionary<CoolerMasterDevicesIndexes, Dictionary<LedId, (int row, int column)>> Mapping =
            new Dictionary<CoolerMasterDevicesIndexes, Dictionary<LedId, (int row, int column)>>
            {
                { CoolerMasterDevicesIndexes.MasterMouse_L, new Dictionary<LedId, (int row, int column)>
                  {
                    { LedId.Mouse1, (0,0) },
                    { LedId.Mouse2, (1,0) },
                    { LedId.Mouse3, (2,0) },
                    { LedId.Mouse4, (3,0) },
                  }
                },

                { CoolerMasterDevicesIndexes.MasterMouse_S, new Dictionary<LedId, (int row, int column)>
                  {
                    { LedId.Mouse1, (0,0) },
                    { LedId.Mouse2, (1,0) },
                    { LedId.Mouse3, (2,0) },
                    { LedId.Mouse4, (3,0) },
                  }
                },
            };

        #endregion
    }
}
