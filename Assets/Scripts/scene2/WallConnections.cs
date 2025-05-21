using System;

[Serializable]
public class WallConnections
{
    public WallConfig[] walls;
}

[Serializable]
public class WallConfig
{
    public string wallName;
    public SlotConfig[] slots;
}

[Serializable]
public class SlotConfig
{
    public string slotName;
    public string[] inputs;
}