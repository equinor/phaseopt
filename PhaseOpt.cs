﻿using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;


namespace PhaseOpt
{
    public class PhaseOpt
    {
        private const double Bara_To_Barg = 1.01325;
        private const double Kelvin_To_Celcius = 273.15;

        /// <summary>
        /// Calculates the criconden points (bar and/or therm).
        /// </summary>
        /// <param name="IND">Index for criconden therm (=1) or criconden bar (=2) calculations, INTEGER, Input.</param>
        /// <param name="NC">Number of mixture components, INTEGER, Input</param>
        /// <param name="ID">Identification number of each mixture component, INTEGER array of 50 elements,
        /// Input (The ID numbers of the compounds, available in UMR database).</param>
        /// <param name="Z">Mixture composition in mol/mol, DOUBLE PRECISION array of 50 elements, Input.</param>
        /// <param name="T">Criconden point temperature [in K], DOUBLE PRECISION, Input/Output</param>
        /// <param name="P">Criconden point pressure [in bar], DOUBLE PRECISION, Input/Output.</param>
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
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "ccd", CallingConvention = CallingConvention.Winapi)]
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
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dewt", CallingConvention = CallingConvention.Winapi)]
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
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dewp", CallingConvention = CallingConvention.Winapi)]
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
        [DllImport(@"C:\UMROL\DLL\umr-ol.dll", EntryPoint = "dens", CallingConvention = CallingConvention.Winapi)]
        private static extern void Dens(ref Int32 NC,
            Int32[] ID, double[] Z, ref double T, ref double P, ref double D1, ref double D2, ref double CF1,
            ref double CF2, double[] XY1, double[] XY2);

        /// <summary>
        /// Scales the sum of Array elements into the range [0..Target].
        /// </summary>
        /// <param name="Array"></param>
        /// <param name="Target"></param>
        private static void Normalize(double[] Array, double Target = 1.0)
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
        /// The second pair is the Cricondentherm point. The following pairs are points on the dew point line.</returns>
        /// <remarks>Compound IDs:
        /// 1	CO2
        /// 2	N2
        /// 3	CH4
        /// 4	C2H6
        /// 5	C3
        /// 6	iC4
        /// 7	nC4
        /// 8	cy-C5
        /// 9	2,2-DM-C3
        /// 10	iC5
        /// 11	nC5
        /// 14	2,2-DM-C4
        /// 15	2,3-DM-C4
        /// 16	2-M-C5
        /// 17	3-M-C5
        /// 18	nC6
        /// 19	nC11
        /// 20	nC14
        /// 21	3-M-4,4-DE-heptane
        /// 22	nC18
        /// 23	nc34
        /// 600	C6 fraction
        /// 700	C7 fraction
        /// 701	nC7
        /// 702	3-M-C6
        /// 703	2-M-C6
        /// 704	2,4-DM-C5
        /// 705	cy-C6
        /// 706	M-cy-C5
        /// 707	Benzene
        /// 709	1,2-DM-cyC5
        /// 710	1,3-dDM-cyC5
        /// 800	C8 fraction
        /// 801	nC8
        /// 802	2-M-C7
        /// 803	3-M-C7
        /// 804	2,3,4-TM-C5
        /// 805	2,4-DM-C6
        /// 806	cy-C7
        /// 807	M-cy-C6
        /// 808	E-cy-C5
        /// 809	cis-1,3-DM-cy-C6
        /// 810	toluene
        /// 811	1,cis-2,trans-4-TMcyC5
        /// 812	2,3-DM-C6
        /// 900	C9 fraction
        /// 901	nC9
        /// 902	2-M-C8
        /// 903	3-M-C8
        /// 904	2,2-DM-C7
        /// 905	2,6-DM-C7
        /// 906	cy-C8
        /// 907	e-cy-C6
        /// 908	i-p-cy-C5
        /// 909	1,1,3-TM-cy-C6
        /// 910	cis-1,2-DM-cy-C6
        /// 911	m-xylene
        /// 912	p-xylene
        /// 913	E-benzene
        /// 914	o-xylene
        /// 915	1,2,3-TMcyC6
        /// 916	4-M-C8
        /// 917	o-E-toluene
        /// 1201	nC12
        /// 1202	nC5-Benzene
        /// 6000	C6+ fraction
        /// 10000	C10+ fraction
        /// 101101	nC10
        /// 101102	1,2,3-TM-Benzene
        /// 101103	cy-C9
        /// 101104	2-E-p-xylene
        /// 131401	nC13
        /// 131402	nC6-Benzene
        /// 131403	nC7-Benzene
        /// 151601	nC15
        /// 151602	nC10-cy-C5
        /// 151603	nC8-Benzene
        /// 151604	nC9-Benzene
        /// 171801	nC17
        /// 171802	nC12-cy-C5
        /// 171803	nC10-Benzene
        /// 192101	nC19
        /// 192102	nC14-cy-C5
        /// 222401	nC22
        /// 222402	nC23
        /// 222403	nC17-cy-C5
        /// 253001	nC27
        /// 318001	nC39
        /// 700900	C7C9 fraction
        /// </remarks>
        public static double[] Calculate_Dew_Point_Line(int[] IDs, double[] Values, uint Points = 5, uint Units = 1)
        {
            if (Units > 1) Units = 1;

            Normalize(Values, 1.0);

            // Calculate the cricondentherm point
            Int32 IND = 1;
            Int32 Components = IDs.Length;
            double CCTT = 0.0;
            double CCTP = 0.0;
            double CCBT = 0.0;
            double CCBP = 0.0;
            List<double> Results = new List<double>();

            Criconden(ref IND, ref Components, IDs, Values, ref CCTT, ref CCTP);

            // Calculate the cricondenbar point
            IND = 2;

            Criconden(ref IND, ref Components, IDs, Values, ref CCBT, ref CCBP);

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
                double[] XY = new double[50];
                Dewt(ref Components, IDs, Values, ref T, ref P, XY);
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
                double[] XY1 = new double[50];
                double[] XY2 = new double[50];
                Dewp(ref Components, IDs, Values, ref T, ref P1, XY1, ref P2, XY2);
                Results.Add(P1 - (Units * Bara_To_Barg)); Results.Add(T - (Units * Kelvin_To_Celcius));
            }
            return Results.ToArray();
        }

        /// <summary>
        /// Calculates the density and the compressibility factor of the composition.
        /// </summary>
        /// <param name="IDs">Composition IDs</param>
        /// <param name="Values">Composition Values</param>
        /// <param name="P">Pressure</param>
        /// <param name="T">Temperature</param>
        /// <returns>An array containing {D1, D2, CF1, CF2}</returns>
        public static double[] Calculate_Density_And_Compressibility(int[] IDs, double[] Values, double P = 1.01325, double T = 288.15)
        {
            double[] Results = new double[4];
            Int32 Components = IDs.Length;
            double D1 = 0.0;
            double D2 = 0.0;
            double CF1 = 0.0;
            double CF2 = 0.0;
            double[] XY1 = new double[50];
            double[] XY2 = new double[50];

            Normalize(Values, 1.0);

            Dens(ref Components, IDs, Values, ref T, ref P, ref D1, ref D2, ref CF1, ref CF2, XY1, XY2);

            Results[0] = D1;
            Results[1] = D2;
            Results[2] = CF1;
            Results[3] = CF2;

            return Results;
        }

        public static double[] Cricondenbar(int[] IDs, double[] Values, double P = 0.0, double T = 0.0, uint Units = 1)
        {
            if (Units > 1) Units = 1;

            Normalize(Values, 1.0);

            // Calculate the cricondenbar point
            Int32 IND = 2;
            Int32 Components = IDs.Length;
            double CCBT = T;
            double CCBP = P;
            List<double> Results = new List<double>();

            Criconden(ref IND, ref Components, IDs, Values, ref CCBT, ref CCBP);

            Results.Add(CCBP - (Units * Bara_To_Barg));
            Results.Add(CCBT - (Units * Kelvin_To_Celcius));

            return Results.ToArray();
        }

        public static double[] Cricondentherm(int[] IDs, double[] Values, double P = 0.0, double T = 0.0, uint Units = 1)
        {
            if (Units > 1) Units = 1;

            Normalize(Values, 1.0);

            // Calculate the cricondenbar point
            Int32 IND = 1;
            Int32 Components = IDs.Length;
            double CCTT = T;
            double CCTP = P;
            List<double> Results = new List<double>();

            Criconden(ref IND, ref Components, IDs, Values, ref CCTT, ref CCTP);

            Results.Add(CCTP - (Units * Bara_To_Barg));
            Results.Add(CCTT - (Units * Kelvin_To_Celcius));

            return Results.ToArray();
        }

    } 
}