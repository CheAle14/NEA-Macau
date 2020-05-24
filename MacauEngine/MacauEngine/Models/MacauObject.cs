using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Models
{
    /// <summary>
    /// Represents a base level object that can be sent over network
    /// </summary>
    public abstract class MacauObject
    {
        /// <summary>
        /// Constructor for new objects, not just recieved from network
        /// </summary>
        public MacauObject() { }
        /// <summary>
        /// Constructor for objects recieved over network
        /// </summary>
        /// <param name="jObj">The json encoded object, generated from the corresponding <see cref="ToJson"/> function</param>
        public MacauObject(JObject jObj) 
        {
            Update(jObj);
        }
        /// <summary>
        /// Loads the values from the JSON object to this object
        /// </summary>
        /// <param name="json">The properties and values to load</param>
        public abstract void Update(JObject json);
        /// <summary>
        /// Encode this object as a JSON object to be sent over network
        /// </summary>
        /// <returns>The Json representation of this object</returns>
        public abstract JObject ToJson();

        /// <summary>
        /// Returns a stringular representation, this should be overriden since it shows JSON in the base
        /// </summary>
        /// <returns>JSON representation</returns>
        public override string ToString()
        {
            return ToJson()?.ToString() ?? "[no json]";
        }
    }
}
