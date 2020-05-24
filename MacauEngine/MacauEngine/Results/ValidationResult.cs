using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Results
{
    public class ValidationResult
    {
        private ValidationResult(bool thing, string reason)
        {
            IsSuccess = thing;
            ErrorReason = reason;
        }
        internal static ValidationResult FromSuccess() => new ValidationResult(true, null);
        internal static ValidationResult FromError(string reason) => new ValidationResult(false, reason);

        public bool IsSuccess { get; }

        public string ErrorReason { get; }
    }
}
