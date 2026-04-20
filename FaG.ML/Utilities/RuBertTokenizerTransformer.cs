using FaG.ML.Models;
using FaG.ML.Utilities;
using Microsoft.ML.Transforms;



[CustomMappingFactoryAttribute(nameof(TokenizeMappings.TokenizeMapping))]
public class TokenizeMappings : CustomMappingFactory<TextSentimentInput, BertTokenOutput>
{
  private static readonly RuBertTokenizer tokenizer = new RuBertTokenizer("./Data/onnx/rubert-tiny2-onnx/vocab.txt");

  // This is the custom mapping. We now separate it into a method, so that we can use it both in training and in loading.
  public static void TokenizeMapping(TextSentimentInput input, BertTokenOutput output) => (output.InputIds, output.AttentionMask) = tokenizer.Tokenize(input.Body);

  // This factory method will be called when loading the model to get the mapping operation.
  public override Action<TextSentimentInput, BertTokenOutput> GetMapping()
  {
    return TokenizeMapping;
  }
}

