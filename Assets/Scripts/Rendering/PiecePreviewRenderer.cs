using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DropStack.Data;

namespace DropStack.Rendering
{
    public class PiecePreviewRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float blockSize = 26f;
        [SerializeField] private RectTransform container;

        private const int MaxBlocks = 5;
        private readonly List<Image> blockImages = new();
        private PolyominoShape displayedShape;
        public void SetBlockSize(float size) { blockSize=size; if(displayedShape!=null) SetShape(displayedShape); }

        private void Awake()
        {
            if (container == null) container = GetComponent<RectTransform>();
            InitializeImages();
        }

        private void InitializeImages()
        {
            if (container == null) return;

            Sprite blockSprite = BlockSpriteGenerator.BlockSprite;

            for (int i = 0; i < MaxBlocks; i++)
            {
                GameObject blockGo = new GameObject($"PreviewBlock_{i}", typeof(RectTransform), typeof(Image));
                blockGo.transform.SetParent(container, false);

                RectTransform rt = blockGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(blockSize, blockSize);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                Image img = blockGo.GetComponent<Image>();
                img.sprite = blockSprite;
                img.raycastTarget = false;
                img.enabled = false;

                blockImages.Add(img);
            }
        }

        public void SetShape(PolyominoShape shape)
        {
            displayedShape=shape;
            if (blockImages.Count == 0) InitializeImages();

            if (shape == null)
            {
                Clear();
                return;
            }

            Vector2Int[] blocks = shape.GetBlocks(0);
            BoundsInt bounds = shape.GetBoundingBox(0);

            // Compute center of bounds to align in the middle of container
            float centerOffsetX = bounds.center.x - 0.5f;
            float centerOffsetY = bounds.center.y - 0.5f;

            for (int i = 0; i < MaxBlocks; i++)
            {
                if (i < blocks.Length)
                {
                    Vector2Int b = blocks[i];
                    Image img = blockImages[i];
                    img.enabled = true;
                    img.color = shape.primaryColor;

                    RectTransform rt = img.rectTransform;
                    float posX = (b.x - centerOffsetX) * blockSize;
                    float posY = (b.y - centerOffsetY) * blockSize;
                    rt.anchoredPosition = new Vector2(posX, posY);
                    rt.sizeDelta = new Vector2(blockSize, blockSize);
                }
                else
                {
                    blockImages[i].enabled = false;
                }
            }
        }

        public void Clear()
        {
            displayedShape=null;
            foreach (var img in blockImages)
            {
                if (img != null) img.enabled = false;
            }
        }
    }
}
