using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacauGame
{
    public static class ControlExtensions
    {
        public static Control GetControl(this Control control, Func<Control, bool> selector)
        {
            foreach(Control child in control.Controls)
            {
                if (selector(child))
                    return child;
                var cnt = child.GetControl(selector);
                if (cnt != null)
                    return cnt;
            }
            return null;
        }
    }
}
