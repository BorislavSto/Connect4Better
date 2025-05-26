using UnityEngine;

public class RaycastInput : MonoBehaviour
{
    public Camera cam;
    public LayerMask boardLayer;
    public BoardManager boardManager;

    void Update()
    {
        if (boardManager.isDropping) return;

        if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f, boardLayer))
        {
            for (int i = 0; i < boardManager.columns.Length; i++)
            {
                if (hit.collider.transform == boardManager.columns[i].columnTransform)
                {
                    if (Input.GetMouseButtonDown(0))
                        boardManager.DropPiece(i);
                    break;
                }
            }
        }
    }
}