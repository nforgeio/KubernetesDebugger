//-----------------------------------------------------------------------------
// FILE:	    ControlExtensions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2023 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Neon.Common;

namespace KubernetesDebugger
{
    /// <summary>
    /// Implemenmts <see cref="Control"/> extensions.
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Ensures that an action is executed on the UI thread.
        /// </summary>
        /// <param name="control">Specifies the Windows Forms control whose UI thread where the action will be invoked.</param>
        /// <param name="action">Specifies the action.</param>
        public static void InvokeOnUiThread(this Control control, Action action)
        {
            Covenant.Requires<ArgumentNullException>(control != null, nameof(control));
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Ensures that the current thread is the control's UI thread.
        /// </summary>
        /// <param name="control">Specifies the control.</param>
        /// <exception cref="InvalidOperationException">Throw when the current thread is not the UI tyhread.</exception>
        public static void EnsureOnUiThread(this Control control)
        {
            if (control.InvokeRequired)
            {
                throw new InvalidOperationException("Not running on the UI thread.");
            }
        }
    }
}
