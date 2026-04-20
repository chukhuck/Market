using System;
using System.Collections.Generic;
using System.Text;

namespace FaG.ML.Utilities
{

  public static class StopWords
  {
    public static readonly HashSet<string> RussianStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "я", "ты", "он", "она", "оно", "мы", "вы", "они",
        "мой", "твой", "его", "её", "наш", "ваш", "их",
        "этот", "тот", "такой", "какой", "который",
        "в", "на", "по", "с", "о", "об", "за", "над", "под", "перед", "между",
        "и", "а", "но", "или", "что", "как", "когда", "где", "чтобы",
        // Добавьте свои слова по необходимости
    };
  }
}
