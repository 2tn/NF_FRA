using System;

namespace NF_FRA
{
    public class FRAData
    {
        public FRAData(double f, double r, double x) { F = f; R = r; X = x; }

        private double f = 0;
        public double F
        { get { return f; } set { f = value; } }

        private double r = 0;
        public double R
        { get { return r; } set { r = value; } }

        private double x = 0;
        public double X
        { get { return x; } set { x = value; } }

        public double Z
        { get { return Math.Sqrt(R * R + X * X); } }

        public double Theta
        { get { return Math.Atan2(-X, R) * 180 / Math.PI; } }
    }
}
