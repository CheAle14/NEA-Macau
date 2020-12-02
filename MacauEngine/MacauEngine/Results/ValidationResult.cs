using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Results
{
    /// <summary>
    /// The result following the validation of card placement
    /// </summary>
    public class ValidationResult : IResult
    {
        private ValidationResult(bool thing, string reason)
        {
            IsSuccess = thing;
            ErrorReason = reason;
        }
        internal static ValidationResult FromSuccess() => new ValidationResult(true, null);
        internal static ValidationResult FromError(string reason) => new ValidationResult(false, reason);

        /// <inheritdoc/>
        public bool IsSuccess { get; }

        /// <inheritdoc/>
        public string ErrorReason { get; }
    }
}
