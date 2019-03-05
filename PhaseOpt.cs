using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;



namespace PhaseOpt
{
    public class PhaseOpt
    {
        private const double Bara_To_Barg = 1.01325;
        private const double Kelvin_To_Celcius = 273.15;

        public double[] Composition_Values;
        public Int32[]  Composition_IDs;

        public PhaseOpt()
        { }
        public PhaseOpt(Int32[] IDs, double[] Values)
        {
            Composition_IDs = IDs;
            Composition_Values = Values;
        }

        /// <summary>
        /// Calculates the criconden points (bar and/or therm).
        /// </summary>
        /// <param name="IND">Index for criconden therm (=1) or criconden bar (=2) calculations, INTEGER, Input.</param>
        /// <param name="NC">Number of mixture components, INTEGER, Input</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">Criconden point temperature [in K], DOUBLE PRECISION, Input/Output</param>
        /// <param name="P">Criconden point pressure [in bara], DOUBLE PRECISION, Input/Output.</param>
        /// <remarks>
        /// <para>For routine calling, T, P must be equal to or less than zero if the program's default initial
        /// values is to be used.
        /// </para>
        /// <para>Otherwise, if the user wants to specify the initial values of temperature
        /// and pressure (e.g for better convergence), the T, P must be set to specific values. It must be
        /// noted that initial values should be at low pressures (e.g. less than 30 bar) and, of course,
        /// inside the phase envelope, for the algorithm to converge.
        /// </para>
        /// </remarks>
        [DllImport(@"umr-ol.dll", EntryPoint = "ccd", CallingConvention = CallingConvention.StdCall)]
        private static extern void Criconden(ref Int32 IND, ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P);

        /// <summary>
        /// Calculates dew point temperature points.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">System dew point temperature [in K], DOUBLE PRECISION, Input/Output. </param>
        /// <param name="P">System pressure at which the dew point temperature is to be calculated [in bar], DOUBLE PRECISION, Input</param>
        /// <param name="XY">Mixture composition of liquid phase in mol/mol, DOUBLE PRECISION array of 50 elements, Output</param>
        /// <remarks>
        /// <para>
        /// For routine calling, T must be equal to or less than zero if the program's default initial
        /// values is to be used. The default initial values are taken using the Sandler's method.
        /// </para>
        /// <para>
        /// Otherwise, if the user wants to specify the initial value of T (e.g for better convergence),
        /// the T must be set to a specific value. It must be noted that initial values should be near
        /// the dewt and inside the phase envelope for the algorithm to converge.
        /// </para>
        /// <para>
        /// After program's execution, the routine returns the calculated value of T.
        /// </para>
        /// </remarks>
        [DllImport(@"umr-ol.dll", EntryPoint = "dewt", CallingConvention = CallingConvention.StdCall)]
        private static extern void Dewt(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P, double[] XY);

        /// <summary>
        /// Calculates dew point pressure.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">System temperature at which the dew point pressure is to be calculated [in K], DOUBLE PRECISION, Input.</param>
        /// <param name="P1">System dew point pressures [in bar] at the given temperature, DOUBLE PRECISION, Input/Output.</param>
        /// <param name="XY1">Mixture composition of liquid phase for each dew point pressure calculated [in mol/mol],
        /// DOUBLE PRECISION array of 50 elements, Output</param>
        /// <param name="P2">System dew point pressures [in bar] at the given temperature, DOUBLE PRECISION, Input/Output.</param>
        /// <param name="XY2">Mixture composition of liquid phase for each dew point pressure calculated [in mol/mol],
        /// DOUBLE PRECISION array of 50 elements, Output</param>
        /// <remarks>
        /// <para>For routine calling, P1, P2 must be equal to or less than zero if the program's default initial
        /// values is to be used. The default initial values are taken using the Sandler's method.
        /// </para>
        /// <para>
        /// Otherwise, if the user wants to specify the initial value of pressure (e.g for better convergence),
        /// the P1, P2 must be set to specific values. It must be noted that initial values should be near the
        /// dewp and inside the phase envelope for the algorithm to converge.
        /// </para>
        /// <para>
        /// After program's execution, the routine returns the calculated values of P1 and P2.
        /// If P2 is equal to 0, no second dew point pressure is present.
        /// </para>
        /// </remarks>
        [DllImport(@"umr-ol.dll", EntryPoint = "dewp", CallingConvention = CallingConvention.StdCall)]
        private static extern void Dewp(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P1, double[] XY1, ref double P2, double[] XY2);

