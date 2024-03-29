using BeauUtil;
using FieldDay;
using UnityEditor;
using UnityEngine;
using Zavala.Sim;
using System;
using FieldDay.Components;

namespace Zavala.World {
    public class TileInstance : BatchedComponent {
        [Header("Tile Top")]
        public MeshRenderer TopRenderer;
        public MeshFilter TopFilter;

        [Header("Pillar")]
        public MeshRenderer PillarRenderer;

        [NonSerialized] public MultiMaterialMeshConfig TopDefaultConfig;
        [NonSerialized] public Material PillarDefaultMat;

        private void Awake() {
            TopDefaultConfig = new MultiMaterialMeshConfig(TopRenderer, TopFilter);
            PillarDefaultMat = PillarRenderer ? PillarRenderer.sharedMaterial : null;
        }
    }
}