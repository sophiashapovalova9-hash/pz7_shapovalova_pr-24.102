using Microsoft.Win32;
using pr3_shapovalova.Models;
using pr3_shapovalova.Properties;
using pr3_shapovalova.Services;
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
using System.Windows.Threading;

namespace pr3_shapovalova.Pages
{
    public partial class Autho : Page
    {
        private int click = 0;
        private int remainingAttempts = 3;
        private DispatcherTimer lockTimer;
        private int secondsLeft = 0;

        private const int MaxAttempts = 3;
        private const int LockoutSeconds = 30;

        public Autho()
        {
            InitializeComponent();

            lockTimer = new DispatcherTimer();
            lockTimer.Interval = TimeSpan.FromSeconds(1);
            lockTimer.Tick += LockTimer_Tick;

            CheckPersistentLockout();
        }

        private void CheckPersistentLockout()
        {
            DateTime lockoutEndTime = Settings.Default.LockoutEndTime;

            if (lockoutEndTime > DateTime.Now)
            {
                TimeSpan remaining = lockoutEndTime - DateTime.Now;
                secondsLeft = (int)Math.Ceiling(remaining.TotalSeconds);

                click = MaxAttempts;
                SetControlsEnabled(false);
                txtAttemptsInfo.Visibility = Visibility.Collapsed;
                txtLockoutInfo.Visibility = Visibility.Visible;
                txtLockoutInfo.Text = $"Превышено число попыток. Повторный ввод доступен через: {secondsLeft} сек.";

                lockTimer.Start();
            }
            else
            {
                click = 0;
                remainingAttempts = MaxAttempts;
                Settings.Default.LockoutEndTime = DateTime.MinValue;
                Settings.Default.Save();
            }
        }

        private void btnEnterGuests_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Client(null, "Гость"));
        }

        private void GenerateCaptcha()
        {
            txtBlockCaptcha.Visibility = Visibility.Visible;
            gridCaptcha.Visibility = Visibility.Visible;
            btnRefreshCaptcha.Visibility = Visibility.Visible;

            string captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tblCaptcha.Text = captchaText;
            tblCaptcha.TextDecorations = TextDecorations.Strikethrough;
        }

        private void btnRefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            if (lockTimer.IsEnabled) return;
            GenerateCaptcha();
            txtboxCaptcha.Clear();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (lockTimer.IsEnabled) return;

            string login = txtLogin.Text.Trim();
            string password = chkShowPassword.IsChecked == true
                ? txtPasswordVisible.Text.Trim()
                : pswbPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните логин и пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (click >= 1 && txtboxCaptcha.Text.Trim() != tblCaptcha.Text)
            {
                MessageBox.Show("Неверная CAPTCHA!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                RegisterAttempt(login, false, "Неверная CAPTCHA");
                GenerateCaptcha();
                txtboxCaptcha.Clear();
                HandleFailedAttempt();
                return;
            }

            string hashedPassword = HashPasswords.Hash.GetHash(password);
            var db = Helper.GetContext();
            var user = db.User.FirstOrDefault(x => x.Login == login && x.PasswordHash == hashedPassword);

            if (user != null)
            {
                RegisterAttempt(login, true, null);
                MessageBox.Show($"Вы вошли под: {user.FirstName} {user.LastName}");
                LoadPage("Клиент", user);
            }
            else
            {
                MessageBox.Show("Вы ввели логин или пароль неверно!");
                RegisterAttempt(login, false, "Неверный логин или пароль");
                GenerateCaptcha();
                txtboxCaptcha.Clear();
                pswbPassword.Clear();
                txtPasswordVisible.Clear();
                HandleFailedAttempt();
            }
        }

        private void HandleFailedAttempt()
        {
            click++;
            remainingAttempts = MaxAttempts - click;

            if (remainingAttempts > 0)
            {
                txtAttemptsInfo.Text = $"Осталось попыток: {remainingAttempts}";
                txtAttemptsInfo.Visibility = Visibility.Visible;
            }
            else
            {
                StartLockout();
            }
        }

        private void StartLockout()
        {
            secondsLeft = LockoutSeconds;

            Settings.Default.LockoutEndTime = DateTime.Now.AddSeconds(secondsLeft);
            Settings.Default.Save();

            SetControlsEnabled(false);
            txtAttemptsInfo.Visibility = Visibility.Collapsed;
            txtLockoutInfo.Visibility = Visibility.Visible;
            txtLockoutInfo.Text = $"Превышено число попыток. Повторный ввод доступен через: {secondsLeft} сек.";

            lockTimer.Start();
        }

        private void LockTimer_Tick(object sender, EventArgs e)
        {
            secondsLeft--;

            if (secondsLeft > 0)
            {
                txtLockoutInfo.Text = $"Превышено число попыток. Повторный ввод доступен через: {secondsLeft} сек.";
            }
            else
            {
                lockTimer.Stop();
                EndLockout();
            }
        }

        private void EndLockout()
        {
            Settings.Default.LockoutEndTime = DateTime.MinValue;
            Settings.Default.Save();

            txtLockoutInfo.Visibility = Visibility.Collapsed;
            SetControlsEnabled(true);

            click = 0;
            remainingAttempts = MaxAttempts;
            txtAttemptsInfo.Visibility = Visibility.Collapsed;

            GenerateCaptcha();
            txtboxCaptcha.Clear();
            pswbPassword.Clear();
            txtPasswordVisible.Clear();
        }

        private void SetControlsEnabled(bool enabled)
        {
            txtLogin.IsEnabled = enabled;
            pswbPassword.IsEnabled = enabled;
            txtPasswordVisible.IsEnabled = enabled;
            chkShowPassword.IsEnabled = enabled;
            txtboxCaptcha.IsEnabled = enabled;
            btnEnter.IsEnabled = enabled;
            btnRefreshCaptcha.IsEnabled = enabled;
        }

        private void RegisterAttempt(string login, bool success, string failReason)
        {
            try
            {
                var db = Helper.GetContext();
                db.LoginHistory.Add(new LoginHistory
                {
                    UserLogin = login,
                    AttemptTime = DateTime.Now,
                    IsSuccess = success,
                    FailReason = failReason
                });
                db.SaveChanges();
            }
            catch
            {
            }
        }

        private void LoadPage(string role, User user)
        {
            click = 0;
            remainingAttempts = MaxAttempts;
            switch (role)
            {
                case "Клиент":
                    NavigationService.Navigate(new Client(user, role));
                    break;
            }
        }

        private void btnRegisterPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Register());
        }

        private void chkShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            if (txtPasswordVisible == null) return;
            txtPasswordVisible.Text = pswbPassword.Password;
            txtPasswordVisible.Visibility = Visibility.Visible;
            pswbPassword.Visibility = Visibility.Collapsed;
        }

        private void chkShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            if (pswbPassword == null) return;
            pswbPassword.Password = txtPasswordVisible.Text;
            pswbPassword.Visibility = Visibility.Visible;
            txtPasswordVisible.Visibility = Visibility.Collapsed;
        }
    }
}