        /// <summary>
        /// Calculates densities and compressibility factors.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">System temperature [in K], DOUBLE PRECISION, Input.</param>
        /// <param name="P">System pressure [in bar], DOUBLE PRECISION, Input.</param>
        /// <param name="D1">Densities [in kg/m3], DOUBLE PRECISION, Output.</param>
        /// <param name="D2">Densities [in kg/m3], DOUBLE PRECISION, Output.</param>
        /// <param name="CF1">Compressibility factors [-], DOUBLE PRECISION, Output.</param>
        /// <param name="CF2">Compressibility factors [-], DOUBLE PRECISION, Output.</param>
        /// <param name="XY1">Phase composition of phase [in mol/mol], DOUBLE PRECISION array of 50 elements, Output.</param>
        /// <param name="XY2">Phase composition of phase [in mol/mol], DOUBLE PRECISION array of 50 elements, Output.</param>
        [DllImport(@"umr-ol.dll", EntryPoint = "dens", CallingConvention = CallingConvention.StdCall)]
        private static extern void Dens(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P, ref double D1, ref double D2, ref double CF1,
            ref double CF2, double[] XY1, double[] XY2);

        /// <summary>
        /// Calculates liquid dropout.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 100 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 100 elements, Input.</param>
        /// <param name="T">System temperature [in K], DOUBLE PRECISION, Input.</param>
        /// <param name="P">System pressure [in bar], DOUBLE PRECISION, Input.</param>
        /// <param name="D1">Densities [in kg/m3], DOUBLE PRECISION, Output.</param>
        /// <param name="D2">Densities [in kg/m3], DOUBLE PRECISION, Output.</param>
        /// <param name="CF1">Liquid dropout mass [%], DOUBLE PRECISION, Output.</param>
        /// <param name="CF2">Liquid dropout volume [%], DOUBLE PRECISION, Output.</param>
        /// <param name="XY1">Phase composition of phase [in mol/mol], DOUBLE PRECISION array of 50 elements, Output.</param>
        /// <param name="XY2">Phase composition of phase [in mol/mol], DOUBLE PRECISION array of 50 elements, Output.</param>
        [DllImport(@"umr-ol.dll", EntryPoint = "vpl", CallingConvention = CallingConvention.StdCall)]
        private static extern void Vpl(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P, ref double D1, ref double D2, ref double CF1,
            ref double CF2, double[] XY1, double[] XY2);

        /// <summary>
        /// New fluid where the amount of C5+ for the returned fluid is tuned so the new fluid has a cricondenbar pressure 2.4 bar higher than that of the original fluid.
        /// </summary>
        /// <param name="NC">Number of mixture components, INTEGER, Input.</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 100 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 100 elements, Input/Output.</param>
        /// <param name="T">Cricondenbar temperature [in K] of the tuned fluid, DOUBLE PRECISION, Output.</param>
        /// <param name="P">Cricondenbar pressure [in bar] of the tuned fluid, DOUBLE PRECISION, Output.</param>
        [DllImport(@"umr-ol.dll", EntryPoint = "tf", CallingConvention = CallingConvention.StdCall)]
        private static extern void Ft(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P); //, ref Int32 I); //, ref double P1, double[] Z1);


        /// <summary>
        /// Scales the sum of Array elements into the range [0..Target].
        /// </summary>
        /// <param name="Array"></param>
        /// <param name="Target"></param>
        private void Normalize(double[] Array, double Target = 1.0)
        {
            double Sum = 0.0;

            foreach (double Value in Array)
            {
                Sum += Value;
            }

            double Factor = Target / Sum;

            for (int i = 0; i < Array.Length; i++)
            {
                Array[i] = Array[i] * Factor;
            }
        }

