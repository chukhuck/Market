using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace FaG.ML.Models
{
  public class EmbeddingInput
  {
    [ColumnName("RawFeatures")]
    public VBuffer<float> RawFeatures { get; set; }
  }

  public class EmbeddingOutput
  {
    [VectorType(1024)]
    [ColumnName("output_0")]
    public float[] Features { get; set; }
  }
}
