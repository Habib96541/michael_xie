using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A single treasure chest, modelled from primitives at build time. The root
/// transform is moved by the game during the shuffle; this component animates
/// the body (hover lift, idle bob), the hinged lid, its gold trim glow and the
/// selection ring beneath it.
/// </summary>
public sealed class Chest3D : MonoBehaviour
{
    public int Number { get; private set; }
    public float Openness => openAmount;
    public bool IsOpen => openAmount > 0.96f;

    Transform bodyRoot;
    Transform lidPivot;
    Transform focusRing;
    Light glowLight;
    Renderer[] goldParts;
    MaterialPropertyBlock goldBlock;
    Color goldEmission = new Color(1f, 0.68f, 0.26f);

    float hoverAmount;
    float hoverTarget;
    float focusAmount;
    float focusTarget;
    float spotlightAmount;
    float spotlightTarget;
    float openAmount;
    float openTarget;
    float bobPhase;

    public void Build(int number)
    {
        Number = number;
        bobPhase = number * 1.7f;
        goldBlock = new MaterialPropertyBlock();

        Material wood = MaterialLibrary.Get("Wood");
        Material woodDark = MaterialLibrary.Get("WoodDark");
        Material gold = MaterialLibrary.Get("Gold");
        Material iron = MaterialLibrary.Get("Iron");
        Material glow = MaterialLibrary.Get("GoldGlow");

        var goldPartsList = new List<Renderer>();

        bodyRoot = new GameObject("Body").transform;
        bodyRoot.SetParent(transform, false);

        // Feet and lower band.
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Box(bodyRoot, "Foot", new Vector3(0.20f, 0.12f, 0.20f),
                    new Vector3(sx * 0.66f, 0.06f, sz * 0.40f), iron);

        goldPartsList.Add(Box(bodyRoot, "Base Band", new Vector3(1.70f, 0.06f, 1.10f),
            new Vector3(0f, 0.16f, 0f), gold));

        // Chest body.
        Box(bodyRoot, "Base", new Vector3(1.64f, 0.62f, 1.04f),
            new Vector3(0f, 0.47f, 0f), wood);
        Box(bodyRoot, "Base Shade", new Vector3(1.66f, 0.08f, 1.06f),
            new Vector3(0f, 0.22f, 0f), woodDark);

        // Vertical straps on the front and back faces.
        for (int sx = -1; sx <= 1; sx += 2)
        {
            goldPartsList.Add(Box(bodyRoot, "Strap Front", new Vector3(0.09f, 0.60f, 0.035f),
                new Vector3(sx * 0.57f, 0.47f, -0.525f), gold));
            goldPartsList.Add(Box(bodyRoot, "Strap Back", new Vector3(0.09f, 0.60f, 0.035f),
                new Vector3(sx * 0.57f, 0.47f, 0.525f), gold));
        }

        // Side handles.
        for (int sx = -1; sx <= 1; sx += 2)
            Box(bodyRoot, "Handle", new Vector3(0.05f, 0.18f, 0.24f),
                new Vector3(sx * 0.855f, 0.50f, 0f), iron);

        // Lock plate and keyhole.
        goldPartsList.Add(Box(bodyRoot, "Lock Plate", new Vector3(0.30f, 0.34f, 0.07f),
            new Vector3(0f, 0.60f, -0.545f), gold));
        Cylinder(bodyRoot, "Keyhole", new Vector3(0.045f, 0.015f, 0.045f),
            new Vector3(0f, 0.58f, -0.585f), iron, new Vector3(90f, 0f, 0f));

        // Gold slab of treasure on top of the body, revealed when the lid opens.
        Box(bodyRoot, "Treasure", new Vector3(1.26f, 0.04f, 0.74f),
            new Vector3(0f, 0.785f, 0f), glow);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * 2.4f + number;
            float radius = 0.10f + 0.09f * (i % 3);
            Cylinder(bodyRoot, "Coin", new Vector3(0.17f, 0.025f, 0.17f),
                new Vector3(Mathf.Cos(angle) * radius * 2.4f, 0.815f, Mathf.Sin(angle) * radius * 1.3f),
                gold, new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
        }

        // Hinged lid. The pivot sits on the back top edge, so a positive X
        // rotation lifts the front edge and swings the lid open.
        lidPivot = new GameObject("Lid Pivot").transform;
        lidPivot.SetParent(bodyRoot, false);
        lidPivot.localPosition = new Vector3(0f, 0.79f, 0.52f);

        Box(lidPivot, "Lid", new Vector3(1.68f, 0.20f, 1.08f),
            new Vector3(0f, 0.10f, -0.53f), wood);
        Cylinder(lidPivot, "Lid Top", new Vector3(0.36f, 0.86f, 0.36f),
            new Vector3(0f, 0.20f, -0.53f), wood, new Vector3(0f, 0f, 90f));

        goldPartsList.Add(Box(lidPivot, "Lid Lip", new Vector3(1.74f, 0.085f, 0.055f),
            new Vector3(0f, 0.02f, -1.065f), gold));
        for (int sx = -1; sx <= 1; sx += 2)
            goldPartsList.Add(Box(lidPivot, "Lid Cap", new Vector3(0.085f, 0.24f, 1.10f),
                new Vector3(sx * 0.85f, 0.10f, -0.53f), gold));
        goldPartsList.Add(Box(lidPivot, "Hasp", new Vector3(0.26f, 0.16f, 0.055f),
            new Vector3(0f, 0.015f, -1.08f), gold));

        for (int sx = -1; sx <= 1; sx += 2)
            Cylinder(lidPivot, "Hinge", new Vector3(0.14f, 0.12f, 0.14f),
                new Vector3(sx * 0.56f, 0f, -0.02f), gold, new Vector3(0f, 0f, 90f));

        goldParts = goldPartsList.ToArray();

        // Warm light that spills out of the chest as it opens.
        var lightObject = new GameObject("Chest Glow");
        lightObject.transform.SetParent(bodyRoot, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.86f, 0f);
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = new Color(1f, 0.76f, 0.36f);
        glowLight.range = 3.4f;
        glowLight.intensity = 0f;
        glowLight.shadows = LightShadows.None;

        // Selection ring.
        var ringObject = new GameObject("Focus Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        var ringFilter = ringObject.AddComponent<MeshFilter>();
        ringFilter.sharedMesh = MeshUtils.Ring(0.98f, 1.20f, 80);
        var ringRenderer = ringObject.AddComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = MaterialLibrary.Get("GoldGlow");
        ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;
        focusRing = ringObject.transform;
        ringObject.SetActive(false);

        // One collider for picking, on the root.
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.size = new Vector3(2.0f, 2.2f, 1.5f);
    }

    public void SetHover(bool on)
    {
        hoverTarget = on ? 1f : 0f;
    }

    public void SetFocused(bool on)
    {
        focusTarget = on ? 1f : 0f;
    }

    public void SetSpotlight(bool on)
    {
        spotlightTarget = on ? 1f : 0f;
    }

    public void SetOpen(bool open)
    {
        openTarget = open ? 1f : 0f;
    }

    public void ResetChest()
    {
        hoverAmount = hoverTarget = 0f;
        focusAmount = focusTarget = 0f;
        spotlightAmount = spotlightTarget = 0f;
        openAmount = openTarget = 0f;
        lidPivot.localRotation = Quaternion.identity;
        bodyRoot.localPosition = Vector3.zero;
        bodyRoot.localScale = Vector3.one;
        glowLight.intensity = 0f;
        focusRing.gameObject.SetActive(false);
    }

    public void Tick(float dt, float time)
    {
        hoverAmount = Mathf.MoveTowards(hoverAmount, hoverTarget, dt * 6f);
        focusAmount = Mathf.MoveTowards(focusAmount, focusTarget, dt * 5f);
        spotlightAmount = Mathf.MoveTowards(spotlightAmount, spotlightTarget, dt * 2.6f);
        openAmount = Mathf.MoveTowards(openAmount, openTarget, dt * 2.2f);

        float openEase = Mathf.SmoothStep(0f, 1f, openAmount);
        lidPivot.localRotation = Quaternion.Euler(openEase * 96f, 0f, 0f);

        float bob = Mathf.Sin(time * 1.6f + bobPhase) * 0.012f;
        float lift = hoverAmount * 0.075f + focusAmount * 0.022f + spotlightAmount * 0.72f;
        bodyRoot.localPosition = new Vector3(0f, bob + lift, 0f);

        float scale = 1f + hoverAmount * 0.03f + focusAmount * 0.012f + spotlightAmount * 0.32f;
        bodyRoot.localScale = new Vector3(scale, scale, scale);

        glowLight.intensity = openEase * (2.2f + Mathf.Sin(time * 6f + bobPhase) * 0.22f) + spotlightAmount * 0.8f;

        float glowStrength = 0.07f + hoverAmount * 0.38f + focusAmount * 0.18f + openEase * 0.30f + spotlightAmount * 0.42f;
        goldBlock.SetColor("_EmissionColor", goldEmission * glowStrength);
        for (int i = 0; i < goldParts.Length; i++)
            if (goldParts[i] != null) goldParts[i].SetPropertyBlock(goldBlock);

        bool showRing = hoverAmount > 0.03f || focusAmount > 0.03f || spotlightAmount > 0.03f;
        if (focusRing.gameObject.activeSelf != showRing) focusRing.gameObject.SetActive(showRing);
        if (showRing)
        {
            float strength = Mathf.Max(spotlightAmount, Mathf.Max(hoverAmount, focusAmount));
            float pulse = 1f + Mathf.Sin(time * 3.2f + bobPhase) * 0.02f;
            focusRing.localScale = new Vector3(pulse * (0.92f + strength * 0.08f), 1f, pulse * (0.92f + strength * 0.08f));
        }
    }

    Renderer Box(Transform parent, string name, Vector3 size, Vector3 center, Material material, Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        RemoveCollider(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return renderer;
    }

    Renderer Cylinder(Transform parent, string name, Vector3 size, Vector3 center, Material material, Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        RemoveCollider(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return renderer;
    }

    static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider == null) return;
        collider.enabled = false;
        Destroy(collider);
    }
}