        /// <summary>
        /// Calculates the Dew Point Line of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="Points">Number of points to calculate from the Cricondenbar and Cricondentherm points.
        /// The total number of points calculated will be double of this, pluss the Cricondenbar and Cricondentherm points.
        /// Total points on the dew point line = 2 + (Points * 2)</param>
        /// <param name="Units">Sets the engineering units of the outputs. 0: use bara and Kelvin. 1: use barg and ­°C.</param>
        /// <returns>An array of (pressure, temperature) pairs. The first pair is the Cricondenbar point.
        /// The second pair is the Cricondentherm point. The following pairs are points on the dew point line. If the
        /// Points parameter is zero, only the two criconden points are returned</returns>
        public double[] Calculate_Dew_Point_Line(Int32[] IDs, double[] Values, uint Points = 5, uint Units = 1)
        {
            if (Units > 1) Units = 1;

            Normalize(Values, 1.0);

            // Calculate the cricondentherm point
            Int32 IND = 1;
            Int32 Components = IDs.Length;
            Int32[] ID = Pad(IDs);
            double[] Z = Pad(Values);
            double CCTT = 0.0;
            double CCTP = 0.0;
            double CCBT = 0.0;
            double CCBP = 0.0;
            List<double> Results = new List<double>();

            Criconden(ref IND, ref Components, ID, Z, ref CCTT, ref CCTP);

            // Calculate the cricondenbar point
            IND = 2;

            Criconden(ref IND, ref Components, ID, Z, ref CCBT, ref CCBP);

            Results.Add(CCBP - (Units * Bara_To_Barg) );
            Results.Add(CCBT - (Units * Kelvin_To_Celcius) );
            Results.Add(CCTP - (Units * Bara_To_Barg) );
            Results.Add(CCTT - (Units * Kelvin_To_Celcius));

            // Dew points from pressure
            // Calculate points on the dew point line starting from the cricondentherm point.
            // Points are calculated approximately halfway towards the cricondenbar point.
            double P_Interval = (CCBP - CCTP) / ((Points * 2) - 2);
            for (Int32 i = 1; i <= Points; i++)
            {
                double P = CCTP + P_Interval * i;
                double T = CCBT;
                double[] XY = new double[100];
                Dewt(ref Components, ID, Z, ref T, ref P, XY);
                Results.Add(P - (Units * Bara_To_Barg)); Results.Add(T - (Units * Kelvin_To_Celcius));
            }

            // Dew points from temperature
            // Calculate points on the dew point line starting from the cricondenbar point.
            // Points are calculated approximately halfway towards the cricondentherm point.
            double T_Interval = (CCTT - CCBT) / ((Points * 2) - 2);
            for (Int32 i = 1; i <= Points; i++)
            {
                double T = CCBT + T_Interval * i;
                double P1 = CCTP;
                double P2 = CCTP;
                double[] XY1 = new double[100];
                double[] XY2 = new double[100];
                Dewp(ref Components, ID, Z, ref T, ref P1, XY1, ref P2, XY2);
                Results.Add(P1 - (Units * Bara_To_Barg)); Results.Add(T - (Units * Kelvin_To_Celcius));
            }
            return Results.ToArray();
        }

        /// <summary>
        /// Calculates the density and the compressibility factor, at given pressure and temperature, of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="P">Pressure [bara]</param>
        /// <param name="T">Temperature [K]</param>
        /// <returns>An array containing {Vapour density, Liquid density, Vapour compressibility factor, Liquid compressibility factor}.
        /// If there is only one phase the values for the non existing phase will be -1.</returns>
        public double[] Calculate_Density_And_Compressibility(Int32[] IDs, double[] Values, double P = 1.01325, double T = 288.15)
        {
            Normalize(Values, 1.0);

            double[] Results = new double[4];
            Int32 Components = IDs.Length;
            Int32[] ID = Pad(IDs);
            double[] Z = Pad(Values);
            double D1 = 0.0;
            double D2 = 0.0;
            double CF1 = 0.0;
            double CF2 = 0.0;
            double[] XY1 = new double[50];
            double[] XY2 = new double[50];

            Dens(ref Components, ID, Z, ref T, ref P, ref D1, ref D2, ref CF1, ref CF2, XY1, XY2);

            Results[0] = D1;
            Results[1] = D2;
            Results[2] = CF1;
            Results[3] = CF2;

            return Results;
        }

