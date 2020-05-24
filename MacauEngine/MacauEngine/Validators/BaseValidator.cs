using MacauEngine.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Validators
{
    internal abstract class BaseValidator
    {
        internal abstract ValidationResult Check();
    }
}
