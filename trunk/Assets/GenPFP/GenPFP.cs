using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PFP {
    public class GenPFP {
        const string DefaultURI = "DefaultURI";

        [MenuItem("Assets/PFPUpdateURI", false, 2)]//MenuPath
        public static void PFPUpdateURI() {
            PFPConfig config = (PFPConfig)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(Selection.activeObject), typeof(PFPConfig));
            string filePath = config._rootPath;
            string uri = config._imageURI;
            DirectoryInfo folder = new DirectoryInfo(filePath + "/pfp/metadata/");
            Debug.Log(folder.FullName);
            foreach (FileInfo file in folder.GetFiles()) {
                if (file.Name.Contains(".meta")) {
                    file.Delete();
                }
                else {
                    // Open the file to read from.
                    string fullStr = "";
                    using (StreamReader sr = file.OpenText()) {
                        string s = "";
                        while ((s = sr.ReadLine()) != null) {
                            if (s.Contains(DefaultURI)) {
                                s = s.Replace(DefaultURI, config._imageURI);
                            }
                            fullStr += s;
                        }
                        //Debug.Log(fullStr);
                    }

                    file.Delete();

                    using (StreamWriter sw = file.CreateText()) {
                        sw.WriteLine(fullStr);
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/PFPGen", false, 2)]//MenuPath
        public static void PFPGen() {
            string filePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            PFPConfig config = (PFPConfig)AssetDatabase.LoadAssetAtPath(filePath, typeof(PFPConfig));
            Debug.Log(config._amount);

            int width = config._width;
            int height = config._height;
            string folderPath = config._rootPath;

            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D outTexture = new Texture2D(width, height, TextureFormat.ARGB32, true);
            Material material = new Material(Shader.Find("Unlit/Transparent"));
            //Material material = (Material)AssetDatabase.LoadAssetAtPath(folderPath + "/PFPMat.mat", typeof(Material));

            int amount = 1;
            List<List<int>> parts = new List<List<int>>();
            for (int layerid = 0; layerid < config._layer.Length; ++layerid) {
                parts.Add(new List<int>(config._layer[layerid]._layerDetail.Length));
                amount *= config._layer[layerid]._layerDetail.Length;
                config._layer[layerid]._weightAmount = 0;
                for (int i = 0; i < config._layer[layerid]._layerDetail.Length; ++i) {
                    //parts[layerid].Add(config._layer[layerid]);
                    config._layer[layerid]._weightAmount += config._layer[layerid]._layerDetail[i]._weight;
                }
            }

            if (config._amount > amount && config._mode != PFPGenMode.One2One) {
                config._amount = amount;
            }

            Dictionary<string, int> pfps = new Dictionary<string, int>();
            for (int i = 0; i < config._amount;) {
                int id = i;
                string pfpname = "";
                for (int layerid = 0; layerid < config._layer.Length; ++layerid) {
                    int curId = 0;
                    switch (config._mode) {
                        case PFPGenMode.Sequence: {
                                curId = id % config._layer[layerid]._layerDetail.Length + 1;
                                id /= config._layer[layerid]._layerDetail.Length;
                            }
                            break;
                        case PFPGenMode.One2One: {
                                curId = i + 1;
                            }
                            break;
                        case PFPGenMode.Random: {
                                int rand = Random.Range(0, config._layer[layerid]._weightAmount - 1);
                                for (int w = 0; w < config._layer[layerid]._layerDetail.Length; ++w) {
                                    if (rand < config._layer[layerid]._layerDetail[w]._weight) {
                                        curId = w + 1;
                                        break;
                                    }
                                    rand -= config._layer[layerid]._layerDetail[w]._weight;
                                }
                            }
                            break;
                    }

                    string srcPath = folderPath + "/src/" + config._layer[layerid]._name + "/" + +curId + ".png";
                    Debug.Log(srcPath);
                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(srcPath, typeof(Texture2D));
                    if (tex == null) {
                        pfpname += 0 + "-";
                        continue;
                    }
                    else {
                        material.mainTexture = tex;
                        Graphics.Blit(tex, renderTexture, material);
                        Resources.UnloadAsset(tex);

                        pfpname += curId + "-";
                    }
                }
                if (pfps.ContainsKey(pfpname)) {
                    continue;
                }
                else {
                    pfps.Add(pfpname, i);
                    ++i;
                }
                RenderTexture.active = renderTexture;
                outTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                outTexture.Apply();

                if (!Directory.Exists(folderPath + "/pfp/")) {
                    Directory.CreateDirectory(folderPath + "/pfp/");
                }

                Texture2D desTexture = ScaleTexture(outTexture, width, height);
                byte[] bytes = desTexture.EncodeToPNG();
                File.WriteAllBytes(folderPath + "/pfp/"
                    + (i + config._startIndex)
                    + (config._debug ? ("@" + pfpname) : "")
                    + ".png", bytes);

                //save metadata file
                string filename = folderPath + "/pfp/metadata/"
                    + (i + config._startIndex)
                    //+ (config._debug ? ("@" + pfpname) : "")
                    + ".json";
                if (!Directory.Exists(folderPath + "/pfp/metadata/")) {
                    Directory.CreateDirectory(folderPath + "/pfp/metadata/");
                }

                TextWriter bw = new StreamWriter(filename);
                //TextWriter bw = new FileStream(filename, FileMode.Create);
                if (config._debug) {
                    bw.WriteLine("{");
                    bw.WriteLine("  \"name\": \"" + pfpname + "\",");
                    bw.WriteLine("  \"image\": \"" + DefaultURI + "\",");
                    bw.WriteLine("}");
                }
                else {
                    bw.Write("{");
                    bw.Write("\"name\": \"" + pfpname + "\",");
                    bw.Write("\"image\": \"" + (config._imageURI.Length == 0 ? DefaultURI : config._imageURI) + "\",");
                    bw.Write("}");
                }
                bw.Close();
            }

            AssetDatabase.Refresh();
            Debug.Log("Success!");
        }

        static private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight) {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = (1.0f / (float)targetWidth);
            float incY = (1.0f / (float)targetHeight);
            for (int px = 0; px < rpixels.Length; px++) {
                rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }
    }
}
