using System;
using System.Collections.Generic;
using System.Globalization;

namespace WpfLibrary
{
    #region ООП ИЕРАРХИЯ КЛАССОВ (НАСЛЕДОВАНИЕ И ПОЛИМОРФИЗМ)

    public abstract class MathOperation
    {
        public abstract string Name { get; }
        public abstract double Execute(double argument);
    }

    public class SquareOperation : MathOperation
    {
        public override string Name => "sqr";
        public override double Execute(double argument) => argument * argument;
    }

    public class RootOperation : MathOperation
    {
        public override string Name => "√";
        public override double Execute(double argument)
        {
            if (argument < 0) throw new CalculatorException("Корень из отрицательного числа!");
            return Math.Sqrt(argument);
        }
    }

    public class ReciprocalOperation : MathOperation
    {
        public override string Name => "1/";
        public override double Execute(double argument)
        {
            if (argument == 0) throw new CalculatorException("На ноль делить нельзя!");
            return 1 / argument;
        }
    }

    public class LnOperation : MathOperation
    {
        public override string Name => "ln";
        public override double Execute(double argument)
        {
            if (argument <= 0) throw new CalculatorException("ln: Аргумент должен быть больше 0!");
            return Math.Log(argument);
        }
    }

    public class PiOperation : MathOperation
    {
        public override string Name => "π";
        public override double Execute(double argument) => Math.PI;
    }

    public class EOperation : MathOperation
    {
        public override string Name => "e";
        public override double Execute(double argument) => Math.E;
    }

    #endregion

    public class CalculatorException : Exception
    {
        public CalculatorException(string message) : base(message) { }
    }

    public class History
    {
        private List<string> _items = new List<string>();
        public IReadOnlyList<string> Items => _items.AsReadOnly();
        public event Action<string> ItemAdded;
        public event Action Cleared;

        public void Add(string entry)
        {
            _items.Add(entry);
            ItemAdded?.Invoke(entry);
        }

        public void Clear()
        {
            _items.Clear();
            Cleared?.Invoke();
        }
    }

    public class Parser
    {
        private string _text;
        private int _pos;

        public double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return 0;

            _text = expression.Replace(" ", "").Replace(",", ".");
            _pos = 0;

            double result = ParseAddSubtract();

            if (_pos < _text.Length)
                throw new CalculatorException("Ошибка в синтаксисе выражения!");

            return result;
        }

        private double ParseAddSubtract()
        {
            double left = ParseMultiplyDivide();

            while (_pos < _text.Length)
            {
                char op = _text[_pos];
                if (op != '+' && op != '-') break;
                _pos++;

                double right = ParseMultiplyDivide();
                left = (op == '+') ? left + right : left - right;
            }
            return left;
        }

        private double ParseMultiplyDivide()
        {
            double left = ParseUnary();

            while (_pos < _text.Length)
            {
                char op = _text[_pos];
                if (op != '*' && op != '/') break;
                _pos++;

                double right = ParseUnary();
                if (op == '*') left *= right;
                else
                {
                    if (right == 0) throw new CalculatorException("На ноль делить нельзя!");
                    left /= right;
                }
            }
            return left;
        }

        private double ParseUnary()
        {
            if (_pos < _text.Length && _text[_pos] == '-')
            {
                _pos++;
                return -ParseNumber();
            }
            if (_pos < _text.Length && _text[_pos] == '+')
            {
                _pos++;
                return ParseNumber();
            }
            return ParseNumber();
        }

