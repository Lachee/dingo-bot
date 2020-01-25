using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DingoBot.Entities
{
    public class Base64Serializable
    {

        /// <summary>
        /// Encodes the profile into Base64
        /// </summary>
        /// <returns></returns>
        public virtual string ToBase64() {
            var serializedProfile = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
            return System.Convert.ToBase64String(serializedProfile);
        }

    }
}
