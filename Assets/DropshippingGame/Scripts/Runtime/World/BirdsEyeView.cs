using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DropshippingGame
{
    /// <summary>
    /// Entwickler-Werkzeug (Admin › Welt &amp; Zeit): orthografische Vogelperspektive hoch über dem Spieler.
    /// WASD/Pfeile = verschieben (Shift schneller), Mausrad / Q/E = zoomen, Esc = zurück.
    /// Benutzt eine eigene Kamera, die Hauptkamera bleibt unverändert.
    /// </summary>
    public sealed class BirdsEyeView : MonoBehaviour
    {
        public static bool Active => _inst != null && _inst._cam != null && _inst._cam.enabled;
        private static BirdsEyeView _inst;

        private Camera _cam;
        private int _openedFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _inst = null;

        private static BirdsEyeView Ensure()
        {
            if (_inst != null) return _inst;
            var go = new GameObject("BirdsEyeView");
            _inst = go.AddComponent<BirdsEyeView>();
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 60f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 600f;
            cam.depth = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.5f);
            cam.enabled = false;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _inst._cam = cam;
            return _inst;
        }

        public static void Toggle()
        {
            var v = Ensure();
            v.SetActive(!Active);
        }

        private void SetActive(bool on)
        {
            if (_cam == null) return;
            if (on)
            {
                Vector3 p = Game.Player != null ? Game.Player.transform.position : Vector3.zero;
                transform.position = new Vector3(p.x, 250f, p.z);
                _openedFrame = Time.frameCount;
            }
            _cam.enabled = on;
            if (Game.Root != null) Game.Root.Lock("birdseye", on);
            Game.Sim?.Notify(on ? "Vogelperspektive: WASD verschieben, Mausrad zoomen, Esc zurück" : "Vogelperspektive aus");
        }

        private void Update()
        {
            if (!Active) return;
            var kb = Keyboard.current;
            float dt = Time.unscaledDeltaTime;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame && Time.frameCount > _openedFrame)
                {
                    SetActive(false);
                    return;
                }
                Vector3 move = Vector3.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                float speed = _cam.orthographicSize * (kb.leftShiftKey.isPressed ? 2.5f : 1f);
                transform.position += move * speed * dt;
                if (kb.qKey.isPressed) _cam.orthographicSize *= 1f + dt;
                if (kb.eKey.isPressed) _cam.orthographicSize *= 1f - dt * 0.8f;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) _cam.orthographicSize *= scroll > 0f ? 0.9f : 1.1f;
            }
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, 5f, 220f);
        }

        /// <summary>Rendert die Vogelperspektive (Ausschnitt wie gerade eingestellt, sonst ganze Stadt) als 4096-px-PNG.</summary>
        public static void SaveMapPng()
        {
            var v = Ensure();
            var cam = v._cam;
            bool wasActive = Active;
            Vector3 oldPos = v.transform.position;
            float oldSize = cam.orthographicSize;
            float oldAspect = cam.aspect;
            if (!wasActive)
            {
                // ganze Stadt (x −150..150, z −50..83)
                v.transform.position = new Vector3(0f, 250f, (WorldLots.CityMinZ + WorldLots.CityMaxZ) * 0.5f);
                cam.orthographicSize = 80f;
            }
            const int size = 4096;
            RenderTexture rt = null;
            Texture2D tex = null;
            RenderTexture prev = RenderTexture.active;
            try
            {
                rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
                cam.aspect = 1f;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                string dir = Path.Combine(Application.persistentDataPath, "screenshots");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "karte_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Game.Sim?.Notify("Karte gespeichert: " + path, "good");
                Debug.Log("[BirdsEyeView] Karte gespeichert: " + path);
            }
            catch (Exception e)
            {
                Game.Sim?.Notify("Karte speichern fehlgeschlagen: " + e.Message, "bad");
            }
            finally
            {
                RenderTexture.active = prev;
                cam.targetTexture = null;
                cam.aspect = oldAspect;
                cam.ResetAspect();
                if (rt != null) Destroy(rt);
                if (tex != null) Destroy(tex);
                v.transform.position = oldPos;
                cam.orthographicSize = oldSize;
            }
        }
    }
}
