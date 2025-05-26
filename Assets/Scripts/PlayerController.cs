using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public CellState myPlayerSide;

    public override void OnStartLocalPlayer()
    {
        Debug.Log("Local Player Ready: " + myPlayerSide);
    }
}