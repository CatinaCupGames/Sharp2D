﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp2D.Core.Graphics
{
    public abstract class Camera
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public abstract void BeforeRender();

        public abstract bool IsOutsideCamera(float X, float Y, float Width, float Height);
    }
}
