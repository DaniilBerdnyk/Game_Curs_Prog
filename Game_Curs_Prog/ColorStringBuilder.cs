using System.Collections.Generic;
using System.Text;

public class ColorStringBuilder
{
    private readonly List<(char Symbol, ConsoleColor Color)> _buffer;
    private ConsoleColor _currentColor;

    public ColorStringBuilder()
    {
        _buffer = new List<(char, ConsoleColor)>();
        _currentColor = Console.ForegroundColor;
    }

    public void Append(char symbol, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            _currentColor = color.Value;
        }
        _buffer.Add((symbol, _currentColor));
    }

    public void Append(string value, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            _currentColor = color.Value;
        }
        foreach (char symbol in value)
        {
            _buffer.Add((symbol, _currentColor));
        }
    }

    public void AppendLine()
    {
        _buffer.Add(('\n', _currentColor)); // Используем '\n' для новой строки
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var (Symbol, _) in _buffer)
        {
            sb.Append(Symbol);
        }
        return sb.ToString();
    }

    public void Render()
    {
        foreach (var (Symbol, Color) in _buffer)
        {
            Console.ForegroundColor = Color;
            Console.Write(Symbol);
        }
        Console.ResetColor();
    }
}


