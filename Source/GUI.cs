using Kopernicus.Configuration.ModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RealSolarSystem
{
    // From Starwaster.
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RealSolarSystemEditor : MonoBehaviour
    {
        static Rect windowPosition = new Rect(64, 64, 320, 640);
        static GUIStyle windowStyle = null;

        bool GUIOpen = false;

        double counter = 0;

        Vector2 scrollPos;

        // Camera parameters.

        List<CameraWrapper> cams = null;

        private string sMinDist = null;
        private string sMinDiv = null;
        private string sMaxDiv = null;

        private string sDepthOffset = null;
        private string sOceanRadiusOffset = null;
        private string sCoastOrder = null;

        private string sOceanFactor = null;
        private string sCoastLessThan = null;
        private string sCoastFactor = null;
        private string sEnhanceOrder = null;

        private string sMinHeightOffset;
        private string sMaxHeightOffset;
        private string sSlopeScale;
        private string sRssDefineOrder;

        private string sHeightStart;
        private string sHeightEnd;
        private string sDeformity;
        private string sFrequency;
        private string sOctaves;
        private string sPersistance;
        private string sHeightNoiseOrder;

        List<PQSMod> modList;
        PQSMod_VertexDefineCoastLine pModDefine = null;
        PQSMod_QuadEnhanceCoast pModEnhance = null;
        PQSMod_VertexDefineCoastSmooth pModRssDefine = null;
        PQSMod_VertexHeightNoiseVertHeight pModHeightNoise = null;
        PQSMod_VertexHeightMapRSS pModRssHMap = null;

        public class CameraWrapper : MonoBehaviour
        {
            public string depth;
            public string farClipPlane;
            public string nearClipPlane;
            public string camName;

            public CameraWrapper()
            {
                depth = farClipPlane = nearClipPlane = camName = "";
            }

            public void Apply()
            {
                Camera[] cameras = Camera.allCameras;

                try
                {
                    bool notFound = true;

                    foreach (Camera cam in cameras)
                    {
                        if (camName.Equals(cam.name))
                        {
                            if (float.TryParse(depth, out float ftmp))
                                cam.depth = ftmp;

                            if (float.TryParse(farClipPlane, out ftmp))
                                cam.farClipPlane = ftmp;

                            if (float.TryParse(nearClipPlane, out ftmp))
                                cam.nearClipPlane = ftmp;

                            depth = cam.depth.ToString();
                            nearClipPlane = cam.nearClipPlane.ToString();
                            farClipPlane = cam.farClipPlane.ToString();

                            notFound = false;
                        }
                    }

                    if (notFound)
                    {
                        Debug.Log($"[RealSolarSystem] Could not find camera {camName} when applying settings!");
                    }
                }
                catch (Exception exceptionStack)
                {
                    Debug.Log($"[RealSolarSystem] Error applying to camera {camName}: exception {exceptionStack.Message}");
                }
            }
        }

        public void Update()
        {
            if (counter < 5)
            {
                counter += TimeWarp.fixedDeltaTime;
                return;
            }

            if (cams == null)
            {
                cams = new List<CameraWrapper>();

                Camera[] cameras = Camera.allCameras;

                foreach (Camera cam in cameras)
                {
                    try
                    {
                        var thisCam = new CameraWrapper
                        {
                            camName = cam.name,

                            depth = cam.depth.ToString()
                        };

                        thisCam.farClipPlane += cam.farClipPlane.ToString();
                        thisCam.nearClipPlane += cam.nearClipPlane.ToString();

                        cams.Add(thisCam);
                    }
                    catch (Exception exceptionStack)
                    {
                        Debug.Log($"[RealSolarSystem] Exception getting camera {cam.name}\n{exceptionStack}");
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftAlt))
            {
                GUIOpen = !GUIOpen;
            }

            if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftAlt))
            {
                FlightGlobals.currentMainBody?.pqsController?.StartUpSphere();
            }
        }

        public void OnGUI()
        {
            if (GUIOpen)
            {
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                    windowPosition = GUILayout.Window(69105, windowPosition, ShowGUI, "RealSolarSystem Parameters", windowStyle);
            }
        }

        public void Start()
        {
            windowStyle = new GUIStyle(HighLogic.Skin.window);
            windowStyle.stretchHeight = true;
        }

        private void ShowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("RSSRunwayFix");

            GUILayout.BeginHorizontal();
            GUILayout.Label("isOnRunway: ");
            GUILayout.Label(RSSRunwayFix.Instance.isOnRunway.ToString(), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("hold: ");
            GUILayout.Label(RSSRunwayFix.Instance.hold.ToString(), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("lastHitCollider: ");
            GUILayout.Label(RSSRunwayFix.Instance.lastHitColliderName, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (cams != null)
            {
                GUILayout.Label("--------------");
                GUILayout.BeginHorizontal();
                GUILayout.Label("CAMERA EDITOR");
                GUILayout.EndHorizontal();

                foreach (CameraWrapper cam in cams)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Camera: " + cam.camName);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Depth");
                    cam.depth = GUILayout.TextField(cam.depth, 10);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Far Clip");
                    cam.farClipPlane = GUILayout.TextField(cam.farClipPlane, 10);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Near Clip");
                    cam.nearClipPlane = GUILayout.TextField(cam.nearClipPlane, 10);
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Apply to " + cam.camName))
                    {
                        cam.Apply();
                    }
                }
            }

            var pqs = FlightGlobals.currentMainBody.pqsController;
            if (modList == null)
            {
                modList = new List<PQSMod>();
                GetChildMods(pqs.gameObject, modList);
                pModDefine = (PQSMod_VertexDefineCoastLine)modList.FirstOrDefault(t => t is PQSMod_VertexDefineCoastLine);
                pModEnhance = (PQSMod_QuadEnhanceCoast)modList.FirstOrDefault(t => t is PQSMod_QuadEnhanceCoast);
                pModRssDefine = (PQSMod_VertexDefineCoastSmooth)modList.FirstOrDefault(t => t is PQSMod_VertexDefineCoastSmooth);
                pModHeightNoise = (PQSMod_VertexHeightNoiseVertHeight)modList.FirstOrDefault(t => t is PQSMod_VertexHeightNoiseVertHeight);
                pModRssHMap = (PQSMod_VertexHeightMapRSS)modList.FirstOrDefault(t => t is PQSMod_VertexHeightMapRSS);
            }

            PQSCache.PQSSpherePreset preset = PQSCache.PresetList?.GetPreset(pqs.gameObject.name);
            if (preset != null)
            {
                if (sMinDist == null)
                {
                    sMinDist = preset.minDistance.ToString();
                    sMinDiv = preset.minSubdivision.ToString();
                    sMaxDiv = preset.maxSubdivision.ToString();

                    if (pModDefine != null)
                    {
                        sDepthOffset = pModDefine.depthOffset.ToString();
                        sOceanRadiusOffset = pModDefine.oceanRadiusOffset.ToString();
                        sCoastOrder = pModDefine.order.ToString();
                    }
                    if (pModEnhance != null)
                    {
                        sOceanFactor = pModEnhance.oceanFactor.ToString();
                        sCoastLessThan = pModEnhance.coastLessThan.ToString();
                        sCoastFactor = pModEnhance.coastFactor.ToString();
                        sEnhanceOrder = pModEnhance.order.ToString();
                    }
                    if (pModRssDefine != null)
                    {
                        sMinHeightOffset = pModRssDefine.minHeightOffset.ToString();
                        sMaxHeightOffset = pModRssDefine.maxHeightOffset.ToString();
                        sSlopeScale = pModRssDefine.slopeScale.ToString();
                        sRssDefineOrder = pModRssDefine.order.ToString();
                    }
                    if (pModHeightNoise != null)
                    {
                        sHeightStart = pModHeightNoise.heightStart.ToString();
                        sHeightEnd = pModHeightNoise.heightEnd.ToString();
                        sDeformity = pModHeightNoise.deformity.ToString();
                        sFrequency = pModHeightNoise.frequency.ToString();
                        sOctaves = pModHeightNoise.octaves.ToString();
                        sPersistance = pModHeightNoise.persistance.ToString();
                        // TODO: other fields
                        sHeightNoiseOrder = pModHeightNoise.order.ToString();
                    }
                }

                GUILayout.Label("Preset " + preset.name);

                GUILayout.BeginHorizontal();
                GUILayout.Label("minDistance: ");
                GUILayout.EndHorizontal();
                sMinDist = GUILayout.TextField(sMinDist);
                if (double.TryParse(sMinDist, out double minDist))
                {
                    preset.minDistance = minDist;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("minSubdivision: ");
                GUILayout.EndHorizontal();
                sMinDiv = GUILayout.TextField(sMinDiv);
                if (int.TryParse(sMinDiv, out int minDiv))
                {
                    preset.minSubdivision = minDiv;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("maxSubdivision: ");
                GUILayout.EndHorizontal();
                sMaxDiv = GUILayout.TextField(sMaxDiv);
                if (int.TryParse(sMaxDiv, out int maxDiv))
                {
                    preset.maxSubdivision = maxDiv;
                }

                GUILayout.Label("-----------------");
                foreach (PQSMod pqsmod in modList)
                {
                    GUILayout.BeginHorizontal();
                    pqsmod.modEnabled = GUILayout.Toggle(pqsmod.modEnabled, pqsmod.GetType().Name);
                    GUILayout.EndHorizontal();
                }

                if (pModDefine != null)
                {
                    GUILayout.Label("-----------------");
                    GUILayout.Label("VertexDefineCoastLine");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Coastline depthOffset: ");
                    GUILayout.EndHorizontal();
                    sDepthOffset = GUILayout.TextField(sDepthOffset);
                    if (double.TryParse(sDepthOffset, out double depthOffset))
                    {
                        pModDefine.depthOffset = depthOffset;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Coastline oceanRadiusOffset: ");
                    GUILayout.EndHorizontal();
                    sOceanRadiusOffset = GUILayout.TextField(sOceanRadiusOffset);
                    if (double.TryParse(sOceanRadiusOffset, out double oceanRadiusOffset))
                    {
                        pModDefine.oceanRadiusOffset = oceanRadiusOffset;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Coastline order: ");
                    GUILayout.EndHorizontal();
                    sCoastOrder = GUILayout.TextField(sCoastOrder);
                    if (int.TryParse(sCoastOrder, out int coastOrder))
                    {
                        pModDefine.order = coastOrder;
                    }
                }

                if (pModEnhance!= null)
                {
                    GUILayout.Label("-----------------");
                    GUILayout.Label("QuadEnhanceCoast");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Enhance oceanFactor: ");
                    GUILayout.EndHorizontal();
                    sOceanFactor = GUILayout.TextField(sOceanFactor);
                    if (double.TryParse(sOceanFactor, out double oceanfactor))
                    {
                        pModEnhance.oceanFactor = oceanfactor;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Enhance coastFactor: ");
                    GUILayout.EndHorizontal();
                    sCoastFactor = GUILayout.TextField(sCoastFactor);
                    if (double.TryParse(sCoastFactor, out double coastFactor))
                    {
                        pModEnhance.coastFactor = coastFactor;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Enhance coastLessThan: ");
                    GUILayout.EndHorizontal();
                    sCoastLessThan = GUILayout.TextField(sCoastLessThan);
                    if (double.TryParse(sCoastLessThan, out double lt))
                    {
                        pModEnhance.coastLessThan = lt;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Enhance order: ");
                    GUILayout.EndHorizontal();
                    sEnhanceOrder = GUILayout.TextField(sEnhanceOrder);
                    if (int.TryParse(sEnhanceOrder, out int order))
                    {
                        pModEnhance.order = order;
                    }
                }

                if (pModRssDefine != null)
                {
                    GUILayout.Label("-----------------");
                    GUILayout.Label("VertexDefineCoastSmooth");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("minHeightOffset: ");
                    GUILayout.EndHorizontal();
                    sMinHeightOffset = GUILayout.TextField(sMinHeightOffset);
                    if (double.TryParse(sMinHeightOffset, out double minHeightOffset))
                    {
                        pModRssDefine.minHeightOffset = minHeightOffset;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("maxHeightOffset: ");
                    GUILayout.EndHorizontal();
                    sMaxHeightOffset = GUILayout.TextField(sMaxHeightOffset);
                    if (double.TryParse(sMaxHeightOffset, out double maxHeightOffset))
                    {
                        pModRssDefine.maxHeightOffset = maxHeightOffset;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("slopeScale: ");
                    GUILayout.EndHorizontal();
                    sSlopeScale = GUILayout.TextField(sSlopeScale);
                    if (double.TryParse(sSlopeScale, out double val))
                    {
                        pModRssDefine.slopeScale = val;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Mod order: ");
                    GUILayout.EndHorizontal();
                    sRssDefineOrder = GUILayout.TextField(sRssDefineOrder);
                    if (int.TryParse(sRssDefineOrder, out int order))
                    {
                        pModRssDefine.order = order;
                    }
                }

                if (pModHeightNoise != null)
                {
                    GUILayout.Label("-----------------");
                    GUILayout.Label("VertexHeightNoiseVertHeight");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("heightStart: ");
                    GUILayout.EndHorizontal();
                    sHeightStart = GUILayout.TextField(sHeightStart);
                    if (double.TryParse(sHeightStart, out double val))
                    {
                        pModHeightNoise.heightStart = (float)val;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("maxHeightOffset: ");
                    GUILayout.EndHorizontal();
                    sHeightEnd = GUILayout.TextField(sHeightEnd);
                    if (double.TryParse(sHeightEnd, out val))
                    {
                        pModHeightNoise.heightEnd = (float)val;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("deformity: ");
                    GUILayout.EndHorizontal();
                    sDeformity = GUILayout.TextField(sDeformity);
                    if (double.TryParse(sDeformity, out val))
                    {
                        pModHeightNoise.deformity = (float)val;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("frequency: ");
                    GUILayout.EndHorizontal();
                    sFrequency = GUILayout.TextField(sFrequency);
                    if (double.TryParse(sFrequency, out val))
                    {
                        pModHeightNoise.frequency = (float)val;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("octaves: ");
                    GUILayout.EndHorizontal();
                    sOctaves = GUILayout.TextField(sOctaves);
                    if (int.TryParse(sOctaves, out int val2))
                    {
                        pModHeightNoise.octaves = val2;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("persistance: ");
                    GUILayout.EndHorizontal();
                    sPersistance = GUILayout.TextField(sPersistance);
                    if (double.TryParse(sPersistance, out val))
                    {
                        pModHeightNoise.persistance = (float)val;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Mod order: ");
                    GUILayout.EndHorizontal();
                    sHeightNoiseOrder = GUILayout.TextField(sHeightNoiseOrder);
                    if (int.TryParse(sHeightNoiseOrder, out int order))
                    {
                        pModHeightNoise.order = order;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void GetChildMods(GameObject obj, List<PQSMod> mods)
        {
            IEnumerator enumerator = obj.transform.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    Transform current = (Transform)enumerator.Current;
                    if (!(current == transform) && !(current.GetComponent<PQS>() != null))
                    {
                        PQSMod[] components = current.GetComponents<PQSMod>();
                        if (components != null)
                        {
                            mods.AddRange(components);
                            GetChildMods(current.gameObject, mods);
                        }
                    }
                }
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
