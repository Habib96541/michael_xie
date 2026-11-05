using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace com.javierquevedo
{
    public class JQUtils
    {
        public static T GetRandomEnum<T>()
        {
            System.Array A = System.Enum.GetValues(typeof(T));
            T V = (T)A.GetValue(UnityEngine.Random.Range(0, A.Length));
            return V;
        }

        public static BubbleColor GetSmartBubbleColor(ArrayList existingBubbles)
        {
            if (existingBubbles == null || existingBubbles.Count == 0)
                return GetRandomEnum<BubbleColor>();

            // Collect distinct colors currently on the board
            List<BubbleColor> activeColors = new List<BubbleColor>();
            foreach (Bubble b in existingBubbles)
            {
                if (b != null && !activeColors.Contains(b.color))
                    activeColors.Add(b.color);
            }

            if (activeColors.Count > 0)
                return activeColors[UnityEngine.Random.Range(0, activeColors.Count)];

            return GetRandomEnum<BubbleColor>();
        }

        public static Color ColorForBubbleColor(BubbleColor color)
        {
            return CyberArtGenerator.GetPrimaryColor(color);
        }

        public static ArrayList FilterByColor(ArrayList bubbles, BubbleColor color)
        {
            ArrayList filtered = new ArrayList();
            if (bubbles == null) return filtered;
            foreach (Bubble bubble in bubbles)
            {
                if (bubble != null && bubble.color == color)
                    filtered.Add(bubble);
            }
            return filtered;
        }
    }
}
