using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PFP {
    public enum PFPGenMode {
        Sequence,
        One2One,
        Random,
    }

    [Serializable]
    [CreateAssetMenu(fileName = "PFPConfig", menuName = "PFPConfig", order = 0)]
    public class PFPConfig : ScriptableObject {
        //[Header("Input Settings")]
        public PFPGenMode _mode;
        public bool _debug;
        public int _startIndex;
        [Range(1, 10000)]
        public int _amount;
        public string _rootPath;

        public PFPLayer[] _layer;

        //[Header("Output Settings")]
        [Range(1, 2048)]
        public int _width;
        [Range(1, 2048)]
        public int _height;

        [Header("Update URI")]
        public string _imageOldURI = "DefaultURI";
        public string _imageNewURI = "DefaultURI";
        public string _description;
    }

    [Serializable]
    public struct PFPLayerDetail {
        public string _name;
        public int _weight;
    }

    [Serializable]
    public struct PFPLayer {
        public string _name;
        public PFPLayerDetail[] _layerDetail;
        [HideInInspector]
        public int _weightAmount;
    }
}