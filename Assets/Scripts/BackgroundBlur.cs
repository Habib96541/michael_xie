using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class BackgroundBlur : MonoBehaviour
{
    Material blurMaterial;
    public bool Active { get; set; }

    void Awake()
    {
        Shader shader = Resources.Load<Shader>("Shaders/ChestShuffleBlur");
        if (shader != null)
        {
            blurMaterial = new Material(shader);
            blurMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!Active || blurMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        int width = Mathf.Max(1, source.width / 3);
        int height = Mathf.Max(1, source.height / 3);
        RenderTexture buffer = RenderTexture.GetTemporary(width, height, 0, source.format);
        blurMaterial.SetFloat("_BlurSize", 1.5f);
        Graphics.Blit(source, buffer, blurMaterial, 0);
        Graphics.Blit(buffer, destination, blurMaterial, 1);
        RenderTexture.ReleaseTemporary(buffer);
    }

    void OnDestroy()
    {
        if (blurMaterial != null) Destroy(blurMaterial);
    }
}
