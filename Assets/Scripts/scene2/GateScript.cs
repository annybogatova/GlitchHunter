using UnityEngine;

public class GateScript : MonoBehaviour
{
    public enum GateType { AND, OR }
    public GateType gateType;

    public bool ComputeOutput(bool input1, bool input2)
    {
        switch (gateType)
        {
            case GateType.AND:
                return input1 && input2;
            case GateType.OR:
                return input1 || input2;
            default:
                return false;
        }
    }
}
