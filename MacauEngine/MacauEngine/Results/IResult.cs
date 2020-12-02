using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Results
{
    /// <summary>
    /// A generic result type
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Gets whether the outcome is a success
        /// </summary>
        bool IsSuccess { get; }
        /// <summary>
        /// If <see cref="IsSuccess"/> is true, this will be null. Otherwise, this gets the reason why this result failed.
        /// </summary>
        string ErrorReason { get; }
    }
}