        /// <summary>
        /// Calculates the cricondenbar point of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="P">Pressure</param>
        /// <param name="T">Temperature</param>
        /// <param name="Units">Sets the engineering units of the outputs. 0: use bara and Kelvin. 1: use barg and ­°C.</param>
        /// <returns>An array containing the cricondenbar pressure and temperature.</returns>
        public double[] Cricondenbar(double P = 0.0, double T = 0.0, uint Units = 1)
        {
            double[] Results = new double[2];
            double CCBT = T;
            double CCBP = P;
            string Arguments = "-ccd -ind 2 -id";
            string output;
            Process p = new Process();

            if (Units > 1) Units = 1;

            foreach (int i in Composition_IDs)
            {
                Arguments += " " + i.ToString();
            }
            Arguments += " -z";
            foreach (double v in Composition_Values)
            {
                // Make the value string into a Fortran style double presicion literal
                string val = " " + v.ToString("G", CultureInfo.InvariantCulture).Replace('E', 'D');
                if (!val.Contains("D"))
                    val += "D0";

                Arguments += val;
            }

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "umrol.exe";
            p.StartInfo.Arguments = Arguments;
            p.Start();
            p.PriorityClass = ProcessPriorityClass.Idle;
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            CCBP = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
            CCBT = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

            if (CCBP < 0.0) CCBP = double.NaN;
            if (CCBT < 0.0) CCBT = double.NaN;

            Results[0] = (CCBP - (Units * Bara_To_Barg));
            Results[1] = (CCBT - (Units * Kelvin_To_Celcius));

            return Results;
        }

        /// <summary>
        /// Calculates the cricondentherm point of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="P">Pressure</param>
        /// <param name="T">Temperature</param>
        /// <param name="Units">Sets the engineering units of the outputs. 0: use bara and Kelvin. 1: use barg and ­°C.</param>
        /// <returns>An array containing the cricondentherm pressure and temperature.</returns>
        public double[] Cricondentherm(Int32[] IDs, double[] Values, double P = 0.0, double T = 0.0, uint Units = 1)
        {
            if (Units > 1) Units = 1;

            Normalize(Values, 1.0);

            // Calculate the cricondentherm point
            Int32 IND = 1;
            Int32 Components = IDs.Length;
            Int32[] ID = Pad(IDs);
            double[] Z = Pad(Values);
            double CCTT = T;
            double CCTP = P;
            double[] Results = new double[2];

            Criconden(ref IND, ref Components, ID, Z, ref CCTT, ref CCTP);

            Results[0] = (CCTP - (Units * Bara_To_Barg));
            Results[1] = (CCTT - (Units * Kelvin_To_Celcius));

            return Results;
        }


        /// <summary>
        /// Calculates the liquid dropout of the composition at given pressure and temperature.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="P">Pressure</param>
        /// <param name="T">Temperature</param>
        /// <returns>An array containing the liquid dropout mass and volume fractions.</returns>
        public double[] Dropout(double P, double T)
        {

            double[] Results = new double[2];

            string Arguments = "-vpl -id";
            string output;
            Process p = new Process();

            foreach (int i in Composition_IDs)
            {
                Arguments += " " + i.ToString();
            }
            Arguments += " -z";
            foreach (double v in Composition_Values)
            {
                // Make the value string into a Fortran style double presicion literal
                string val = " " + v.ToString("G", CultureInfo.InvariantCulture).Replace('E', 'D');
                if (!val.Contains("D"))
                    val += "D0";

                Arguments += val;
            }
            Arguments += " -p " + P.ToString("G", CultureInfo.InvariantCulture) + "D0";
            Arguments += " -t " + T.ToString("G", CultureInfo.InvariantCulture) + "D0";

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "umrol.exe";
            p.StartInfo.Arguments = Arguments;
            p.Start();
            p.PriorityClass = ProcessPriorityClass.Idle;
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            Results[0] = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
            Results[1] = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

            return Results;
        }

