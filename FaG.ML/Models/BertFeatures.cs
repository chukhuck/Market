using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace FaG.ML.Models
{
  public class BertFeatures
  {
    [ColumnName("output_0")]
    public float[] Features { get; set; }
  }
}
