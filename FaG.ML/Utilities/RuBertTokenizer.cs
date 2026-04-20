namespace FaG.ML.Utilities
{
  public class RuBertTokenizer
  {
    private readonly Dictionary<string, int> _vocab;
    private const int MaxLength = 512;
    private const long PadTokenId = 0;   // [PAD]
    private const long ClsTokenId = 2;  // 
    private const long SepTokenId = 3;  // 

    public RuBertTokenizer(string vocabPath)
    {
      _vocab = LoadVocabulary(vocabPath);
    }

    private Dictionary<string, int> LoadVocabulary(string path)
    {
      return File.ReadLines(path)
          .Select((line, index) => new { Token = line, Id = index })
          .ToDictionary(x => x.Token, x => x.Id);
    }

    public (long[] inputIds, long[] attentionMask) Tokenize(string text)
    {
      if (text.Length > 2500)
      {
        text = text.Substring(0, 2500);
      }

      var words = text.Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '(', ')' },
          StringSplitOptions.RemoveEmptyEntries);

      var filteredWords = words.Where(word => !StopWords.RussianStopWords.Contains(word) && word.Length > 2);

      var tokens = new List<long> { ClsTokenId }; // ClsTokenId тоже должен быть long

      foreach (var word in filteredWords)
      {
        if (tokens.Count >= MaxLength - 2) break;
        var lowerWord = word;
        tokens.Add(_vocab.TryGetValue(lowerWord, out var tokenId) ? tokenId : 1L); // 1L = [UNK]
      }

      tokens.Add(SepTokenId); // SepTokenId тоже должен быть long

      // Дополняем до максимальной длины
      var paddingLength = MaxLength - tokens.Count;
      var inputIds = tokens.Concat(Enumerable.Repeat(PadTokenId, paddingLength)).ToArray();
      var attentionMask = Enumerable.Repeat(1L, tokens.Count)
          .Concat(Enumerable.Repeat(0L, paddingLength))
          .ToArray();

      return (inputIds.Take(MaxLength).ToArray(), attentionMask.Take(MaxLength).ToArray());
    }

  }

}
