using WpfLibrary; // Подключение твоей обновленной библиотеки
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfCalculator
{
    public partial class MainWindow : Window
    {
        private CalculatorLogic _logic = new CalculatorLogic();

        public MainWindow()
        {
            InitializeComponent();
            _logic.DisplayChanged += (text) => txtDisplay.Text = text;
            txtDisplay.Text = _logic.CurrentInput;

            this.Focusable = true;
            this.Focus();
        }

        // --- ВВОД ЦИФР И ОПЕРАЦИЙ ---
        private void Digit_Click(object sender, RoutedEventArgs e) =>
            _logic.ProcessDigit((sender as Button).Content.ToString());

        private void Op_Click(object sender, RoutedEventArgs e) =>
            _logic.ProcessOperation((sender as Button).Content.ToString());

        private void Equal_Click(object sender, RoutedEventArgs e)
        {
            try { _logic.ProcessEquals(); }
            catch (CalculatorException ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        }

        // --- ОЧИСТКА И УПРАВЛЕНИЕ ---
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).Content.ToString() == "CE") _logic.ProcessClearEntry();
            else _logic.ProcessClear();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => _logic.ProcessBackspace();
        private void Point_Click(object sender, RoutedEventArgs e) => _logic.ProcessDecimalPoint();
        private void Sign_Click(object sender, RoutedEventArgs e) => _logic.ProcessSignChange();

        // --- ПОЛИМОРФНЫЙ ВЫЗОВ НА ОГРОМНЫЙ ПЛЮС НА ЗАЩИТЕ ЛАБЫ ---
        private void Ln_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Передаем экземпляр класса-наследника в единый метод
                _logic.ExecutePolymorphicOperation(new LnOperation());
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        }

        // Константы математики
        private void Pi_Click(object sender, RoutedEventArgs e) => _logic.ProcessDigit("3,1415926535");
        private void E_Click(object sender, RoutedEventArgs e) => _logic.ProcessDigit("2,7182818284");

        // --- ИНТЕРФЕЙС И ИСТОРИЯ ---
        private void ToggleExtendedMode(object sender, RoutedEventArgs e)
        {
            bool isExtended = (sender as MenuItem).IsChecked;
            ExtraColumn.Width = isExtended ? new GridLength(100) : new GridLength(0);
            ExtraPanel.Visibility = isExtended ? Visibility.Visible : Visibility.Collapsed;
            this.Width = isExtended ? 450 : 350;
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            string historyLog = string.Join("\n", _logic.History.Items);
            if (string.IsNullOrEmpty(historyLog)) historyLog = "История операций пуста.";
            MessageBox.Show($"История вычислений:\n\n{historyLog}", "Справка / Логи");
        }

        // Физическая клавиатура
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.D0 && e.Key <= Key.D9) _logic.ProcessDigit((e.Key - Key.D0).ToString());
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) _logic.ProcessDigit((e.Key - Key.NumPad0).ToString());
            else if (e.Key == Key.Add) _logic.ProcessOperation("+");
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus) _logic.ProcessOperation("-");
            else if (e.Key == Key.Multiply) _logic.ProcessOperation("*");
            else if (e.Key == Key.Divide) _logic.ProcessOperation("/");
            else if (e.Key == Key.Enter) Equal_Click(this, null);
            else if (e.Key == Key.Back) _logic.ProcessBackspace();
            else if (e.Key == Key.Escape) _logic.ProcessClear();
        }
    }
}