using System.Collections.Generic;
using NeonDash.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Per-seed record book: every seed played, its best distance, holder and score.</summary>
    public sealed class UiRecords : UiScreen
    {
        RectTransform card;
        RectTransform list;
        Text emptyText;
        readonly List<GameObject> rows = new List<GameObject>();

        protected override void Construct()
        {
            var shade = UiKit.Sprite9(Root, "Shade", new Color(0.01f, 0.02f, 0.05f, 0.78f), UiKit.Glow, false);
            UiKit.Stretch(shade.rectTransform);

            var glow = UiKit.Sprite9(Root, "Card Glow", new Color(0.1f, 0.55f, 0.95f, 0.09f), UiKit.Glow, false);
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1260f, 940f));

            card = UiKit.Frame(Root, "Record archive", Palette.Violet);
            UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1000f, 700f));

            var title = UiKit.Label(card, "SEED RECORDS", 46, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(700f, 56f));

            var subtitle = UiKit.Label(card, "EVERY SEED KEEPS ITS OWN BEST DISTANCE AND PAYOUT", 18, Palette.TextDim);
            UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(880f, 28f));

            var header = UiKit.Container(card, "Column headings");
            UiKit.Place(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(890f, 30f));
            AddHeading(header, "SEED", 20f);
            AddHeading(header, "BEST", 280f);
            AddHeading(header, "PLAYER", 407f);
            AddHeading(header, "MULTIPLIER", 618f);
            AddHeading(header, "SCORE", 870f, true);

            var listRect = UiKit.Container(card, "List");
            list = listRect;
            UiKit.Place(listRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 460f));

            emptyText = UiKit.Label(list, "NO RUNS YET - PLAY A SEED TO START A RECORD", 24, Palette.TextDim);
            UiKit.Place(emptyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(800f, 40f));

            Text backLabel;
            Button back = UiKit.Button(card, "BACK", Palette.PanelSoft, out backLabel, 26);
            UiKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(240f, 62f));
            UiKit.AddButtonIcon(backLabel, IconKind.Back);
            back.onClick.AddListener(delegate
            {
                App.CloseRecords();
            });
        }

        static void AddHeading(RectTransform row, string caption, float x, bool right = false)
        {
            var alignment = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            var label = UiKit.Label(row, caption, 20, Palette.Violet, alignment, FontStyle.Bold);
            var anchor = new Vector2(0f, 0.5f);
            var pivot = right ? new Vector2(1f, 0.5f) : anchor;
            UiKit.Place(label.rectTransform, anchor, pivot, new Vector2(x, 0f), new Vector2(160f, 28f));
        }

        public void Present(RecordTable table)
        {
            gameObject.SetActive(true);
            UiKit.FadeIn(Group, 0.2f);
            BuildRows(table);
            card.anchoredPosition = new Vector2(0f, -40f);
            card.localScale = Vector3.one * 0.94f;
            TweenRunner.Run(0.42f, k =>
            {
                if (card == null) return;
                card.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-40f, 0f, k));
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.94f, 1f, k);
            }, 0f, Ease.OutBack);
        }

        void BuildRows(RecordTable table)
        {
            for (int i = 0; i < rows.Count; i++) Destroy(rows[i]);
            rows.Clear();

            List<SeedRecord> best = table.Best(9);
            emptyText.gameObject.SetActive(best.Count == 0);

            for (int i = 0; i < best.Count; i++)
            {
                SeedRecord record = best[i];
                var rowImage = UiKit.Sprite9(list, "Row " + i, i % 2 == 0
                    ? Palette.Alpha(Palette.PanelSoft, 0.55f)
                    : new Color(0.05f, 0.07f, 0.14f, 0.35f), UiKit.Chamfered);
                UiKit.Place(rowImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -6f - i * 50f), new Vector2(890f, 44f));

                var rowAccent = UiKit.Sprite9(rowImage.transform, "Accent", i == 0 ? Palette.Gold : Palette.Alpha(Palette.Cyan, 0.35f), UiKit.Chamfered);
                UiKit.Place(rowAccent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(3f, 22f));

                Color textColor = i == 0 ? Palette.Gold : Palette.Text;
                var seedLabel = UiKit.Label(rowImage.transform, record.Seed, 18, textColor, TextAnchor.MiddleLeft, FontStyle.Bold);
                UiKit.Place(seedLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(245f, 34f));

                var distanceLabel = UiKit.Label(rowImage.transform, record.BestDistance + " m", 22, Palette.Cyan, TextAnchor.MiddleLeft);
                UiKit.Place(distanceLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(280f, 0f), new Vector2(160f, 34f));

                var nameLabel = UiKit.Label(rowImage.transform, record.Name, 22, Palette.Text, TextAnchor.MiddleLeft);
                UiKit.Place(nameLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(407f, 0f), new Vector2(200f, 34f));

                var multiplierLabel = UiKit.Label(rowImage.transform, record.BestMultiplier.ToString("0.00") + "x", 22, Palette.Magenta, TextAnchor.MiddleLeft);
                UiKit.Place(multiplierLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(618f, 0f), new Vector2(160f, 34f));

                var payoutLabel = UiKit.Label(rowImage.transform, record.BestPayout.ToString("N0"), 22, Palette.Gold, TextAnchor.MiddleRight);
                UiKit.Place(payoutLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(200f, 34f));

                rows.Add(rowImage.gameObject);

                // Staggered slide-in.
                RectTransform rt = rowImage.rectTransform;
                Vector2 target = rt.anchoredPosition;
                rt.anchoredPosition = target + new Vector2(0f, -18f);
                float delay = 0.03f * i;
                TweenRunner.Run(0.3f, k =>
                {
                    if (rt == null) return;
                    rt.anchoredPosition = Vector2.LerpUnclamped(target + new Vector2(0f, -18f), target, k);
                }, delay, Ease.OutCubic);

                var group = rowImage.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                UiKit.FadeIn(group, 0.28f, delay);
            }
        }
    }
}
