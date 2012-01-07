//=================================================================================================
//=================================================================================================
//
// Copyright (c) 2012 Dennis Mnuskin
//
// This file is part of Droppy application.
//
// This source code is distributed under the MIT license.  For full text, see
// http://www.opensource.org/licenses/mit-license.php Same text is found in LICENSE file which
// is located in root directory of the project.
//
//=================================================================================================
//=================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Droppy
{
    public class MouseEx
    {
        public static Point GetPosition(Visual relativeTo)
        {
            Win32.Point w32Mouse = new Win32.Point();

            Win32.GetCursorPos( ref w32Mouse );

            return relativeTo.PointFromScreen( new Point( w32Mouse.X, w32Mouse.Y ) );
        }
    }
}
