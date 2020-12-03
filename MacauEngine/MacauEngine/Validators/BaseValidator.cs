using MacauEngine.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Validators
{
    // For the small validation steps, not the whole PlaceValidator.
    internal abstract class BaseValidator
    {
        internal abstract ValidationResult Check();
    }
}
