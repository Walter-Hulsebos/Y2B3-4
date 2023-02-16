/*
** SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008) 
** Copyright (C) 2011 Silicon Graphics, Inc.
** All Rights Reserved.
**
** Permission is hereby granted, free of charge, to any person obtaining a copy
** of this software and associated documentation files (the "Software"), to deal
** in the Software without restriction, including without limitation the rights
** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
** of the Software, and to permit persons to whom the Software is furnished to do so,
** subject to the following conditions:
** 
** The above copyright notice including the dates of first publication and either this
** permission notice or a reference to http://oss.sgi.com/projects/FreeB/ shall be
** included in all copies or substantial portions of the Software. 
**
** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
** INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
** PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL SILICON GRAPHICS, INC.
** BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
** TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
** OR OTHER DEALINGS IN THE SOFTWARE.
** 
** Except as contained in this notice, the name of Silicon Graphics, Inc. shall not
** be used in advertising or otherwise to promote the sale, use or other dealings in
** this Software without prior written authorization from Silicon Graphics, Inc.
*/
/*
** Original Author: Eric Veach, July 1994.
** libtess2: Mikko Mononen, http://code.google.com/p/libtess2/.
** LibTessDotNet: Remi Gillig, https://github.com/speps/LibTessDotNet
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace LibTessDotNet
{
    /// <summary>
    /// Changed _vertices, _vertexCount, _elements and _elementCount
    /// from private to protected in the original class.
    /// 
    /// Added IsPointInTriangle(...) and IsPointInShape(...).
    /// </summary>
    public class ExtendedTess : Tess
    {
        /// <summary>
        /// Thanks to "Glenn Slayden" https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public bool IsPointInTriangle(float px, float py, float p0x, float p0y, float p1x, float p1y, float p2x, float p2y)
        {
            var s = p0y * p2x - p0x * p2y + (p2y - p0y) * px + (p0x - p2x) * py;
            var t = p0x * p1y - p0y * p1x + (p0y - p1y) * px + (p1x - p0x) * py;

            if ((s < 0) != (t < 0))
            {
                return false;
            }

            var A = -p1y * p2x + p0y * (p2x - p1x) + p0x * (p1y - p2y) + p1x * p2y;

            return A < 0 ?
                    (s <= 0 && s + t >= A) :
                    (s >= 0 && s + t <= A);
        }

        public bool IsPointInShape(UnityEngine.Vector2 point)
        {
            float p0x, p0y, p1x, p1y, p2x, p2y;
            for (int i = 0; i < _elementCount; i++)
            {
                p0x = _vertices[_elements[i * 3]].Position.X;
                p0y = _vertices[_elements[i * 3]].Position.Y;
                p1x = _vertices[_elements[i * 3 + 1]].Position.X;
                p1y = _vertices[_elements[i * 3 + 1]].Position.Y;
                p2x = _vertices[_elements[i * 3 + 2]].Position.X;
                p2y = _vertices[_elements[i * 3 + 2]].Position.Y;
                if (IsPointInTriangle(point.x, point.y, p0x, p0y, p1x, p1y, p2x, p2y))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