        public double Dropout_Search(double wd, double T, double P_Max, double limit = 0.01, int max_iterations = 25)
        {
            double P = double.NaN;

            string Arguments = "-ds -id";
            string output;
            Process p = new Process();

            foreach (int i in Composition_IDs)
            {
                Arguments += " " + i.ToString();
            }
            Arguments += " -z";
            foreach (double v in Composition_Values)
            {
                // Make the value string into a Fortran style double presicion literal
                string val = " " + v.ToString("G", CultureInfo.InvariantCulture).Replace('E', 'D');
                if (!val.Contains("D"))
                    val += "D0";

                Arguments += val;
            }
            Arguments += " -wd "      + wd.ToString("G", CultureInfo.InvariantCulture)    + "D0";
            Arguments += " -p "       + P_Max.ToString("G", CultureInfo.InvariantCulture) + "D0";
            Arguments += " -t "       + T.ToString("G", CultureInfo.InvariantCulture)     + "D0";
            Arguments += " -limit "   + limit.ToString("G", CultureInfo.InvariantCulture) + "D0";
            Arguments += " -max-itr " + max_iterations.ToString();

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "umrol.exe";
            p.StartInfo.Arguments = Arguments;
            p.Start();
            p.PriorityClass = ProcessPriorityClass.Idle;
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            P = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);

            /*
            while (diff > limit && n < 15)
            {
                n++;
                if (Result > wd)
                {
                    PMin = P;
                }
                else if (Result < wd)
                {
                    PMax = P;
                }

                P = PMin + (PMax - PMin) / 2;

                Result = Dropout(P, T)[0] * 100.0;
                diff = Math.Abs(Result - wd);

                System.Console.WriteLine("dropout: {0}", Result);
                System.Console.WriteLine("diff:    {0}", diff);
            }

            if (n == 15) P = double.NaN;
            */
            return P;
        }


        /// <summary>
        /// Calculates the dew point pressure of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="P">Pressure</param>
        /// <param name="T">Temperature</param>
        /// <returns>An array containing the liquid dropout mass and volume fractions.</returns>
        public double DewP(double T)
        {
            double P1 = 0.0;
            double P2 = 0.0;
            double[] XY1 = new double[100];
            double[] XY2 = new double[100];

            string Arguments = "-dewp -id";
            string output;
            Process p = new Process();

            foreach (int i in Composition_IDs)
            {
                Arguments += " " + i.ToString();
            }
            Arguments += " -z";
            foreach (double v in Composition_Values)
            {
                // Make the value string into a Fortran style double presicion literal
                string val = " " + v.ToString("G", CultureInfo.InvariantCulture).Replace('E', 'D');
                if (!val.Contains("D"))
                    val += "D0";

                Arguments += val;
            }
            Arguments += " -t " + T.ToString("G", CultureInfo.InvariantCulture) + "D0";

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "umrol.exe";
            p.StartInfo.Arguments = Arguments;
            p.Start();
            p.PriorityClass = ProcessPriorityClass.Idle;
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            P1 = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
            P2 = Convert.ToDouble(output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

            if (P1 > 900.0) P1 = 0.0;
            if (P2 > 900.0) P2 = 0.0;

            return Math.Max(P1, P2);
        }

        public double[] Fluid_Tune(Int32[] IDs, double[] Values)
        {
            Normalize(Values, 1.0);

            Int32 Components = IDs.Length;
            Int32[] ID = Pad(IDs);
            double[] Z = Pad(Values);
            double T = 0.0;
            double P = 0.0;

            Ft(ref Components, ID, Z, ref T, ref P);

            return Z;
        }

        private double[] Pad(double[] In)
        {
            double[] Out = new double[100];
            for (int n = 0; n < In.Length; n++)
            {
                Out[n] = In[n];
            }
            return Out;
        }

        private Int32[] Pad(Int32[] In)
        {
            Int32[] Out = new Int32[100];
            for (int n = 0; n < In.Length; n++)
            {
                Out[n] = In[n];
            }
            return Out;
        }

    } 
}
