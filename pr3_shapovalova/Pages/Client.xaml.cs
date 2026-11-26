using pr3_shapovalova.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace pr3_shapovalova.Pages
{
    /// <summary>
    /// Логика взаимодействия для Client.xaml
    /// </summary>
    public partial class Client : Page
    {
        public Client(User user, string role)
        {
            InitializeComponent();

            if (user != null)
            {
                txtWelcome.Text = $"Добро пожаловать, {user.FirstName}!\nРоль: {role}";
            }
            else
            {
                txtWelcome.Text = "Добро пожаловать, Гость!";
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Autho());
        }
    }
}