        private double ParseNumber()
        {
            int start = _pos;

            while (_pos < _text.Length && char.IsDigit(_text[_pos])) _pos++;

            if (_pos < _text.Length && _text[_pos] == '.')
            {
                _pos++;
                while (_pos < _text.Length && char.IsDigit(_text[_pos])) _pos++;
            }

            if (_pos < _text.Length && (_text[_pos] == 'E' || _text[_pos] == 'e'))
            {
                _pos++;
                if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-')) _pos++;
                while (_pos < _text.Length && char.IsDigit(_text[_pos])) _pos++;
            }

            if (start == _pos) throw new CalculatorException("Ожидается число!");

            string numStr = _text.Substring(start, _pos - start);
            if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                throw new CalculatorException($"Неверное число: {numStr}");

            return result;
        }
    }

    public class CalculatorLogic
    {
        private Parser _parser = new Parser();
        private History _history = new History();
        private string _display = "0";
        private bool _newNumber = true;

        public History History => _history;
        public string CurrentInput => _display;
        public event Action<string> DisplayChanged;

        private void Notify() => DisplayChanged?.Invoke(_display);

        public void SetDisplay(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            _display = value;
            _newNumber = true;
            Notify();
        }

        private string Format(double number)
        {
            if (double.IsInfinity(number)) throw new CalculatorException("Слишком большое число!");

            if (Math.Abs(number) >= 1e15 || (Math.Abs(number) > 0 && Math.Abs(number) < 1e-10))
                return number.ToString("E10", CultureInfo.InvariantCulture).Replace(".", ",");

            return number.ToString("G15", CultureInfo.CurrentCulture);
        }

        private double ParseNumber(string text)
        {
            text = text.Replace(",", ".");
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;
            throw new CalculatorException($"Не удалось распознать число: {text}");
        }

        private double EvaluateDisplay()
        {
            string expression = _display.Replace(",", ".");

            while (expression.Length > 0)
            {
                char last = expression[expression.Length - 1];
                if (last == '+' || last == '-' || last == '*' || last == '/')
                    expression = expression.Remove(expression.Length - 1);
                else break;
            }

            return _parser.Evaluate(expression);
        }

        private string GetLastNumber()
        {
            string result = "";
            for (int i = _display.Length - 1; i >= 0; i--)
            {
                char c = _display[i];

                
                if ((c == '+' || c == '-') && i > 0)
                {
                    char prev = _display[i - 1];
                    if (prev == 'E' || prev == 'e')
                    {
                        result = c + result;
                        continue; 
                    }
                }

                if (c == '+' || c == '*' || c == '/') break;

                if (c == '-' && i > 0)
                {
                    char prev = _display[i - 1];
                    if (prev == '+' || prev == '*' || prev == '/')
                    {
                        result = c + result;
                        continue;
                    }
                    break;
                }
                result = c + result;
            }
            return result;
        }

        private void ReplaceLastNumber(double newValue)
        {
            string lastNumber = GetLastNumber();
            if (string.IsNullOrEmpty(lastNumber)) return;

            int lastNumStart = _display.Length - lastNumber.Length;
            string before = _display.Substring(0, lastNumStart);
            _display = before + Format(newValue);
            _newNumber = true;
        }

        public void ExecutePolymorphicOperation(MathOperation operation)
        {
            if (_display == "Ошибка") throw new CalculatorException("Исправьте ошибку!");

            if (operation is PiOperation || operation is EOperation)
            {
                double constVal = operation.Execute(0);
                if (_newNumber || _display == "0") _display = Format(constVal);
                else _display += Format(constVal);
                _newNumber = false;
                Notify();
                return;
            }

            string lastNumber = GetLastNumber();
            if (string.IsNullOrEmpty(lastNumber)) throw new CalculatorException("Нечего вычислять!");

            double num = ParseNumber(lastNumber);
            double result = operation.Execute(num);

            if (operation.Name == "1/")
                _history.Add($"1/({lastNumber}) = {Format(result)}");
            else
                _history.Add($"{operation.Name}({lastNumber}) = {Format(result)}");

            ReplaceLastNumber(result);
            Notify();
        }

        public void ProcessDigit(string digit)
        {
            if (_display == "Ошибка") { _display = "0"; _newNumber = true; }

            if (_newNumber)
            {
                _display = digit;
                _newNumber = false;
            }
            else
            {
                if (_display.Length < 45) _display += digit;
            }
            Notify();
        }

        public void ProcessOperation(string op)
        {
            if (_display == "Ошибка") { _display = "0"; _newNumber = true; }

            if (_display.Length > 0)
            {
                char last = _display[_display.Length - 1];

                // Защита от удаления знака внутри экспоненты (чтобы не стирать '+' из 'E+10')
                if (_display.Length > 1)
                {
                    char prev = _display[_display.Length - 2];
                    if (prev == 'E' || prev == 'e')
                    {
                        _display += op;
                        _newNumber = false;
                        Notify();
                        return;
                    }
                }

                if (last == '+' || last == '*' || last == '/')
                    _display = _display.Remove(_display.Length - 1);
                else if (last == '-' && _display.Length > 1)
                {
                    char prev = _display[_display.Length - 2];
                    if (prev == '+' || prev == '*' || prev == '/')
                        _display = _display.Remove(_display.Length - 2);
                }
            }

            _display += op;
            _newNumber = false;
            Notify();
        }

        public void ProcessEquals()
        {
            if (_display == "Ошибка") return;

            try
            {
                double result = EvaluateDisplay();
                _history.Add($"{_display} = {Format(result)}");
                _display = Format(result);
                _newNumber = true;
            }
            catch (CalculatorException)
            {
                _display = "Ошибка";
                throw;
            }
            Notify();
        }

        public void ProcessSignChange()
        {
            if (_display == "Ошибка") return;

            if (_display.Length > 0)
            {
                char last = _display[_display.Length - 1];
                if (last == '+' || last == '-' || last == '*' || last == '/') return;
            }

            string lastNumber = GetLastNumber();
            if (string.IsNullOrEmpty(lastNumber)) return;

            double num = ParseNumber(lastNumber);
            if (num == 0 && !lastNumber.Contains(",")) return;

            double result = -num;
            _history.Add($"±({lastNumber}) = {Format(result)}");
            ReplaceLastNumber(result);
            Notify();
        }

        public void ProcessPercent()
        {
            if (_display == "Ошибка") throw new CalculatorException("Исправьте ошибку!");

            string lastNumber = GetLastNumber();
            if (string.IsNullOrEmpty(lastNumber)) throw new CalculatorException("Нечего вычислять!");

            double percentNum = ParseNumber(lastNumber);
            string beforePercent = _display.Substring(0, _display.Length - lastNumber.Length);

            char op = '+';
            if (beforePercent.Length > 0)
            {
                char lastCh = beforePercent[beforePercent.Length - 1];
                if (lastCh == '+' || lastCh == '-' || lastCh == '*' || lastCh == '/')
                {
                    op = lastCh;
                    beforePercent = beforePercent.Remove(beforePercent.Length - 1);
                }
            }

            if (!string.IsNullOrEmpty(beforePercent))
            {
                double baseValue = _parser.Evaluate(beforePercent.Replace(",", "."));
                double percentValue = (baseValue * percentNum) / 100;

                double finalResult = baseValue;
                switch (op)
                {
                    case '+': finalResult = baseValue + percentValue; break;
                    case '-': finalResult = baseValue - percentValue; break;
                    case '*': finalResult = baseValue * (percentNum / 100); break;
                    case '/': finalResult = baseValue / (percentNum / 100); break;
                }

                _history.Add($"{beforePercent} {op} {percentNum}% = {Format(finalResult)}");
                _display = Format(finalResult);
            }
            else
            {
                double result = percentNum / 100;
                _history.Add($"{lastNumber}% = {Format(result)}");
                _display = Format(result);
            }

            _newNumber = true;
            Notify();
        }

        public void ProcessDecimalPoint()
        {
            if (_display == "Ошибка") { _display = "0"; _newNumber = true; }

            if (_newNumber)
            {
                _display = "0,";
                _newNumber = false;
            }
            else
            {
                string lastNum = GetLastNumber();
                if (string.IsNullOrEmpty(lastNum)) _display += "0,";
                else if (!lastNum.Contains(",") && !lastNum.ToUpper().Contains("E")) _display += ",";
            }
            Notify();
        }

        public void ProcessClear() { _display = "0"; _newNumber = true; Notify(); }
        public void ProcessClearEntry() { _display = "0"; _newNumber = true; Notify(); }

        public void ProcessBackspace()
        {
            if (_display == "Ошибка") { _display = "0"; _newNumber = true; Notify(); return; }

            if (_display.Length > 1)
            {
                _display = _display.Remove(_display.Length - 1);
                if (_display == "-") { _display = "0"; _newNumber = true; }
            }
            else { _display = "0"; _newNumber = true; }
            Notify();
        }
    }
}