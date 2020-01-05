using System;

namespace CobraCompiler
{
    struct Point
    {
        public int X;
        public int Y;

        public static Point operator +(Point lhs, Point rhs)
        {
            return new Point() {X = lhs.X + rhs.X, Y = lhs.Y + rhs.Y};
        }

        public int Calculate(Func<int, int, int> func)
        {
            return func(X, Y);
        }

        public void Function(Action<int> func)
        {
            func(2);
        }

        public void Function(Action<string> func)
        {
            func("hello world");
        }

        public static int Magnitude(int x, int y)
        {
            return (int) Math.Sqrt(x * x + y * y);
        }
    }

    class Class1
    {
        private int myval = 1;
        public int Test(int parameter)
        {
            int a = parameter;
            a = a + 2;
            int b = 10;
            a = b * a;
            parameter = a;

            float e = 2.1f;

            for (int j = 0; j < 10; j++)
            {
                int f = myval;
                e += f;
            }

            {
                int f = myval * 2;
                e += f;
            }

            Point p = new Point() {X = a, Y = b};

            Point i = new Point() { X = b, Y = a };

            Point z = p + i;

            int mag = z.Calculate(Point.Magnitude);

            string str = "hello";

            b = str.Length;

            Console.WriteLine("Hello");
            return a;
        }
    }
}
