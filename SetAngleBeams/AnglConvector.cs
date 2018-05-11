using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Diss
{
    public class AngleType : ValidationRule
    {
        private double _min = 0;
        private double _max = 25;

        public AngleType() { }

        public double Min { get => _min; set => _min = value; }

        public double Max { get => _max; set => _max = value; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double angl = 0;

            try
            {
                if (((string)value).Length > 0)
                    angl = Double.Parse((String)value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((angl < Min) || (angl > Max))
            {
                return new ValidationResult(false,
                  "Please enter an age in the range: " + Min + " - " + Max + ".");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }

    class AnglConvector
    {
        public static readonly int SizeBeam = 4;
        public static readonly double Phi = 11.65;
        public static readonly double Tetta = 16.53;
        static Int16 AngleToInt16(double angl) { return (Int16)(angl * 512); }
        static UInt16 AngleSetCs(Int16 aInt16)
        {
            return (UInt16)((aInt16 & 0xF)
                + ((aInt16 >> 4) & 0xF)
                + ((aInt16 >> 8) & 0xF));
        }
        static UInt16 AngleRaw(Int16 aInt16) { return (UInt16)((aInt16 << 4) | AngleSetCs(aInt16)); }
        static bool AngleIsGood(UInt16 aRaw)
        {
            return (((~(((aRaw >> 4) & 0xF)
                + ((aRaw >> 8) & 0xF)
                + ((aRaw >> 12) & 0xF))) & 0xF) == (aRaw & 0xF));
        }
        static double AngleToDouble(UInt16 aRaw)
        {
            return ((aRaw <= 0x7FFF ?
                ((aRaw >> 4)) / 512.0
                : ((double)(aRaw >> 4) - 4096) / 512.0));
        }


        public static byte[] SetPhiTetta(byte beam, double dPhi, double dTetta)
        {
            Int16 phiInt16, tettaInt16;
            UInt16 phiRaw, tettaRaw;
            if (dPhi > 7 || dPhi < -7
              || dTetta > 7 || dTetta < -7)
                throw new Exception("Вышли за допустимый диапазон");

            phiInt16 = AngleToInt16(dPhi);
            tettaInt16 = AngleToInt16(dTetta);
            phiRaw = AngleRaw(phiInt16);
            tettaRaw = AngleRaw(tettaInt16);
            byte[] res = new byte[4];
            Array.Copy(BitConverter.GetBytes(phiRaw), 0, res, 0, sizeof(UInt16));
            Array.Copy(BitConverter.GetBytes(tettaRaw), 0, res, sizeof(UInt16), sizeof(UInt16));
            return res;
        }

        public static double[] GetPhiTetta(byte[] rawArray)
        {
            double[] a = new double[8];
            for (int i = 0; i < 8; i++)
            {
                UInt16 raw = BitConverter.ToUInt16(rawArray, i * sizeof(UInt16));
                if (AngleIsGood(raw))
                {
                    a[i] = AngleToDouble(raw);
                }
                else
                {
                    a[i] = 0;
                    //throw new Exception("Контрольная сумма не совпала");
                }
            }
            return a;
        }
    }
}