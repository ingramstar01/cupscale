﻿using System.IO;

namespace Cupscale.Data
{
  struct ModelData
  {
    public string model1Name;
    public string model2Name;
    public string model1Path;
    public string model2Path;
    public enum ModelMode { Single, Interp, Chain, Advanced }
    public ModelMode mode;
    public int interp;

    public ModelData(string model1, string model2, ModelMode modelMode, int interpolation = 0)
    {
      model1Name = Path.GetFileNameWithoutExtension(model1);
      model2Name = Path.GetFileNameWithoutExtension(model2);
      model1Path = model1;
      model2Path = model2;
      mode = modelMode;
      interp = interpolation;
    }
  }
}
