﻿using UnityEngine;
using System.Collections;
[System.Serializable]
public class UnitBase {
    public int ID;
    public string Name;
    public void Clone(UnitBase copier)
    {
        ID = copier.ID;
        Name = copier.Name;
    }
}
