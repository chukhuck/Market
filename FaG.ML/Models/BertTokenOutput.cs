using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace FaG.ML.Models
{
  public class BertTokenOutput
  {
    [ColumnName("input_ids")]
    public long[] InputIds { get; set; } = [];

    [ColumnName("attention_mask")]
    public long[] AttentionMask { get; set; } = [];
  }
}
