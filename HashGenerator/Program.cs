using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HashGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введите пароль: ");
            string password = Console.ReadLine();

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] sourceBytePassw = Encoding.UTF8.GetBytes(password);
                byte[] hashSourceBytePassw = sha256Hash.ComputeHash(sourceBytePassw);
                string hashPassw = BitConverter.ToString(hashSourceBytePassw).Replace("-", String.Empty);
                Console.WriteLine($"Хеш: {hashPassw}");
            }
            Console.ReadKey();
        }
    }
}
