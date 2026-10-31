using System.Collections.Generic;
using UnityEngine;
using DropStack.Data;
using DropStack.Game;

namespace DropStack.Rendering
{
    public class ActivePieceRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardRenderer boardRenderer;
        [SerializeField] private ActivePieceController pieceController;

        private const int MaxBlocks = 5; // Max polyomino block count
        private readonly List<SpriteRenderer> activeBlockRenderers = new();
        private readonly List<SpriteRenderer> ghostBlockRenderers = new();

        private void Awake()
        {
            InitializeRenderers();
        }

        private void InitializeRenderers()
        {
            Sprite blockSprite = BlockSpriteGenerator.BlockSprite;
            Sprite ghostSprite = BlockSpriteGenerator.GhostSprite;

            GameObject activeRoot = new GameObject("ActivePieceBlocks");
            activeRoot.transform.SetParent(transform, false);

            GameObject ghostRoot = new GameObject("GhostPieceBlocks");
            ghostRoot.transform.SetParent(transform, false);

            for (int i = 0; i < MaxBlocks; i++)
            {
                // Active piece block
                GameObject aGo = new GameObject($"ActiveBlock_{i}");
                aGo.transform.SetParent(activeRoot.transform, false);
                SpriteRenderer aSr = aGo.AddComponent<SpriteRenderer>();
                aSr.sprite = blockSprite;
                aSr.sortingOrder = 20;
                aSr.enabled = false;
                activeBlockRenderers.Add(aSr);

                // Ghost block
                GameObject gGo = new GameObject($"GhostBlock_{i}");
                gGo.transform.SetParent(ghostRoot.transform, false);
                SpriteRenderer gSr = gGo.AddComponent<SpriteRenderer>();
                gSr.sprite = ghostSprite;
                gSr.sortingOrder = 15;
                gSr.enabled = false;
                ghostBlockRenderers.Add(gSr);
            }
        }

        public void Bind(BoardRenderer bRenderer, ActivePieceController controller)
        {
            if (pieceController != null) pieceController.OnPieceMoved -= HandlePieceMoved;
            boardRenderer = bRenderer;
            pieceController = controller;
            if (isActiveAndEnabled && pieceController != null) pieceController.OnPieceMoved += HandlePieceMoved;
        }

        private void OnEnable()
        {
            if (pieceController != null) pieceController.OnPieceMoved += HandlePieceMoved;
        }

        private void OnDisable()
        {
            if (pieceController != null) pieceController.OnPieceMoved -= HandlePieceMoved;
        }

        private void OnDestroy()
        {
            if (pieceController != null)
            {
                pieceController.OnPieceMoved -= HandlePieceMoved;
            }
        }

        private void HandlePieceMoved(PolyominoShape shape, Vector2Int pos, int rot)
        {
            if (shape == null || boardRenderer == null)
            {
                HideAll();
                return;
            }

            Vector2Int[] blocks = shape.GetBlocks(rot);
            int ghostY = pieceController != null ? pieceController.CalculateGhostY() : pos.y;
            float cellSize = boardRenderer.CellSize;

            for (int i = 0; i < MaxBlocks; i++)
            {
                if (i < blocks.Length)
                {
                    Vector2Int b = blocks[i];

                    // Active block
                    Vector2Int activeCell = pos + b;
                    SpriteRenderer aSr = activeBlockRenderers[i];
                    if (activeCell.y < DropStackBoard.VisibleRows)
                    {
                        aSr.enabled = true;
                        aSr.transform.position = boardRenderer.GridToWorld(activeCell.x, activeCell.y);
                        aSr.transform.localScale = Vector3.one * cellSize;
                        aSr.color = shape.primaryColor;
                    }
                    else
                    {
                        // In buffer zone above visible rows
                        aSr.enabled = false;
                        aSr.transform.position = boardRenderer.GridToWorld(activeCell.x, activeCell.y);
                        aSr.transform.localScale = Vector3.one * cellSize;
                        Color c = shape.primaryColor;
                        c.a = 0.45f;
                        aSr.color = c;
                    }

                    // Ghost block
                    Vector2Int ghostCell = new Vector2Int(pos.x, ghostY) + b;
                    SpriteRenderer gSr = ghostBlockRenderers[i];
                    if (ghostCell.y < DropStackBoard.VisibleRows && ghostY != pos.y)
                    {
                        gSr.enabled = true;
                        gSr.transform.position = boardRenderer.GridToWorld(ghostCell.x, ghostCell.y);
                        gSr.transform.localScale = Vector3.one * cellSize;
                        Color gc = shape.primaryColor;
                        gc.a = 0.4f;
                        gSr.color = gc;
                    }
                    else
                    {
                        gSr.enabled = false;
                    }
                }
                else
                {
                    activeBlockRenderers[i].enabled = false;
                    ghostBlockRenderers[i].enabled = false;
                }
            }
        }

        public void HideAll()
        {
            foreach (var sr in activeBlockRenderers) if (sr != null) sr.enabled = false;
            foreach (var sr in ghostBlockRenderers) if (sr != null) sr.enabled = false;
        }
    }
}
