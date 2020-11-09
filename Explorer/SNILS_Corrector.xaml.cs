using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace EGISSOEditor
{
    /// <summary>
    /// Логика взаимодействия для SNILS_Corrector.xaml
    /// </summary>
    public partial class SNILS_Corrector : Window
    {
        public SNILS_Corrector()
        {
            InitializeComponent();
        }

        private void tbSNILS_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lbSNILS != null)
                lbSNILS.Text = "";
            
            if (Regex.IsMatch(tbSNILS.Text, @"^\d{3}-\d{3}-\d{3} \d{2}$"))
            {
                string curSNILS = tbSNILS.Text;
                curSNILS = curSNILS.Replace("-","");
                curSNILS = curSNILS.Replace(" ", "");

                if (!CheckSNILS(curSNILS, out string result))
                {
                    List<string> ListSNILS = FindCorrectSNILS(curSNILS);
                    if (result != "")
                        ListSNILS.Add(result);
                    
                    foreach (string str in ListSNILS)
                        lbSNILS.Text += long.Parse(str).ToString("000-000-000 00") + Environment.NewLine;
                    shadowTextBox.Color = Color.FromRgb(255, 0, 0);
                }
                else
                    shadowTextBox.Color = Color.FromRgb(0, 255, 0);
            }
            else
                shadowTextBox.Color = Color.FromRgb(55, 55, 55);   
        }

        public static bool CheckSNILS(string value, out string result)
        {
            result = "";

            value = value.PadLeft(11, '0');
            string snilsNumber = value.Substring(0, 9);

            if (int.Parse(value.Substring(0, 9)) > 1001998)
            {
                int controlNumber = GetControlNumber(snilsNumber);

                if (controlNumber == int.Parse(value.Substring(9, 2)))
                {
                    result = value;
                    return true;
                }
                else
                {
                    result = snilsNumber + controlNumber.ToString().PadLeft(2, '0');
                    return false;
                }
            }
            else
                return false;
        }

        public static int GetControlNumber(string value)
        {
            int ControlNumber = 0;

            for (int i = 0; i < 9; i++)
                ControlNumber += (9 - i) * int.Parse(value[i] + "");

            if (ControlNumber > 100) ControlNumber %= 101;
            if (ControlNumber == 100) ControlNumber = 0;

            return ControlNumber;
        }

        public static List<string> FindCorrectSNILS(string value)
        {
            List<String> ListCorrectSnils = new List<string>();
            string ResultValue = "";


            if (value.Length == 10)
            {
                for (int i = 0; i <= 10; i++)
                {
                    for (int j = 0; j <= 9; j++)
                    {
                        if (i == 0)
                            ResultValue = j.ToString() + value;
                        else
                            ResultValue = value.Substring(0, i) + j.ToString() + value.Substring(i);
                        if (CheckSNILS(ResultValue, out string result))
                        ListCorrectSnils.Add(ResultValue);
                    }
                }
            }

            value = value.PadLeft(11, '0');
            string fixCN = value.Substring(9, 2);
            value = value.Substring(0, 9);

            for (int i = 0; i <= 8; i++)
            {
                for (int j = 0; j <= 9; j++)
                {
                    if (i == 0)
                        ResultValue = j.ToString() + value.Substring(1, 8) + fixCN;
                    else
                        ResultValue = value.Substring(0, i) + j.ToString() + value.Substring(i + 1, 8 - i) + fixCN;
                    if (CheckSNILS(ResultValue, out string result))
                        ListCorrectSnils.Add(ResultValue);
                }
            }
            return ListCorrectSnils;
        }


    }
}
