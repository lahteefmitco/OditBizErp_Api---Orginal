using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace MictcoWebService.Common
{
    public class CommonHelper
    {
        public static int Decimalpoint = 3;
        public static string crncymax, crncymin;
        public static string UploadedFile(IWebHostEnvironment webHostEnvironment, IFormFile file)
        {
            string uniqueFileName = null;

            if (file != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        public static string SeperateNumberbycomma(string textboxText)
        {
            try
            {
                return String.Format("{0:n}", Convert.ToDecimal(textboxText));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static decimal stringToDecimal(string textboxText)
        {
            return Convert.ToDecimal(string.IsNullOrEmpty(textboxText) ? "0" : textboxText);
        }

        public static decimal getRoudedValue(string textboxText)
        {
            try
            {
                return Convert.ToDecimal(string.IsNullOrEmpty(textboxText) ? "0" : textboxText);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public static decimal GetTextboxValue(string textboxText)
        {
            try
            {
                return Convert.ToDecimal(string.IsNullOrEmpty(textboxText) ? "0" : textboxText);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public static string tokenEncrypt(string token)
        {
            try
            {
                string textToEncrypt = token;
                string ToReturn = "";
                string publickey = "aglrhklt";
                string secretkey = "otepiklm";
                byte[] secretkeyByte = { };
                secretkeyByte = System.Text.Encoding.UTF8.GetBytes(secretkey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = System.Text.Encoding.UTF8.GetBytes(textToEncrypt);
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateEncryptor(publickeybyte, secretkeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    ToReturn = Convert.ToBase64String(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }
        public static string NumberToWordsDouble(double doubleNumber)
        {
            var beforeFloatingPoint = (int)Math.Floor(doubleNumber);
            var beforeFloatingPointWord = NumberToWords(beforeFloatingPoint);
            var afterFloatingPointWord = "0";
            int i = 0; i = Convert.ToInt16((doubleNumber - beforeFloatingPoint) * 100);
            if (i > 0) { afterFloatingPointWord = SmallNumberToWord((int)i, ""); }
            else { afterFloatingPointWord = "0"; }
            if (crncymax == "")
            {
                if (afterFloatingPointWord == "0")
                {
                    return beforeFloatingPointWord + " Rupees";
                }
                else
                {
                    return beforeFloatingPointWord + " Rupees and " + afterFloatingPointWord + " Paise";
                }
            }
            else
            {
                if (afterFloatingPointWord == "0")
                {
                    return beforeFloatingPointWord + " " + crncymax;
                }
                else
                {
                    return beforeFloatingPointWord + "  " + crncymax + " and " + afterFloatingPointWord + " " + crncymin;
                }
            }
        }
        private static string NumberToWords(int number)
        {
            if (number == 0)
                return "zero";

            if (number < 0)
                return "minus " + NumberToWords(Math.Abs(number));

            var words = "";

            if (number / 1000000000 > 0)
            {
                words += NumberToWords(number / 1000000000) + " Billion ";
                number %= 1000000000;
            }

            if (number / 1000000 > 0)
            {
                words += NumberToWords(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if (number / 100000 > 0)
            {
                words += NumberToWords(number / 100000) + " Lakh ";
                number %= 100000;
            }

            if (number / 1000 > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if (number / 100 > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            words = SmallNumberToWord(number, words);

            return words;
        }

        private static string SmallNumberToWord(int number, string words)
        {
            if (number <= 0) return words;
            if (words != "")
                words += " ";

            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Fourty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += "-" + unitsMap[number % 10];
            }
            return words;
        }
        public static string tokenDecrypt(string token)
        {
            try
            {
                string textToDecrypt = token;
                string ToReturn = "";
                string publickey = "aglrhklt";
                string privatekey = "otepiklm";
                byte[] privatekeyByte = { };
                privatekeyByte = System.Text.Encoding.UTF8.GetBytes(privatekey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = new byte[textToDecrypt.Replace(" ", "+").Length];
                inputbyteArray = Convert.FromBase64String(textToDecrypt.Replace(" ", "+"));
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateDecryptor(publickeybyte, privatekeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    ToReturn = encoding.GetString(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ae)
            {
                throw new Exception(ae.Message, ae.InnerException);
            }
        }
        
    }
}
