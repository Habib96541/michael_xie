using System;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths
{
    public partial class FishingHUD
    {
        Action refreshWindow;
        RectTransform openPanel;

        void OnRectTransformDimensionsChange()
        {
            if (IsModalOpen) FitOpenPanel();
        }

        void FitOpenPanel()
        {
            if (!openPanel) return;
            Vector2 available = modalLayer.rect.size - Vector2.one * 24;
            if (available.x <= 0 || available.y <= 0) return;
            Vector2 size = openPanel.rect.size;
            float scale = Mathf.Min(1, available.x / size.x, available.y / size.y);
            openPanel.GetComponent<UiMotion>().SetRestScale(Vector3.one * scale);
        }

        void ClearWindowChildren()
        {
            refreshWindow=null;
            openPanel=null;
            if(prizeReveal!=null){StopCoroutine(prizeReveal);prizeReveal=null;}
            foreach(Transform child in modalLayer){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        void AddWindowClose(RectTransform panel)
        {
            openPanel=panel;
            FitOpenPanel();
            var size=panel.rect.size;
            var button=UiKit.Button("Close window",panel,Art("ref_icon_close"),Color.white,size.x/2-36,size.y/2-36,64,64);
            if(panel.GetComponent<UnityEngine.UI.Image>().sprite==Art("ref_collection_panel"))
                button.GetComponent<RectTransform>().anchoredPosition=new Vector2(size.x/2-66,size.y/2-64);
            button.image.preserveAspect=true;
            PadHitArea(button,17,17);
            button.onClick.AddListener(()=>{game.Sound(game.clickSound);CloseModal();});
        }
        static string Remaining(long seconds)
        {
            var time=TimeSpan.FromSeconds(Math.Max(0,seconds));
            return ((int)time.TotalHours).ToString("00")+":"+time.Minutes.ToString("00")+":"+time.Seconds.ToString("00");
        }
    }
}
