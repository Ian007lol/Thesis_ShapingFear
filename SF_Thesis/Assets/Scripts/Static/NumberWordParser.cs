using System;
using System.Collections.Generic;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public static class NumberWordParser
{
    // 0–19
    private static readonly Dictionary<string, int> small = new Dictionary<string, int>()
    {
        {"zero",0},{"one",1},{"two",2},{"three",3},{"four",4},{"five",5},
        {"six",6},{"seven",7},{"eight",8},{"nine",9},{"ten",10},
        {"eleven",11},{"twelve",12},{"thirteen",13},{"fourteen",14},
        {"fifteen",15},{"sixteen",16},{"seventeen",17},{"eighteen",18},{"nineteen",19}
    };

    // 20,30,...90
    private static readonly Dictionary<string, int> tens = new Dictionary<string, int>()
    {
        {"twenty",20},{"thirty",30},{"forty",40},{"fifty",50},
        {"sixty",60},{"seventy",70},{"eighty",80},{"ninety",90}
    };

    /// <summary>
    /// Parses simple English number words like:
    /// "thirty two", "forty-two", "ninety nine" → int
    /// Returns false if it sees an unknown word.
    /// </summary>
    public static bool TryParseNumberWords(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        text = text.ToLower().Replace("-", " ");

        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int result = 0;

        foreach (var raw in parts)
        {
            string p = raw;

            // Ignore filler like "and" or Spanish "e"
            if (p == "and" || p == "e")
                continue;

            if (small.TryGetValue(p, out int sVal))
            {
                result += sVal;
            }
            else if (tens.TryGetValue(p, out int tVal))
            {
                result += tVal;
            }
            else
            {
                return false; // unknown word
            }
        }

        value = result;
        return result > 0;
    }
    public static string ToWords(string digits)
{
    if (!int.TryParse(digits, out int number))
        return digits;

    return ToWords(number);
}

public static string ToWords(int number)
{
    if (number < 0 || number > 99)
        return number.ToString();

    string[] ones =
    {
        "zero","one","two","three","four","five","six","seven","eight","nine",
        "ten","eleven","twelve","thirteen","fourteen","fifteen","sixteen",
        "seventeen","eighteen","nineteen"
    };

    string[] tens =
    {
        "", "", "twenty","thirty","forty","fifty","sixty","seventy","eighty","ninety"
    };

    if (number < 20)
        return ones[number];

    int t = number / 10;
    int o = number % 10;

    if (o == 0)
        return tens[t];

    return $"{tens[t]} {ones[o]}";
}

}
