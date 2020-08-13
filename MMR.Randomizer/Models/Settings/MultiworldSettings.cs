using System;
using System.Collections.Generic;
using System.Text;
using MMR.Randomizer.Asm;
using MMR.Randomizer.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Linq;

namespace MMR.Randomizer.Models.Settings
{
    public class MultiworldSettings
    {
        #region Multi

        public string sPlayerCount { get; set; } = "";

        /// <summary>
        /// Is the game a multiworld?
        /// </summary>
        public bool IsMultiworld { get; set; } = false;


        #endregion Multi

        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new WritablePropertiesOnlyResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        static MultiworldSettings()
        {
            _jsonSerializerSettings.Converters.Add(new StringEnumConverter());
        }

    }
}
