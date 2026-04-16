using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace FaG.ML.Models
{
  public class TextSentimentOutput
  {
    /// <summary>
    /// Column 1: Sentiment score from -1 (negative) to 1 (positive)
    /// </summary>
    public float Score { get; set; }
  }
}
