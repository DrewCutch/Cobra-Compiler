using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CobraCompiler.TypeCheck.CFG
{
    class BitMatrix
    {
        private readonly BitArray _bitArray;
        public readonly int Width;
        public readonly int Height;

        public BitMatrix(int width, int height)
        {
            Width = width;
            Height = height;
            _bitArray = new BitArray(width * height);
        }

        public bool this[int i, int j]
        {
            get => _bitArray.Get(Width * i + j);
            set => _bitArray.Set(Width * i + j, value);
        }
    }
}
