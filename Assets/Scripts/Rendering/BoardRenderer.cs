using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DropStack.Data;

namespace DropStack.Rendering
{
    public class BoardRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float cellSize = 0.8f;
        [SerializeField] private Vector2 originOffset = new Vector2(-4.0f, -8.0f); // Centers 10x20 nicely

        private SpriteRenderer[,] gridTileRenderers;
        private SpriteRenderer[,] blockRenderers;
        private Transform gridParent;
        private Transform blocksParent;

        public float CellSize => cellSize;
        public Vector2 OriginOffset => originOffset;

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(originOffset.x + x * cellSize + cellSize * 0.5f,
                               originOffset.y + y * cellSize + cellSize * 0.5f,
                               0f);
        }

        public void Initialize(int columns, int visibleRows, int totalRows)
        {
            // Clear any old children
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            gridParent = new GameObject("GridBackground").transform;
            gridParent.SetParent(transform, false);

            blocksParent = new GameObject("LockedBlocks").transform;
            blocksParent.SetParent(transform, false);

            gridTileRenderers = new SpriteRenderer[columns, visibleRows];
            blockRenderers = new SpriteRenderer[columns, totalRows];

            Sprite gridSprite = BlockSpriteGenerator.GridTileSprite;
            Sprite blockSprite = BlockSpriteGenerator.BlockSprite;

            // Spawn background grid tiles
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < visibleRows; y++)
                {
                    GameObject go = new GameObject($"Tile_{x}_{y}");
                    go.transform.SetParent(gridParent, false);
                    go.transform.position = GridToWorld(x, y);
                    go.transform.localScale = Vector3.one * (cellSize);

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = gridSprite;
                    sr.sortingOrder = 0;
                    gridTileRenderers[x, y] = sr;
                }
            }

            // Spawn block renderers pool for entire matrix
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < totalRows; y++)
                {
                    GameObject go = new GameObject($"Block_{x}_{y}");
                    go.transform.SetParent(blocksParent, false);
                    go.transform.position = GridToWorld(x, y);
                    go.transform.localScale = Vector3.one * (cellSize);

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = blockSprite;
                    sr.sortingOrder = 10;
                    sr.enabled = false;
                    blockRenderers[x, y] = sr;
                }
            }
        }

        public void RefreshBoard(int columns, int totalRows, System.Func<int, int, int> getCellFunc)
        {
            StopAllCoroutines();
            if (blockRenderers == null) return;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < totalRows; y++)
                {
                    int cellType = getCellFunc(x, y);
                    SpriteRenderer sr = blockRenderers[x, y];
                    if (sr == null) continue;

                    if (cellType > 0 && y < DropStack.Game.DropStackBoard.VisibleRows)
                    {
                        var shape = PolyominoDatabase.GetShape((PolyominoType)cellType);
                        sr.enabled = true;
                        sr.color = shape != null ? shape.primaryColor : Color.white;
                        sr.transform.localScale = Vector3.one * cellSize;
                    }
                    else
                    {
                        sr.enabled = false;
                    }
                }
            }
        }

        public void PlayLineClearFlash(List<int> lines, int columns)
        {
            if (lines == null || blockRenderers == null) return;
            StartCoroutine(FlashLinesRoutine(lines, columns));
        }

        private IEnumerator FlashLinesRoutine(List<int> lines, int columns)
        {
            float duration = 0.18f;
            float elapsed = 0f;

            // Highlight white
            foreach (int y in lines)
            {
                if (y >= DropStack.Game.DropStackBoard.VisibleRows) continue;
                for (int x = 0; x < columns; x++)
                {
                    SpriteRenderer sr = blockRenderers[x, y];
                    if (sr != null && sr.enabled)
                    {
                        sr.color = Color.white;
                    }
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(1.0f, 0.0f, t);

                foreach (int y in lines)
                {
                    if (y >= DropStack.Game.DropStackBoard.VisibleRows) continue;
                    for (int x = 0; x < columns; x++)
                    {
                        SpriteRenderer sr = blockRenderers[x, y];
                        if (sr != null && sr.enabled)
                        {
                            sr.transform.localScale = Vector3.one * (cellSize * scale);
                        }
                    }
                }
                yield return null;
            }
        }
    }
}
