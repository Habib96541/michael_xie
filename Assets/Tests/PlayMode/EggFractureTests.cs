using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EggDrop.Tests
{
    public sealed class EggFractureTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator FragmentsSettleOnPadAndRestartReusesThem()
        {
            yield return SceneManager.LoadSceneAsync("EggDrop");
            yield return null;
            EggDropGame game = Object.FindFirstObjectByType<EggDropGame>();
            EggBody egg = Object.FindFirstObjectByType<EggBody>();
            EggFracture fracture = egg.GetComponentInChildren<EggFracture>(true);
            Rigidbody[] fragments = fracture.GetComponentsInChildren<Rigidbody>(true);
            int[] identities = fragments.Select(fragment => fragment.GetInstanceID()).ToArray();
            Collider surface = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Collider>(true)).Single(collider => collider.name == "Landing Surface");
            VisualElement ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Assert.That(fragments, Has.Length.EqualTo(10));
            game.StartRound();

            for (int round = 0; round < 3; round++)
            {
                SelectCrackRoll();
                game.Drop();
                yield return WaitUntil(() => ui.Q<Label>("result-title").text == "Oh, shell.");
                yield return new WaitForFixedUpdate();
                Assert.That(fragments.All(fragment => fragment.gameObject.activeSelf && !fragment.isKinematic), Is.True);
                Assert.That(fracture.transform.Find("Egg White").gameObject.activeSelf, Is.True);
                Assert.That(fracture.transform.Find("Yolk").gameObject.activeSelf, Is.True);
                yield return new WaitForSeconds(2.6f);
                foreach (Rigidbody fragment in fragments)
                {
                    Collider shellCollider = fragment.GetComponentInChildren<Collider>();
                    Assert.That(shellCollider.bounds.min.y, Is.GreaterThanOrEqualTo(surface.bounds.max.y - 0.05f), "A shell fell through the landing pad.");
                    Mesh visibleShell = fragment.GetComponent<MeshFilter>().sharedMesh;
                    float lowestVertex = visibleShell.vertices.Min(vertex => fragment.transform.TransformPoint(vertex).y);
                    Assert.That(lowestVertex, Is.GreaterThanOrEqualTo(surface.bounds.max.y - 0.015f), "A visible shell penetrated the landing pad.");
                    Assert.That(Mathf.Abs(fragment.position.x), Is.LessThan(1.9f));
                    Assert.That(Mathf.Abs(fragment.position.z), Is.LessThan(1.6f));
                    Assert.That(fragment.linearVelocity.magnitude, Is.LessThan(0.2f), "A shell did not settle.");
                }
                Assert.That(fragments.Any(fragment => Quaternion.Angle(fragment.rotation, Quaternion.identity) > 20f), Is.True);
                Assert.That(fracture.transform.Find("Egg White").localScale, Is.EqualTo(Vector3.one));
                if (round == 0) CapturePreview();
                game.Restart();
                Assert.That(fragments.All(fragment => !fragment.gameObject.activeSelf && fragment.isKinematic), Is.True);
                Assert.That(fracture.GetComponentsInChildren<Rigidbody>(true).Select(fragment => fragment.GetInstanceID()), Is.EqualTo(identities));
                Assert.That(fracture.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(fracture.transform.Find("Egg White").gameObject.activeSelf, Is.False);
                Assert.That(fracture.transform.Find("Yolk").gameObject.activeSelf, Is.False);
                Assert.That(egg.transform.Find("Intact shell").gameObject.activeSelf, Is.True);
                Assert.That(ui.Q<Label>("bank").text, Is.EqualTo("0"));
                Assert.That(ui.Q<Button>("drop").enabledSelf, Is.True);
            }
        }

        private static void SelectCrackRoll()
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                Random.InitState(seed);
                Random.State state = Random.state;
                if (Random.value >= 0.08f) continue;
                Random.state = state;
                return;
            }
            Assert.Fail("Could not select a deterministic crack roll.");
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 7f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "The crack did not complete.");
        }

        private static void CapturePreview()
        {
            string path = System.Environment.GetEnvironmentVariable("EGGDROP_PREVIEW_PATH");
            if (string.IsNullOrEmpty(path)) return;
            Camera camera = Camera.main;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float size = camera.orthographicSize;
            float aspect = camera.aspect;
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(1200, 800, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(1200, 800, TextureFormat.RGB24, false);
            try
            {
                camera.transform.position = new Vector3(1.8f, 2.3f, -3f);
                camera.transform.LookAt(new Vector3(0, 0.1f, 0));
                camera.orthographicSize = 1.6f;
                camera.aspect = 1.5f;
                camera.SubmitRenderRequest(new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0);
                image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.orthographicSize = size;
                camera.aspect = aspect;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
        }
    }
}
